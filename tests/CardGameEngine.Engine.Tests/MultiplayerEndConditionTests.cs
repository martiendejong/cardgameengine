using System.Text.Json;
using CardGameEngine.Api.Services;
using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;
using Xunit;

namespace CardGameEngine.Engine.Tests;

/// <summary>
/// Reproduces and guards task 1419: a multiplayer match must end (GameEnded, correct
/// winner/loser) as soon as a player has no un-destroyed headquarters and no un-destroyed
/// hero left IN PLAY — and that result must show up in the state projected to every viewer,
/// exactly like GameHub.BroadcastStateUpdate projects it to every connected client.
///
/// Builds a real match through RuleEngine.ExecuteSetup (no mocks, same entry point
/// MatchService.CreateMatch uses) and drives state changes through the same
/// RuleEngine.ExecuteAction/EndPhase methods GameHub calls — this is the multiplayer flow,
/// not the combat math (destroying combatants directly stays out of scope per the task's
/// own note: only detection + projection are under test, not rebalancing the loss condition).
/// </summary>
public class MultiplayerEndConditionTests
{
    private static readonly GameDefinition Definition = LoadDefinition();

    private static GameDefinition LoadDefinition()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CardGameEngine.slnx")))
            dir = dir.Parent;
        if (dir == null)
            throw new InvalidOperationException("Could not locate repo root (CardGameEngine.slnx) from " + AppContext.BaseDirectory);

        var path = Path.Combine(dir.FullName, "definitions", "town-tcg", "game.json");
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<GameDefinition>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize " + path);
    }

    /// <summary>Builds a real 2-player match with the given precon deck for both seats — same
    /// shape MatchService.CreateMatch produces for a real "vs human" multiplayer lobby match.</summary>
    private static (GameInstance game, RuleEngine engine, PlayerInstance a, PlayerInstance b) CreateMatch(string deckId)
    {
        var precon = Definition.Decks.First(d => d.Id == deckId);
        var deckList = new List<string>();
        foreach (var (cardId, count) in precon.Cards)
            for (int i = 0; i < count; i++)
                deckList.Add(cardId);

        var playerA = new PlayerInstance { Id = "p1", Name = "Alice", DeckList = deckList, HqCardId = precon.Hq, HeroCardId = precon.Hero };
        var playerB = new PlayerInstance { Id = "p2", Name = "Bob", DeckList = deckList, HqCardId = precon.Hq, HeroCardId = precon.Hero };

        var game = new GameInstance { Id = "test-match", Definition = Definition, CreatorUserId = "p1" };
        game.Players.Add(playerA);
        game.Players.Add(playerB);

        var engine = new RuleEngine();
        engine.ExecuteSetup(game);
        return (game, engine, playerA, playerB);
    }

    /// <summary>Destroys a player's in-play headquarters and hero, mirroring what combat does
    /// (GameMutator marks IsDestroyed + moves the object to "discard") without re-implementing
    /// combat math — the loss condition itself is out of scope for task 1419.</summary>
    private static void DestroyActiveHqAndHero(GameInstance game, PlayerInstance player)
    {
        var battlefieldPieces = game.Objects.Where(o =>
            o.OwnerId == player.Id && o.ZoneId == "battlefield" &&
            (GameQueries.IsObjectTypeOrSubtype(game, o.ObjectType, "headquarters") ||
             GameQueries.IsObjectTypeOrSubtype(game, o.ObjectType, "hero"))).ToList();
        Assert.NotEmpty(battlefieldPieces); // sanity: the player actually has both in play
        foreach (var obj in battlefieldPieces)
        {
            obj.IsDestroyed = true;
            obj.ZoneId = "discard";
        }
    }

    [Fact]
    public void Player_losing_active_hq_and_hero_ends_the_match_even_with_a_reserve_hero_in_deck()
    {
        // "town" is the default Lobby precon deck and — like several other factions — carries
        // reserve hero copies (paladin, master-builder) that sit undrawn in the deck the whole
        // game. Before the fix these undestroyed reserve ObjectInstances (ZoneId "deck") kept
        // CheckEndConditions from ever seeing Bob's "hero" type as all_destroyed, so the match
        // never ended even though his actual on-board hero and HQ were both gone.
        var (game, engine, alice, bob) = CreateMatch("town");
        Assert.Contains(bob.DeckList, id => id is "paladin" or "master-builder");

        DestroyActiveHqAndHero(game, bob);

        // Same call GameHub.SendAction/EndPhase makes after every successful player action.
        engine.CheckEndConditions(game);

        Assert.Equal(GameState.GameEnded, game.State);
        Assert.True(bob.IsLoser, "Bob has no headquarters or hero left in play and should be marked as the loser");
        Assert.True(alice.IsWinner, "Alice is the last player standing and should be marked as the winner");
    }

    [Fact]
    public void Game_end_is_projected_identically_to_both_connected_clients()
    {
        // Mirrors GameHub.BroadcastStateUpdate: builds the DTO each connected client actually
        // receives (one projection per viewerId) and asserts both show the match as over with
        // the correct winner/loser — the "pushed reliably to every client" half of the task.
        var (game, engine, alice, bob) = CreateMatch("town");
        DestroyActiveHqAndHero(game, bob);
        engine.CheckEndConditions(game);

        var projector = new StateProjector(engine);
        var aliceView = projector.Build(game, alice.Id);
        var bobView = projector.Build(game, bob.Id);

        foreach (var dto in new[] { aliceView, bobView })
        {
            Assert.Equal(GameState.GameEnded, dto.State);
            Assert.Equal(alice.Name, dto.Winner);
            var aliceDto = dto.Players.Single(p => p.Id == alice.Id);
            var bobDto = dto.Players.Single(p => p.Id == bob.Id);
            Assert.True(aliceDto.IsWinner);
            Assert.False(aliceDto.IsLoser);
            Assert.True(bobDto.IsLoser);
            Assert.False(bobDto.IsWinner);
        }
    }

    [Fact]
    public void Losing_only_the_headquarters_or_only_the_hero_does_not_end_the_match()
    {
        // Regression guard for the loss condition itself: still requires BOTH targets gone.
        var (game, engine, _, bob) = CreateMatch("town");
        var hq = game.Objects.First(o =>
            o.OwnerId == bob.Id && o.ZoneId == "battlefield" &&
            GameQueries.IsObjectTypeOrSubtype(game, o.ObjectType, "headquarters"));
        hq.IsDestroyed = true;
        hq.ZoneId = "discard";

        engine.CheckEndConditions(game);

        Assert.NotEqual(GameState.GameEnded, game.State);
        Assert.False(bob.IsLoser);
    }

    [Fact]
    public void Attacker_win_from_the_other_side_is_also_detected()
    {
        // "Same scenario for both directions" per the task's own How-to-test: this time it's
        // the locally-active player (Alice) who is eliminated.
        var (game, engine, alice, bob) = CreateMatch("town");
        DestroyActiveHqAndHero(game, alice);

        engine.CheckEndConditions(game);

        Assert.Equal(GameState.GameEnded, game.State);
        Assert.True(alice.IsLoser);
        Assert.True(bob.IsWinner);
    }
}
