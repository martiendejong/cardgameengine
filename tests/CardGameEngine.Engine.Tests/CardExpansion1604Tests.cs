using System.Text.Json;
using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;
using Xunit;

namespace CardGameEngine.Engine.Tests;

/// <summary>
/// Task 1604: QA pass on PR #52's 196 new cards (636 -> 832 total, 18 precon-deck variants
/// across the 9 real factions). All 196 were completely orphaned (not referenced by any
/// precon deck, so the deck-builder faction filter never showed them) and ~140 used a
/// hallucinated schema (snake_case trigger events / effect types / scopes the engine never
/// registered), so most of their abilities silently no-op'd. This suite drives the real
/// engine directly (no bot AI, no mocks) through the specific fixed mechanisms, since a bot
/// won't reliably choose to play any one given spell.
/// </summary>
public class CardExpansion1604Tests
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

    private static (GameInstance game, RuleEngine engine, PlayerInstance p1) CreateMatch(string deckAId, string deckBId = "town")
    {
        var deckA = Definition.Decks.First(d => d.Id == deckAId);
        var deckB = Definition.Decks.First(d => d.Id == deckBId);

        PlayerInstance BuildPlayer(string id, string name, PreconDeckDefinition deck)
        {
            var deckList = new List<string>();
            foreach (var (cardId, count) in deck.Cards)
                for (int i = 0; i < count; i++)
                    deckList.Add(cardId);
            return new PlayerInstance { Id = id, Name = name, DeckList = deckList, HqCardId = deck.Hq, HeroCardId = deck.Hero };
        }

        var p1 = BuildPlayer("p1", "P1", deckA);
        var p2 = BuildPlayer("p2", "P2", deckB);

        var game = new GameInstance { Id = "test-match", Definition = Definition, CreatorUserId = "p1" };
        game.Players.Add(p1);
        game.Players.Add(p2);

        var engine = new RuleEngine();
        engine.ExecuteSetup(game);

        game.CurrentPhaseId = "main";
        game.ActivePlayerId = p1.Id;
        game.State = GameState.WaitingForAction;
        foreach (var player in game.Players)
        {
            foreach (var resDef in Definition.Resources.Where(r => r.Scope == "player"))
                player.Resources[resDef.Id] = 99;
        }

        return (game, engine, p1);
    }

    private static int _testObjectCounter;

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

    [Fact]
    public void No196NewCard_UsesAnEffectTypeOrTriggerEventTheEngineDoesNotRegister()
    {
        // Cross-check every effect "type" and trigger "event" string actually used by the
        // 196 new (faction-tagged) cards against DefaultHandlers' registered effect types
        // and TriggerService's fired trigger events. A typo'd/hallucinated string here
        // deserializes fine (System.Text.Json ignores unknown values in a plain string
        // field) but silently no-ops forever at runtime with no error anywhere.
        var registeredEffects = new HashSet<string>
        {
            "add_progress", "afflict_enemy_units", "buff_own_units_until_end_of_turn", "buff_tag_until_end_of_turn",
            "buff_target_until_end_of_turn", "cancel_ability", "cost_discount", "counter_spell", "damage",
            "damage_all_units", "damage_enemy_units", "damage_infiltrators", "destroy", "destroy_infiltrators",
            "direct_damage", "discard_cards", "drain_resource", "draw_cards", "freeze", "gain_bank_resource",
            "gain_resource", "gain_resource_all_tagged", "heal", "heal_all_tagged", "heal_own_units", "infiltrate",
            "modify_property", "modify_property_enemy_units", "plunder_resource", "reveal", "reveal_attachments",
            "revive_hero", "salvage_module", "set_property", "set_resource", "steal_resource", "summon", "tap",
            "tap_enemy_units", "transfer_resource", "transform", "untap",
        };
        var firedTriggerEvents = new HashSet<string>
        {
            "onTurnStart", "onPlay", "onKill", "onDestroyBuilding", "onDeath", "onUnitDied",
            "onDealCombatDamage", "onDamaged", "onFriendlyDamageHqOrHero", "onEnemyAttack",
        };

        // Every card with a declared Faction includes all 196 new orphaned cards from PR #52
        // plus pre-existing, already deck-referenced cards the faction tag was also backfilled
        // onto (harmless: DeckBuilderPanel unions declared-faction with deck-membership) — so
        // this is a superset of the 196, not an exact count.
        var newCards = Definition.Cards.Where(c => !string.IsNullOrEmpty(c.Faction)).ToList();
        Assert.True(newCards.Count >= 196, $"Expected at least 196 faction-tagged cards, found {newCards.Count}");

        var badEffects = new List<string>();
        var badTriggers = new List<string>();
        void CheckEffects(string cardId, IEnumerable<EffectDefinition>? effects)
        {
            foreach (var e in effects ?? Enumerable.Empty<EffectDefinition>())
                if (!registeredEffects.Contains(e.Type))
                    badEffects.Add($"{cardId}: {e.Type}");
        }

        foreach (var c in newCards)
        {
            CheckEffects(c.Id, c.OnPlay?.Effects);
            foreach (var trigger in c.Triggers)
            {
                if (!firedTriggerEvents.Contains(trigger.Event))
                    badTriggers.Add($"{c.Id}: {trigger.Event}");
                CheckEffects(c.Id, trigger.Effects);
            }
        }

        Assert.True(badEffects.Count == 0, "Unregistered effect types: " + string.Join(", ", badEffects));
        Assert.True(badTriggers.Count == 0, "Unfired trigger events: " + string.Join(", ", badTriggers));
    }

    [Fact]
    public void Reaver_onPlay_trigger_now_actually_fires_and_grants_glory()
    {
        // Reaver (new, PR #52) declares its play effect only via triggers:[{event:"onPlay"}]
        // — before this task's TriggerService.CardPlayed case, nothing ever fired that event,
        // so this (and 23 other cards, 21 of them pre-existing) silently did nothing on play.
        var (game, engine, p1) = CreateMatch("raiders");
        PutOnBattlefield(game, "house", p1); // Reaver has a housingCost; give it room to land
        var before = p1.Resources.GetValueOrDefault("glory");
        var reaver = PutInHand(game, "raiders-reaver", p1);

        var (ok, error) = engine.ExecuteAction(game, p1.Id, new ActionRequest { Type = "playCard", SourceObjectId = reaver.Id });

        Assert.True(ok, error);
        Assert.Equal(before + 1, p1.Resources.GetValueOrDefault("glory"));
    }

    [Fact]
    public void Predator_surge_buffs_own_units_draws_and_gains_biomass_via_the_new_buff_own_units_handler()
    {
        var (game, engine, p1) = CreateMatch("brood");
        var hunter = PutOnBattlefield(game, "brood-warrior", p1);
        var baseAttack = GameQueries.GetEffectiveProperty(game, hunter, "attack");
        var handBefore = game.Objects.Count(o => o.OwnerId == p1.Id && o.ZoneId == "hand");
        var biomassBefore = p1.Resources.GetValueOrDefault("biomass");
        var surge = PutInHand(game, "apex-predator-surge", p1);

        var (ok, error) = engine.ExecuteAction(game, p1.Id, new ActionRequest { Type = "playCard", SourceObjectId = surge.Id });

        Assert.True(ok, error);
        Assert.Equal(baseAttack + 2, GameQueries.GetEffectiveProperty(game, hunter, "attack"));
        // handBefore excludes Surge itself; playing it removes 1 (itself) and draw_cards adds 1 back.
        Assert.Equal(handBefore + 1, game.Objects.Count(o => o.OwnerId == p1.Id && o.ZoneId == "hand"));
        Assert.Equal(biomassBefore + 1, p1.Resources.GetValueOrDefault("biomass"));
    }

    [Fact]
    public void Payroll_heals_every_own_unit_via_the_new_heal_own_units_handler()
    {
        var (game, engine, p1) = CreateMatch("town");
        var guard = PutOnBattlefield(game, "town-watch", p1);
        guard.Properties["currentHp"] = 1;
        var payroll = PutInHand(game, "merch-payroll", p1);

        var (ok, error) = engine.ExecuteAction(game, p1.Id, new ActionRequest { Type = "playCard", SourceObjectId = payroll.Id });

        Assert.True(ok, error);
        Assert.Equal(3, guard.Properties["currentHp"]); // +2 heal
    }

    [Fact]
    public void Smoke_veil_taps_every_enemy_unit_via_the_new_tap_enemy_units_handler()
    {
        var (game, engine, p1) = CreateMatch("shadow");
        var opp = game.Players.First(p => p.Id != p1.Id);
        var enemy1 = PutOnBattlefield(game, "town-watch", opp);
        var enemy2 = PutOnBattlefield(game, "town-watch", opp);
        var veil = PutInHand(game, "shadow-smoke-veil", p1);

        var (ok, error) = engine.ExecuteAction(game, p1.Id, new ActionRequest { Type = "playCard", SourceObjectId = veil.Id });

        Assert.True(ok, error);
        Assert.True(enemy1.IsTapped);
        Assert.True(enemy2.IsTapped);
    }

    [Theory]
    [InlineData("town-merchant")]
    [InlineData("raiders-warbond")]
    [InlineData("machine-sentry")]
    [InlineData("conclave-storm")]
    public void Precon_decks_carrying_their_own_hq_as_a_reserve_copy_pass_deck_validation(string deckId)
    {
        // These 4 alt decks include their own HQ card in Cards (a "reserve copy", task 906)
        // but IsDeckEligible only exempted "hero" type cards, so a non-admin match/deck
        // validation rejected every one of them outright — pre-dating PR #52, but only
        // caught by this task's bot-simulation sweep across all 18 decks.
        var deck = Definition.Decks.First(d => d.Id == deckId);
        var error = GameQueries.ValidateDeck(Definition, deck.Cards, isAdmin: false);
        Assert.Null(error);
    }

    [Fact]
    public void Bloodfangs_wrath_meta_deck_now_declares_its_hq_and_hero_so_setup_does_not_crash()
    {
        // bloodfangs-wrath's own Cards list already carried "town-hall" (hq) and "bloodfang"
        // (hero) as singleton reserve copies, but the deck never pointed its Hq/Hero fields
        // at them — MatchService.CreateMatch defaulted both to null, and SetupService's
        // PlaceStartingCard(game, null, ...) threw ("Sequence contains no matching element")
        // the instant a bot-vs-bot simulation actually tried to create this match.
        var (game, _, p1) = CreateMatch("bloodfangs-wrath");
        Assert.Contains(game.Objects, o => o.OwnerId == p1.Id && o.ZoneId == "battlefield" &&
            GameQueries.IsObjectTypeOrSubtype(game, o.ObjectType, "headquarters"));
        Assert.Contains(game.Objects, o => o.OwnerId == p1.Id && o.ZoneId == "battlefield" &&
            GameQueries.IsObjectTypeOrSubtype(game, o.ObjectType, "hero"));
    }
}
