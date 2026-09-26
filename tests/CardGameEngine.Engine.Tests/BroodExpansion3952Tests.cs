using System.Reflection;
using System.Text.Json;
using CardGameEngine.Api.Services;
using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;
using Xunit;

namespace CardGameEngine.Engine.Tests;

/// <summary>
/// Task 3952: batch 1 of the Brood faction expansion toward the 200-card baseline
/// (40 -> 73 faction-exclusive cards). PR #52's batch shipped orphaned and mostly used vocabulary the
/// engine never fires, so this suite is deliberately stricter than the 1604 vocabulary check: it
/// (1) lints every new card's raw JSON against the engine's real schema, (2) proves each one is wired
/// into a Brood precon deck, and (3) drives every new card's actual mechanic through the real
/// RuleEngine (no mocks) and asserts the state change, so a card whose ability quietly does nothing
/// fails here.
/// </summary>
public class BroodExpansion3952Tests
{
    private const string HqId = "brood-incubation-hive";
    private const string HeroId = "brood-matriarch-vess";
    private const string DeckId = "brood-incubation";

    // The 33 cards this batch adds. Order = HQ, hero, buildings, units, spells.
    private static readonly string[] NewCardIds =
    {
        HqId, HeroId,
        "brood-warren", "brood-mending-cocoon", "brood-spore-vent", "brood-acid-moat", "brood-gestation-vat",
        "brood-bile-tower", "brood-chitin-forge", "brood-siege-gland", "brood-corrosive-bog",
        "brood-stinging-thicket", "brood-neural-node", "brood-womb-of-return",
        "brood-carrion-crawler", "brood-venomfang-skitterer", "brood-hive-scout", "brood-nectar-drone",
        "brood-shell-tender", "brood-wall-gnawer", "brood-plague-bearer", "brood-frenzied-rusher",
        "brood-acid-blood-brute", "brood-bulwark-beetle", "brood-gorging-horror",
        "brood-carrion-call", "brood-adrenal-rush", "brood-necrotic-spores", "brood-paralytic-venom",
        "brood-regrowth", "brood-mind-rend", "brood-swarm-strike", "brood-devouring-swarm",
    };

    private static readonly string DefinitionPath = LocateDefinition();
    private static readonly GameDefinition Definition = LoadDefinition();

    private static string LocateDefinition()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CardGameEngine.slnx")))
            dir = dir.Parent;
        if (dir == null)
            throw new InvalidOperationException("Could not locate repo root (CardGameEngine.slnx) from " + AppContext.BaseDirectory);
        return Path.Combine(dir.FullName, "definitions", "town-tcg", "game.json");
    }

    private static GameDefinition LoadDefinition()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<GameDefinition>(File.ReadAllText(DefinitionPath), options)
            ?? throw new InvalidOperationException("Failed to deserialize " + DefinitionPath);
    }

    private static CardDefinition Card(string id) => Definition.Cards.First(c => c.Id == id);

    // ------------------------------------------------------------------ harness

    private static (GameInstance game, RuleEngine engine, PlayerInstance p1, PlayerInstance p2) CreateMatch(
        string deckAId = DeckId, string deckBId = "town", bool richResources = true)
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

        if (richResources)
        {
            foreach (var player in game.Players)
                foreach (var resDef in Definition.Resources.Where(r => r.Scope == "player"))
                    player.Resources[resDef.Id] = 99;
            // Deterministic board: no opening hand (a stray reaction card in the opponent's hand would
            // open a reaction window mid-test) and none of the setup-summoned units (the Hive's Larva,
            // the Town Hall's Peasant) so unit counts in the assertions below are exactly what each test
            // places. The decks themselves are untouched.
            foreach (var card in game.Objects.Where(o => o.ZoneId == "hand"))
                card.ZoneId = "discard";
            foreach (var unit in game.Objects.Where(o => o.ZoneId == "battlefield"
                         && GameQueries.IsObjectTypeOrSubtype(game, o.ObjectType, "unit")).ToList())
            {
                unit.IsDestroyed = true;
                unit.ZoneId = "discard";
            }
            SetBiomass(game, p1, 14);
        }

        return (game, engine, p1, p2);
    }

    private static int _counter;

    private static ObjectInstance Spawn(GameInstance game, string cardId, PlayerInstance player, string zone)
    {
        var def = game.Definition.Cards.First(c => c.Id == cardId);
        var obj = new ObjectFactory().CreateObjectInstance(game, def, player.Id, zone);
        obj.Id = $"t3952_{++_counter}";
        game.Objects.Add(obj);
        return obj;
    }

    private static ObjectInstance InHand(GameInstance game, string cardId, PlayerInstance p) => Spawn(game, cardId, p, "hand");

    private static ObjectInstance OnField(GameInstance game, string cardId, PlayerInstance p, string line = "front")
    {
        var obj = Spawn(game, cardId, p, "battlefield");
        obj.Line = line;
        return obj;
    }

    /// <summary>A plain body with deterministic stats (the real town-watch card, restatted).</summary>
    private static ObjectInstance Body(GameInstance game, PlayerInstance owner, int attack, int hp, int armor = 0, string line = "front")
    {
        var obj = OnField(game, "town-watch", owner, line);
        obj.Properties["attack"] = attack;
        obj.Properties["currentHp"] = hp;
        obj.Properties["maxHp"] = hp;
        obj.Properties["armor"] = armor;
        return obj;
    }

    private static ObjectInstance Hero(GameInstance game, PlayerInstance p) =>
        GameQueries.FindLivingHero(game, p.Id) ?? throw new InvalidOperationException("no hero");

    private static ObjectInstance Hq(GameInstance game, PlayerInstance p) =>
        GameQueries.FindResourceBank(game, p.Id) ?? throw new InvalidOperationException("no hq");

    private static void Play(GameInstance game, RuleEngine engine, PlayerInstance p, ObjectInstance card, params ObjectInstance[] targets)
    {
        var (ok, error) = engine.ExecuteAction(game, p.Id, new ActionRequest
        {
            Type = "playCard",
            SourceObjectId = card.Id,
            TargetIds = targets.Select(t => t.Id).ToList()
        });
        Assert.True(ok, $"playing {card.Name}: {error}");
    }

    private static (bool ok, string? error) TryUse(GameInstance game, RuleEngine engine, PlayerInstance p,
        ObjectInstance source, string abilityId, params ObjectInstance[] targets) =>
        engine.ExecuteAction(game, p.Id, new ActionRequest
        {
            Type = "activateAbility",
            SourceObjectId = source.Id,
            AbilityId = abilityId,
            TargetIds = targets.Select(t => t.Id).ToList()
        });

    private static void Use(GameInstance game, RuleEngine engine, PlayerInstance p, ObjectInstance source, string abilityId,
        params ObjectInstance[] targets)
    {
        var (ok, error) = TryUse(game, engine, p, source, abilityId, targets);
        Assert.True(ok, $"{source.Name}.{abilityId}: {error}");
    }

    private static (bool ok, string? error) TryAttack(GameInstance game, RuleEngine engine, ObjectInstance attacker, ObjectInstance defender)
    {
        game.ActivePlayerId = attacker.ControllerId;
        game.CurrentPhaseId = "combat";
        return engine.ExecuteAction(game, attacker.ControllerId, new ActionRequest
        {
            Type = "attack",
            SourceObjectId = attacker.Id,
            TargetIds = new List<string> { defender.Id }
        });
    }

    private static void Attack(GameInstance game, RuleEngine engine, ObjectInstance attacker, ObjectInstance defender)
    {
        var (ok, error) = TryAttack(game, engine, attacker, defender);
        Assert.True(ok, $"{attacker.Name} attacking {defender.Name}: {error}");
    }

    /// <summary>Walk the phase machine so it is <paramref name="p"/>'s turn start (fires onTurnStart triggers).</summary>
    private static void StartTurnOf(GameInstance game, RuleEngine engine, PlayerInstance p)
    {
        var other = game.Players.First(x => x.Id != p.Id);
        game.ActivePlayerId = other.Id;
        game.CurrentPhaseId = "end";
        game.State = GameState.WaitingForAction;
        engine.EndPhase(game, other.Id);
        Assert.Equal(p.Id, game.ActivePlayerId);
        game.CurrentPhaseId = "main";
    }

    /// <summary>Run <paramref name="p"/>'s end phase (expires end-of-turn buffs, ticks poison on their units).</summary>
    private static void EndPhaseOf(GameInstance game, RuleEngine engine, PlayerInstance p)
    {
        game.ActivePlayerId = p.Id;
        game.CurrentPhaseId = "combat";
        game.State = GameState.WaitingForAction;
        engine.EndPhase(game, p.Id);
        Assert.Equal("end", game.CurrentPhaseId);
    }

    private static int Hp(ObjectInstance o) => o.Properties["currentHp"];
    private static int Poison(ObjectInstance o) => o.Resources.GetValueOrDefault("poison");
    private static int Biomass(GameInstance game, PlayerInstance p) => Hq(game, p).Resources.GetValueOrDefault("biomass");
    private static void SetBiomass(GameInstance game, PlayerInstance p, int amount) => Hq(game, p).Resources["biomass"] = amount;
    private static int HandCount(GameInstance game, PlayerInstance p) =>
        game.Objects.Count(o => o.OwnerId == p.Id && o.ZoneId == "hand");
    private static int OwnBattlefield(GameInstance game, PlayerInstance p, string cardId) =>
        game.Objects.Count(o => o.OwnerId == p.Id && !o.IsDestroyed && o.ZoneId == "battlefield" && o.DefinitionId == cardId);

    // ------------------------------------------------------------------ data contract

    private static readonly HashSet<string> RegisteredEffects = new()
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

    // Trigger events TriggerService actually fires, and whether each hands the effect a Target.
    private static readonly Dictionary<string, bool> FiredTriggerEvents = new()
    {
        ["onTurnStart"] = false, ["onPlay"] = false, ["onKill"] = true, ["onDestroyBuilding"] = true,
        ["onDeath"] = true, ["onUnitDied"] = true, ["onDealCombatDamage"] = true, ["onDamaged"] = true,
        ["onFriendlyDamageHqOrHero"] = false, ["onEnemyAttack"] = true,
    };

    private static readonly HashSet<string> RegisteredCostTypes = new() { "tap", "resource", "sacrifice", "crew", "sacrifice_units" };

    // ConditionEvaluator treats an UNREGISTERED condition type as satisfied, so a typo silently passes.
    private static readonly HashSet<string> RegisteredConditionTypes = new()
    {
        "not_tapped", "is_tapped", "resource_gte", "resource_lte", "has_tag", "is_phase", "own_hero_destroyed",
        "controls_no_tagged", "controls_tagged", "is_attached", "not_attached",
    };

    private static readonly HashSet<string> EffectScopes = new() { "self", "target", "host", "opponent", "player" };

    // Effect types whose handler resolves its object through ResolveScope("target") when no scope is given.
    private static readonly HashSet<string> TargetByDefault = new()
    {
        "direct_damage", "buff_target_until_end_of_turn", "add_progress", "freeze", "reveal",
        "reveal_attachments", "destroy_infiltrators", "damage_infiltrators", "salvage_module",
    };

    private static IEnumerable<CardDefinition> NewCards => NewCardIds.Select(Card);

    private static IEnumerable<CardDefinition> PlayableNewCards => NewCards.Where(c => c.Id != HqId && c.Id != HeroId);

    public static IEnumerable<object[]> PlayableCardIds =>
        NewCardIds.Where(id => id != HqId && id != HeroId).Select(id => new object[] { id });

    private static IEnumerable<(string where, AbilityDefinition ability)> Abilities(CardDefinition c)
    {
        foreach (var a in c.Abilities) yield return ($"{c.Id}/ability {a.Id}", a);
        if (c.OnPlay != null) yield return ($"{c.Id}/onPlay", c.OnPlay);
    }

    [Fact]
    public void Batch_takes_the_brood_faction_from_40_to_at_least_73_exclusive_cards()
    {
        var before = Definition.Cards.Count(c => c.Faction == "brood" && !NewCardIds.Contains(c.Id));
        var after = Definition.Cards.Count(c => c.Faction == "brood");

        Assert.Equal(40, before); // the corrected baseline task 3952 was refined against
        Assert.Equal(33, NewCardIds.Distinct().Count());
        Assert.All(NewCards, c => Assert.Equal("brood", c.Faction));
        Assert.True(after >= 73, $"expected at least 73 faction-exclusive Brood cards, found {after}");

        // Buildings were the thinnest sub-type (7 vs 21 units vs 12 spells): the batch weights toward them.
        Assert.True(NewCards.Count(c => c.ObjectType == "building") >= 12, "batch should add at least 12 buildings");
        foreach (var type in new[] { "building", "unit", "spell", "hive-hq", "hero" })
            Assert.True(NewCards.Any(c => c.ObjectType == type), $"batch adds no '{type}' card");
    }

    [Fact]
    public void Every_new_card_uses_only_effects_triggers_costs_and_conditions_the_engine_registers()
    {
        var problems = new List<string>();
        foreach (var c in NewCards)
        {
            foreach (var (where, a) in Abilities(c))
            {
                Assert.NotEmpty(a.Effects); // an ability with no effects is a silent no-op by construction
                foreach (var e in a.Effects)
                    if (!RegisteredEffects.Contains(e.Type)) problems.Add($"{where}: unregistered effect '{e.Type}'");
                foreach (var cost in a.Costs)
                    if (!RegisteredCostTypes.Contains(cost.Type)) problems.Add($"{where}: unregistered cost '{cost.Type}'");
                foreach (var cond in a.Conditions)
                    if (!RegisteredConditionTypes.Contains(cond.Type)) problems.Add($"{where}: unregistered condition '{cond.Type}'");
            }
            foreach (var t in c.Triggers)
            {
                if (!FiredTriggerEvents.ContainsKey(t.Event)) problems.Add($"{c.Id}/trigger: unfired event '{t.Event}'");
                foreach (var e in t.Effects)
                    if (!RegisteredEffects.Contains(e.Type)) problems.Add($"{c.Id}/trigger {t.Event}: unregistered effect '{e.Type}'");
                foreach (var cond in t.Conditions)
                    if (!RegisteredConditionTypes.Contains(cond.Type)) problems.Add($"{c.Id}/trigger {t.Event}: unregistered condition '{cond.Type}'");
            }
        }
        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }

    [Fact]
    public void Every_key_in_every_new_card_is_a_real_schema_field()
    {
        // System.Text.Json silently drops unknown keys (PR #52's "property"/"target" typos deserialised
        // fine and did nothing), so lint the raw JSON against the C# schema types themselves.
        static HashSet<string> Keys(Type t) =>
            t.GetProperties().Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name)).ToHashSet();

        var cardKeys = Keys(typeof(CardDefinition));
        var abilityKeys = Keys(typeof(AbilityDefinition));
        var effectKeys = Keys(typeof(EffectDefinition));
        var costKeys = Keys(typeof(CostDefinition));
        var conditionKeys = Keys(typeof(ConditionDefinition));
        var choiceKeys = Keys(typeof(ChoiceDefinition));
        var triggerKeys = Keys(typeof(TriggerDefinition));

        var problems = new List<string>();
        void Check(JsonElement el, HashSet<string> allowed, string where)
        {
            foreach (var p in el.EnumerateObject())
                if (!allowed.Contains(p.Name)) problems.Add($"{where}: unknown key '{p.Name}'");
        }
        void CheckAbility(JsonElement a, string where)
        {
            Check(a, abilityKeys, where);
            if (a.TryGetProperty("costs", out var costs)) foreach (var c in costs.EnumerateArray()) Check(c, costKeys, where + " cost");
            if (a.TryGetProperty("conditions", out var conds)) foreach (var c in conds.EnumerateArray()) Check(c, conditionKeys, where + " condition");
            if (a.TryGetProperty("effects", out var effs)) foreach (var e in effs.EnumerateArray()) Check(e, effectKeys, where + " effect");
            if (a.TryGetProperty("choice", out var ch)) Check(ch, choiceKeys, where + " choice");
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(DefinitionPath));
        var byId = doc.RootElement.GetProperty("cards").EnumerateArray()
            .ToDictionary(c => c.GetProperty("id").GetString()!);

        foreach (var id in NewCardIds)
        {
            var c = byId[id];
            Check(c, cardKeys, id);
            if (c.TryGetProperty("abilities", out var abilities)) foreach (var a in abilities.EnumerateArray()) CheckAbility(a, id + "/ability");
            if (c.TryGetProperty("onPlay", out var onPlay)) CheckAbility(onPlay, id + "/onPlay");
            if (c.TryGetProperty("triggers", out var triggers))
                foreach (var t in triggers.EnumerateArray())
                {
                    Check(t, triggerKeys, id + "/trigger");
                    if (t.TryGetProperty("conditions", out var conds)) foreach (var cond in conds.EnumerateArray()) Check(cond, conditionKeys, id + "/trigger condition");
                    foreach (var e in t.GetProperty("effects").EnumerateArray()) Check(e, effectKeys, id + "/trigger effect");
                }
        }
        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }

    [Fact]
    public void Every_value_in_every_new_card_points_at_something_that_exists()
    {
        // A resourceId/propertyId/cardId/scope/line that names nothing deserialises fine and no-ops at runtime.
        var resourceIds = Definition.Resources.Select(r => r.Id).ToHashSet();
        var propertyIds = Definition.Properties.Select(p => p.Id).ToHashSet();
        var cardIds = Definition.Cards.Select(c => c.Id).ToHashSet();
        var lines = Definition.BattlefieldLines!.Lines.ToHashSet();
        var problems = new List<string>();

        void CheckEffect(string where, EffectDefinition e)
        {
            if (e.Scope != null && !EffectScopes.Contains(e.Scope)) problems.Add($"{where}: unknown scope '{e.Scope}'");
            if (e.ResourceId != null && !resourceIds.Contains(e.ResourceId)) problems.Add($"{where}: unknown resource '{e.ResourceId}'");
            if (e.PropertyId != null && !propertyIds.Contains(e.PropertyId)) problems.Add($"{where}: unknown property '{e.PropertyId}'");
            if (e.CardId != null && !cardIds.Contains(e.CardId)) problems.Add($"{where}: unknown card '{e.CardId}'");
            if (e.Line != null && !lines.Contains(e.Line)) problems.Add($"{where}: unknown line '{e.Line}'");
            if (e.Type == "gain_resource" && e.ResourceId == null) problems.Add($"{where}: gain_resource without resourceId");
        }

        foreach (var c in NewCards)
        {
            foreach (var k in c.Properties.Keys) if (!propertyIds.Contains(k)) problems.Add($"{c.Id}: unknown property '{k}'");
            foreach (var k in c.Resources.Keys) if (!resourceIds.Contains(k)) problems.Add($"{c.Id}: unknown resource '{k}'");
            foreach (var k in c.ResourceCapacities?.Keys ?? Enumerable.Empty<string>())
                if (!resourceIds.Contains(k)) problems.Add($"{c.Id}: unknown capacity resource '{k}'");
            foreach (var k in c.PlayCosts?.Keys ?? Enumerable.Empty<string>())
                if (!resourceIds.Contains(k)) problems.Add($"{c.Id}: unknown play-cost resource '{k}'");

            foreach (var (where, a) in Abilities(c))
            {
                foreach (var e in a.Effects) CheckEffect(where, e);
                foreach (var cost in a.Costs)
                    if (cost.Type == "resource" &&
                        (cost.ResourceId == null || !resourceIds.Contains(cost.ResourceId) || cost.Scope is not ("player" or "self")))
                        problems.Add($"{where}: bad resource cost {cost.ResourceId}/{cost.Scope}");
                if (a.Choice != null && (a.Choice.Type != "entity" || a.Choice.Controller is not ("self" or "opponent" or "any")))
                    problems.Add($"{where}: bad choice {a.Choice.Type}/{a.Choice.Controller}");
                if (a.Choice?.ObjectType != null && Definition.ObjectTypes.All(t => t.Id != a.Choice.ObjectType))
                    problems.Add($"{where}: choice names unknown object type '{a.Choice.ObjectType}'");
            }
            foreach (var t in c.Triggers)
                foreach (var e in t.Effects) CheckEffect($"{c.Id}/trigger {t.Event}", e);
        }
        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }

    [Fact]
    public void Target_scoped_effects_always_have_something_to_target()
    {
        // heal/direct_damage/freeze... with scope "target" and no ability "choice" resolve a null
        // target and do nothing: the silent no-op that hit several of the pre-batch Brood cards.
        var problems = new List<string>();
        foreach (var c in NewCards)
        {
            foreach (var (where, a) in Abilities(c))
                foreach (var e in a.Effects)
                {
                    bool wantsTarget = e.Scope == "target" || (e.Scope == null && TargetByDefault.Contains(e.Type));
                    if (wantsTarget && a.Choice == null) problems.Add($"{where}: '{e.Type}' targets nothing (no choice)");
                }
            foreach (var t in c.Triggers)
                foreach (var e in t.Effects)
                {
                    bool wantsTarget = e.Scope == "target" || (e.Scope == null && TargetByDefault.Contains(e.Type));
                    if (wantsTarget && !FiredTriggerEvents[t.Event])
                        problems.Add($"{c.Id}/trigger {t.Event}: '{e.Type}' wants an event target but {t.Event} supplies none");
                }
        }
        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }

    [Fact]
    public void Spell_effects_live_in_onPlay_because_abilities_on_a_spell_are_never_activated()
    {
        // CardPlayService only ever resolves a spell's OnPlay; an "abilities" block on a spell card in
        // hand is unreachable (that is why several pre-batch Brood spells do nothing when cast).
        foreach (var c in NewCards.Where(c => c.ObjectType == "spell"))
        {
            Assert.Empty(c.Abilities);
            Assert.NotNull(c.OnPlay);
            Assert.NotEmpty(c.OnPlay!.Effects);
        }
    }

    [Fact]
    public void Every_new_card_is_referenced_by_a_brood_precon_deck()
    {
        // PR #52's 20 newest Brood cards shipped orphaned (referenced by no deck). Each of these must be
        // reachable through a precon deck of the brood faction, in its card list or as its HQ/hero.
        var broodDecks = Definition.Decks.Where(d => d.Faction == "brood").ToList();
        Assert.Contains(broodDecks, d => d.Id == DeckId);
        var orphans = NewCardIds.Where(id => !broodDecks.Any(d =>
            d.Cards.ContainsKey(id) || d.Hq == id || d.Hero == id || d.HqOptions.Contains(id) || d.HeroOptions.Contains(id))).ToList();
        Assert.True(orphans.Count == 0, "orphaned: " + string.Join(", ", orphans));
    }

    [Fact]
    public void Incubation_deck_is_a_legal_60_card_deck_with_its_own_hq_and_hero()
    {
        // The base `brood` and `brood-apex` decks are both already at the 60-card deck maximum, so the
        // new cards get a third Brood precon instead of evicting curated cards.
        Assert.Equal(60, Definition.Decks.First(d => d.Id == "brood").Cards.Values.Sum());
        Assert.Equal(60, Definition.Decks.First(d => d.Id == "brood-apex").Cards.Values.Sum());

        var deck = Definition.Decks.First(d => d.Id == DeckId);
        Assert.Equal("brood", deck.Faction);
        Assert.Equal(60, deck.Cards.Values.Sum());
        Assert.Null(GameQueries.ValidateDeck(Definition, deck.Cards, isAdmin: false, enforceMinSize: true));
        Assert.Equal(PlayableNewCards.Select(c => c.Id).OrderBy(x => x), deck.Cards.Keys.OrderBy(x => x)); // exactly the new playables

        Assert.True(GameQueries.IsObjectTypeOrSubtype(Definition, Card(deck.Hq).ObjectType, "headquarters"));
        Assert.True(GameQueries.IsObjectTypeOrSubtype(Definition, Card(deck.Hero).ObjectType, "hero"));
        Assert.Equal(HqId, deck.Hq);
        Assert.Equal(HeroId, deck.Hero);
        Assert.Contains(deck.Hq, deck.HqOptions);
        Assert.Contains(deck.Hero, deck.HeroOptions);
    }

    [Fact]
    public void New_cards_are_deck_eligible_and_paid_in_biomass_because_brood_has_no_gold_income()
    {
        // Nothing in a Brood match grants gold, so a gold-priced Brood card is uncastable. Biomass is
        // entity-scoped: it is paid from (and only ever gained into) the Hive HQ's own bank.
        foreach (var c in NewCards)
        {
            Assert.True(GameQueries.IsDeckEligible(Definition, c), $"{c.Id} is not deck-eligible");
            var costs = GameQueries.BasePlayCosts(c);
            if (c.ObjectType == "hero") { Assert.Empty(costs); continue; }
            Assert.Equal(new[] { "biomass" }, costs.Keys.ToArray());
            Assert.True(costs["biomass"] >= 1, $"{c.Id} has no biomass cost");
        }
        Assert.All(new[] { "gold", "energy" }, res =>
            Assert.DoesNotContain(NewCards, c => GameQueries.BasePlayCosts(c).ContainsKey(res)));
    }

    [Fact]
    public void New_units_stay_within_the_armor_and_housing_rules_and_the_hive_can_house_them()
    {
        var hq = Card(HqId);
        Assert.True(hq.HousingProvided >= 5, "the new HQ must house units; the three existing hives provide none");
        foreach (var c in NewCards.Where(c => c.ObjectType == "unit"))
        {
            Assert.True(c.HousingCost >= 1, $"{c.Id} has no housing cost");
            Assert.True(c.Properties["armor"] <= 2, $"{c.Id} armor {c.Properties["armor"]} breaks the armor cap");
            Assert.Contains("organic", c.Tags); // what Consume/Devour, Gestation Vat and Swarm Strike key on
        }
    }

    // ------------------------------------------------------------------ role distinctness

    private static readonly HashSet<string> MechanicalTags = new()
    {
        "ranged", "cleave", "splash", "guard", "retaliate", "blocking", "fortification", "builder",
    };

    /// <summary>
    /// A structural fingerprint of what a card DOES (object type, engine-relevant tags, and per
    /// ability/trigger/onPlay: costs, choice filter, conditions and effect kind + scope + resource/
    /// property/card/tag/line) - never stats or names. Two cards with the same fingerprint would be the
    /// "reskinned clone under a new name" the task forbids. Banking a resource is one role whatever the
    /// scope is spelled (gain_bank_resource vs gain_resource scope player/self).
    /// </summary>
    private static string Fingerprint(CardDefinition c)
    {
        static string Tok(EffectDefinition e)
        {
            var type = e.Type == "gain_resource" || e.Type == "gain_bank_resource" ? "gain_resource" : e.Type;
            var scope = e.Scope;
            if (type == "gain_resource" && (scope is null or "player" or "self")) scope = "own";
            var parts = new[] { type, scope, e.ResourceId, e.PropertyId, e.CardId, e.Tag, e.Line,
                e.PerTaggedBuilding == null ? null : "perTagged" };
            return string.Join(":", parts.Where(x => !string.IsNullOrEmpty(x)));
        }
        static string Effects(IEnumerable<EffectDefinition> effects) =>
            string.Join("+", effects.Select(Tok).OrderBy(x => x, StringComparer.Ordinal));
        static string Ab(string label, AbilityDefinition a) =>
            $"{label}[{string.Join("+", a.Costs.Select(x => x.Type == "resource" ? "resource:" + x.ResourceId : x.Type).OrderBy(x => x, StringComparer.Ordinal))}]" +
            $"[{(a.Choice == null ? "" : $"{a.Choice.Controller}/{a.Choice.ObjectType}/{a.Choice.Tag}")}]" +
            $"[{string.Join("+", a.Conditions.Select(x => x.Type).OrderBy(x => x, StringComparer.Ordinal))}]" +
            $"[{Effects(a.Effects)}]";

        var parts = new List<string> { c.ObjectType };
        parts.AddRange(c.Tags.Where(MechanicalTags.Contains).OrderBy(x => x, StringComparer.Ordinal).Select(t => "tag:" + t));
        if (c.HousingProvided > 0) parts.Add("housing");
        if (c.Slot != null) parts.Add("slot:" + c.Slot);
        if (c.BonusAttackVsBuildings != null) parts.Add("bonusVsBuildings");
        foreach (var a in c.Abilities) parts.Add(Ab("ability", a));
        if (c.OnPlay != null) parts.Add(Ab("onPlay", c.OnPlay));
        foreach (var t in c.Triggers) parts.Add($"trigger:{t.Event}[{Effects(t.Effects)}]");
        return string.Join("|", parts.OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void New_cards_are_role_distinct_from_each_other_and_from_the_pre_batch_brood_cards()
    {
        var existing = Definition.Cards
            .Where(c => c.Faction == "brood" && !NewCardIds.Contains(c.Id))
            .ToDictionary(c => c.Id, Fingerprint);

        var seen = new Dictionary<string, string>();
        var problems = new List<string>();
        foreach (var c in NewCards)
        {
            var fp = Fingerprint(c);
            if (seen.TryGetValue(fp, out var twin)) problems.Add($"{c.Id} duplicates the role of new card {twin}");
            else seen[fp] = c.Id;
            var clash = existing.FirstOrDefault(kv => kv.Value == fp);
            if (clash.Key != null) problems.Add($"{c.Id} duplicates the role of existing card {clash.Key}");
        }
        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }

    [Fact]
    public void The_role_fingerprint_really_catches_a_reskinned_clone()
    {
        // Guard the guard: a copy of an existing card under a new id/name/stats must collide.
        var clone = JsonSerializer.Deserialize<CardDefinition>(JsonSerializer.Serialize(Card("brood-mending-cocoon")))!;
        clone.Id = "brood-mending-cocoon-reskin";
        clone.Name = "Soothing Chrysalis";
        clone.PlayCosts = new Dictionary<string, int> { ["biomass"] = 9 };
        Assert.Equal(Fingerprint(Card("brood-mending-cocoon")), Fingerprint(clone));
        Assert.NotEqual(Fingerprint(Card("brood-mending-cocoon")), Fingerprint(Card("brood-spore-vent")));
    }

    // ------------------------------------------------------------------ economy premise

    [Fact]
    public void Opening_biomass_pays_for_a_two_drop_on_turn_one_and_the_hive_provides_the_housing()
    {
        // No resource cheating here: the real starting economy of the new deck.
        var (game, engine, p1, _) = CreateMatch(richResources: false);
        var hive = Hq(game, p1);
        Assert.Equal(HqId, hive.DefinitionId);
        Assert.Equal(3, Biomass(game, p1));
        Assert.Equal(6, GameQueries.HousingCapacity(game, p1.Id));
        Assert.Equal(1, OwnBattlefield(game, p1, "larva")); // the Hive hatches one at setup

        var crawler = InHand(game, "brood-carrion-crawler", p1);
        Play(game, engine, p1, crawler);

        Assert.Equal("battlefield", crawler.ZoneId);
        Assert.Equal(1, Biomass(game, p1)); // 3 - 2
        Assert.Equal(1, GameQueries.HousingUsed(game, p1.Id));

        Use(game, engine, p1, hive, "incubation-ferment");
        Assert.Equal(4, Biomass(game, p1)); // +3, ready for a 4-drop next turn
    }

    // ------------------------------------------------------------------ every card is castable

    [Theory]
    [MemberData(nameof(PlayableCardIds))]
    public void Every_new_card_can_be_played_from_hand_and_pays_exactly_its_biomass_price(string cardId)
    {
        var (game, engine, p1, p2) = CreateMatch();
        SetBiomass(game, p1, 10);
        Body(game, p1, 2, 3);
        Body(game, p2, 1, 5);
        OnField(game, "house", p2);
        var def = Card(cardId);
        var card = InHand(game, cardId, p1);
        var targets = def.OnPlay?.Choice is { } choice
            ? new TargetingService().GetValidTargets(game, choice, p1.Id, card.Id)
                .Take(1).Select(id => game.Objects.First(o => o.Id == id)).ToArray()
            : Array.Empty<ObjectInstance>();
        if (def.OnPlay?.Choice != null) Assert.NotEmpty(targets);

        Play(game, engine, p1, card, targets);

        Assert.NotEqual("hand", card.ZoneId);
        var expected = 10 - def.PlayCosts!["biomass"] + (cardId == "brood-carrion-call" ? 3 : 0);
        Assert.Equal(expected, Biomass(game, p1));
    }

    // ------------------------------------------------------------------ headquarters + hero

    [Fact]
    public void Ferment_banks_3_biomass_and_is_capped_at_the_hives_14()
    {
        var (game, engine, p1, _) = CreateMatch();
        var hive = Hq(game, p1);
        SetBiomass(game, p1, 5);

        Use(game, engine, p1, hive, "incubation-ferment");

        Assert.Equal(8, Biomass(game, p1));
        Assert.True(hive.IsTapped);

        hive.IsTapped = false;
        SetBiomass(game, p1, 13);
        Use(game, engine, p1, hive, "incubation-ferment");
        Assert.Equal(14, Biomass(game, p1));
    }

    [Fact]
    public void Hatch_brood_pays_2_biomass_to_summon_a_larva_and_draw_a_card()
    {
        var (game, engine, p1, _) = CreateMatch();
        var hive = Hq(game, p1);
        SetBiomass(game, p1, 10);
        var handBefore = HandCount(game, p1);

        Use(game, engine, p1, hive, "incubation-hatch");

        Assert.Equal(8, Biomass(game, p1));
        Assert.Equal(1, OwnBattlefield(game, p1, "larva"));
        Assert.Equal(handBefore + 1, HandCount(game, p1));
        Assert.True(hive.IsTapped);
    }

    [Fact]
    public void Hatch_brood_is_refused_without_the_biomass_to_pay_for_it()
    {
        var (game, engine, p1, _) = CreateMatch();
        SetBiomass(game, p1, 1);

        var (ok, _) = TryUse(game, engine, p1, Hq(game, p1), "incubation-hatch");

        Assert.False(ok);
        Assert.Equal(0, OwnBattlefield(game, p1, "larva"));
    }

    [Fact]
    public void Shared_molt_heals_every_own_unit_2_for_2_ap()
    {
        var (game, engine, p1, _) = CreateMatch();
        var vess = Hero(game, p1);
        Assert.Equal(HeroId, vess.DefinitionId);
        var a = Body(game, p1, 1, 5);
        var b = Body(game, p1, 1, 5);
        a.Properties["currentHp"] = 2;
        b.Properties["currentHp"] = 4;
        vess.Resources["ap"] = 1;

        var (tooEarly, _) = TryUse(game, engine, p1, vess, "vess-molt");
        Assert.False(tooEarly); // 2 AP needed

        vess.Resources["ap"] = 2;
        Use(game, engine, p1, vess, "vess-molt");

        Assert.Equal(4, Hp(a));
        Assert.Equal(5, Hp(b)); // capped at max HP
        Assert.Equal(0, vess.Resources["ap"]);
    }

    [Fact]
    public void Barbed_lash_deals_2_armor_ignoring_damage_for_1_ap_and_a_tap()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var vess = Hero(game, p1);
        vess.Resources["ap"] = 1;
        var armored = Body(game, p2, 1, 5, armor: 2);
        var own = Body(game, p1, 1, 5);

        var (badOk, _) = TryUse(game, engine, p1, vess, "vess-lash", own);
        Assert.False(badOk); // the lash only picks enemy units

        Use(game, engine, p1, vess, "vess-lash", armored);

        Assert.Equal(3, Hp(armored)); // 2 direct damage ignores the 2 armor
        Assert.Equal(0, vess.Resources["ap"]);
        Assert.True(vess.IsTapped);
    }

    // ------------------------------------------------------------------ buildings

    [Fact]
    public void Warren_adds_5_housing()
    {
        var (game, engine, p1, _) = CreateMatch();
        Assert.Equal(6, GameQueries.HousingCapacity(game, p1.Id)); // the Hive alone

        Play(game, engine, p1, InHand(game, "brood-warren", p1));

        Assert.Equal(11, GameQueries.HousingCapacity(game, p1.Id));
        Assert.Equal(11, Biomass(game, p1)); // 14 - 3
    }

    [Fact]
    public void Mending_cocoon_mends_every_own_unit_1_hp_each_turn_start()
    {
        var (game, engine, p1, _) = CreateMatch();
        OnField(game, "brood-mending-cocoon", p1);
        var hurt = Body(game, p1, 1, 5);
        hurt.Properties["currentHp"] = 2;

        StartTurnOf(game, engine, p1);

        Assert.Equal(3, Hp(hurt));
    }

    [Fact]
    public void Spore_vent_poisons_every_enemy_unit_each_turn_start_and_the_poison_bites_on_their_turn()
    {
        var (game, engine, p1, p2) = CreateMatch();
        OnField(game, "brood-spore-vent", p1);
        var front = Body(game, p2, 1, 5);
        var back = Body(game, p2, 1, 5, line: "back");
        var mine = Body(game, p1, 1, 5);

        StartTurnOf(game, engine, p1);

        Assert.Equal(1, Poison(front));
        Assert.Equal(1, Poison(back));
        Assert.Equal(0, Poison(mine));

        EndPhaseOf(game, engine, p2); // their end phase ticks poison
        Assert.Equal(4, Hp(front));
        Assert.Equal(0, Poison(front));
        Assert.Equal(5, Hp(mine));
    }

    [Fact]
    public void Acid_moat_is_a_fortification_that_scorches_whatever_hits_it_through_armor()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var moat = OnField(game, "brood-acid-moat", p1);
        var house = OnField(game, "house", p1);
        var attacker = Body(game, p2, 1, 8, armor: 2);
        Assert.Contains("fortification", moat.Tags);

        var (houseOk, _) = TryAttack(game, engine, attacker, house);
        Assert.False(houseOk); // the untapped fortification shields the other buildings

        Attack(game, engine, attacker, moat);

        Assert.Equal(6, Hp(moat)); // armor 1 vs attack 1: buildings always take at least 1
        Assert.Equal(6, Hp(attacker)); // 2 direct damage ignores the attacker's 2 armor
    }

    [Fact]
    public void Gestation_vat_makes_only_the_next_organic_unit_cost_2_less_biomass()
    {
        var (game, engine, p1, _) = CreateMatch();
        var vat = OnField(game, "brood-gestation-vat", p1);
        SetBiomass(game, p1, 10);

        Use(game, engine, p1, vat, "vat-accelerate");
        Play(game, engine, p1, InHand(game, "brood-bulwark-beetle", p1)); // costs 4, discounted to 2
        Assert.Equal(8, Biomass(game, p1));

        Play(game, engine, p1, InHand(game, "brood-carrion-crawler", p1)); // the discount is spent: full 2
        Assert.Equal(6, Biomass(game, p1));
        Assert.True(vat.IsTapped);
    }

    [Fact]
    public void Bile_tower_hits_the_enemy_hq_for_1_armor_ignoring_damage_each_turn_start()
    {
        var (game, engine, p1, p2) = CreateMatch();
        OnField(game, "brood-bile-tower", p1);
        var enemyHq = Hq(game, p2);
        var before = Hp(enemyHq);
        Assert.True(enemyHq.Properties["armor"] >= 1);

        StartTurnOf(game, engine, p1);

        Assert.Equal(before - 1, Hp(enemyHq));
    }

    [Fact]
    public void Chitin_forge_lends_every_own_unit_1_armor_each_turn_until_end_of_turn()
    {
        var (game, engine, p1, p2) = CreateMatch();
        OnField(game, "brood-chitin-forge", p1);
        var mine = Body(game, p1, 1, 5, armor: 0);
        var theirs = Body(game, p2, 1, 5, armor: 0);

        StartTurnOf(game, engine, p1);
        Assert.Equal(1, GameQueries.GetEffectiveProperty(game, mine, "armor"));
        Assert.Equal(0, GameQueries.GetEffectiveProperty(game, theirs, "armor"));

        EndPhaseOf(game, engine, p1);
        Assert.Equal(0, GameQueries.GetEffectiveProperty(game, mine, "armor"));
    }

    [Fact]
    public void Siege_gland_spits_3_armor_ignoring_damage_at_an_enemy_building_only()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var gland = OnField(game, "brood-siege-gland", p1);
        var house = OnField(game, "house", p2);
        house.Properties["armor"] = 2;
        var enemyUnit = Body(game, p2, 1, 5);

        var (badOk, _) = TryUse(game, engine, p1, gland, "siege-gland-spit", enemyUnit);
        Assert.False(badOk); // buildings only
        Assert.Equal(5, Hp(enemyUnit));
        Assert.False(gland.IsTapped);

        Use(game, engine, p1, gland, "siege-gland-spit", house);

        Assert.Equal(1, Hp(house)); // 4 - 3, the 2 armor is ignored
        Assert.True(gland.IsTapped);
    }

    [Fact]
    public void Corrosive_bog_strips_1_armor_from_every_enemy_unit_each_turn_start_down_to_zero()
    {
        var (game, engine, p1, p2) = CreateMatch();
        OnField(game, "brood-corrosive-bog", p1);
        var enemy = Body(game, p2, 1, 5, armor: 2);
        var mine = Body(game, p1, 1, 5, armor: 2);
        var enemyHqArmor = Hq(game, p2).Properties["armor"];

        StartTurnOf(game, engine, p1);
        Assert.Equal(1, enemy.Properties["armor"]);
        StartTurnOf(game, engine, p1);
        Assert.Equal(0, enemy.Properties["armor"]);
        StartTurnOf(game, engine, p1);
        Assert.Equal(0, enemy.Properties["armor"]); // floored, never negative

        Assert.Equal(2, mine.Properties["armor"]);
        Assert.Equal(enemyHqArmor, Hq(game, p2).Properties["armor"]); // units only
    }

    [Fact]
    public void Stinging_thicket_pins_the_whole_enemy_front_line_and_leaves_the_back_alone()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var thicket = OnField(game, "brood-stinging-thicket", p1);
        var front1 = Body(game, p2, 1, 5);
        var front2 = Body(game, p2, 1, 5);
        var back = Body(game, p2, 1, 5, line: "back");

        Use(game, engine, p1, thicket, "thicket-sting");

        Assert.True(front1.IsTapped);
        Assert.True(front2.IsTapped);
        Assert.False(back.IsTapped);
        Assert.True(thicket.IsTapped);
    }

    [Fact]
    public void Neural_node_draws_one_extra_card_each_turn_start()
    {
        var (withNode, engineA, a1, _) = CreateMatch();
        var (without, engineB, b1, _) = CreateMatch();
        OnField(withNode, "brood-neural-node", a1);

        StartTurnOf(withNode, engineA, a1);
        StartTurnOf(without, engineB, b1);

        Assert.Equal(HandCount(without, b1) + 1, HandCount(withNode, a1));
    }

    [Fact]
    public void Womb_of_return_rebuilds_a_fallen_hero_at_full_hp_and_only_when_one_has_fallen()
    {
        var (game, engine, p1, _) = CreateMatch();
        var womb = OnField(game, "brood-womb-of-return", p1);
        var vess = Hero(game, p1);

        var (aliveOk, _) = TryUse(game, engine, p1, womb, "womb-rebirth");
        Assert.False(aliveOk); // nothing to rebuild while the hero lives

        vess.Properties["currentHp"] = 0;
        vess.IsDestroyed = true;
        vess.ZoneId = "discard";
        Assert.Null(GameQueries.FindLivingHero(game, p1.Id));

        Use(game, engine, p1, womb, "womb-rebirth");

        var revived = GameQueries.FindLivingHero(game, p1.Id);
        Assert.NotNull(revived);
        Assert.Equal(HeroId, revived!.DefinitionId);
        Assert.Equal(8, Hp(revived));
        Assert.True(womb.IsTapped);
    }

    // ------------------------------------------------------------------ units

    [Fact]
    public void Carrion_crawler_banks_2_biomass_whenever_it_kills()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var crawler = OnField(game, "brood-carrion-crawler", p1);
        var victim = Body(game, p2, 1, 1);
        SetBiomass(game, p1, 0);

        Attack(game, engine, crawler, victim);

        Assert.True(victim.IsDestroyed);
        Assert.Equal(2, Biomass(game, p1));
    }

    [Fact]
    public void Venomfang_skitterer_leaves_2_poison_on_whatever_it_bites()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var skitterer = OnField(game, "brood-venomfang-skitterer", p1);
        var victim = Body(game, p2, 1, 6);

        Attack(game, engine, skitterer, victim);

        Assert.Equal(5, Hp(victim));
        Assert.Equal(2, Poison(victim));
    }

    [Fact]
    public void Hive_scout_draws_a_card_when_played()
    {
        var (game, engine, p1, _) = CreateMatch();
        var scout = InHand(game, "brood-hive-scout", p1);
        var handBefore = HandCount(game, p1); // includes the Scout

        Play(game, engine, p1, scout);

        Assert.Equal("battlefield", scout.ZoneId);
        Assert.Equal(handBefore, HandCount(game, p1)); // -1 played, +1 drawn
    }

    [Fact]
    public void Nectar_drone_milks_1_biomass_into_the_hive_per_tap()
    {
        var (game, engine, p1, _) = CreateMatch();
        var drone = OnField(game, "brood-nectar-drone", p1);
        SetBiomass(game, p1, 0);

        Use(game, engine, p1, drone, "nectar-drone-milk");

        Assert.Equal(1, Biomass(game, p1));
        Assert.True(drone.IsTapped);
        var (again, _) = TryUse(game, engine, p1, drone, "nectar-drone-milk");
        Assert.False(again); // one tap per turn
        Assert.Equal(1, Biomass(game, p1));
        Assert.Equal(0, drone.Properties["attack"]); // a pure economy body
    }

    [Fact]
    public void Shell_tender_lends_2_armor_to_an_ally_until_end_of_turn()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var tender = OnField(game, "brood-shell-tender", p1);
        var ally = Body(game, p1, 2, 4, armor: 0);
        var enemy = Body(game, p2, 2, 4, armor: 0);

        var (badOk, _) = TryUse(game, engine, p1, tender, "shell-tender-glaze", enemy);
        Assert.False(badOk); // allies only

        Use(game, engine, p1, tender, "shell-tender-glaze", ally);

        Assert.Equal(2, GameQueries.GetEffectiveProperty(game, ally, "armor"));
        Assert.True(tender.IsTapped);
        EndPhaseOf(game, engine, p1);
        Assert.Equal(0, GameQueries.GetEffectiveProperty(game, ally, "armor"));
    }

    [Fact]
    public void Wall_gnawer_shreds_buildings_and_banks_biomass_from_the_wreck()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var gnawer = OnField(game, "brood-wall-gnawer", p1);
        var house = OnField(game, "house", p2);
        house.Properties["currentHp"] = 5;
        house.Properties["maxHp"] = 5;
        house.Properties["armor"] = 0;
        SetBiomass(game, p1, 0);

        Attack(game, engine, gnawer, house);

        Assert.True(house.IsDestroyed); // 2 attack + 3 bonus vs buildings = 5
        Assert.Equal(2, Biomass(game, p1));
    }

    [Fact]
    public void Plague_bearer_bursts_into_poison_over_the_whole_enemy_line_when_it_dies()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var bearer = OnField(game, "brood-plague-bearer", p1);
        var attacker = Body(game, p2, 5, 6);
        var bystander = Body(game, p2, 1, 6, line: "back");

        Attack(game, engine, attacker, bearer);

        Assert.True(bearer.IsDestroyed);
        Assert.Equal(2, Poison(attacker));
        Assert.Equal(2, Poison(bystander));
    }

    [Fact]
    public void Frenzied_rusher_untaps_after_a_kill_and_chains_a_second_attack()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var rusher = OnField(game, "brood-frenzied-rusher", p1);
        var first = Body(game, p2, 1, 1);
        var second = Body(game, p2, 1, 1);

        Attack(game, engine, rusher, first);
        Assert.True(first.IsDestroyed);
        Assert.False(rusher.IsTapped);

        Attack(game, engine, rusher, second);
        Assert.True(second.IsDestroyed);
    }

    [Fact]
    public void Acid_blood_brute_scorches_whatever_wounds_it_through_armor()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var brute = OnField(game, "brood-acid-blood-brute", p1);
        var attacker = Body(game, p2, 2, 8, armor: 2);

        Attack(game, engine, attacker, brute);

        Assert.Equal(5, Hp(brute)); // 2 attack - 1 armor
        Assert.Equal(6, Hp(attacker)); // 2 direct damage ignores the attacker's 2 armor
    }

    [Fact]
    public void Bulwark_beetle_forces_the_enemy_to_hit_it_before_anything_else_on_the_line()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var beetle = OnField(game, "brood-bulwark-beetle", p1);
        var soft = OnField(game, "brood-carrion-crawler", p1);
        var raider = Body(game, p2, 1, 5);
        Assert.Contains("blocking", beetle.Tags);

        var (softOk, _) = TryAttack(game, engine, raider, soft);
        Assert.False(softOk);
        Assert.Equal(2, Hp(soft));

        var (beetleOk, error) = TryAttack(game, engine, raider, beetle);
        Assert.True(beetleOk, error);
    }

    [Fact]
    public void Gorging_horror_grows_1_attack_every_turn_start()
    {
        var (game, engine, p1, _) = CreateMatch();
        var horror = OnField(game, "brood-gorging-horror", p1);
        Assert.Equal(2, horror.Properties["attack"]);

        StartTurnOf(game, engine, p1);
        Assert.Equal(3, horror.Properties["attack"]);
        StartTurnOf(game, engine, p1);
        Assert.Equal(4, horror.Properties["attack"]);
    }

    // ------------------------------------------------------------------ spells

    [Fact]
    public void Carrion_call_banks_3_biomass_and_draws_a_card()
    {
        var (game, engine, p1, _) = CreateMatch();
        SetBiomass(game, p1, 6);
        var call = InHand(game, "brood-carrion-call", p1);
        var handBefore = HandCount(game, p1); // includes the spell

        Play(game, engine, p1, call);

        Assert.Equal(7, Biomass(game, p1)); // 6 - 2 + 3
        Assert.Equal(handBefore, HandCount(game, p1)); // -1 cast, +1 drawn
    }

    [Fact]
    public void Adrenal_rush_untaps_a_spent_unit_and_pumps_it_2_attack_for_the_turn()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var spent = Body(game, p1, 2, 4);
        spent.IsTapped = true;
        var enemy = Body(game, p2, 2, 4);
        var rush = InHand(game, "brood-adrenal-rush", p1);

        var (badOk, _) = engine.ExecuteAction(game, p1.Id, new ActionRequest
        {
            Type = "playCard", SourceObjectId = rush.Id, TargetIds = new List<string> { enemy.Id }
        });
        Assert.False(badOk); // own units only

        Play(game, engine, p1, rush, spent);

        Assert.False(spent.IsTapped);
        Assert.Equal(4, GameQueries.GetEffectiveProperty(game, spent, "attack"));
        EndPhaseOf(game, engine, p1);
        Assert.Equal(2, GameQueries.GetEffectiveProperty(game, spent, "attack"));
    }

    [Fact]
    public void Necrotic_spores_poison_every_enemy_unit_for_2()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var front = Body(game, p2, 1, 5);
        var back = Body(game, p2, 1, 5, line: "back");
        var mine = Body(game, p1, 1, 5);

        Play(game, engine, p1, InHand(game, "brood-necrotic-spores", p1));

        Assert.Equal(2, Poison(front));
        Assert.Equal(2, Poison(back));
        Assert.Equal(0, Poison(mine));
    }

    [Fact]
    public void Paralytic_venom_freezes_the_chosen_enemy_unit_solid_and_nothing_else()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var target = Body(game, p2, 3, 5);
        var bystander = Body(game, p2, 3, 5);
        var mine = Body(game, p1, 3, 5);
        var venom = InHand(game, "brood-paralytic-venom", p1);

        var (badOk, _) = engine.ExecuteAction(game, p1.Id, new ActionRequest
        {
            Type = "playCard", SourceObjectId = venom.Id, TargetIds = new List<string> { mine.Id }
        });
        Assert.False(badOk); // enemy units only

        Play(game, engine, p1, venom, target);

        Assert.True(target.IsTapped);
        Assert.True(target.SkipNextUntap);
        Assert.False(bystander.IsTapped);
    }

    [Fact]
    public void Regrowth_heals_every_own_unit_3()
    {
        var (game, engine, p1, _) = CreateMatch();
        var a = Body(game, p1, 1, 6);
        var b = Body(game, p1, 1, 4);
        a.Properties["currentHp"] = 1;
        b.Properties["currentHp"] = 3;

        Play(game, engine, p1, InHand(game, "brood-regrowth", p1));

        Assert.Equal(4, Hp(a));
        Assert.Equal(4, Hp(b)); // capped at max HP
    }

    [Fact]
    public void Mind_rend_draws_a_card_and_forces_the_opponent_to_discard_one()
    {
        var (game, engine, p1, p2) = CreateMatch();
        Spawn(game, "town-watch", p2, "hand");
        Spawn(game, "town-watch", p2, "hand");
        var rend = InHand(game, "brood-mind-rend", p1);
        var mineBefore = HandCount(game, p1); // includes the spell

        Play(game, engine, p1, rend);

        Assert.Equal(1, HandCount(game, p2));
        Assert.Equal(mineBefore, HandCount(game, p1)); // -1 cast, +1 drawn
    }

    [Fact]
    public void Swarm_strike_scales_with_the_swarm_and_ignores_armor()
    {
        var (game, engine, p1, p2) = CreateMatch();
        OnField(game, "brood-carrion-crawler", p1);
        OnField(game, "brood-carrion-crawler", p1);
        OnField(game, "brood-carrion-crawler", p1);
        Body(game, p1, 1, 5); // a town-watch is not organic: it does not count
        var target = Body(game, p2, 1, 10, armor: 2);

        Play(game, engine, p1, InHand(game, "brood-swarm-strike", p1), target);

        Assert.Equal(7, Hp(target)); // 3 organic bodies = 3 direct damage, the 2 armor is ignored
    }

    [Fact]
    public void Swarm_strike_can_finish_the_enemy_hq_and_scales_up_as_the_swarm_grows()
    {
        var (game, engine, p1, p2) = CreateMatch();
        for (int i = 0; i < 5; i++) OnField(game, "brood-carrion-crawler", p1);
        var enemyHq = Hq(game, p2);
        var before = Hp(enemyHq);

        Play(game, engine, p1, InHand(game, "brood-swarm-strike", p1), enemyHq);

        Assert.Equal(before - 5, Hp(enemyHq));
    }

    [Fact]
    public void Devouring_swarm_hits_the_whole_enemy_front_line_for_3_and_spares_the_back()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var armored = Body(game, p2, 1, 5, armor: 1);
        var frail = Body(game, p2, 1, 3);
        var back = Body(game, p2, 1, 5, line: "back");
        var mine = Body(game, p1, 1, 5);

        Play(game, engine, p1, InHand(game, "brood-devouring-swarm", p1));

        Assert.Equal(3, Hp(armored)); // 3 - 1 armor
        Assert.True(frail.IsDestroyed);
        Assert.Equal(5, Hp(back));
        Assert.Equal(5, Hp(mine));
    }

    // ------------------------------------------------------------------ live bot-vs-bot smoke

    [Theory]
    [InlineData("town")]
    [InlineData("brood")]
    [InlineData("raiders")]
    [InlineData("undead")]
    public void Incubation_deck_plays_real_bot_matches_without_errors_and_actually_casts_its_new_cards(string opponentDeck)
    {
        // Real economy (no free resources), real bots, alternating first player - the same loop the
        // /api/simulate endpoint runs. Proves the deck is not merely legal but playable: the new cards
        // must be castable from the deck's own biomass income and housing.
        var castNewCards = new HashSet<string>();
        int games = 6;
        for (int i = 0; i < games; i++)
        {
            bool incubationFirst = i % 2 == 0;
            var deckIds = incubationFirst ? (DeckId, opponentDeck) : (opponentDeck, DeckId);
            var (game, engine, p1, p2) = CreateMatch(deckIds.Item1, deckIds.Item2, richResources: false);
            p1.IsBot = true;
            p2.IsBot = true;

            var bot = new BotService(engine);
            int safety = 0;
            while (game.State != GameState.GameEnded && game.TurnNumber < 60 && safety++ < 100)
            {
                var turn = game.TurnNumber;
                bot.PlayBotTurns(game);
                if (game.TurnNumber == turn) break;
            }

            var incubationPlayer = incubationFirst ? p1 : p2;
            foreach (var o in game.Objects.Where(o => o.OwnerId == incubationPlayer.Id
                         && NewCardIds.Contains(o.DefinitionId) && o.ZoneId is "battlefield" or "discard"))
                castNewCards.Add(o.DefinitionId);
            Assert.DoesNotContain(game.Log, l => l.Contains("Exception", StringComparison.OrdinalIgnoreCase));
        }

        Assert.True(castNewCards.Count >= 8,
            $"only {castNewCards.Count} distinct new cards were ever cast across {games} games vs {opponentDeck}: {string.Join(", ", castNewCards)}");
    }
}
