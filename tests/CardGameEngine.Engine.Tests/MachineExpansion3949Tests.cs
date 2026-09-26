using System.Reflection;
using System.Text.Json;
using CardGameEngine.Api.Services;
using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;
using Xunit;

namespace CardGameEngine.Engine.Tests;

/// <summary>
/// Task 3949: batch 1 of the Machine faction expansion toward the 200-card baseline
/// (42 -> 74 faction-exclusive cards). PR #52's batch shipped orphaned and mostly used
/// hallucinated effect/trigger vocabulary that silently no-op'd, so this suite is deliberately
/// stricter than the 1604 vocabulary check: it (1) lints every new card's raw JSON against the
/// engine's real schema, (2) proves each one is wired into a Machine precon deck, and (3) drives
/// every new card's actual mechanic through the real RuleEngine (no mocks, no bot AI) and
/// asserts the state change, so a card whose ability quietly does nothing fails here.
/// </summary>
public class MachineExpansion3949Tests
{
    // The 32 cards this batch adds. Order = HQ, hero, modules, units, buildings, spells, casters.
    private static readonly string[] NewCardIds =
    {
        "machine-assembly-hub", "machine-foreman-f7",
        "machine-suppression-autocannon", "machine-cryo-lance", "machine-reactive-plating",
        "machine-salvage-hook", "machine-nanite-weave", "machine-overdrive-governor",
        "machine-scrap-collector", "machine-shield-drone", "machine-repair-swarm", "machine-jammer-bot",
        "machine-recon-skimmer", "machine-breacher-tank", "machine-static-lattice-drone",
        "machine-salvage-titan", "machine-overclocked-runner",
        "machine-habitat-stack", "machine-tesla-pylon", "machine-perimeter-scanner",
        "machine-orbital-uplink", "machine-railgun-emplacement",
        "machine-credit-siphon", "machine-cascade-failure", "machine-mass-repair", "machine-signal-jammer",
        "machine-viral-payload", "machine-data-mine", "machine-demolition-charge", "machine-hot-swap",
        "machine-logic-core", "machine-purge-caster",
    };

    private const string DeckId = "machine-assembly";

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
            // Deterministic board: no opening hand cards (a stray reaction card in the opponent's
            // hand would open a reaction window mid-test). The decks themselves are untouched.
            foreach (var card in game.Objects.Where(o => o.ZoneId == "hand"))
                card.ZoneId = "discard";
        }

        return (game, engine, p1, p2);
    }

    private static int _counter;

    private static ObjectInstance Spawn(GameInstance game, string cardId, PlayerInstance player, string zone)
    {
        var def = game.Definition.Cards.First(c => c.Id == cardId);
        var obj = new ObjectFactory().CreateObjectInstance(game, def, player.Id, zone);
        obj.Id = $"t3949_{++_counter}";
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

    /// <summary>A plain enemy body with deterministic stats (the real town-watch card, restatted).</summary>
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

    /// <summary>Walk the phase machine so it is <paramref name="playerId"/>'s turn start (fires onTurnStart triggers).</summary>
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

    private static int Hp(ObjectInstance o) => o.Properties["currentHp"];
    private static int Energy(PlayerInstance p) => p.Resources.GetValueOrDefault("energy");
    private static int HandCount(GameInstance game, PlayerInstance p) =>
        game.Objects.Count(o => o.OwnerId == p.Id && o.ZoneId == "hand");

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

    // Effect types whose handler resolves its object through ResolveScope("target") when no scope is given.
    private static readonly HashSet<string> TargetByDefault = new()
    {
        "direct_damage", "buff_target_until_end_of_turn", "add_progress", "freeze", "reveal",
        "reveal_attachments", "destroy_infiltrators", "damage_infiltrators", "salvage_module",
    };

    private static IEnumerable<CardDefinition> NewCards => NewCardIds.Select(Card);

    private static IEnumerable<(string where, AbilityDefinition ability)> Abilities(CardDefinition c)
    {
        foreach (var a in c.Abilities) yield return ($"{c.Id}/ability {a.Id}", a);
        if (c.OnPlay != null) yield return ($"{c.Id}/onPlay", c.OnPlay);
    }

    [Fact]
    public void Batch_takes_the_machine_faction_from_42_to_at_least_74_exclusive_cards()
    {
        var before = Definition.Cards.Count(c => c.Faction == "machine" && !NewCardIds.Contains(c.Id));
        var after = Definition.Cards.Count(c => c.Faction == "machine");

        Assert.Equal(42, before); // the corrected baseline task 3949 was refined against
        Assert.Equal(32, NewCardIds.Distinct().Count());
        Assert.All(NewCards, c => Assert.Equal("machine", c.Faction));
        Assert.True(after >= 74, $"expected at least 74 faction-exclusive Machine cards, found {after}");

        // every sub-type keeps growing, not just the common ones
        foreach (var type in new[] { "module", "unit", "building", "spell", "caster" })
            Assert.True(NewCards.Count(c => c.ObjectType == type) >= 2, $"batch adds too few '{type}' cards");
    }

    [Fact]
    public void Every_new_card_uses_only_effects_triggers_and_costs_the_engine_registers()
    {
        var problems = new List<string>();
        foreach (var c in NewCards)
        {
            foreach (var (where, a) in Abilities(c))
            {
                foreach (var e in a.Effects)
                    if (!RegisteredEffects.Contains(e.Type)) problems.Add($"{where}: unregistered effect '{e.Type}'");
                foreach (var cost in a.Costs)
                    if (!RegisteredCostTypes.Contains(cost.Type)) problems.Add($"{where}: unregistered cost '{cost.Type}'");
                Assert.NotEmpty(a.Effects); // an ability with no effects is a silent no-op by construction
            }
            foreach (var t in c.Triggers)
            {
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
        static HashSet<string> Keys(Type t, params string[] extra) =>
            t.GetProperties().Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name)).Concat(extra).ToHashSet();

        var cardKeys = Keys(typeof(CardDefinition));
        var abilityKeys = Keys(typeof(AbilityDefinition));
        var effectKeys = Keys(typeof(EffectDefinition));
        var costKeys = Keys(typeof(CostDefinition));
        var choiceKeys = Keys(typeof(ChoiceDefinition));
        var triggerKeys = Keys(typeof(TriggerDefinition));
        var modKeys = Keys(typeof(AttachModifierDefinition));

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
                    foreach (var e in t.GetProperty("effects").EnumerateArray()) Check(e, effectKeys, id + "/trigger effect");
                }
            if (c.TryGetProperty("attachModifiers", out var mods)) foreach (var m in mods.EnumerateArray()) Check(m, modKeys, id + "/attachModifier");
        }
        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }

    [Fact]
    public void Target_scoped_effects_always_have_something_to_target()
    {
        // heal/direct_damage/freeze... with scope "target" and no ability "choice" resolve a null
        // target and do nothing: the exact silent no-op that hit 15 of the 42 pre-batch Machine cards.
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
    public void Every_new_card_is_referenced_by_a_machine_precon_deck()
    {
        // PR #52's cards shipped orphaned (referenced by no deck). Each of these must be reachable
        // through a precon deck of the machine faction, either in its card list or as its HQ/hero.
        var machineDecks = Definition.Decks.Where(d => d.Faction == "machine").ToList();
        Assert.Contains(machineDecks, d => d.Id == DeckId);
        var orphans = NewCardIds.Where(id => !machineDecks.Any(d =>
            d.Cards.ContainsKey(id) || d.Hq == id || d.Hero == id || d.HqOptions.Contains(id) || d.HeroOptions.Contains(id))).ToList();
        Assert.True(orphans.Count == 0, "orphaned: " + string.Join(", ", orphans));
    }

    [Fact]
    public void Assembly_deck_is_a_legal_60_card_deck_with_its_own_hq_and_hero()
    {
        var deck = Definition.Decks.First(d => d.Id == DeckId);
        Assert.Equal("machine", deck.Faction);
        Assert.Equal(60, deck.Cards.Values.Sum());
        Assert.Null(GameQueries.ValidateDeck(Definition, deck.Cards, isAdmin: false, enforceMinSize: true));

        var hq = Card(deck.Hq);
        var hero = Card(deck.Hero);
        Assert.True(GameQueries.IsObjectTypeOrSubtype(Definition, hq.ObjectType, "headquarters"));
        Assert.True(GameQueries.IsObjectTypeOrSubtype(Definition, hero.ObjectType, "hero"));
        Assert.True(hq.HousingProvided > 0, "units in this deck need housing; the HQ must provide some");
        Assert.Contains(deck.Hq, deck.HqOptions);
        Assert.Contains(deck.Hero, deck.HeroOptions);
    }

    [Fact]
    public void New_cards_are_deck_eligible_and_paid_in_energy_because_machine_has_no_gold_income()
    {
        // The Machine HQs only ever produce energy; a gold-priced Machine card is uncastable in
        // a real match (the pre-batch Logistics Coordinator costs the very gold it produces).
        foreach (var c in NewCards)
        {
            Assert.True(GameQueries.IsDeckEligible(Definition, c), $"{c.Id} is not deck-eligible");
            var costs = GameQueries.BasePlayCosts(c);
            if (c.ObjectType == "hero") { Assert.Empty(costs); continue; }
            Assert.Equal(new[] { "energy" }, costs.Keys.ToArray());
            Assert.True(costs["energy"] >= 1, $"{c.Id} has no energy cost");
        }
    }

    [Fact]
    public void New_cards_are_role_distinct_from_each_other_and_from_the_pre_batch_machine_cards()
    {
        // A structural fingerprint (object type + which effect types run on which event) - not stats.
        // Two cards with the same fingerprint would be the "reskinned clone under a new name" the
        // task forbids; this fails the moment a future batch copy-pastes an existing role.
        string Fingerprint(CardDefinition c)
        {
            var parts = new List<string> { c.ObjectType };
            foreach (var a in c.Abilities) parts.Add("ability:" + string.Join("+", a.Effects.Select(e => e.Type).OrderBy(x => x)));
            if (c.OnPlay != null) parts.Add("onPlay:" + string.Join("+", c.OnPlay.Effects.Select(e => e.Type).OrderBy(x => x)));
            foreach (var t in c.Triggers) parts.Add($"{t.Event}:" + string.Join("+", t.Effects.Select(e => e.Type).OrderBy(x => x)));
            if (c.Slot != null) parts.Add("slot:" + c.Slot);
            if (c.BonusAttackVsBuildings != null) parts.Add("bonusVsBuildings");
            return string.Join("|", parts.OrderBy(x => x, StringComparer.Ordinal));
        }

        var existing = Definition.Cards
            .Where(c => c.Faction == "machine" && !NewCardIds.Contains(c.Id) && (c.Abilities.Count + c.Triggers.Count > 0 || c.OnPlay != null))
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

    // ------------------------------------------------------------------ economy premise

    [Fact]
    public void Opening_energy_pays_for_a_two_drop_on_turn_one_and_the_hub_provides_the_housing()
    {
        // No resource cheating here: the real starting economy of the new deck.
        var (game, engine, p1, _) = CreateMatch(richResources: false);
        Assert.Equal(2, Energy(p1)); // Assembly Hub's onPlay residual charge at setup

        var hub = Hq(game, p1);
        Assert.Equal(5, GameQueries.HousingCapacity(game, p1.Id));
        game.State = GameState.WaitingForAction;
        game.CurrentPhaseId = "main";
        Use(game, engine, p1, hub, "assembly-hub-recharge");
        Assert.Equal(5, Energy(p1)); // +3 per Recharge

        var skimmer = InHand(game, "machine-recon-skimmer", p1);
        var handBefore = HandCount(game, p1);
        Play(game, engine, p1, skimmer);

        Assert.Equal("battlefield", skimmer.ZoneId);
        Assert.Equal(3, Energy(p1)); // 5 - 2
        Assert.Equal(handBefore, HandCount(game, p1)); // -1 (played) +1 (Skimmer's onPlay draw)
    }

    // ------------------------------------------------------------------ headquarters + hero

    [Fact]
    public void Assembly_hub_queue_pays_3_energy_to_draw_2_cards()
    {
        var (game, engine, p1, _) = CreateMatch();
        var hub = Hq(game, p1);
        var handBefore = HandCount(game, p1);

        Use(game, engine, p1, hub, "assembly-hub-queue");

        Assert.Equal(96, Energy(p1));
        Assert.Equal(handBefore + 2, HandCount(game, p1));
        Assert.True(hub.IsTapped);
    }

    [Fact]
    public void Foreman_directive_rallies_every_own_unit_for_2_ap()
    {
        var (game, engine, p1, _) = CreateMatch();
        var foreman = Hero(game, p1);
        Assert.Equal("machine-foreman-f7", foreman.DefinitionId);
        var a = Body(game, p1, 2, 3);
        var b = Body(game, p1, 3, 3);
        foreman.Resources["ap"] = 2;

        Use(game, engine, p1, foreman, "foreman-f7-directive");

        Assert.Equal(3, GameQueries.GetEffectiveProperty(game, a, "attack"));
        Assert.Equal(4, GameQueries.GetEffectiveProperty(game, b, "attack"));
        Assert.Equal(0, foreman.Resources["ap"]);
    }

    [Fact]
    public void Foreman_requisition_draws_a_card_and_refunds_2_energy_for_3_ap_and_a_tap()
    {
        var (game, engine, p1, _) = CreateMatch();
        var foreman = Hero(game, p1);
        foreman.Resources["ap"] = 3;
        p1.Resources["energy"] = 0;
        var handBefore = HandCount(game, p1);

        Use(game, engine, p1, foreman, "foreman-f7-requisition");

        Assert.Equal(2, Energy(p1));
        Assert.Equal(handBefore + 1, HandCount(game, p1));
        Assert.True(foreman.IsTapped);
    }

    // ------------------------------------------------------------------ modules

    private static ObjectInstance Install(GameInstance game, RuleEngine engine, PlayerInstance p, string moduleId)
    {
        var module = InHand(game, moduleId, p);
        Play(game, engine, p, module);
        Assert.Equal(Hero(game, p).Id, module.AttachedToId);
        Assert.Equal("battlefield", module.ZoneId);
        return module;
    }

    [Fact]
    public void Suppression_autocannon_hits_only_the_enemy_front_line()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var hero = Hero(game, p1);
        var atkBefore = GameQueries.GetEffectiveProperty(game, hero, "attack");
        var cannon = Install(game, engine, p1, "machine-suppression-autocannon");
        Assert.Equal(atkBefore + 1, GameQueries.GetEffectiveProperty(game, hero, "attack"));
        var front = Body(game, p2, 1, 5);
        var back = Body(game, p2, 1, 5, line: "back");

        Use(game, engine, p1, cannon, "suppression-autocannon-fire");

        Assert.Equal(3, Hp(front));
        Assert.Equal(5, Hp(back));
        Assert.True(cannon.IsTapped);
        Assert.Equal(99 - 3 - 1, Energy(p1)); // 3 to install + 1 to fire
    }

    [Fact]
    public void Cryo_lance_freezes_whatever_the_hero_hits()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var hero = Hero(game, p1);
        hero.Line = "front";
        Install(game, engine, p1, "machine-cryo-lance");
        var victim = Body(game, p2, 1, 10);

        Attack(game, engine, hero, victim);

        Assert.True(victim.IsTapped);
        Assert.True(victim.SkipNextUntap);
        Assert.True(Hp(victim) < 10);
    }

    [Fact]
    public void Reactive_plating_punches_back_through_armor_when_the_hero_is_struck()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var hero = Hero(game, p1);
        hero.Line = "front";
        Install(game, engine, p1, "machine-reactive-plating");
        var attacker = Body(game, p2, 2, 6, armor: 2);

        Attack(game, engine, attacker, hero);

        Assert.Equal(4, Hp(attacker)); // 2 direct damage ignores the attacker's 2 armor
    }

    [Fact]
    public void Salvage_hook_turns_a_hero_kill_into_2_energy()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var hero = Hero(game, p1);
        hero.Line = "front";
        Install(game, engine, p1, "machine-salvage-hook");
        p1.Resources["energy"] = 0;
        var prey = Body(game, p2, 1, 1);

        Attack(game, engine, hero, prey);

        Assert.True(prey.IsDestroyed);
        Assert.Equal(2, Energy(p1));
    }

    [Fact]
    public void Nanite_weave_heals_the_host_hero_2_at_the_start_of_every_own_turn()
    {
        var (game, engine, p1, _) = CreateMatch();
        var hero = Hero(game, p1);
        Install(game, engine, p1, "machine-nanite-weave");
        hero.Properties["currentHp"] = 5;

        StartTurnOf(game, engine, p1);

        Assert.Equal(7, Hp(hero));
    }

    [Fact]
    public void Overdrive_governor_boosts_the_hero_permanently_and_bursts_for_3_more_until_end_of_turn()
    {
        var (game, engine, p1, _) = CreateMatch();
        var hero = Hero(game, p1);
        var baseAttack = GameQueries.GetEffectiveProperty(game, hero, "attack");
        var governor = Install(game, engine, p1, "machine-overdrive-governor");
        Assert.Equal(baseAttack + 1, GameQueries.GetEffectiveProperty(game, hero, "attack"));

        Use(game, engine, p1, governor, "overdrive-governor-override");

        Assert.Equal(baseAttack + 1 + 3, GameQueries.GetEffectiveProperty(game, hero, "attack"));
        Assert.True(governor.IsTapped);
    }

    // ------------------------------------------------------------------ units

    [Fact]
    public void Scrap_collector_earns_1_energy_whenever_any_unit_dies()
    {
        var (game, engine, p1, p2) = CreateMatch();
        OnField(game, "machine-scrap-collector", p1);
        var hitter = Body(game, p1, 5, 5);
        var victim = Body(game, p2, 1, 1);
        p1.Resources["energy"] = 0;

        Attack(game, engine, hitter, victim);

        Assert.True(victim.IsDestroyed);
        Assert.Equal(1, Energy(p1));
    }

    [Fact]
    public void Shield_drone_lends_2_armor_to_an_ally_until_end_of_turn()
    {
        var (game, engine, p1, _) = CreateMatch();
        var drone = OnField(game, "machine-shield-drone", p1);
        var ally = Body(game, p1, 2, 4, armor: 0);

        Use(game, engine, p1, drone, "shield-drone-projector", ally);

        Assert.Equal(2, GameQueries.GetEffectiveProperty(game, ally, "armor"));
        Assert.True(drone.IsTapped);
        game.CurrentPhaseId = "combat";
        engine.EndPhase(game, p1.Id); // -> end phase expires end-of-turn modifiers
        Assert.Equal(0, GameQueries.GetEffectiveProperty(game, ally, "armor"));
    }

    [Fact]
    public void Repair_swarm_mends_every_own_unit_1_hp_each_turn_start()
    {
        var (game, engine, p1, _) = CreateMatch();
        OnField(game, "machine-repair-swarm", p1);
        var hurt = Body(game, p1, 1, 5);
        hurt.Properties["currentHp"] = 2;

        StartTurnOf(game, engine, p1);

        Assert.Equal(3, Hp(hurt));
    }

    [Fact]
    public void Jammer_bot_jams_every_enemy_unit_when_it_dies()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var bot = OnField(game, "machine-jammer-bot", p1);
        var attacker = Body(game, p2, 4, 6);
        var bystander = Body(game, p2, 1, 6);
        Assert.False(bystander.IsTapped);

        Attack(game, engine, attacker, bot);

        Assert.True(bot.IsDestroyed);
        Assert.True(bystander.IsTapped); // the whole enemy line is tapped out by the death burst
    }

    [Fact]
    public void Recon_skimmer_draws_a_card_when_played()
    {
        var (game, engine, p1, _) = CreateMatch();
        var skimmer = InHand(game, "machine-recon-skimmer", p1);
        var handBefore = HandCount(game, p1); // includes the Skimmer

        Play(game, engine, p1, skimmer);

        Assert.Equal(handBefore, HandCount(game, p1)); // -1 played, +1 drawn
    }

    [Fact]
    public void Breacher_tank_shreds_buildings_and_scavenges_energy_from_the_wreck()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var tank = OnField(game, "machine-breacher-tank", p1);
        var house = OnField(game, "house", p2, line: "front");
        house.Properties["currentHp"] = 6;
        house.Properties["armor"] = 0;
        p1.Resources["energy"] = 0;

        Attack(game, engine, tank, house);

        Assert.True(house.IsDestroyed); // 3 attack + 3 bonus vs buildings = 6
        Assert.Equal(2, Energy(p1));
    }

    [Fact]
    public void Static_lattice_drone_freezes_whatever_hits_it()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var drone = OnField(game, "machine-static-lattice-drone", p1);
        var attacker = Body(game, p2, 1, 8);

        Attack(game, engine, attacker, drone);

        Assert.True(attacker.IsTapped);
        Assert.True(attacker.SkipNextUntap);
    }

    [Fact]
    public void Salvage_titan_refunds_4_energy_when_destroyed()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var titan = OnField(game, "machine-salvage-titan", p1);
        var attacker = Body(game, p2, 10, 8);
        p1.Resources["energy"] = 0;

        Attack(game, engine, attacker, titan);

        Assert.True(titan.IsDestroyed);
        Assert.Equal(4, Energy(p1));
    }

    [Fact]
    public void Overclocked_runner_untaps_after_a_kill_and_chains_a_second_attack()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var runner = OnField(game, "machine-overclocked-runner", p1);
        var first = Body(game, p2, 1, 1);
        var second = Body(game, p2, 1, 1);

        Attack(game, engine, runner, first);
        Assert.True(first.IsDestroyed);
        Assert.False(runner.IsTapped);

        Attack(game, engine, runner, second);
        Assert.True(second.IsDestroyed);
    }

    // ------------------------------------------------------------------ buildings

    [Fact]
    public void Habitat_stack_adds_4_housing()
    {
        var (game, engine, p1, _) = CreateMatch();
        var before = GameQueries.HousingCapacity(game, p1.Id);

        Play(game, engine, p1, InHand(game, "machine-habitat-stack", p1));

        Assert.Equal(before + 4, GameQueries.HousingCapacity(game, p1.Id));
    }

    [Fact]
    public void Tesla_pylon_zaps_attackers_for_2_ignoring_armor()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var pylon = OnField(game, "machine-tesla-pylon", p1, line: "front");
        var attacker = Body(game, p2, 1, 6, armor: 2);

        Attack(game, engine, attacker, pylon);

        Assert.Equal(4, Hp(attacker));
    }

    [Fact]
    public void Perimeter_scanner_exposes_and_damages_infiltrators_hiding_in_a_building()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var scanner = OnField(game, "machine-perimeter-scanner", p1);
        var hub = Hq(game, p1);
        var spy = Body(game, p2, 1, 5);
        spy.AttachedToId = hub.Id;
        spy.Slot = "infiltrated";
        spy.FaceDown = true;

        Use(game, engine, p1, scanner, "perimeter-scanner-sweep", hub);

        Assert.False(spy.FaceDown);
        Assert.Equal(2, Hp(spy));
    }

    [Fact]
    public void Orbital_uplink_chips_the_enemy_hq_1_hp_at_every_own_turn_start()
    {
        var (game, engine, p1, p2) = CreateMatch();
        OnField(game, "machine-orbital-uplink", p1, line: "back");
        var enemyHq = Hq(game, p2);
        var before = Hp(enemyHq);

        StartTurnOf(game, engine, p1);

        Assert.Equal(before - 1, Hp(enemyHq));
    }

    [Fact]
    public void Railgun_emplacement_pierces_armor_for_3_and_refuses_non_unit_targets()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var railgun = OnField(game, "machine-railgun-emplacement", p1);
        var armored = Body(game, p2, 1, 6, armor: 2);

        var (badOk, _) = TryUse(game, engine, p1, railgun, "railgun-emplacement-fire", Hq(game, p2));
        Assert.False(badOk); // a unit-only weapon

        Use(game, engine, p1, railgun, "railgun-emplacement-fire", armored);

        Assert.Equal(3, Hp(armored));
        Assert.True(railgun.IsTapped);
    }

    // ------------------------------------------------------------------ spells

    [Fact]
    public void Credit_siphon_moves_3_gold_from_the_enemy_to_you()
    {
        var (game, engine, p1, p2) = CreateMatch();
        p1.Resources["gold"] = 0;
        p2.Resources["gold"] = 5;

        Play(game, engine, p1, InHand(game, "machine-credit-siphon", p1));

        Assert.Equal(2, p2.Resources["gold"]);
        Assert.Equal(3, p1.Resources["gold"]);
    }

    [Fact]
    public void Cascade_failure_hits_every_unit_on_both_sides_through_armor()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var mine = Body(game, p1, 1, 5, armor: 0);
        var theirs = Body(game, p2, 1, 5, armor: 1);

        Play(game, engine, p1, InHand(game, "machine-cascade-failure", p1));

        Assert.Equal(2, Hp(mine));
        Assert.Equal(3, Hp(theirs));
    }

    [Fact]
    public void Mass_repair_heals_every_own_unit_3()
    {
        var (game, engine, p1, _) = CreateMatch();
        var a = Body(game, p1, 1, 6);
        var b = Body(game, p1, 1, 6);
        a.Properties["currentHp"] = 1;
        b.Properties["currentHp"] = 2;

        Play(game, engine, p1, InHand(game, "machine-mass-repair", p1));

        Assert.Equal(4, Hp(a));
        Assert.Equal(5, Hp(b));
    }

    [Fact]
    public void Signal_jammer_taps_every_enemy_unit()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var a = Body(game, p2, 1, 3);
        var b = Body(game, p2, 1, 3);

        Play(game, engine, p1, InHand(game, "machine-signal-jammer", p1));

        Assert.True(a.IsTapped);
        Assert.True(b.IsTapped);
    }

    [Fact]
    public void Viral_payload_poisons_every_enemy_unit_and_the_poison_bites_on_their_turn()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var a = Body(game, p2, 1, 6);
        var b = Body(game, p2, 1, 6);

        Play(game, engine, p1, InHand(game, "machine-viral-payload", p1));
        Assert.Equal(2, a.Resources["poison"]);
        Assert.Equal(2, b.Resources["poison"]);

        game.ActivePlayerId = p2.Id;
        game.CurrentPhaseId = "combat";
        engine.EndPhase(game, p2.Id); // entering their end phase ticks poison
        Assert.Equal(4, Hp(a));
        Assert.Equal(1, a.Resources["poison"]);
    }

    [Fact]
    public void Data_mine_draws_two_then_discards_one_of_your_own_cards_never_itself()
    {
        // A spell stays in hand while its effects resolve, and discard_cards picks at random, so
        // before the DefaultHandlers fix Data Mine could pick itself (1 in 7). Loop to make that
        // a near-certain catch instead of a flake.
        for (int i = 0; i < 40; i++)
        {
            var (game, engine, p1, _) = CreateMatch();
            var mine = InHand(game, "machine-data-mine", p1);
            var handBefore = HandCount(game, p1); // includes Data Mine
            var discardBefore = game.Objects.Count(o => o.OwnerId == p1.Id && o.ZoneId == "discard");

            Play(game, engine, p1, mine);

            Assert.Equal(handBefore, HandCount(game, p1)); // -1 cast, +2 drawn, -1 discarded
            Assert.Equal(discardBefore + 2, game.Objects.Count(o => o.OwnerId == p1.Id && o.ZoneId == "discard"));
        }
    }

    [Fact]
    public void Demolition_charge_pierces_armor_for_6_against_a_building_and_refuses_units()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var wall = OnField(game, "house", p2, line: "back");
        wall.Properties["currentHp"] = 8;
        wall.Properties["maxHp"] = 8;
        wall.Properties["armor"] = 2;
        var unit = Body(game, p2, 1, 5);
        var charge = InHand(game, "machine-demolition-charge", p1);

        var (badOk, _) = engine.ExecuteAction(game, p1.Id, new ActionRequest
        {
            Type = "playCard", SourceObjectId = charge.Id, TargetIds = new List<string> { unit.Id }
        });
        Assert.False(badOk); // buildings only
        Assert.Equal("hand", charge.ZoneId);

        Play(game, engine, p1, charge, wall);

        Assert.Equal(2, Hp(wall)); // 6 direct damage ignores the 2 armor
        Assert.Equal(5, Hp(unit));
    }

    [Fact]
    public void Hot_swap_untaps_a_tapped_character_and_refunds_1_energy()
    {
        var (game, engine, p1, _) = CreateMatch();
        var hero = Hero(game, p1);
        hero.IsTapped = true;
        p1.Resources["energy"] = 1;

        Play(game, engine, p1, InHand(game, "machine-hot-swap", p1), hero);

        Assert.False(hero.IsTapped);
        Assert.Equal(1, Energy(p1)); // paid 1, refunded 1
    }

    // ------------------------------------------------------------------ casters

    [Fact]
    public void Logic_core_permanently_saps_1_attack_from_every_enemy_unit()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var core = OnField(game, "machine-logic-core", p1);
        var strong = Body(game, p2, 3, 4);
        var toothless = Body(game, p2, 0, 4);

        Use(game, engine, p1, core, "logic-core-corrupt");

        Assert.Equal(2, strong.Properties["attack"]);
        Assert.Equal(0, toothless.Properties["attack"]); // floored, never negative
        Assert.True(core.IsTapped);
    }

    [Fact]
    public void Purge_caster_destroys_frail_units_and_cannot_touch_sturdy_ones()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var caster = OnField(game, "machine-purge-caster", p1);
        var frail = Body(game, p2, 1, 2);
        var sturdy = Body(game, p2, 1, 3);

        var (badOk, _) = TryUse(game, engine, p1, caster, "purge-caster-purge", sturdy);
        Assert.False(badOk); // maxHp 3 is above the maxHp<=2 execute threshold
        Assert.False(sturdy.IsDestroyed);

        Use(game, engine, p1, caster, "purge-caster-purge", frail);
        Assert.True(frail.IsDestroyed);
    }

    // ------------------------------------------------------------------ live bot-vs-bot smoke

    [Theory]
    [InlineData("town")]
    [InlineData("machine")]
    [InlineData("raiders")]
    [InlineData("undead")]
    public void Assembly_deck_plays_real_bot_matches_without_errors_and_actually_casts_its_new_cards(string opponentDeck)
    {
        // Real economy (no free resources), real bots, alternating first player - same loop the
        // /api/simulate endpoint runs. Proves the deck is not merely legal but playable: the new
        // cards must be castable from the deck's own energy income.
        var castNewCards = new HashSet<string>();
        int games = 6;
        for (int i = 0; i < games; i++)
        {
            bool assemblyFirst = i % 2 == 0;
            var deckIds = assemblyFirst ? (DeckId, opponentDeck) : (opponentDeck, DeckId);
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

            var assemblyPlayer = assemblyFirst ? p1 : p2;
            foreach (var o in game.Objects.Where(o => o.OwnerId == assemblyPlayer.Id
                         && NewCardIds.Contains(o.DefinitionId) && o.ZoneId is "battlefield" or "discard"))
                castNewCards.Add(o.DefinitionId);
            Assert.DoesNotContain(game.Log, l => l.Contains("Exception", StringComparison.OrdinalIgnoreCase));
        }

        Assert.True(castNewCards.Count >= 6,
            $"only {castNewCards.Count} distinct new cards were ever cast across {games} games vs {opponentDeck}: {string.Join(", ", castNewCards)}");
    }
}
