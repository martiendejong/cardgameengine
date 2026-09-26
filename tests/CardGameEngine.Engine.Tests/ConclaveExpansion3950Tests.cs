using System.Reflection;
using System.Text.Json;
using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;
using Xunit;

namespace CardGameEngine.Engine.Tests;

/// <summary>
/// Task 3950: batch 1 of the Conclave faction expansion toward the 200-card baseline
/// (55 -> 87 faction-exclusive cards; equipment 3 -> 13, buildings 8 -> 15, plus a new
/// headquarters, hero, units and spells), wired into a new "conclave-runecraft" precon deck.
///
/// PR #52's batch shipped effects/keys the engine silently ignored, so this suite is stricter
/// than the 1604 vocabulary check: it (1) lints every new card's RAW json against the engine's
/// real schema types (System.Text.Json drops unknown keys without an error), (2) proves every
/// card is reachable from a precon deck and legally castable with the deck's own HQ, and
/// (3) drives each card's actual mechanic through the real RuleEngine (no bot AI, no mocks) and
/// asserts the state change, so a card whose ability quietly does nothing fails here.
/// </summary>
public class ConclaveExpansion3950Tests
{
    private const string HqId = "rune-forge-sanctum";
    private const string HeroId = "rune-runemaster";
    private const string DeckId = "conclave-runecraft";

    private static readonly string[] EquipmentIds =
    {
        "rune-warding-circlet", "rune-thorned-vestments", "rune-siphon-blade", "rune-farsight-lance",
        "rune-aegis-tome", "rune-soulbinder-sigil", "rune-chronal-band", "rune-frostbrand",
        "rune-sweeping-staff", "rune-scholars-monocle",
    };

    private static readonly string[] BuildingIds =
    {
        "rune-leyline-siphon", "rune-warded-bulwark", "rune-obelisk-of-silence", "rune-runic-workshop",
        "rune-eldritch-orrery", "rune-banner-hall", "rune-scribe-archive",
    };

    private static readonly string[] UnitIds =
    {
        "rune-hexblade-duelist", "rune-frost-effigy", "rune-tribute-collector", "rune-grave-reader",
        "rune-breach-mage", "rune-hexstalker", "rune-disenchanter",
    };

    private static readonly string[] SpellIds =
    {
        "rune-disjunction", "rune-mass-ward", "rune-return-of-the-archon", "rune-sapping-hex",
        "rune-wordbreaker", "rune-conflux-ritual",
    };

    private static readonly string[] NewCardIds =
        new[] { HqId, HeroId }.Concat(EquipmentIds).Concat(BuildingIds).Concat(UnitIds).Concat(SpellIds).ToArray();

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

    private static IEnumerable<CardDefinition> NewCards => NewCardIds.Select(Card);

    // ------------------------------------------------------------------ harness

    /// <summary>
    /// P1 plays the runecraft deck (HQ + hero placed by the real setup), P2 plays Town.
    /// Player-scoped resources are topped up so Town's gold-priced cards are castable; the
    /// Conclave HQ's entity-scoped mana bank (what Conclave play costs are actually paid from)
    /// is filled to its cap unless <paramref name="fillMana"/> is false.
    /// </summary>
    private static (GameInstance game, RuleEngine engine, PlayerInstance p1, PlayerInstance p2) CreateMatch(bool fillMana = true)
    {
        var deckA = Definition.Decks.First(d => d.Id == DeckId);
        var deckB = Definition.Decks.First(d => d.Id == "town");

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
            foreach (var resDef in Definition.Resources.Where(r => r.Scope == "player"))
                player.Resources[resDef.Id] = 99;
        // Deterministic board: no opening hand (a stray reaction card in a hand would open a
        // reaction window mid-test). The decks themselves are untouched.
        foreach (var card in game.Objects.Where(o => o.ZoneId == "hand"))
            card.ZoneId = "discard";

        if (fillMana)
            Hq(game, p1).Resources["mana"] = 12;

        return (game, engine, p1, p2);
    }

    private static int _counter;

    private static ObjectInstance Spawn(GameInstance game, string cardId, PlayerInstance player, string zone)
    {
        var def = game.Definition.Cards.First(c => c.Id == cardId);
        var obj = new ObjectFactory().CreateObjectInstance(game, def, player.Id, zone);
        obj.Id = $"t3950_{++_counter}";
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

    /// <summary>A plain body with deterministic stats and tags (the real town-watch card, restated).</summary>
    private static ObjectInstance Body(GameInstance game, PlayerInstance owner, int attack, int hp, int armor = 0,
        string line = "front", params string[] tags)
    {
        var obj = OnField(game, "town-watch", owner, line);
        obj.Properties["attack"] = attack;
        obj.Properties["currentHp"] = hp;
        obj.Properties["maxHp"] = hp;
        obj.Properties["armor"] = armor;
        obj.Tags = tags.ToList();
        return obj;
    }

    private static ObjectInstance Hero(GameInstance game, PlayerInstance p) =>
        GameQueries.FindLivingHero(game, p.Id) ?? throw new InvalidOperationException("no hero");

    private static ObjectInstance Hq(GameInstance game, PlayerInstance p) =>
        GameQueries.FindResourceBank(game, p.Id) ?? throw new InvalidOperationException("no hq");

    private static int Mana(GameInstance game, PlayerInstance p) => Hq(game, p).Resources.GetValueOrDefault("mana");

    private static void SetMana(GameInstance game, PlayerInstance p, int amount) => Hq(game, p).Resources["mana"] = amount;

    private static void ToMain(GameInstance game, PlayerInstance p)
    {
        game.ActivePlayerId = p.Id;
        game.CurrentPhaseId = "main";
        game.State = GameState.WaitingForAction;
    }

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

    /// <summary>Put an equipment card in hand and play it onto <paramref name="host"/> through the real play path.</summary>
    private static ObjectInstance Equip(GameInstance game, RuleEngine engine, PlayerInstance p, string cardId, ObjectInstance host)
    {
        ToMain(game, p);
        var card = InHand(game, cardId, p);
        Play(game, engine, p, card, host);
        Assert.Equal(host.Id, card.AttachedToId);
        return card;
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

    private static void Attack(GameInstance game, RuleEngine engine, ObjectInstance attacker, ObjectInstance defender)
    {
        game.ActivePlayerId = attacker.ControllerId;
        game.CurrentPhaseId = "combat";
        var (ok, error) = engine.ExecuteAction(game, attacker.ControllerId, new ActionRequest
        {
            Type = "attack",
            SourceObjectId = attacker.Id,
            TargetIds = new List<string> { defender.Id }
        });
        Assert.True(ok, $"{attacker.Name} attacking {defender.Name}: {error}");
    }

    /// <summary>Walk the real phase machine so it is <paramref name="p"/>'s turn start (fires onTurnStart triggers).</summary>
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

    private static int Effective(GameInstance game, ObjectInstance o, string property) =>
        GameQueries.GetEffectiveProperty(game, o, property);

    private static int Hp(ObjectInstance o) => o.Properties["currentHp"];

    private static int HandCount(GameInstance game, PlayerInstance p) =>
        game.Objects.Count(o => o.OwnerId == p.Id && o.ZoneId == "hand");

    private static List<string> Targets(GameInstance game, ObjectInstance attacker) =>
        new TargetingService().GetAttackTargets(game, attacker);

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

    // Trigger events TriggerService actually fires, and whether each hands the effect an event Target.
    private static readonly Dictionary<string, bool> FiredTriggerEvents = new()
    {
        ["onTurnStart"] = false, ["onPlay"] = false, ["onKill"] = true, ["onDestroyBuilding"] = true,
        ["onDeath"] = true, ["onUnitDied"] = true, ["onDealCombatDamage"] = true, ["onDamaged"] = true,
        ["onFriendlyDamageHqOrHero"] = false, ["onEnemyAttack"] = true,
    };

    private static readonly HashSet<string> RegisteredCostTypes = new() { "tap", "resource", "sacrifice", "crew", "sacrifice_units" };
    private static readonly HashSet<string> RegisteredConditionTypes = new()
    {
        "not_tapped", "is_tapped", "resource_gte", "resource_lte", "has_tag", "is_phase", "own_hero_destroyed",
        "controls_no_tagged", "controls_tagged", "is_attached", "not_attached",
    };

    // Effect types whose handler resolves its object through ResolveScope("target") when no scope is given.
    private static readonly HashSet<string> TargetByDefault = new()
    {
        "direct_damage", "buff_target_until_end_of_turn", "add_progress", "freeze", "reveal",
        "reveal_attachments", "destroy_infiltrators", "damage_infiltrators", "salvage_module",
    };

    private static IEnumerable<(string where, AbilityDefinition ability)> Abilities(CardDefinition c)
    {
        foreach (var a in c.Abilities) yield return ($"{c.Id}/ability {a.Id}", a);
        if (c.OnPlay != null) yield return ($"{c.Id}/onPlay", c.OnPlay);
    }

    [Fact]
    public void Batch_takes_the_conclave_faction_from_55_to_87_exclusive_cards_led_by_equipment_and_buildings()
    {
        var before = Definition.Cards.Where(c => c.Faction == "conclave" && !NewCardIds.Contains(c.Id)).ToList();
        var after = Definition.Cards.Where(c => c.Faction == "conclave").ToList();

        // the corrected baseline task 3950 was refined against (NOT the stale 20/200)
        Assert.Equal(55, before.Count);
        Assert.Equal(25, before.Count(c => c.ObjectType == "unit"));
        Assert.Equal(19, before.Count(c => c.ObjectType == "spell"));
        Assert.Equal(8, before.Count(c => c.ObjectType == "building"));
        Assert.Equal(3, before.Count(c => c.ObjectType == "equipment"));

        Assert.Equal(32, NewCardIds.Distinct().Count());
        Assert.Equal(87, after.Count);
        Assert.Equal(13, after.Count(c => c.ObjectType == "equipment")); // the thinnest category grew the most
        Assert.Equal(15, after.Count(c => c.ObjectType == "building"));
        Assert.Equal(10, EquipmentIds.Length);
        Assert.Equal(7, BuildingIds.Length);
        Assert.Equal(1, after.Count(c => c.ObjectType == "nexus"));
        Assert.Equal(1, after.Count(c => c.ObjectType == "caster-hero"));
    }

    [Fact]
    public void Every_new_card_carries_the_real_faction_field_and_no_card_uses_the_nonexistent_underscore_one()
    {
        // "_faction" is not in the schema: System.Text.Json drops it silently and the card would
        // vanish from every faction/deck filter (the failure mode PR #55 had to repair for PR #52).
        using var doc = JsonDocument.Parse(File.ReadAllText(DefinitionPath));
        var cards = doc.RootElement.GetProperty("cards").EnumerateArray().ToList();
        Assert.DoesNotContain(cards, c => c.TryGetProperty("_faction", out _));

        var byId = cards.ToDictionary(c => c.GetProperty("id").GetString()!);
        foreach (var id in NewCardIds)
        {
            Assert.True(byId[id].TryGetProperty("faction", out var f), $"{id} has no \"faction\" key");
            Assert.Equal("conclave", f.GetString());
        }
        Assert.All(NewCards, c => Assert.Equal("conclave", c.Faction));
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
                Assert.NotEmpty(t.Effects);
                if (!FiredTriggerEvents.ContainsKey(t.Event)) problems.Add($"{c.Id}/trigger: unfired event '{t.Event}'");
                foreach (var e in t.Effects)
                    if (!RegisteredEffects.Contains(e.Type)) problems.Add($"{c.Id}/trigger {t.Event}: unregistered effect '{e.Type}'");
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
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name)).ToHashSet();

        var cardKeys = Keys(typeof(CardDefinition));
        var abilityKeys = Keys(typeof(AbilityDefinition));
        var effectKeys = Keys(typeof(EffectDefinition));
        var costKeys = Keys(typeof(CostDefinition));
        var condKeys = Keys(typeof(ConditionDefinition));
        var choiceKeys = Keys(typeof(ChoiceDefinition));
        var triggerKeys = Keys(typeof(TriggerDefinition));
        var modKeys = Keys(typeof(AttachModifierDefinition));
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
            if (a.TryGetProperty("conditions", out var conds)) foreach (var c in conds.EnumerateArray()) Check(c, condKeys, where + " condition");
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
                    if (t.TryGetProperty("conditions", out var conds)) foreach (var cnd in conds.EnumerateArray()) Check(cnd, condKeys, id + "/trigger condition");
                    foreach (var e in t.GetProperty("effects").EnumerateArray()) Check(e, effectKeys, id + "/trigger effect");
                }
            if (c.TryGetProperty("attachModifiers", out var mods)) foreach (var m in mods.EnumerateArray()) Check(m, modKeys, id + "/attachModifier");
        }

        var deck = doc.RootElement.GetProperty("decks").EnumerateArray().First(d => d.GetProperty("id").GetString() == DeckId);
        Check(deck, deckKeys, DeckId);

        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }

    [Fact]
    public void Target_scoped_effects_always_have_something_to_target()
    {
        // heal/direct_damage/freeze... with scope "target" and no ability "choice" resolve a null
        // target and do nothing; a trigger event that supplies no target does the same.
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
    public void Every_new_equipment_declares_real_slots_because_slotless_equipment_never_attaches()
    {
        // conclave-spellshard-staff / conclave-focus-crystal (pre-batch) declare no slots, so they
        // land on the battlefield unattached and their is_attached abilities can never fire.
        var slots = new HashSet<string> { "mainHand", "offHand", "body", "head", "mutation" };
        foreach (var id in EquipmentIds)
        {
            var c = Card(id);
            Assert.NotNull(c.Slots);
            Assert.NotEmpty(c.Slots!);
            Assert.All(c.Slots!, s => Assert.Contains(s, slots));
            Assert.Equal("chooseCharacter", c.AttachTo);
        }
    }

    [Fact]
    public void Every_new_card_is_referenced_by_a_conclave_precon_deck()
    {
        // PR #52's cards shipped orphaned. Each of these must be reachable through a precon deck
        // of the conclave faction, either in its card list or as its HQ/hero.
        var conclaveDecks = Definition.Decks.Where(d => d.Faction == "conclave").ToList();
        Assert.Contains(conclaveDecks, d => d.Id == DeckId);
        var orphans = NewCardIds.Where(id => !conclaveDecks.Any(d =>
            d.Cards.ContainsKey(id) || d.Hq == id || d.Hero == id || d.HqOptions.Contains(id) || d.HeroOptions.Contains(id))).ToList();
        Assert.True(orphans.Count == 0, "orphaned: " + string.Join(", ", orphans));
    }

    [Fact]
    public void Runecraft_deck_is_a_legal_60_card_deck_with_its_own_hq_and_hero()
    {
        var deck = Definition.Decks.First(d => d.Id == DeckId);
        Assert.Equal("conclave", deck.Faction);
        Assert.StartsWith("conclave-", deck.Id); // DeckFactionTests pins {faction}-{subname}
        Assert.Equal(60, deck.Cards.Values.Sum());
        Assert.Null(GameQueries.ValidateDeck(Definition, deck.Cards, isAdmin: false, enforceMinSize: true));

        Assert.Equal(HqId, deck.Hq);
        Assert.Equal(HeroId, deck.Hero);
        Assert.Contains(HqId, deck.HqOptions);
        Assert.Contains(HeroId, deck.HeroOptions);
        Assert.True(GameQueries.IsObjectTypeOrSubtype(Definition, Card(HqId).ObjectType, "headquarters"));
        Assert.True(GameQueries.IsObjectTypeOrSubtype(Definition, Card(HeroId).ObjectType, "hero"));
        // the deck's units carry housing costs: its HQ must supply living space
        Assert.True(Card(HqId).HousingProvided >= 4);
    }

    [Fact]
    public void New_cards_are_deck_eligible_and_priced_in_mana_the_resource_the_conclave_hq_actually_produces()
    {
        // Mana is an entity resource banked on the Conclave nexus (paid from there by play costs);
        // gold has no Conclave income, so a gold-priced card can be uncastable in a real match.
        var cap = Card(HqId).ResourceCapacities!["mana"];
        foreach (var c in NewCards)
        {
            Assert.True(GameQueries.IsDeckEligible(Definition, c), $"{c.Id} is not deck-eligible");
            var costs = GameQueries.BasePlayCosts(c);
            if (c.Id is HqId or HeroId) { Assert.Empty(costs); continue; }
            Assert.Equal(new[] { "mana" }, costs.Keys.ToArray());
            Assert.InRange(costs["mana"], 1, cap);
        }
    }

    [Fact]
    public void New_cards_are_role_distinct_from_each_other_and_from_the_55_pre_batch_conclave_cards()
    {
        // A structural fingerprint (object type + slot/tags + which effect types run on which
        // event) - not stats. Two cards with the same fingerprint would be the "reskinned clone
        // under a new name" the task forbids; this fails the moment a future batch copy-pastes a role.
        static string Fingerprint(CardDefinition c)
        {
            static string Effects(IEnumerable<EffectDefinition> es) => string.Join("+", es.Select(e => e.Type).OrderBy(x => x, StringComparer.Ordinal));
            var parts = new List<string> { c.ObjectType };
            if (c.Slots != null) parts.Add("slots:" + string.Join("+", c.Slots.OrderBy(x => x, StringComparer.Ordinal)));
            if (c.AttachTags.Count > 0) parts.Add("attachTags:" + string.Join("+", c.AttachTags.OrderBy(x => x, StringComparer.Ordinal)));
            if (c.AttachModifiers.Count > 0)
                parts.Add("mods:" + string.Join("+", c.AttachModifiers.Select(m => m.PropertyId + (m.Amount < 0 ? "-" : "+")).OrderBy(x => x, StringComparer.Ordinal)));
            foreach (var tag in new[] { "guard", "fortification", "ranged", "cleave", "splash", "retaliate", "blocking" })
                if (c.Tags.Contains(tag)) parts.Add("tag:" + tag);
            foreach (var a in c.Abilities) parts.Add("ability:" + Effects(a.Effects));
            if (c.OnPlay != null) parts.Add("onPlay:" + Effects(c.OnPlay.Effects));
            foreach (var t in c.Triggers) parts.Add($"{t.Event}:" + Effects(t.Effects));
            if (c.Timing != "main") parts.Add("timing:" + c.Timing + ":" + string.Join(",", c.ReactionTo));
            if (c.BonusAttackVsBuildings != null) parts.Add("bonusVsBuildings");
            return string.Join("|", parts.OrderBy(x => x, StringComparer.Ordinal));
        }

        var existing = Definition.Cards
            .Where(c => c.Faction == "conclave" && !NewCardIds.Contains(c.Id))
            .ToDictionary(c => c.Id, Fingerprint);

        var seen = new Dictionary<string, string>();
        foreach (var c in NewCards)
        {
            var fp = Fingerprint(c);
            Assert.True(!existing.ContainsValue(fp), $"{c.Id} duplicates the role of an existing conclave card: {fp}");
            Assert.True(seen.TryAdd(fp, c.Id), $"{c.Id} duplicates the role of {(seen.TryGetValue(fp, out var other) ? other : "?")}: {fp}");
        }
    }

    // ------------------------------------------------------------------ casting through the real play path

    [Fact]
    public void Runecraft_match_starts_with_its_hq_and_hero_and_the_hq_channels_mana_up_to_its_cap()
    {
        var (game, engine, p1, _) = CreateMatch(fillMana: false);
        var hq = Hq(game, p1);
        Assert.Equal(HqId, hq.DefinitionId);
        Assert.Equal(HeroId, Hero(game, p1).DefinitionId);
        Assert.Equal(5, Mana(game, p1));
        Assert.Equal(4, GameQueries.HousingCapacity(game, p1.Id));

        Use(game, engine, p1, hq, "rune-forge-channel");
        Assert.Equal(7, Mana(game, p1));

        hq.IsTapped = false;
        SetMana(game, p1, 11);
        Use(game, engine, p1, hq, "rune-forge-channel");
        Assert.Equal(12, Mana(game, p1)); // capped at the nexus capacity
    }

    [Fact]
    public void Runemaster_meditates_for_mana_and_raises_a_team_armor_barrier_paid_from_its_own_mana()
    {
        var (game, engine, p1, _) = CreateMatch();
        var hero = Hero(game, p1);
        var unitA = Body(game, p1, 2, 4);
        var unitB = Body(game, p1, 2, 4);
        Assert.Equal(2, hero.Resources["mana"]);

        Use(game, engine, p1, hero, "runemaster-barrier");
        Assert.Equal(0, hero.Resources["mana"]);
        Assert.Equal(1, Effective(game, unitA, "armor"));
        Assert.Equal(1, Effective(game, unitB, "armor"));
        Assert.False(TryUse(game, engine, p1, hero, "runemaster-barrier").ok); // no mana left

        Use(game, engine, p1, hero, "runemaster-meditate");
        Assert.Equal(2, hero.Resources["mana"]);
    }

    [Theory]
    [MemberData(nameof(UnitCardIds))]
    public void Every_new_unit_is_castable_from_hand_with_the_decks_own_hq_housing_and_mana(string cardId)
    {
        var (game, engine, p1, _) = CreateMatch();
        var card = InHand(game, cardId, p1);
        var cost = Card(cardId).PlayCosts!["mana"];
        var before = Mana(game, p1);

        Play(game, engine, p1, card);

        Assert.Equal("battlefield", card.ZoneId);
        Assert.Equal(before - cost, Mana(game, p1));
    }

    [Theory]
    [MemberData(nameof(BuildingCardIds))]
    public void Every_new_building_is_castable_from_hand_and_lands_on_the_battlefield(string cardId)
    {
        var (game, engine, p1, _) = CreateMatch();
        var card = InHand(game, cardId, p1);
        var cost = Card(cardId).PlayCosts!["mana"];
        var before = Mana(game, p1);

        Play(game, engine, p1, card);

        Assert.Equal("battlefield", card.ZoneId);
        Assert.False(card.UnderConstruction);
        Assert.Equal(before - cost, Mana(game, p1));
    }

    [Theory]
    [MemberData(nameof(EquipmentCardIds))]
    public void Every_new_equipment_attaches_to_a_unit_through_the_real_play_path(string cardId)
    {
        var (game, engine, p1, _) = CreateMatch();
        var host = Body(game, p1, 2, 5);

        var gear = Equip(game, engine, p1, cardId, host);

        Assert.Equal("battlefield", gear.ZoneId);
        Assert.Equal(Card(cardId).Slots, gear.OccupiedSlots);
    }

    public static IEnumerable<object[]> UnitCardIds() => UnitIds.Select(id => new object[] { id });
    public static IEnumerable<object[]> BuildingCardIds() => BuildingIds.Select(id => new object[] { id });
    public static IEnumerable<object[]> EquipmentCardIds() => EquipmentIds.Select(id => new object[] { id });

    // ------------------------------------------------------------------ equipment behaviour (real engine)

    [Fact]
    public void Warding_circlet_gives_armor_and_heals_its_bearer_at_every_turn_start()
    {
        var (game, engine, p1, _) = CreateMatch();
        var host = Body(game, p1, 2, 5);
        host.Properties["currentHp"] = 2;

        Equip(game, engine, p1, "rune-warding-circlet", host);
        Assert.Equal(1, Effective(game, host, "armor"));

        StartTurnOf(game, engine, p1);
        Assert.Equal(3, Hp(host));
        StartTurnOf(game, engine, p1);
        Assert.Equal(4, Hp(host));
    }

    [Fact]
    public void Thorned_vestments_hit_whoever_strikes_the_bearer_with_direct_damage()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var host = Body(game, p1, 1, 5);
        var attacker = Body(game, p2, 3, 5);
        Equip(game, engine, p1, "rune-thorned-vestments", host);

        Attack(game, engine, attacker, host);

        Assert.Equal(3, Hp(host));     // 3 attack - 1 armor from the vestments
        Assert.Equal(3, Hp(attacker)); // thorns: 2 direct damage back
    }

    [Fact]
    public void Siphon_blade_adds_attack_and_heals_the_wielder_for_every_hit_it_lands()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var host = Body(game, p1, 2, 5);
        host.Properties["currentHp"] = 2;
        var target = Body(game, p2, 1, 9);
        Equip(game, engine, p1, "rune-siphon-blade", host);

        Attack(game, engine, host, target);

        Assert.Equal(6, Hp(target)); // 2 attack + 1 from the blade
        Assert.Equal(4, Hp(host));   // healed 2
    }

    [Fact]
    public void Farsight_lance_grants_ranged_reach_to_the_enemy_back_line()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var host = Body(game, p1, 2, 5);
        Body(game, p2, 1, 5, 0, "front");
        var back = Body(game, p2, 1, 5, 0, "back");
        Assert.DoesNotContain(back.Id, Targets(game, host)); // a melee unit cannot reach it

        Equip(game, engine, p1, "rune-farsight-lance", host);

        Assert.Contains("ranged", host.Tags);
        Assert.Equal(3, Effective(game, host, "attack"));
        Assert.Contains(back.Id, Targets(game, host));
    }

    [Fact]
    public void Aegis_tome_turns_its_bearer_into_a_guard_that_shields_the_hero_and_hq()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var host = Body(game, p1, 1, 6, 0, "back");
        var attacker = Body(game, p2, 2, 5);
        var before = Targets(game, attacker);
        Assert.Contains(Hq(game, p1).Id, before);
        Assert.Contains(Hero(game, p1).Id, before);

        Equip(game, engine, p1, "rune-aegis-tome", host);

        Assert.Contains("guard", host.Tags);
        Assert.Equal(1, Effective(game, host, "armor"));
        var after = Targets(game, attacker);
        Assert.DoesNotContain(Hq(game, p1).Id, after);
        Assert.DoesNotContain(Hero(game, p1).Id, after);
        Assert.Contains(host.Id, after);
    }

    [Fact]
    public void Soulbinder_sigil_feeds_the_hq_mana_bank_when_its_bearer_makes_a_kill()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var host = Body(game, p1, 5, 5);
        var victim = Body(game, p2, 1, 1);
        Equip(game, engine, p1, "rune-soulbinder-sigil", host);
        SetMana(game, p1, 3);

        Attack(game, engine, host, victim);

        Assert.True(victim.IsDestroyed);
        Assert.Equal(5, Mana(game, p1));
    }

    [Fact]
    public void Chronal_band_untaps_its_bearer_for_a_second_strike_once_per_turn()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var host = Body(game, p1, 2, 5);
        var first = Body(game, p2, 1, 2);
        var second = Body(game, p2, 1, 2);
        var band = Equip(game, engine, p1, "rune-chronal-band", host);

        Attack(game, engine, host, first);
        Assert.True(first.IsDestroyed);
        Assert.True(host.IsTapped);

        Use(game, engine, p1, band, "chronal-band-rewind");
        Assert.False(host.IsTapped);
        Attack(game, engine, host, second);
        Assert.True(second.IsDestroyed);

        Assert.False(TryUse(game, engine, p1, band, "chronal-band-rewind").ok); // the band itself is tapped now
    }

    [Fact]
    public void Frostbrand_freezes_what_it_hits_through_the_enemys_next_untap()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var host = Body(game, p1, 1, 5);
        var victim = Body(game, p2, 1, 9);
        Equip(game, engine, p1, "rune-frostbrand", host);

        Attack(game, engine, host, victim);

        Assert.Equal(7, Hp(victim)); // 1 attack + 1 from the brand
        Assert.True(victim.IsTapped);
        Assert.True(victim.SkipNextUntap);
        StartTurnOf(game, engine, p2);
        Assert.True(victim.IsTapped);  // frozen through its untap step
        StartTurnOf(game, engine, p1);
        StartTurnOf(game, engine, p2);
        Assert.False(victim.IsTapped); // and free again a turn later
    }

    [Fact]
    public void Sweeping_staff_is_two_handed_grants_splash_and_is_evicted_by_an_offhand_item()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var host = Body(game, p1, 2, 8);
        var struck = Body(game, p2, 1, 6);
        var bystander = Body(game, p2, 1, 6);
        var staff = Equip(game, engine, p1, "rune-sweeping-staff", host);

        Assert.Equal(new[] { "mainHand", "offHand" }, staff.OccupiedSlots);
        Assert.Contains("splash", host.Tags);
        Assert.Equal(4, Effective(game, host, "attack"));

        Attack(game, engine, host, struck);
        Assert.Equal(2, Hp(struck));
        Assert.Equal(5, Hp(bystander)); // splash clips every other enemy unit on the line for 1

        Equip(game, engine, p1, "rune-aegis-tome", host); // needs the offHand the staff is holding
        Assert.Null(staff.AttachedToId);
        Assert.DoesNotContain("splash", host.Tags);
        Assert.Equal(2, Effective(game, host, "attack"));
    }

    [Fact]
    public void Scholars_monocle_draws_a_card_every_turn_start_then_fades_after_three_turns()
    {
        var (game, engine, p1, _) = CreateMatch();
        var host = Body(game, p1, 2, 5);
        var monocle = Equip(game, engine, p1, "rune-scholars-monocle", host);
        Assert.Equal(3, monocle.Lifetime);
        var handBefore = HandCount(game, p1);

        StartTurnOf(game, engine, p1);
        Assert.Equal(handBefore + 2, HandCount(game, p1)); // the normal turn draw + the monocle's

        StartTurnOf(game, engine, p1);
        StartTurnOf(game, engine, p1);
        Assert.True(monocle.IsDestroyed);
        Assert.Equal(handBefore + 6, HandCount(game, p1));
    }

    // ------------------------------------------------------------------ building behaviour (real engine)

    [Fact]
    public void Leyline_siphon_banks_two_mana_in_the_hq_every_turn_start()
    {
        var (game, engine, p1, _) = CreateMatch();
        OnField(game, "rune-leyline-siphon", p1, "back");
        SetMana(game, p1, 4);

        StartTurnOf(game, engine, p1);

        Assert.Equal(6, Mana(game, p1));
    }

    [Fact]
    public void Warded_bulwark_is_a_fortification_that_shields_the_hq_from_attack()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var attacker = Body(game, p2, 3, 5);
        Assert.Contains(Hq(game, p1).Id, Targets(game, attacker));

        var bulwark = OnField(game, "rune-warded-bulwark", p1, "back");

        Assert.Contains("fortification", bulwark.Tags);
        var targets = Targets(game, attacker);
        Assert.DoesNotContain(Hq(game, p1).Id, targets);
        Assert.Contains(bulwark.Id, targets);
        Assert.Equal(3, Effective(game, bulwark, "armor"));
    }

    [Fact]
    public void Obelisk_of_silence_taps_the_enemy_guards_opening_the_hq_and_hero_to_ranged_attackers()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var archer = Body(game, p1, 3, 5, 0, "front", "ranged");
        var guardA = Body(game, p2, 1, 4, 0, "front", "guard");
        var guardB = Body(game, p2, 1, 4, 0, "front", "guard");
        var enemyHq = Hq(game, p2);
        var enemyHero = Hero(game, p2);
        Assert.DoesNotContain(enemyHq.Id, Targets(game, archer));   // guards protect them
        Assert.DoesNotContain(enemyHero.Id, Targets(game, archer));
        var obelisk = OnField(game, "rune-obelisk-of-silence", p1, "back");

        Use(game, engine, p1, obelisk, "obelisk-silence");

        Assert.True(guardA.IsTapped);
        Assert.True(guardB.IsTapped);
        Assert.False(enemyHero.IsTapped); // heroes are not units
        Assert.Contains(enemyHq.Id, Targets(game, archer));
        Assert.Contains(enemyHero.Id, Targets(game, archer));
    }

    [Fact]
    public void Runic_workshop_makes_only_the_next_spell_cost_one_less_mana()
    {
        var (game, engine, p1, _) = CreateMatch();
        var workshop = OnField(game, "rune-runic-workshop", p1, "back");
        Use(game, engine, p1, workshop, "workshop-inscribe");
        SetMana(game, p1, 12);

        Play(game, engine, p1, InHand(game, "rune-sapping-hex", p1)); // base cost 4
        Assert.Equal(12 - 3, Mana(game, p1));

        Play(game, engine, p1, InHand(game, "rune-sapping-hex", p1)); // discount already consumed
        Assert.Equal(12 - 3 - 4, Mana(game, p1));
    }

    [Fact]
    public void Eldritch_orrery_pings_the_enemy_hq_every_turn_start_ignoring_armor()
    {
        var (game, engine, p1, p2) = CreateMatch();
        OnField(game, "rune-eldritch-orrery", p1, "back");
        var enemyHq = Hq(game, p2);
        var before = Hp(enemyHq);

        StartTurnOf(game, engine, p1);

        Assert.Equal(before - 1, Hp(enemyHq)); // 1 damage although the Town Hall has 2 armor
    }

    [Fact]
    public void Banner_hall_gives_every_own_unit_plus_one_attack_at_turn_start_and_houses_two()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var capacityBefore = GameQueries.HousingCapacity(game, p1.Id);
        OnField(game, "rune-banner-hall", p1, "back");
        var mine = Body(game, p1, 2, 4);
        var theirs = Body(game, p2, 2, 4);

        StartTurnOf(game, engine, p1);

        Assert.Equal(3, Effective(game, mine, "attack"));
        Assert.Equal(2, Effective(game, theirs, "attack"));
        Assert.Equal(capacityBefore + 2, GameQueries.HousingCapacity(game, p1.Id));
    }

    [Fact]
    public void Scribe_archive_transcribes_a_card_and_adds_four_housing()
    {
        var (game, engine, p1, _) = CreateMatch();
        var capacityBefore = GameQueries.HousingCapacity(game, p1.Id);
        var archive = OnField(game, "rune-scribe-archive", p1, "back");
        var handBefore = HandCount(game, p1);

        Use(game, engine, p1, archive, "archive-transcribe");

        Assert.Equal(handBefore + 1, HandCount(game, p1));
        Assert.Equal(capacityBefore + 4, GameQueries.HousingCapacity(game, p1.Id));
        Assert.False(TryUse(game, engine, p1, archive, "archive-transcribe").ok); // tapped
    }

    // ------------------------------------------------------------------ unit behaviour (real engine)

    [Fact]
    public void Hexblade_duelist_draws_a_card_when_it_makes_a_kill_and_not_when_it_only_wounds()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var duelist = OnField(game, "rune-hexblade-duelist", p1);
        var tough = Body(game, p2, 1, 9);
        var frail = Body(game, p2, 1, 1);
        var handBefore = HandCount(game, p1);

        Attack(game, engine, duelist, tough);
        Assert.Equal(handBefore, HandCount(game, p1));

        duelist.IsTapped = false;
        Attack(game, engine, duelist, frail);
        Assert.True(frail.IsDestroyed);
        Assert.Equal(handBefore + 1, HandCount(game, p1));
    }

    [Fact]
    public void Frost_effigy_freezes_whatever_attacks_it()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var effigy = OnField(game, "rune-frost-effigy", p1);
        var attacker = Body(game, p2, 3, 5);

        Attack(game, engine, attacker, effigy);

        Assert.Equal(3, Hp(effigy)); // 3 attack - 1 armor
        Assert.True(attacker.SkipNextUntap);
        StartTurnOf(game, engine, p2);
        Assert.True(attacker.IsTapped);
    }

    [Fact]
    public void Tribute_collector_banks_a_mana_whenever_a_friendly_card_hurts_the_enemy_hq_or_hero()
    {
        var (game, engine, p1, p2) = CreateMatch();
        OnField(game, "rune-tribute-collector", p1, "back");
        var striker = Body(game, p1, 5, 5);
        var enemyHq = Hq(game, p2);
        var hqHpBefore = Hp(enemyHq);
        SetMana(game, p1, 3);

        Attack(game, engine, striker, enemyHq);

        Assert.True(Hp(enemyHq) < hqHpBefore);
        Assert.Equal(4, Mana(game, p1));
    }

    [Fact]
    public void Grave_reader_banks_a_mana_whenever_any_unit_dies_on_either_side()
    {
        var (game, engine, p1, p2) = CreateMatch();
        OnField(game, "rune-grave-reader", p1, "back");
        var killer = Body(game, p1, 5, 5);
        var victim = Body(game, p2, 1, 1);
        SetMana(game, p1, 3);

        Attack(game, engine, killer, victim);

        Assert.True(victim.IsDestroyed);
        Assert.Equal(4, Mana(game, p1));
    }

    [Fact]
    public void Breach_mage_shatters_a_building_with_its_siege_bonus_and_banks_two_mana_for_it()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var mage = OnField(game, "rune-breach-mage", p1);
        var barracks = OnField(game, "barracks", p2, "front");
        barracks.Properties["currentHp"] = 5;
        barracks.Properties["maxHp"] = 5;
        barracks.Properties["armor"] = 0;
        SetMana(game, p1, 3);

        Attack(game, engine, mage, barracks); // 2 attack + 3 vs buildings = exactly 5

        Assert.True(barracks.IsDestroyed);
        Assert.Equal(5, Mana(game, p1));
    }

    [Fact]
    public void Hexstalker_permanently_saps_one_attack_from_whatever_it_shoots()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var stalker = OnField(game, "rune-hexstalker", p1);
        var target = Body(game, p2, 3, 9, 0, "back");

        Attack(game, engine, stalker, target); // ranged: reaches the back line

        Assert.Equal(7, Hp(target));
        Assert.Equal(2, Effective(game, target, "attack"));
    }

    [Fact]
    public void Disenchanter_dispels_enemy_equipment_spending_its_own_mana_and_recharges_each_turn()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var enemy = Body(game, p2, 2, 5);
        ToMain(game, p2);
        var sword = InHand(game, "short-sword", p2);
        Play(game, engine, p2, sword, enemy);
        Assert.Equal(3, Effective(game, enemy, "attack"));
        ToMain(game, p1);

        var disenchanter = OnField(game, "rune-disenchanter", p1, "back");
        Assert.Equal(2, disenchanter.Resources["mana"]);

        Use(game, engine, p1, disenchanter, "disenchanter-dispel", sword);

        Assert.True(sword.IsDestroyed);
        Assert.Equal(2, Effective(game, enemy, "attack"));
        Assert.Equal(0, disenchanter.Resources["mana"]);
        Assert.False(TryUse(game, engine, p1, disenchanter, "disenchanter-dispel", sword).ok);

        StartTurnOf(game, engine, p1);
        Assert.Equal(1, disenchanter.Resources["mana"]);
    }

    // ------------------------------------------------------------------ spell behaviour (real engine)

    [Fact]
    public void Disjunction_cancels_an_activated_enemy_ability_from_the_reaction_window()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var townHall = Hq(game, p2);
        var goldBefore = p2.Resources["gold"];
        var disjunction = InHand(game, "rune-disjunction", p1);

        game.ActivePlayerId = p2.Id;
        Use(game, engine, p2, townHall, "collect-taxes");
        Assert.Equal(GameState.WaitingForReaction, game.State);
        Assert.Equal(p1.Id, game.ReactionPlayerId);

        var (ok, error) = TryPlay(game, engine, p1, disjunction);
        Assert.True(ok, error);
        Assert.True(engine.ExecuteAction(game, p1.Id, new ActionRequest { Type = "pass" }).success);

        Assert.Equal(GameState.WaitingForAction, game.State);
        Assert.Equal(goldBefore, p2.Resources["gold"]); // the 2 gold never arrived
        Assert.Equal(12 - 2, Mana(game, p1));

        // control: with no reaction in hand the same ability resolves normally
        townHall.IsTapped = false;
        Use(game, engine, p2, townHall, "collect-taxes");
        Assert.Equal(goldBefore + 2, p2.Resources["gold"]);
    }

    [Fact]
    public void Mass_ward_answers_an_attack_by_giving_every_own_unit_two_armor_before_damage()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var defender = Body(game, p1, 2, 5);
        var other = Body(game, p1, 2, 5);
        var attacker = Body(game, p2, 3, 5);
        var ward = InHand(game, "rune-mass-ward", p1);

        Attack(game, engine, attacker, defender);
        Assert.Equal(GameState.WaitingForReaction, game.State);

        var (ok, error) = TryPlay(game, engine, p1, ward);
        Assert.True(ok, error);
        Assert.True(engine.ExecuteAction(game, p1.Id, new ActionRequest { Type = "pass" }).success);

        Assert.Equal(4, Hp(defender)); // 3 attack - 2 armor = 1 damage, not 3
        Assert.Equal(2, Effective(game, other, "armor"));
        Assert.Equal(12 - 3, Mana(game, p1));
    }

    [Fact]
    public void Return_of_the_archon_revives_a_fallen_hero_at_full_health()
    {
        var (game, engine, p1, _) = CreateMatch();
        var hero = Hero(game, p1);
        hero.IsDestroyed = true;
        hero.ZoneId = "discard";
        hero.Properties["currentHp"] = 0;
        Assert.Null(GameQueries.FindLivingHero(game, p1.Id));

        Play(game, engine, p1, InHand(game, "rune-return-of-the-archon", p1));

        Assert.Same(hero, GameQueries.FindLivingHero(game, p1.Id));
        Assert.Equal(hero.Properties["maxHp"], Hp(hero));
        Assert.Equal(12 - 5, Mana(game, p1));
    }

    [Fact]
    public void Sapping_hex_permanently_lowers_every_enemy_units_attack_but_not_your_own()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var enemyA = Body(game, p2, 3, 4);
        var enemyB = Body(game, p2, 1, 4);
        var mine = Body(game, p1, 3, 4);

        Play(game, engine, p1, InHand(game, "rune-sapping-hex", p1));

        Assert.Equal(2, Effective(game, enemyA, "attack"));
        Assert.Equal(0, Effective(game, enemyB, "attack")); // clamps at zero
        Assert.Equal(3, Effective(game, mine, "attack"));
    }

    [Fact]
    public void Wordbreaker_destroys_only_a_wounded_enemy_unit_and_never_a_healthy_unit_or_a_hero()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var wounded = Body(game, p2, 1, 3);
        var healthy = Body(game, p2, 1, 4);
        var enemyHero = Hero(game, p2);
        enemyHero.Properties["currentHp"] = 2;
        var spell = InHand(game, "rune-wordbreaker", p1);
        var manaBefore = Mana(game, p1);

        Assert.False(TryPlay(game, engine, p1, spell, healthy).ok);
        Assert.False(TryPlay(game, engine, p1, spell, enemyHero).ok);
        Assert.Equal(manaBefore, Mana(game, p1)); // nothing was paid for the rejected casts

        Play(game, engine, p1, spell, wounded);

        Assert.True(wounded.IsDestroyed);
        Assert.False(healthy.IsDestroyed);
        Assert.False(enemyHero.IsDestroyed);
        Assert.Equal(manaBefore - 2, Mana(game, p1));
    }

    [Fact]
    public void Conflux_ritual_nets_two_mana_in_the_hq_bank_and_respects_the_cap()
    {
        var (game, engine, p1, _) = CreateMatch();
        SetMana(game, p1, 2);
        Play(game, engine, p1, InHand(game, "rune-conflux-ritual", p1));
        Assert.Equal(2 - 1 + 3, Mana(game, p1));

        SetMana(game, p1, 11);
        Play(game, engine, p1, InHand(game, "rune-conflux-ritual", p1));
        Assert.Equal(12, Mana(game, p1)); // 11 - 1 + 3 = 13, clamped to the nexus capacity
    }
}
