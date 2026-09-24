using System.Reflection;
using System.Text.Json;
using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;
using Xunit;

namespace CardGameEngine.Engine.Tests;

/// <summary>
/// Task 3953: batch 1 of the Shadow faction expansion toward the 200-card baseline
/// (42 -> 80 faction-exclusive cards). PR #52's batch shipped orphaned and mostly used vocabulary the
/// engine never fires, so this suite is deliberately stricter than the 1604 vocabulary check: it
/// (1) lints every new card's raw JSON against the engine's real schema, (2) proves each one is wired
/// into a Shadow precon deck, and (3) drives every new card's actual mechanic through the real
/// RuleEngine (no mocks) and asserts the state change, so a card whose ability quietly does nothing
/// fails here.
/// </summary>
public class ShadowExpansion3953Tests
{
    private const string HqId = "shadow-undercity-exchange";
    private const string HeroId = "shadow-guildmistress-vesper";
    private const string DeckId = "shadow-undercity";

    // The 38 cards this batch adds. Order = HQ, hero, spy-units, units, equipment, buildings, spells.
    private static readonly string[] NewCardIds =
    {
        HqId, HeroId,
        "shadow-vault-cracker", "shadow-sapper", "shadow-leak-artist", "shadow-blight-agent",
        "shadow-guild-pickpocket", "shadow-bounty-hunter", "shadow-rooftop-sniper", "shadow-blackmailer",
        "shadow-trapwire-bodyguard", "shadow-safecracker", "shadow-fixer", "shadow-alley-thug",
        "shadow-poisoned-stiletto", "shadow-garrote-wire", "shadow-watchmans-cowl",
        "shadow-tenement-flophouse", "shadow-counting-room", "shadow-listening-post", "shadow-thieves-den",
        "shadow-barred-warehouse", "shadow-back-alley-clinic", "shadow-protection-racket",
        "shadow-carrion-broker", "shadow-guild-archive",
        "shadow-whisper-campaign", "shadow-cutthroat-contract", "shadow-insider-deal", "shadow-smash-and-grab",
        "shadow-hamstring", "shadow-cook-the-books", "shadow-night-raid", "shadow-call-in-favours",
        "shadow-trapdoor", "shadow-venomed-needle", "shadow-powder-keg", "shadow-sleight-of-hand",
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
            // open a reaction window mid-test) and none of the setup-summoned units (the Exchange's
            // Footpad, the Town Hall's Peasant) so unit counts in the assertions below are exactly what
            // each test places. The decks themselves are untouched.
            foreach (var card in game.Objects.Where(o => o.ZoneId == "hand"))
                card.ZoneId = "discard";
            foreach (var unit in game.Objects.Where(o => o.ZoneId == "battlefield"
                         && GameQueries.IsObjectTypeOrSubtype(game, o.ObjectType, "unit")).ToList())
            {
                unit.IsDestroyed = true;
                unit.ZoneId = "discard";
            }
            SetIntel(game, p1, 8);
        }

        return (game, engine, p1, p2);
    }

    private static int _counter;

    private static ObjectInstance Spawn(GameInstance game, string cardId, PlayerInstance player, string zone)
    {
        var def = game.Definition.Cards.First(c => c.Id == cardId);
        var obj = new ObjectFactory().CreateObjectInstance(game, def, player.Id, zone);
        obj.Id = $"t3953_{++_counter}";
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

    /// <summary>An enemy building (the real house card) with deterministic HP and no armor.</summary>
    private static ObjectInstance House(GameInstance game, PlayerInstance owner, int hp)
    {
        var obj = OnField(game, "house", owner);
        obj.Properties["currentHp"] = hp;
        obj.Properties["maxHp"] = hp;
        obj.Properties["armor"] = 0;
        return obj;
    }

    private static ObjectInstance Hero(GameInstance game, PlayerInstance p) =>
        GameQueries.FindLivingHero(game, p.Id) ?? throw new InvalidOperationException("no hero");

    private static ObjectInstance Hq(GameInstance game, PlayerInstance p) =>
        GameQueries.FindResourceBank(game, p.Id) ?? throw new InvalidOperationException("no hq");

    private static (bool ok, string? error) TryPlay(GameInstance game, RuleEngine engine, PlayerInstance p, ObjectInstance card,
        params ObjectInstance[] targets) =>
        engine.ExecuteAction(game, p.Id, new ActionRequest
        {
            Type = "playCard",
            SourceObjectId = card.Id,
            TargetIds = targets.Select(t => t.Id).ToList()
        });

    private static void Play(GameInstance game, RuleEngine engine, PlayerInstance p, ObjectInstance card, params ObjectInstance[] targets)
    {
        var (ok, error) = TryPlay(game, engine, p, card, targets);
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

    /// <summary>Run <paramref name="p"/>'s end phase (expires end-of-turn buffs).</summary>
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
    private static int Gold(PlayerInstance p) => p.Resources.GetValueOrDefault("gold");
    private static void SetGold(PlayerInstance p, int amount) => p.Resources["gold"] = amount;
    private static int Intel(GameInstance game, PlayerInstance p) => Hq(game, p).Resources.GetValueOrDefault("intel");
    private static void SetIntel(GameInstance game, PlayerInstance p, int amount) => Hq(game, p).Resources["intel"] = amount;
    private static int SelfIntel(ObjectInstance o) => o.Resources.GetValueOrDefault("intel");
    private static int Attack(GameInstance game, ObjectInstance o) => GameQueries.GetEffectiveProperty(game, o, "attack");
    private static int Armor(GameInstance game, ObjectInstance o) => GameQueries.GetEffectiveProperty(game, o, "armor");
    private static int HandCount(GameInstance game, PlayerInstance p) =>
        game.Objects.Count(o => o.OwnerId == p.Id && o.ZoneId == "hand");
    private static int OwnBattlefield(GameInstance game, PlayerInstance p, string cardId) =>
        game.Objects.Count(o => o.OwnerId == p.Id && !o.IsDestroyed && o.ZoneId == "battlefield" && o.DefinitionId == cardId);

    /// <summary>A spy of ours burrowed into an enemy house; returns both. The spy has no summoning sickness (spawned).</summary>
    private static (ObjectInstance spy, ObjectInstance host) Infiltrated(GameInstance game, RuleEngine engine,
        PlayerInstance p1, PlayerInstance p2, string cardId, string slug, int hostHp = 6)
    {
        var host = House(game, p2, hostHp);
        var spy = OnField(game, cardId, p1);
        Use(game, engine, p1, spy, slug + "-infiltrate", host);
        Assert.Equal(host.Id, spy.AttachedToId);
        Assert.True(spy.FaceDown);
        return (spy, host);
    }

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

    private static readonly HashSet<string> ReactionWindows = new() { "attackDeclared", "spellCast", "abilityActivated" };

    // Effect types whose handler resolves its object through ResolveScope("target") when no scope is given.
    private static readonly HashSet<string> TargetByDefault = new()
    {
        "direct_damage", "buff_target_until_end_of_turn", "add_progress", "freeze", "reveal",
        "reveal_attachments", "destroy_infiltrators", "damage_infiltrators", "salvage_module",
    };

    private static IEnumerable<CardDefinition> NewCards => NewCardIds.Select(Card);

    private static IEnumerable<CardDefinition> PlayableNewCards => NewCards.Where(c => c.Id != HqId && c.Id != HeroId);

    /// <summary>Playable from the main phase: everything except the reaction spell (which only answers an attack).</summary>
    public static IEnumerable<object[]> MainPhasePlayableCardIds =>
        NewCardIds.Where(id => id != HqId && id != HeroId && Card(id).Timing != "reaction").Select(id => new object[] { id });

    private static IEnumerable<(string where, AbilityDefinition ability)> Abilities(CardDefinition c)
    {
        foreach (var a in c.Abilities) yield return ($"{c.Id}/ability {a.Id}", a);
        if (c.OnPlay != null) yield return ($"{c.Id}/onPlay", c.OnPlay);
    }

    [Fact]
    public void Batch_takes_the_shadow_faction_from_42_to_at_least_80_exclusive_cards()
    {
        var before = Definition.Cards.Count(c => c.Faction == "shadow" && !NewCardIds.Contains(c.Id));
        var after = Definition.Cards.Count(c => c.Faction == "shadow");

        Assert.Equal(42, before); // the corrected baseline task 3953 was refined against
        Assert.Equal(38, NewCardIds.Distinct().Count());
        Assert.All(NewCards, c => Assert.Equal("shadow", c.Faction));
        Assert.True(after >= 80, $"expected at least 80 faction-exclusive Shadow cards, found {after}");

        // Shadow is the only faction with the spy-unit sub-type (3 cards) and had a single equipment card:
        // the batch grows both alongside unit/building/spell.
        Assert.Equal(3, Definition.Cards.Count(c => c.Faction == "shadow" && c.ObjectType == "spy-unit" && !NewCardIds.Contains(c.Id)));
        Assert.Equal(4, NewCards.Count(c => c.ObjectType == "spy-unit"));
        Assert.True(NewCards.Count(c => c.ObjectType == "equipment") >= 3, "batch should add at least 3 equipment");
        Assert.True(NewCards.Count(c => c.ObjectType == "building") >= 9, "batch should add at least 9 buildings");
        Assert.True(NewCards.Count(c => c.ObjectType == "spell") >= 12, "batch should add at least 12 spells");
        foreach (var type in new[] { "building", "unit", "spy-unit", "spell", "equipment", "headquarters", "hero" })
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
                Assert.NotEmpty(t.Effects);
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
        var attachModifierKeys = Keys(typeof(AttachModifierDefinition));
        var deckKeys = Keys(typeof(PreconDeckDefinition));

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
            if (c.TryGetProperty("attachModifiers", out var mods)) foreach (var m in mods.EnumerateArray()) Check(m, attachModifierKeys, id + "/attachModifier");
            if (c.TryGetProperty("triggers", out var triggers))
                foreach (var t in triggers.EnumerateArray())
                {
                    Check(t, triggerKeys, id + "/trigger");
                    if (t.TryGetProperty("conditions", out var conds)) foreach (var cond in conds.EnumerateArray()) Check(cond, conditionKeys, id + "/trigger condition");
                    foreach (var e in t.GetProperty("effects").EnumerateArray()) Check(e, effectKeys, id + "/trigger effect");
                }
        }

        var deck = doc.RootElement.GetProperty("decks").EnumerateArray().First(d => d.GetProperty("id").GetString() == DeckId);
        Check(deck, deckKeys, DeckId);

        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }

    [Fact]
    public void Every_value_in_every_new_card_points_at_something_that_exists()
    {
        // A resourceId/propertyId/cardId/scope/line/tag that names nothing deserialises fine and no-ops at runtime.
        var resourceIds = Definition.Resources.Select(r => r.Id).ToHashSet();
        var playerResources = Definition.Resources.Where(r => r.Scope == "player").Select(r => r.Id).ToHashSet();
        var entityResources = Definition.Resources.Where(r => r.Scope == "entity").Select(r => r.Id).ToHashSet();
        var propertyIds = Definition.Properties.Select(p => p.Id).ToHashSet();
        var cardIds = Definition.Cards.Select(c => c.Id).ToHashSet();
        var lines = Definition.BattlefieldLines!.Lines.ToHashSet();
        var slotIds = Definition.DefaultEquipmentSlots!.Keys.ToHashSet();
        var problems = new List<string>();

        void CheckEffect(string where, EffectDefinition e)
        {
            if (e.Scope != null && !EffectScopes.Contains(e.Scope)) problems.Add($"{where}: unknown scope '{e.Scope}'");
            if (e.ResourceId != null && !resourceIds.Contains(e.ResourceId)) problems.Add($"{where}: unknown resource '{e.ResourceId}'");
            if (e.PropertyId != null && !propertyIds.Contains(e.PropertyId)) problems.Add($"{where}: unknown property '{e.PropertyId}'");
            if (e.CardId != null && !cardIds.Contains(e.CardId)) problems.Add($"{where}: unknown card '{e.CardId}'");
            if (e.Line != null && !lines.Contains(e.Line)) problems.Add($"{where}: unknown line '{e.Line}'");
            if (e.Type == "gain_resource" && e.ResourceId == null) problems.Add($"{where}: gain_resource without resourceId");
            // gain_resource scope player writes player.Resources; an entity-scoped resource there is a pool nothing
            // reads (the pre-batch Shadow cards put Intel there: 27 effects that bank into the void).
            if (e.Type == "gain_resource" && e.Scope == "player" && e.ResourceId != null && !playerResources.Contains(e.ResourceId))
                problems.Add($"{where}: gain_resource scope player names entity resource '{e.ResourceId}' (use gain_bank_resource or scope self)");
            if (e.Type == "gain_bank_resource" && (e.ResourceId == null || !entityResources.Contains(e.ResourceId)))
                problems.Add($"{where}: gain_bank_resource must name an entity-scoped resource");
            if (e.Type == "summon" && e.CardId == null) problems.Add($"{where}: summon without cardId");
            if (e.Type == "cost_discount" && e.Tag != null && !NewCards.Any(c => c.Tags.Contains(e.Tag)))
                problems.Add($"{where}: cost_discount tag '{e.Tag}' matches no new card");
        }

        foreach (var c in NewCards)
        {
            foreach (var k in c.Properties.Keys) if (!propertyIds.Contains(k)) problems.Add($"{c.Id}: unknown property '{k}'");
            foreach (var k in c.Resources.Keys) if (!resourceIds.Contains(k)) problems.Add($"{c.Id}: unknown resource '{k}'");
            foreach (var k in c.ResourceCapacities?.Keys ?? Enumerable.Empty<string>())
                if (!resourceIds.Contains(k)) problems.Add($"{c.Id}: unknown capacity resource '{k}'");
            foreach (var k in c.PlayCosts?.Keys ?? Enumerable.Empty<string>())
                if (!resourceIds.Contains(k)) problems.Add($"{c.Id}: unknown play-cost resource '{k}'");
            foreach (var m in c.AttachModifiers)
                if (!propertyIds.Contains(m.PropertyId)) problems.Add($"{c.Id}: attach modifier names unknown property '{m.PropertyId}'");
            foreach (var s in c.Slots ?? new List<string>())
                if (!slotIds.Contains(s)) problems.Add($"{c.Id}: unknown equipment slot '{s}'");
            foreach (var r in c.ReactionTo)
                if (!ReactionWindows.Contains(r)) problems.Add($"{c.Id}: unknown reaction window '{r}'");

            foreach (var (where, a) in Abilities(c))
            {
                foreach (var e in a.Effects) CheckEffect(where, e);
                foreach (var cost in a.Costs)
                {
                    if (cost.Type != "resource") continue;
                    if (cost.ResourceId == null || !resourceIds.Contains(cost.ResourceId) || cost.Scope is not ("player" or "self"))
                        problems.Add($"{where}: bad resource cost {cost.ResourceId}/{cost.Scope}");
                    // A player-scope cost reads the player pool, a self-scope cost the object's own bank.
                    else if (cost.Scope == "player" && !playerResources.Contains(cost.ResourceId))
                        problems.Add($"{where}: player-scope cost names entity resource '{cost.ResourceId}'");
                    else if (cost.Scope == "self" && !entityResources.Contains(cost.ResourceId))
                        problems.Add($"{where}: self-scope cost names player resource '{cost.ResourceId}'");
                }
                if (a.Choice != null && (a.Choice.Type != "entity" || a.Choice.Controller is not ("self" or "opponent" or "any")))
                    problems.Add($"{where}: bad choice {a.Choice.Type}/{a.Choice.Controller}");
                if (a.Choice?.ObjectType != null && Definition.ObjectTypes.All(t => t.Id != a.Choice.ObjectType))
                    problems.Add($"{where}: choice names unknown object type '{a.Choice.ObjectType}'");
                if (a.Choice?.MaxPropertyId != null && !propertyIds.Contains(a.Choice.MaxPropertyId))
                    problems.Add($"{where}: choice names unknown property '{a.Choice.MaxPropertyId}'");
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
        // target and do nothing: the silent no-op that hit several of the pre-batch Shadow cards.
        // scope "host" only resolves while the spy is infiltrated, so it needs the is_attached gate.
        var problems = new List<string>();
        foreach (var c in NewCards)
        {
            foreach (var (where, a) in Abilities(c))
                foreach (var e in a.Effects)
                {
                    bool wantsTarget = e.Scope == "target" || (e.Scope == null && TargetByDefault.Contains(e.Type));
                    if (wantsTarget && a.Choice == null) problems.Add($"{where}: '{e.Type}' targets nothing (no choice)");
                    if (e.Scope == "host" && a.Conditions.All(cond => cond.Type != "is_attached"))
                        problems.Add($"{where}: scope host without an is_attached condition");
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
    public void Spell_effects_live_where_the_engine_actually_runs_them()
    {
        // CardPlayService only ever resolves a spell's OnPlay; an "abilities" block on a spell card in
        // hand is unreachable (that is why 9 pre-batch Shadow spells do nothing when cast). Secrets are the
        // one exception: face-down, they carry triggers on the enemy's attack instead of an onPlay.
        foreach (var c in NewCards.Where(c => c.ObjectType == "spell"))
        {
            Assert.Empty(c.Abilities);
            if (c.IsSecret)
            {
                Assert.Null(c.OnPlay);
                Assert.NotEmpty(c.Triggers);
                Assert.All(c.Triggers, t => Assert.Equal("onEnemyAttack", t.Event));
            }
            else
            {
                Assert.NotNull(c.OnPlay);
                Assert.NotEmpty(c.OnPlay!.Effects);
                Assert.Empty(c.Triggers);
            }
            if (c.Timing == "reaction")
            {
                Assert.NotEmpty(c.ReactionTo);
                Assert.NotNull(c.OnPlay);
            }
        }
        // Only spells can be secrets or reactions.
        Assert.All(NewCards.Where(c => c.IsSecret || c.Timing == "reaction"), c => Assert.Equal("spell", c.ObjectType));
    }

    [Fact]
    public void Equipment_declares_slots_and_a_bearer_or_it_lands_unattached_and_does_nothing()
    {
        foreach (var c in NewCards.Where(c => c.ObjectType == "equipment"))
        {
            Assert.NotEmpty(c.Slots!);
            Assert.Equal("chooseCharacter", c.AttachTo);
            Assert.True(c.AttachModifiers.Count > 0 || c.AttachTags.Count > 0 || c.Triggers.Count > 0,
                $"{c.Id} would grant its bearer nothing");
        }
    }

    [Fact]
    public void Every_new_card_is_referenced_by_a_shadow_precon_deck()
    {
        // 22 of the 42 pre-batch Shadow cards are referenced by no deck. Each of the new ones must be
        // reachable through a precon deck of the shadow faction, in its card list or as its HQ/hero.
        var shadowDecks = Definition.Decks.Where(d => d.Faction == "shadow").ToList();
        Assert.Contains(shadowDecks, d => d.Id == DeckId);
        var orphans = NewCardIds.Where(id => !shadowDecks.Any(d =>
            d.Cards.ContainsKey(id) || d.Hq == id || d.Hero == id || d.HqOptions.Contains(id) || d.HeroOptions.Contains(id))).ToList();
        Assert.True(orphans.Count == 0, "orphaned: " + string.Join(", ", orphans));
    }

    [Fact]
    public void Undercity_deck_is_a_legal_60_card_deck_with_its_own_hq_and_hero()
    {
        // The base `shadow` and `shadow-stranglehold` decks are both already at the 60-card deck maximum,
        // so the new cards get a third Shadow precon instead of evicting curated cards.
        Assert.Equal(60, Definition.Decks.First(d => d.Id == "shadow").Cards.Values.Sum());
        Assert.Equal(60, Definition.Decks.First(d => d.Id == "shadow-stranglehold").Cards.Values.Sum());

        var deck = Definition.Decks.First(d => d.Id == DeckId);
        Assert.Equal("shadow", deck.Faction);
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
    public void New_cards_are_deck_eligible_and_priced_in_gold_and_intel_which_the_undercity_exchange_makes()
    {
        // Gold is player-scoped (the Exchange skims 2 a turn); Intel is entity-scoped, paid from the HQ bank
        // (the Exchange banks 1 a turn, capacity 8). Nothing else may appear in a price, or the card is uncastable.
        var hq = Card(HqId);
        var intelCap = hq.ResourceCapacities!["intel"];
        foreach (var c in NewCards)
        {
            Assert.True(GameQueries.IsDeckEligible(Definition, c), $"{c.Id} is not deck-eligible");
            var costs = GameQueries.BasePlayCosts(c);
            if (c.ObjectType is "hero" or "headquarters") { Assert.Empty(costs); continue; }
            Assert.True(costs.ContainsKey("gold"), $"{c.Id} has no gold price");
            Assert.All(costs.Keys, k => Assert.Contains(k, new[] { "gold", "intel" }));
            Assert.InRange(costs["gold"], 1, 4);
            if (costs.TryGetValue("intel", out var intel)) Assert.InRange(intel, 1, intelCap);
        }
        Assert.Contains(NewCards, c => GameQueries.BasePlayCosts(c).ContainsKey("intel")); // the second resource is really used
    }

    [Fact]
    public void New_units_stay_within_the_armor_and_housing_rules_and_the_exchange_can_house_them()
    {
        var hq = Card(HqId);
        Assert.True(hq.HousingProvided >= 5, "the new HQ must house units; the three existing Shadow HQs provide none");
        foreach (var c in NewCards.Where(c => c.ObjectType is "unit" or "spy-unit"))
        {
            Assert.True(c.HousingCost >= 1, $"{c.Id} has no housing cost");
            Assert.True(c.HousingCost <= hq.HousingProvided, $"{c.Id} cannot fit in the Exchange");
            Assert.True(c.Properties["armor"] <= 2, $"{c.Id} armor {c.Properties["armor"]} breaks the armor cap");
        }
        foreach (var c in NewCards.Where(c => c.ObjectType == "spy-unit"))
            Assert.Contains("spy", c.Tags); // what the Fixer's discount keys on
    }

    // ------------------------------------------------------------------ role distinctness

    private static readonly HashSet<string> MechanicalTags = new()
    {
        "ranged", "cleave", "splash", "guard", "retaliate", "blocking", "fortification", "builder", "spy",
    };

    /// <summary>
    /// A structural fingerprint of what a card DOES (object type, engine-relevant tags, equipment slots and
    /// granted tags, timing, and per ability/trigger/onPlay: costs, choice filter, conditions and effect
    /// kind + scope + resource/property/card/tag/line) - never stats or names. Two cards with the same
    /// fingerprint would be the "reskinned clone under a new name" the task forbids. Banking a resource is
    /// one role whatever the scope is spelled (gain_bank_resource vs gain_resource scope player/self), and a
    /// spell's "abilities" block is read as the onPlay the author meant (the pre-batch spells wrote it that
    /// way, so they would otherwise look like a different role from a working copy of themselves).
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
        static string Ab(string label, AbilityDefinition a, bool withCosts) =>
            $"{label}[{(withCosts ? string.Join("+", a.Costs.Select(x => x.Type == "resource" ? "resource:" + x.ResourceId : x.Type).OrderBy(x => x, StringComparer.Ordinal)) : "")}]" +
            $"[{(a.Choice == null ? "" : $"{a.Choice.Controller}/{a.Choice.ObjectType}/{a.Choice.Tag}/{a.Choice.MaxPropertyId}")}]" +
            $"[{string.Join("+", a.Conditions.Select(x => x.Type).OrderBy(x => x, StringComparer.Ordinal))}]" +
            $"[{Effects(a.Effects)}]";

        bool spell = c.ObjectType == "spell";
        var parts = new List<string> { c.ObjectType };
        parts.AddRange(c.Tags.Where(MechanicalTags.Contains).OrderBy(x => x, StringComparer.Ordinal).Select(t => "tag:" + t));
        parts.AddRange(c.AttachTags.OrderBy(x => x, StringComparer.Ordinal).Select(t => "attachTag:" + t));
        parts.AddRange(c.AttachModifiers.Select(m => "attachMod:" + m.PropertyId).OrderBy(x => x, StringComparer.Ordinal));
        if (c.HousingProvided > 0) parts.Add("housing");
        if (c.Slot != null) parts.Add("slot:" + c.Slot);
        if (c.Slots is { Count: > 0 }) parts.Add("slots:" + string.Join("+", c.Slots.OrderBy(x => x, StringComparer.Ordinal)));
        if (c.BonusAttackVsBuildings != null) parts.Add("bonusVsBuildings");
        if (c.IsSecret) parts.Add("secret");
        if (c.Timing != "main") parts.Add("timing:" + c.Timing + ":" + string.Join("+", c.ReactionTo.OrderBy(x => x, StringComparer.Ordinal)));
        foreach (var a in c.Abilities) parts.Add(Ab(spell ? "onPlay" : "ability", a, withCosts: !spell));
        if (c.OnPlay != null) parts.Add(Ab("onPlay", c.OnPlay, withCosts: !spell));
        foreach (var t in c.Triggers) parts.Add($"trigger:{t.Event}[{Effects(t.Effects)}]");
        return string.Join("|", parts.OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void New_cards_are_role_distinct_from_each_other_and_from_the_42_pre_batch_shadow_cards()
    {
        var existing = Definition.Cards
            .Where(c => c.Faction == "shadow" && !NewCardIds.Contains(c.Id))
            .ToDictionary(c => c.Id, Fingerprint);
        Assert.Equal(42, existing.Count);

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
        var clone = JsonSerializer.Deserialize<CardDefinition>(JsonSerializer.Serialize(Card("shadow-listening-post")))!;
        clone.Id = "shadow-listening-post-reskin";
        clone.Name = "Eavesdropping Chimney";
        clone.PlayCost = 9;
        Assert.Equal(Fingerprint(Card("shadow-listening-post")), Fingerprint(clone));
        Assert.NotEqual(Fingerprint(Card("shadow-listening-post")), Fingerprint(Card("shadow-counting-room")));

        // and a pre-batch dead ability-spell is read as the onPlay it meant to be
        var deadSpell = Card("shadow-lockdown"); // freeze target, written under abilities[]
        Assert.Contains("onPlay", Fingerprint(deadSpell));
        Assert.DoesNotContain("ability", Fingerprint(deadSpell));
    }

    // ------------------------------------------------------------------ economy premise

    [Fact]
    public void Opening_gold_and_intel_pay_for_a_two_drop_on_turn_one_and_the_exchange_provides_the_housing()
    {
        // No resource cheating here: the real starting economy of the new deck.
        var (game, engine, p1, _) = CreateMatch(richResources: false);
        var exchange = Hq(game, p1);
        Assert.Equal(HqId, exchange.DefinitionId);
        Assert.Equal(0, Gold(p1));
        Assert.Equal(6, GameQueries.HousingCapacity(game, p1.Id));
        Assert.Equal(1, OwnBattlefield(game, p1, "footpad")); // the Exchange sends one out at setup

        Use(game, engine, p1, exchange, "exchange-skim");
        Assert.Equal(2, Gold(p1));
        Assert.Equal(1, Intel(game, p1));

        // 2 gold + 1 intel afford the two-drop but not a 3-gold, 1-intel spy
        var pickpocket = InHand(game, "shadow-guild-pickpocket", p1);
        var cracker = InHand(game, "shadow-vault-cracker", p1);
        var available = engine.GetAvailableActions(game, p1.Id).Where(a => a.Type == "playCard" && a.Available).Select(a => a.SourceObjectId).ToList();
        Assert.Contains(pickpocket.Id, available);
        Assert.DoesNotContain(cracker.Id, available);

        Play(game, engine, p1, pickpocket);
        Assert.Equal("battlefield", pickpocket.ZoneId);
        Assert.Equal(0, Gold(p1));
        Assert.Equal(1, GameQueries.HousingUsed(game, p1.Id));
    }

    // ------------------------------------------------------------------ every card is castable

    // What a cast changes beyond paying its price (gold, intel).
    private static readonly Dictionary<string, (int gold, int intel)> CastGains = new()
    {
        ["shadow-cook-the-books"] = (3, 1),
        ["shadow-smash-and-grab"] = (3, 0), // drains 3 gold from the opponent's 99
    };

    [Theory]
    [MemberData(nameof(MainPhasePlayableCardIds))]
    public void Every_new_card_can_be_played_from_hand_and_pays_exactly_its_price(string cardId)
    {
        var (game, engine, p1, p2) = CreateMatch();
        SetGold(p1, 20);
        SetIntel(game, p1, 7); // one below the Exchange's cap of 8, so Cook the Books' +1 is not clipped
        var host = Body(game, p1, 2, 3);
        Body(game, p2, 1, 5);
        House(game, p2, 6);
        var def = Card(cardId);
        var card = InHand(game, cardId, p1);
        ObjectInstance[] targets = Array.Empty<ObjectInstance>();
        if (def.AttachTo == "chooseCharacter" && def.Slots is { Count: > 0 })
            targets = new[] { host };
        else if (def.OnPlay?.Choice is { } choice)
        {
            targets = new TargetingService().GetValidTargets(game, choice, p1.Id, card.Id)
                .Take(1).Select(id => game.Objects.First(o => o.Id == id)).ToArray();
            Assert.NotEmpty(targets);
        }

        Play(game, engine, p1, card, targets);

        Assert.NotEqual("hand", card.ZoneId);
        var costs = GameQueries.BasePlayCosts(def);
        var (gainGold, gainIntel) = CastGains.GetValueOrDefault(cardId);
        Assert.Equal(20 - costs.GetValueOrDefault("gold") + gainGold, Gold(p1));
        Assert.Equal(7 - costs.GetValueOrDefault("intel") + gainIntel, Intel(game, p1));
    }

    [Fact]
    public void A_reaction_spell_is_refused_in_the_main_phase()
    {
        var (game, engine, p1, _) = CreateMatch();
        var (ok, error) = TryPlay(game, engine, p1, InHand(game, "shadow-sleight-of-hand", p1));
        Assert.False(ok);
        Assert.Contains("Reaction", error!);
    }

    // ------------------------------------------------------------------ headquarters + hero

    [Fact]
    public void Skim_the_till_banks_2_gold_and_1_intel_and_intel_is_capped_at_the_exchanges_8()
    {
        var (game, engine, p1, _) = CreateMatch();
        var exchange = Hq(game, p1);
        SetGold(p1, 0);
        SetIntel(game, p1, 5);

        Use(game, engine, p1, exchange, "exchange-skim");

        Assert.Equal(2, Gold(p1));
        Assert.Equal(6, Intel(game, p1));
        Assert.True(exchange.IsTapped);
        var (twice, _) = TryUse(game, engine, p1, exchange, "exchange-skim");
        Assert.False(twice); // one tap a turn

        exchange.IsTapped = false;
        SetIntel(game, p1, 8);
        Use(game, engine, p1, exchange, "exchange-skim");
        Assert.Equal(8, Intel(game, p1));
        Assert.Equal(4, Gold(p1));
    }

    [Fact]
    public void Spin_the_web_banks_2_intel_and_draws_a_card_for_2_ap_and_a_tap()
    {
        var (game, engine, p1, _) = CreateMatch();
        var vesper = Hero(game, p1);
        Assert.Equal(HeroId, vesper.DefinitionId);
        SetIntel(game, p1, 1);
        vesper.Resources["ap"] = 1;
        var (tooEarly, _) = TryUse(game, engine, p1, vesper, "vesper-spin-web");
        Assert.False(tooEarly); // 2 AP needed

        vesper.Resources["ap"] = 2;
        var handBefore = HandCount(game, p1);
        Use(game, engine, p1, vesper, "vesper-spin-web");

        Assert.Equal(3, Intel(game, p1));
        Assert.Equal(handBefore + 1, HandCount(game, p1));
        Assert.Equal(0, vesper.Resources["ap"]);
        Assert.True(vesper.IsTapped);
    }

    [Fact]
    public void Silent_kill_destroys_an_enemy_unit_of_3_hp_or_less_for_3_ap_and_nothing_sturdier()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var vesper = Hero(game, p1);
        vesper.Resources["ap"] = 3;
        var frail = Body(game, p2, 2, 3);
        var sturdy = Body(game, p2, 2, 4);
        var own = Body(game, p1, 1, 1);

        foreach (var wrong in new[] { sturdy, own })
        {
            var (ok, _) = TryUse(game, engine, p1, vesper, "vesper-silent-kill", wrong);
            Assert.False(ok);
        }
        Assert.Equal(3, vesper.Resources["ap"]); // a refused target costs nothing

        Use(game, engine, p1, vesper, "vesper-silent-kill", frail);

        Assert.True(frail.IsDestroyed);
        Assert.False(sturdy.IsDestroyed);
        Assert.Equal(0, vesper.Resources["ap"]);

        // wounds count: the sturdy one becomes a valid mark once it is down to 3 HP
        vesper.Resources["ap"] = 3;
        sturdy.Properties["currentHp"] = 3;
        Use(game, engine, p1, vesper, "vesper-silent-kill", sturdy);
        Assert.True(sturdy.IsDestroyed);
    }

    // ------------------------------------------------------------------ spy-units

    [Fact]
    public void Vault_cracker_steals_2_gold_from_inside_once_its_intel_reaches_2()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var outside = OnField(game, "shadow-vault-cracker", p1);
        outside.Resources["intel"] = 4;
        var (notInside, _) = TryUse(game, engine, p1, outside, "vault-cracker-crack");
        Assert.False(notInside); // Crack the Safe only works from inside a building

        var (spy, host) = Infiltrated(game, engine, p1, p2, "shadow-vault-cracker", "vault-cracker");
        Assert.Equal(1, SelfIntel(spy)); // enters with 1 Intel
        SetGold(p1, 0);
        SetGold(p2, 5);
        var (tooEarly, _) = TryUse(game, engine, p1, spy, "vault-cracker-crack");
        Assert.False(tooEarly);

        StartTurnOf(game, engine, p1);
        Assert.Equal(2, SelfIntel(spy)); // +1 Intel at the start of each of its controller's turns
        Use(game, engine, p1, spy, "vault-cracker-crack");

        Assert.Equal(2, Gold(p1));
        Assert.Equal(3, Gold(p2));
        Assert.Equal(0, SelfIntel(spy));
        Assert.Equal(host.Id, spy.AttachedToId);
    }

    [Fact]
    public void A_spys_intel_stops_growing_at_its_cap_of_4()
    {
        var (game, engine, p1, _) = CreateMatch();
        var spy = OnField(game, "shadow-leak-artist", p1);
        spy.Resources["intel"] = 4;

        StartTurnOf(game, engine, p1);

        Assert.Equal(4, SelfIntel(spy));
    }

    [Fact]
    public void Sapper_chips_1_hp_a_turn_off_the_building_it_hides_in_and_dies_with_it()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var outside = OnField(game, "shadow-sapper", p1);
        var (notInside, _) = TryUse(game, engine, p1, outside, "sapper-undermine");
        Assert.False(notInside);

        var (sapper, host) = Infiltrated(game, engine, p1, p2, "shadow-sapper", "sapper", hostHp: 4);
        Use(game, engine, p1, sapper, "sapper-undermine");
        Assert.Equal(3, Hp(host));
        var (twice, _) = TryUse(game, engine, p1, sapper, "sapper-undermine");
        Assert.False(twice); // once a turn

        StartTurnOf(game, engine, p1);
        Use(game, engine, p1, sapper, "sapper-undermine");
        Assert.Equal(2, Hp(host));

        host.Properties["currentHp"] = 1;
        StartTurnOf(game, engine, p1);
        Use(game, engine, p1, sapper, "sapper-undermine");
        Assert.True(host.IsDestroyed);
        Assert.True(sapper.IsDestroyed); // modules go down with their host
    }

    [Fact]
    public void Leak_artist_draws_2_cards_for_3_intel_from_inside()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var (spy, _) = Infiltrated(game, engine, p1, p2, "shadow-leak-artist", "leak-artist");
        spy.Resources["intel"] = 2;
        var (tooEarly, _) = TryUse(game, engine, p1, spy, "leak-artist-leak");
        Assert.False(tooEarly);

        spy.Resources["intel"] = 3;
        var handBefore = HandCount(game, p1);
        Use(game, engine, p1, spy, "leak-artist-leak");

        Assert.Equal(handBefore + 2, HandCount(game, p1));
        Assert.Equal(0, SelfIntel(spy));
    }

    [Fact]
    public void Blight_agent_poisons_every_enemy_unit_for_2_intel_from_inside_and_spares_its_own_side()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var (spy, _) = Infiltrated(game, engine, p1, p2, "shadow-blight-agent", "blight-agent");
        var enemyA = Body(game, p2, 1, 5);
        var enemyB = Body(game, p2, 1, 5, line: "back");
        var own = Body(game, p1, 1, 5);
        spy.Resources["intel"] = 2;

        Use(game, engine, p1, spy, "blight-agent-taint");

        Assert.Equal(1, Poison(enemyA));
        Assert.Equal(1, Poison(enemyB));
        Assert.Equal(0, Poison(own));
        Assert.Equal(0, SelfIntel(spy));
    }

    // ------------------------------------------------------------------ units

    [Fact]
    public void Guild_pickpocket_lifts_1_gold_for_a_tap_and_a_dry_purse_yields_nothing()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var thief = OnField(game, "shadow-guild-pickpocket", p1);
        SetGold(p1, 0);
        SetGold(p2, 5);

        Use(game, engine, p1, thief, "pickpocket-lift");

        Assert.Equal(1, Gold(p1));
        Assert.Equal(4, Gold(p2));
        Assert.True(thief.IsTapped);

        thief.IsTapped = false;
        SetGold(p2, 0);
        Use(game, engine, p1, thief, "pickpocket-lift");
        Assert.Equal(1, Gold(p1)); // nothing to take
    }

    [Fact]
    public void Bounty_hunter_pockets_2_gold_for_a_kill_and_nothing_for_a_scratch()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var hunter = OnField(game, "shadow-bounty-hunter", p1);
        var tough = Body(game, p2, 1, 10);
        var frail = Body(game, p2, 1, 1);
        SetGold(p1, 0);

        Attack(game, engine, hunter, tough);
        Assert.Equal(0, Gold(p1));
        Assert.False(tough.IsDestroyed);

        hunter.IsTapped = false;
        Attack(game, engine, hunter, frail);
        Assert.True(frail.IsDestroyed);
        Assert.Equal(2, Gold(p1));
    }

    [Fact]
    public void Rooftop_sniper_hits_only_the_enemy_hero_for_2_armor_ignoring_damage()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var sniper = OnField(game, "shadow-rooftop-sniper", p1, line: "back");
        Assert.Contains("ranged", sniper.Tags);
        var enemyHero = Hero(game, p2);
        var unit = Body(game, p2, 1, 5);
        var hpBefore = Hp(enemyHero);
        Assert.True(Armor(game, enemyHero) >= 1); // town-chief: the shot must ignore armor

        var (wrong, _) = TryUse(game, engine, p1, sniper, "sniper-take-the-shot", unit);
        Assert.False(wrong); // heroes only
        Assert.False(sniper.IsTapped);

        Use(game, engine, p1, sniper, "sniper-take-the-shot", enemyHero);

        Assert.Equal(hpBefore - 2, Hp(enemyHero));
        Assert.True(sniper.IsTapped);
    }

    [Fact]
    public void Blackmailer_makes_the_opponent_discard_a_card_when_it_enters()
    {
        var (game, engine, p1, p2) = CreateMatch();
        InHand(game, "town-watch", p2);
        InHand(game, "town-watch", p2);
        var discardBefore = game.Objects.Count(o => o.OwnerId == p2.Id && o.ZoneId == "discard");

        Play(game, engine, p1, InHand(game, "shadow-blackmailer", p1));

        Assert.Equal(1, HandCount(game, p2));
        Assert.Equal(discardBefore + 1, game.Objects.Count(o => o.OwnerId == p2.Id && o.ZoneId == "discard"));
    }

    [Fact]
    public void Trapwire_bodyguard_shocks_whoever_lands_a_hit_on_it_for_2_armor_ignoring_damage()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var guard = OnField(game, "shadow-trapwire-bodyguard", p1);
        Assert.Contains("guard", guard.Tags);
        var glancing = Body(game, p2, 1, 5); // 1 attack vs 1 armor: no damage dealt, so no trigger
        var brute = Body(game, p2, 3, 5);

        Attack(game, engine, glancing, guard);
        Assert.Equal(5, Hp(guard));
        Assert.Equal(5, Hp(glancing));

        Attack(game, engine, brute, guard);
        Assert.Equal(3, Hp(guard)); // 3 attack - 1 armor
        Assert.Equal(3, Hp(brute)); // shocked for 2 by the trapwire
    }

    [Fact]
    public void Safecracker_hits_buildings_2_harder_and_pockets_3_gold_for_razing_one()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var cracker = OnField(game, "shadow-safecracker", p1);
        var solid = House(game, p2, 6);
        var flimsy = House(game, p2, 5);
        SetGold(p1, 0);

        Attack(game, engine, cracker, solid);
        Assert.Equal(1, Hp(solid)); // 3 attack + 2 against buildings = 5
        Assert.Equal(0, Gold(p1));

        cracker.IsTapped = false;
        Attack(game, engine, cracker, flimsy);
        Assert.True(flimsy.IsDestroyed);
        Assert.Equal(3, Gold(p1));
    }

    [Fact]
    public void The_fixer_discounts_the_next_spy_by_2_gold_and_ignores_everyone_else()
    {
        var (game, engine, p1, _) = CreateMatch();
        var fixer = OnField(game, "shadow-fixer", p1);
        SetGold(p1, 20);
        SetIntel(game, p1, 8);

        Use(game, engine, p1, fixer, "fixer-forged-papers");
        Assert.Single(game.ActiveCostModifiers);

        Play(game, engine, p1, InHand(game, "shadow-alley-thug", p1)); // not a spy: full price, discount waits
        Assert.Equal(17, Gold(p1));
        Assert.Single(game.ActiveCostModifiers);

        Play(game, engine, p1, InHand(game, "shadow-vault-cracker", p1)); // 3 gold - 2
        Assert.Equal(16, Gold(p1));
        Assert.Equal(7, Intel(game, p1)); // the intel price is untouched
        Assert.Empty(game.ActiveCostModifiers);

        Play(game, engine, p1, InHand(game, "shadow-sapper", p1)); // discount spent: full 3
        Assert.Equal(13, Gold(p1));
    }

    // ------------------------------------------------------------------ equipment

    [Fact]
    public void Poisoned_stiletto_adds_1_attack_and_poisons_whatever_the_bearer_wounds()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var bearer = Body(game, p1, 2, 5);
        var stiletto = InHand(game, "shadow-poisoned-stiletto", p1);

        Play(game, engine, p1, stiletto, bearer);

        Assert.Equal(bearer.Id, stiletto.AttachedToId);
        Assert.Equal(3, Attack(game, bearer));
        var armored = Body(game, p2, 1, 6, armor: 5);
        var victim = Body(game, p2, 1, 6);

        Attack(game, engine, bearer, armored); // 3 attack vs 5 armor: no damage, so no poison
        Assert.Equal(0, Poison(armored));

        bearer.IsTapped = false;
        Attack(game, engine, bearer, victim);
        Assert.Equal(3, Hp(victim));
        Assert.Equal(1, Poison(victim));
    }

    [Fact]
    public void Garrote_wire_is_two_handed_adds_2_attack_and_draws_a_card_on_a_kill()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var bearer = Body(game, p1, 2, 5);
        var stiletto = InHand(game, "shadow-poisoned-stiletto", p1);
        var wire = InHand(game, "shadow-garrote-wire", p1);
        Play(game, engine, p1, stiletto, bearer);

        Play(game, engine, p1, wire, bearer);

        Assert.Equal(bearer.Id, wire.AttachedToId);
        Assert.Null(stiletto.AttachedToId); // the main-hand slot was needed
        Assert.Equal("discard", stiletto.ZoneId);
        Assert.Equal(4, Attack(game, bearer));

        var frail = Body(game, p2, 1, 1);
        var handBefore = HandCount(game, p1);
        Attack(game, engine, bearer, frail);
        Assert.True(frail.IsDestroyed);
        Assert.Equal(handBefore + 1, HandCount(game, p1));
    }

    [Fact]
    public void Watchmans_cowl_turns_its_bearer_into_a_guard_that_shields_the_hero_and_hq()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var bearer = Body(game, p1, 1, 5);
        var raider = Body(game, p2, 2, 5);
        raider.Tags.Add("ranged"); // reaches both lines, so the guard rule is what decides
        var hero = Hero(game, p1);
        var targeting = new TargetingService();
        Assert.Contains(hero.Id, targeting.GetAttackTargets(game, raider));

        Play(game, engine, p1, InHand(game, "shadow-watchmans-cowl", p1), bearer);

        Assert.Contains("guard", bearer.Tags);
        Assert.Equal(1, Armor(game, bearer));
        var reachable = targeting.GetAttackTargets(game, raider);
        Assert.Contains(bearer.Id, reachable);
        Assert.DoesNotContain(hero.Id, reachable);
        Assert.DoesNotContain(Hq(game, p1).Id, reachable);
    }

    // ------------------------------------------------------------------ buildings

    [Fact]
    public void Tenement_flophouse_adds_4_housing()
    {
        var (game, engine, p1, _) = CreateMatch();
        Assert.Equal(6, GameQueries.HousingCapacity(game, p1.Id)); // the Exchange alone

        Play(game, engine, p1, InHand(game, "shadow-tenement-flophouse", p1));

        Assert.Equal(10, GameQueries.HousingCapacity(game, p1.Id));
    }

    [Fact]
    public void Counting_room_pays_1_gold_at_the_start_of_every_turn()
    {
        var (game, engine, p1, _) = CreateMatch();
        OnField(game, "shadow-counting-room", p1);
        SetGold(p1, 0);

        StartTurnOf(game, engine, p1);

        Assert.Equal(1, Gold(p1));
    }

    [Fact]
    public void Listening_post_banks_1_intel_each_turn_up_to_the_exchanges_cap()
    {
        var (game, engine, p1, _) = CreateMatch();
        OnField(game, "shadow-listening-post", p1);
        SetIntel(game, p1, 3);

        StartTurnOf(game, engine, p1);
        Assert.Equal(4, Intel(game, p1));

        SetIntel(game, p1, 8);
        StartTurnOf(game, engine, p1);
        Assert.Equal(8, Intel(game, p1));
    }

    [Fact]
    public void Thieves_den_recruits_a_footpad_for_2_gold_and_a_tap()
    {
        var (game, engine, p1, _) = CreateMatch();
        var den = OnField(game, "shadow-thieves-den", p1);
        SetGold(p1, 1);
        var (broke, _) = TryUse(game, engine, p1, den, "thieves-den-recruit");
        Assert.False(broke);

        SetGold(p1, 5);
        Use(game, engine, p1, den, "thieves-den-recruit");

        Assert.Equal(3, Gold(p1));
        Assert.Equal(1, OwnBattlefield(game, p1, "footpad"));
        Assert.True(den.IsTapped);
    }

    [Fact]
    public void Barred_warehouse_protects_every_other_building_while_it_stands_untapped()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var warehouse = OnField(game, "shadow-barred-warehouse", p1);
        Assert.Contains("fortification", warehouse.Tags);
        var house = House(game, p1, 4);
        var attacker = Body(game, p2, 2, 3);
        attacker.Tags.Add("ranged"); // reaches both lines, so the fortification rule is what decides
        var targeting = new TargetingService();

        var reachable = targeting.GetAttackTargets(game, attacker);
        Assert.Contains(warehouse.Id, reachable);
        Assert.DoesNotContain(house.Id, reachable);
        Assert.DoesNotContain(Hq(game, p1).Id, reachable);

        warehouse.IsTapped = true; // a tapped fortification stops protecting
        Assert.Contains(house.Id, targeting.GetAttackTargets(game, attacker));
    }

    [Fact]
    public void Back_alley_clinic_heals_every_own_unit_2_for_1_gold_and_a_tap()
    {
        var (game, engine, p1, _) = CreateMatch();
        var clinic = OnField(game, "shadow-back-alley-clinic", p1);
        var a = Body(game, p1, 1, 5);
        var b = Body(game, p1, 1, 5);
        a.Properties["currentHp"] = 2;
        b.Properties["currentHp"] = 4;
        SetGold(p1, 5);

        Use(game, engine, p1, clinic, "clinic-patch-up");

        Assert.Equal(4, Hp(a));
        Assert.Equal(5, Hp(b)); // capped at max HP
        Assert.Equal(4, Gold(p1));
        Assert.True(clinic.IsTapped);
    }

    [Fact]
    public void Protection_racket_pays_2_gold_the_first_time_each_turn_a_friendly_hits_the_enemy_hero()
    {
        var (game, engine, p1, p2) = CreateMatch();
        OnField(game, "shadow-protection-racket", p1);
        var first = Body(game, p1, 4, 3);
        var second = Body(game, p1, 4, 3);
        var enemyHero = Hero(game, p2);
        SetGold(p1, 0);

        Attack(game, engine, first, enemyHero);
        Assert.Equal(2, Gold(p1));

        Attack(game, engine, second, enemyHero);
        Assert.Equal(2, Gold(p1)); // once per turn
        Assert.Equal(1, Hp(enemyHero)); // 5 - 2 - 2

        StartTurnOf(game, engine, p1); // the counter resets with the turn
        enemyHero.Properties["currentHp"] = 5;
        var enemyUnit = Body(game, p2, 1, 9, line: "back"); // both in reach: the front line stays empty
        Attack(game, engine, second, enemyUnit);
        Assert.Equal(2, Gold(p1)); // hitting a unit never pays
        Attack(game, engine, first, enemyHero);
        Assert.Equal(4, Gold(p1));
    }

    [Fact]
    public void Carrion_broker_pays_1_gold_for_the_first_unit_that_dies_each_turn_whoever_it_belonged_to()
    {
        var (game, engine, p1, p2) = CreateMatch();
        OnField(game, "shadow-carrion-broker", p1);
        var a = Body(game, p1, 3, 3);
        var b = Body(game, p1, 3, 3);
        var victimOne = Body(game, p2, 1, 1);
        var victimTwo = Body(game, p2, 1, 1);
        SetGold(p1, 0);

        Attack(game, engine, a, victimOne);
        Assert.True(victimOne.IsDestroyed);
        Assert.Equal(1, Gold(p1));

        Attack(game, engine, b, victimTwo);
        Assert.True(victimTwo.IsDestroyed);
        Assert.Equal(1, Gold(p1)); // once per turn
    }

    [Fact]
    public void Guild_archive_draws_a_card_for_2_gold_and_a_tap()
    {
        var (game, engine, p1, _) = CreateMatch();
        var archive = OnField(game, "shadow-guild-archive", p1);
        SetGold(p1, 5);
        var handBefore = HandCount(game, p1);

        Use(game, engine, p1, archive, "archive-pull-file");

        Assert.Equal(3, Gold(p1));
        Assert.Equal(handBefore + 1, HandCount(game, p1));
        Assert.True(archive.IsTapped);
    }

    // ------------------------------------------------------------------ spells

    [Fact]
    public void Whisper_campaign_permanently_shaves_1_attack_off_every_enemy_unit_and_only_them()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var a = Body(game, p2, 3, 3);
        var b = Body(game, p2, 1, 3, line: "back");
        var own = Body(game, p1, 3, 3);
        var enemyHero = Hero(game, p2);
        var heroAttack = Attack(game, enemyHero);

        Play(game, engine, p1, InHand(game, "shadow-whisper-campaign", p1));

        Assert.Equal(2, Attack(game, a));
        Assert.Equal(0, Attack(game, b)); // 1 -> 0
        Assert.Equal(3, Attack(game, own));
        Assert.Equal(heroAttack, Attack(game, enemyHero));
    }

    [Fact]
    public void Cutthroat_contract_deals_5_armor_ignoring_damage_to_one_enemy_unit()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var armored = Body(game, p2, 2, 8, armor: 3);
        var own = Body(game, p1, 2, 8);
        var contract = InHand(game, "shadow-cutthroat-contract", p1);
        SetGold(p1, 10);
        SetIntel(game, p1, 4);

        var (wrong, _) = TryPlay(game, engine, p1, contract, own);
        Assert.False(wrong);
        Assert.Equal(10, Gold(p1)); // a refused target costs nothing
        Assert.Equal(4, Intel(game, p1));

        Play(game, engine, p1, contract, armored);

        Assert.Equal(3, Hp(armored));
        Assert.Equal(7, Gold(p1));
        Assert.Equal(3, Intel(game, p1));
    }

    [Fact]
    public void Insider_deal_draws_a_card_and_makes_the_next_card_cost_2_less_gold()
    {
        var (game, engine, p1, _) = CreateMatch();
        SetGold(p1, 20);
        var deal = InHand(game, "shadow-insider-deal", p1);
        var thug = InHand(game, "shadow-alley-thug", p1);
        var secondThug = InHand(game, "shadow-alley-thug", p1);

        Play(game, engine, p1, deal);
        Assert.Equal(19, Gold(p1));
        Assert.Equal(3, HandCount(game, p1)); // two thugs + the drawn card

        Play(game, engine, p1, thug);
        Assert.Equal(18, Gold(p1)); // 3 - 2 = 1
        Play(game, engine, p1, secondThug);
        Assert.Equal(15, Gold(p1)); // the discount was one-shot
    }

    [Fact]
    public void Smash_and_grab_loots_3_gold_and_a_raid_on_an_empty_till_still_nets_the_scrap()
    {
        var (game, engine, p1, p2) = CreateMatch();
        SetGold(p1, 5);
        SetGold(p2, 5);

        Play(game, engine, p1, InHand(game, "shadow-smash-and-grab", p1));

        Assert.Equal(6, Gold(p1)); // 5 - 2 + 3
        Assert.Equal(2, Gold(p2));

        SetGold(p1, 5);
        SetGold(p2, 0);
        p2.Resources["training"] = 0; // the raid also skims training points, so empty that pool too
        Play(game, engine, p1, InHand(game, "shadow-smash-and-grab", p1));
        Assert.Equal(4, Gold(p1)); // 5 - 2 + 1 scrap
        Assert.Equal(0, Gold(p2));
    }

    [Fact]
    public void Hamstring_permanently_cuts_an_enemy_units_attack_by_2_and_taps_it()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var target = Body(game, p2, 3, 4);
        var own = Body(game, p1, 3, 4);
        var spell = InHand(game, "shadow-hamstring", p1);

        var (wrong, _) = TryPlay(game, engine, p1, spell, own);
        Assert.False(wrong);

        Play(game, engine, p1, spell, target);

        Assert.Equal(1, Attack(game, target));
        Assert.True(target.IsTapped);
        Assert.Equal(3, Attack(game, own));
    }

    [Fact]
    public void Cook_the_books_nets_2_gold_and_1_intel_for_1_gold()
    {
        var (game, engine, p1, _) = CreateMatch();
        SetGold(p1, 1);
        SetIntel(game, p1, 2);

        Play(game, engine, p1, InHand(game, "shadow-cook-the-books", p1));

        Assert.Equal(3, Gold(p1));
        Assert.Equal(3, Intel(game, p1));
    }

    [Fact]
    public void Night_raid_gives_every_own_unit_2_attack_until_the_end_of_the_turn()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var a = Body(game, p1, 2, 3);
        var b = Body(game, p1, 1, 3, line: "back");
        var enemy = Body(game, p2, 2, 3);

        Play(game, engine, p1, InHand(game, "shadow-night-raid", p1));

        Assert.Equal(4, Attack(game, a));
        Assert.Equal(3, Attack(game, b));
        Assert.Equal(2, Attack(game, enemy));

        EndPhaseOf(game, engine, p1);

        Assert.Equal(2, Attack(game, a));
        Assert.Equal(1, Attack(game, b));
    }

    [Fact]
    public void Call_in_favours_summons_two_footpads_that_cannot_act_this_turn()
    {
        var (game, engine, p1, _) = CreateMatch();

        Play(game, engine, p1, InHand(game, "shadow-call-in-favours", p1));

        Assert.Equal(2, OwnBattlefield(game, p1, "footpad"));
        Assert.All(game.Objects.Where(o => o.OwnerId == p1.Id && o.DefinitionId == "footpad" && o.ZoneId == "battlefield"),
            o => Assert.True(o.HasSummoningSickness));
    }

    // ------------------------------------------------------------------ secrets + reaction

    [Fact]
    public void Trapdoor_waits_face_down_then_freezes_whoever_attacks_the_hero()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var trap = InHand(game, "shadow-trapdoor", p1);
        Play(game, engine, p1, trap);
        Assert.Equal("secrets", trap.ZoneId);
        Assert.True(trap.FaceDown);

        var attacker = Body(game, p2, 2, 5);
        Attack(game, engine, attacker, Hero(game, p1));

        Assert.True(attacker.IsTapped);
        Assert.True(attacker.SkipNextUntap); // it will not untap next turn
        Assert.Equal("discard", trap.ZoneId);
        Assert.False(trap.FaceDown);
    }

    [Fact]
    public void A_secret_stays_hidden_when_something_other_than_the_hero_is_attacked()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var trap = InHand(game, "shadow-trapdoor", p1);
        Play(game, engine, p1, trap);
        var guard = Body(game, p1, 1, 5);
        var attacker = Body(game, p2, 2, 5);

        Attack(game, engine, attacker, guard);

        Assert.Equal("secrets", trap.ZoneId);
        Assert.False(attacker.SkipNextUntap);
    }

    [Fact]
    public void Venomed_needle_leaves_the_attacker_with_3_poison()
    {
        var (game, engine, p1, p2) = CreateMatch();
        Play(game, engine, p1, InHand(game, "shadow-venomed-needle", p1));
        var attacker = Body(game, p2, 2, 8);

        Attack(game, engine, attacker, Hero(game, p1));

        Assert.Equal(3, Poison(attacker));
    }

    [Fact]
    public void Powder_keg_blasts_every_enemy_unit_for_3_when_the_hero_is_attacked()
    {
        var (game, engine, p1, p2) = CreateMatch();
        Play(game, engine, p1, InHand(game, "shadow-powder-keg", p1));
        var attacker = Body(game, p2, 2, 5);
        var bystander = Body(game, p2, 1, 5, line: "back");
        var own = Body(game, p1, 1, 5, line: "back"); // an empty front line keeps the hero within melee reach

        Attack(game, engine, attacker, Hero(game, p1));

        Assert.Equal(2, Hp(attacker));
        Assert.Equal(2, Hp(bystander));
        Assert.Equal(5, Hp(own));
    }

    [Fact]
    public void Sleight_of_hand_answers_an_attack_by_cutting_the_attackers_damage_by_3()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var sleight = InHand(game, "shadow-sleight-of-hand", p1);
        var defender = Body(game, p1, 1, 8);
        var attacker = Body(game, p2, 4, 5);
        SetGold(p1, 5);

        Attack(game, engine, attacker, defender);
        Assert.Equal(GameState.WaitingForReaction, game.State);
        Assert.Equal(p1.Id, game.ReactionPlayerId);
        Assert.Equal(8, Hp(defender)); // nothing has landed yet

        Play(game, engine, p1, sleight, attacker);
        Assert.Equal(4, Gold(p1));
        Assert.Equal(1, Attack(game, attacker));

        var (passed, error) = engine.ExecuteAction(game, p1.Id, new ActionRequest { Type = "pass" });
        Assert.True(passed, error);

        Assert.Equal(7, Hp(defender)); // 4 - 3 = 1 instead of 4
        Assert.Equal(GameState.WaitingForAction, game.State);
    }
}
