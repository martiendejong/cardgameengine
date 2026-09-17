using System.Text.Json;
using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;
using Xunit;

namespace CardGameEngine.Engine.Tests;

/// <summary>
/// Task 1499: bot-vs-bot simulation showed Hunks losing almost every game to fast decks
/// (Raiders, Shadow Guild) and logs revealed why — Hunk Stronghold, the default Hunks HQ,
/// was the only headquarters in the whole game with no direct resource-generation ability.
/// Every other faction HQ (Town Hall's Collect Taxes, Raider Camp's combat-triggered gold,
/// Arcane Nexus's Channel, Graveyard's Exhume, Thieves' Guild's Extort, Laboratory's
/// Distill — even the Hunks' own alternate HQ, Hunk Village's Barn Raising) grants a
/// resource for a bare tap; Hunk Stronghold's only ability, Grow Community, only ever
/// summoned a free Hunk. With no way to generate gold except drawing one of 5 zero-cost
/// bootstrap cards out of ~58, the bot spent many games doing nothing but summon 1-atk
/// tokens while its opponent developed — see AGENT_PROGRESS.md for full turn-by-turn logs.
/// Fix: Grow Community now also grants 1 gold, mirroring every other HQ's bootstrap
/// ability (matched to Hunk Village's Barn Raising rather than Town Hall's stronger
/// Collect Taxes, since Hunk Stronghold's own summon is already a repeatable free body
/// Town's paid Recruit Peasant doesn't have).
/// </summary>
public class HunksEconomyTests
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

    private static (GameInstance game, RuleEngine engine, PlayerInstance hunks) CreateMatch()
    {
        var hunksDeck = Definition.Decks.First(d => d.Id == "hunks");
        var oppDeck = Definition.Decks.First(d => d.Id == "town");

        PlayerInstance BuildPlayer(string id, string name, PreconDeckDefinition deck)
        {
            var deckList = new List<string>();
            foreach (var (cardId, count) in deck.Cards)
                for (int i = 0; i < count; i++)
                    deckList.Add(cardId);
            return new PlayerInstance { Id = id, Name = name, DeckList = deckList, HqCardId = deck.Hq, HeroCardId = deck.Hero };
        }

        var hunks = BuildPlayer("p1", "Hunks", hunksDeck);
        var opp = BuildPlayer("p2", "Opp", oppDeck);

        var game = new GameInstance { Id = "test-match", Definition = Definition, CreatorUserId = "p1" };
        game.Players.Add(hunks);
        game.Players.Add(opp);

        var engine = new RuleEngine();
        engine.ExecuteSetup(game);

        game.CurrentPhaseId = "main";
        game.ActivePlayerId = hunks.Id;
        game.State = GameState.WaitingForAction;

        return (game, engine, hunks);
    }

    private static ObjectInstance FindHq(GameInstance game, string playerId) =>
        game.Objects.First(o => o.OwnerId == playerId && o.ZoneId == "battlefield" &&
            GameQueries.IsObjectTypeOrSubtype(game, o.ObjectType, "headquarters"));

    [Fact]
    public void Grow_community_now_also_grants_1_gold_giving_hunk_stronghold_a_bootstrap_economy()
    {
        var (game, engine, hunks) = CreateMatch();
        var stronghold = FindHq(game, hunks.Id);
        Assert.False(stronghold.IsTapped);
        var goldBefore = hunks.Resources.GetValueOrDefault("gold");

        var (ok, error) = engine.ExecuteAction(game, hunks.Id, new ActionRequest
        {
            Type = "activateAbility",
            SourceObjectId = stronghold.Id,
            AbilityId = "grow-community"
        });

        Assert.True(ok, error);
        Assert.Equal(goldBefore + 1, hunks.Resources.GetValueOrDefault("gold"));
        Assert.True(stronghold.IsTapped);
        // Still summons a Hunk exactly as before — this is additive, not a replacement.
        Assert.Contains(game.Objects, o => o.OwnerId == hunks.Id && o.ZoneId == "battlefield" &&
            o.DefinitionId == "hunk");
    }
}
