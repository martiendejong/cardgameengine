using System.Text.Json;
using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;
using Xunit;

namespace CardGameEngine.Engine.Tests;

/// <summary>
/// Task 1508: the Hunks alternate precon deck "Fortress Eternal" (hunks-fortress, fort-
/// cards) shipped in PR #42 with three distinct bug classes, all silent (no crash, no
/// engine error — the cards just did nothing or the wrong thing):
///   1. 9 cards used "triggers":[{"event":"onPlay"}], which TriggerService never fires
///      (no CardPlayed case in its event switch) — dead JSON, only cardDef.OnPlay actually
///      runs on play.
///   2. 3 of those 9 are equipment with no slots/attachTo, so CardPlayService's
///      needsEquipTarget check never triggers and they land on the battlefield as inert
///      floating objects instead of attaching to anything.
///   3. 5 cards used heal/modify_property with scope:"player"+tag:"building" intending a
///      mass building heal/buff — EffectContext.ResolveScope() only understands
///      self/target/host, so this silently no-ops. Plus every "deal damage to the
///      opponent's HQ" building/spell across all 9 new decks used scope:"opponent",
///      which resolved to the effect's own Source (itself) rather than the enemy HQ.
/// This suite drives the real engine (RuleEngine, no mocks) through each fixed mechanism
/// directly, sidestepping the bot AI — the /api/simulate bot doesn't reliably choose to
/// play cheap units/equipment/situational spells, so bot-vs-bot win-rate telemetry alone
/// can't prove any specific one of these fixes actually fires.
/// </summary>
public class FortressEternalDeckTests
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

    /// <summary>Builds a real match (hunks-fortress vs town), skips straight to Main Phase
    /// with deep resource pools so the specific card under test is always affordable and
    /// playable regardless of the randomly-shuffled opening hand.</summary>
    private static (GameInstance game, RuleEngine engine, PlayerInstance fort, PlayerInstance opp) CreateMatch()
    {
        var fortDeck = Definition.Decks.First(d => d.Id == "hunks-fortress");
        var oppDeck = Definition.Decks.First(d => d.Id == "town");

        PlayerInstance BuildPlayer(string id, string name, PreconDeckDefinition deck)
        {
            var deckList = new List<string>();
            foreach (var (cardId, count) in deck.Cards)
                for (int i = 0; i < count; i++)
                    deckList.Add(cardId);
            return new PlayerInstance { Id = id, Name = name, DeckList = deckList, HqCardId = deck.Hq, HeroCardId = deck.Hero };
        }

        var fort = BuildPlayer("p1", "Fort", fortDeck);
        var opp = BuildPlayer("p2", "Opp", oppDeck);

        var game = new GameInstance { Id = "test-match", Definition = Definition, CreatorUserId = "p1" };
        game.Players.Add(fort);
        game.Players.Add(opp);

        var engine = new RuleEngine();
        engine.ExecuteSetup(game);

        game.CurrentPhaseId = "main";
        game.ActivePlayerId = fort.Id;
        game.State = GameState.WaitingForAction;
        foreach (var player in game.Players)
        {
            player.Resources["gold"] = 99;
            player.Resources["training"] = 99;
            // "growth" is entity-scoped (owned by the homestead-hq itself, not the player —
            // see the "homestead-hq" objectType's resources list), so it lives on the HQ's
            // own bank, not PlayerInstance.Resources.
            FindHq(game, player.Id).Resources["growth"] = 99;
        }

        return (game, engine, fort, opp);
    }

    // ObjectFactory numbers ids "obj_1", "obj_2", ... per-instance — a fresh ObjectFactory
    // per call would collide with the setup-created HQ/hero/deck objects that already claimed
    // those same low numbers, so ExecuteAction's SourceObjectId lookup could silently resolve
    // to one of those instead of the card this test actually put in hand. One counter shared
    // across every Put* call in the whole test run keeps every id unique.
    private static int _testObjectCounter;

    /// <summary>Puts a fresh copy of the given card into the player's hand, bypassing the
    /// shuffled deck draw so the exact card under test is available every run.</summary>
    private static ObjectInstance PutInHand(GameInstance game, string cardId, PlayerInstance player)
    {
        var def = game.Definition.Cards.First(c => c.Id == cardId);
        var obj = new ObjectFactory().CreateObjectInstance(game, def, player.Id, "hand");
        obj.Id = $"test_obj_{++_testObjectCounter}";
        game.Objects.Add(obj);
        return obj;
    }

    private static ObjectInstance PutOnBattlefield(GameInstance game, string cardId, PlayerInstance player)
    {
        var def = game.Definition.Cards.First(c => c.Id == cardId);
        var obj = new ObjectFactory().CreateObjectInstance(game, def, player.Id, "battlefield");
        obj.Id = $"test_obj_{++_testObjectCounter}";
        game.Objects.Add(obj);
        return obj;
    }

    private static ObjectInstance FindHq(GameInstance game, string playerId) =>
        game.Objects.First(o => o.OwnerId == playerId && o.ZoneId == "battlefield" &&
            GameQueries.IsObjectTypeOrSubtype(game, o.ObjectType, "headquarters"));

    private static ObjectInstance FindHero(GameInstance game, string playerId) =>
        game.Objects.First(o => o.OwnerId == playerId && o.ZoneId == "battlefield" &&
            GameQueries.IsObjectTypeOrSubtype(game, o.ObjectType, "hero"));

    [Fact]
    public void Mason_onPlay_grants_growth_now_that_it_is_a_real_onPlay_field_not_a_dead_trigger()
    {
        var (game, engine, fort, _) = CreateMatch();
        var before = fort.Resources.GetValueOrDefault("growth");
        var mason = PutInHand(game, "fort-mason", fort);

        var (ok, error) = engine.ExecuteAction(game, fort.Id, new ActionRequest { Type = "playCard", SourceObjectId = mason.Id });

        Assert.True(ok, error);
        Assert.Equal(before + 1, fort.Resources.GetValueOrDefault("growth"));
    }

    [Fact]
    public void Fortify_heals_and_buffs_every_building_the_caster_controls_via_heal_all_tagged()
    {
        var (game, engine, fort, _) = CreateMatch();
        var wall = PutOnBattlefield(game, "fort-stone-wall", fort);
        wall.Properties["currentHp"] = 3; // damaged, below its maxHp of 8
        var fortify = PutInHand(game, "fort-fortify", fort);

        var (ok, error) = engine.ExecuteAction(game, fort.Id, new ActionRequest { Type = "playCard", SourceObjectId = fortify.Id });

        Assert.True(ok, error);
        Assert.Equal(7, wall.Properties["currentHp"]); // +4 heal, capped at maxHp 8
        Assert.Contains(game.ActiveModifiers, m => m.TargetObjectId == wall.Id && m.PropertyId == "armor" && m.Amount == 1);
    }

    [Fact]
    public void Siege_barrage_hits_the_opponents_hq_via_the_new_opponent_scope_not_the_spell_itself()
    {
        var (game, engine, fort, opp) = CreateMatch();
        var oppHq = FindHq(game, opp.Id);
        var startingHp = oppHq.Properties["currentHp"];
        var barrage = PutInHand(game, "fort-siege-barrage", fort);

        var (ok, error) = engine.ExecuteAction(game, fort.Id, new ActionRequest { Type = "playCard", SourceObjectId = barrage.Id });

        Assert.True(ok, error);
        Assert.Equal(startingHp - 6, oppHq.Properties["currentHp"]);
    }

    [Fact]
    public void Siege_harness_actually_attaches_via_slots_attachTo_instead_of_floating_inert_on_the_battlefield()
    {
        var (game, engine, fort, _) = CreateMatch();
        var wallGuard = PutOnBattlefield(game, "fort-wall-guard", fort);
        var baseArmor = wallGuard.Properties["armor"];
        var baseAttack = wallGuard.Properties["attack"];
        var harness = PutInHand(game, "fort-siege-harness", fort);

        var (ok, error) = engine.ExecuteAction(game, fort.Id, new ActionRequest
        {
            Type = "playCard",
            SourceObjectId = harness.Id,
            TargetIds = new List<string> { wallGuard.Id }
        });

        Assert.True(ok, error);
        Assert.Equal(wallGuard.Id, harness.AttachedToId);
        Assert.Equal(baseArmor + 2, GameQueries.GetEffectiveProperty(game, wallGuard, "armor"));
        Assert.Equal(baseAttack + 1, GameQueries.GetEffectiveProperty(game, wallGuard, "attack"));
    }

    [Fact]
    public void Breach_stopper_freezes_a_chosen_enemy_soldier_via_a_real_choice_instead_of_a_dead_opponent_scope()
    {
        var (game, engine, fort, opp) = CreateMatch();
        var enemySoldier = PutOnBattlefield(game, "town-watch", opp); // a "soldier"-tagged town unit
        Assert.Contains("soldier", enemySoldier.Tags);
        var stopper = PutInHand(game, "fort-breach-stopper", fort);

        var (ok, error) = engine.ExecuteAction(game, fort.Id, new ActionRequest
        {
            Type = "playCard",
            SourceObjectId = stopper.Id,
            TargetIds = new List<string> { enemySoldier.Id }
        });

        Assert.True(ok, error);
        Assert.True(enemySoldier.IsTapped);
        Assert.True(enemySoldier.SkipNextUntap);
    }

    /// <summary>
    /// PR #45 review round 2 (task 1508): the hero's "Coordinated Barrage" ability paired a
    /// no-op gain_resource_all_tagged (amount:0) with a direct_damage effect carrying a
    /// perTaggedBuilding field that EffectDefinition didn't have, so System.Text.Json silently
    /// dropped it and the ability always dealt a flat 2 damage regardless of siege buildings
    /// controlled. Fixed by adding EffectDefinition.PerTaggedBuilding and having direct_damage's
    /// handler multiply Amount by the caster's own tagged-object count (mirrors the tag
    /// enumeration already used by heal_all_tagged/gain_resource_all_tagged); the no-op
    /// companion effect was dropped from the card data.
    /// </summary>
    [Fact]
    public void Coordinated_barrage_scales_damage_by_siege_buildings_controlled_via_perTaggedBuilding()
    {
        var (game, engine, fort, opp) = CreateMatch();
        var oppHq = FindHq(game, opp.Id);
        var startingHp = oppHq.Properties["currentHp"];
        var hero = FindHero(game, fort.Id);
        hero.Resources["ap"] = 5;
        PutOnBattlefield(game, "fort-stone-wall", fort);       // siege-tagged building #1
        PutOnBattlefield(game, "fort-cannon-emplacement", fort); // siege-tagged building #2

        var (ok, error) = engine.ExecuteAction(game, fort.Id, new ActionRequest
        {
            Type = "activateAbility",
            SourceObjectId = hero.Id,
            AbilityId = "commander-barrage"
        });

        Assert.True(ok, error);
        Assert.Equal(startingHp - 4, oppHq.Properties["currentHp"]); // base 2 damage * 2 siege buildings
    }
}
