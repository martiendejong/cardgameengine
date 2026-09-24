using System.Text.Json;
using CardGameEngine.Api.Services;
using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;
using Xunit;

namespace CardGameEngine.Engine.Tests;

/// <summary>
/// Task 3951: batch 1 of the Undead faction expansion toward the 200-card baseline
/// (42 -> 74 faction-exclusive cards). PR #52's batch shipped orphaned and mostly used
/// hallucinated effect/trigger vocabulary that silently no-op'd, so this suite is deliberately
/// strict: it (1) lints every new card's raw JSON against the engine's real schema, (2) proves
/// each one is wired into an Undead precon deck, and (3) drives every new card's actual mechanic
/// through the real RuleEngine (no mocks, no bot AI) and asserts the state change, so a card
/// whose ability quietly does nothing fails here.
/// </summary>
public class UndeadExpansion3951Tests
{
    // The 32 cards this batch adds. Order = HQ, hero, units, buildings, spells, equipment.
    private static readonly string[] NewCardIds =
    {
        "undead-bone-citadel", "undead-grave-regent",
        "undead-carrion-ghoul", "undead-cairn-sentinel", "undead-pyre-zealot", "undead-barrow-sentinel",
        "undead-grave-vulture", "undead-marrow-leech", "undead-ravenous-revenant", "undead-withering-shade",
        "undead-grave-broker", "undead-bone-drover", "undead-hexbolt-acolyte", "undead-dirge-bearer",
        "undead-thorn-ossuary", "undead-miasma-well", "undead-bone-cradle", "undead-cursed-archive",
        "undead-sepulchral-shrine",
        "undead-skeletal-warband", "undead-grave-hush", "undead-cascade-of-bone", "undead-unquiet-grave",
        "undead-spirit-ward", "undead-plague-snare", "undead-blood-pact", "undead-crumbling-curse",
        "undead-mend-the-crypt",
        "undead-soulreaper-scythe", "undead-wraithmail", "undead-coffin-lid-bulwark", "undead-phylactery-shard",
    };

    private const string DeckId = "undead-legion";

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
            {
                foreach (var resDef in Definition.Resources.Where(r => r.Scope == "player"))
                    player.Resources[resDef.Id] = 99;
                // Undead pay in corpses, an HQ-bank resource: stock any graveyard-type HQ.
                var bank = GameQueries.FindResourceBank(game, player.Id);
                if (bank != null && bank.Resources.ContainsKey("corpses"))
                    bank.Resources["corpses"] = 10;
            }
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
        obj.Id = $"t3951_{++_counter}";
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

    /// <summary>A plain body with deterministic stats (the real town-watch card, restated).</summary>
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

    private static int Corpses(GameInstance game, PlayerInstance p) => Hq(game, p).Resources.GetValueOrDefault("corpses");

    /// <summary>The Citadel banks a corpse the first time any unit dies each turn; tests that measure
    /// one card's own corpse income mark that trigger as already spent so it cannot muddy the count.</summary>
    private static void SilenceCitadel(GameInstance game, PlayerInstance p) =>
        game.FiredOncePerTurn.Add($"{Hq(game, p).Id}:onUnitDied");

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

    /// <summary>Walk the phase machine so it is p's turn start (fires onTurnStart triggers).</summary>
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
    private static int Atk(GameInstance game, ObjectInstance o) => GameQueries.GetEffectiveProperty(game, o, "attack");
    private static int HandCount(GameInstance game, PlayerInstance p) =>
        game.Objects.Count(o => o.OwnerId == p.Id && o.ZoneId == "hand");
    private static int Skeletons(GameInstance game, PlayerInstance p) =>
        game.Objects.Count(o => o.OwnerId == p.Id && o.DefinitionId == "skeleton" && o.ZoneId == "battlefield" && !o.IsDestroyed);

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

    // ConditionEvaluator treats an unregistered condition type as TRUE, so a hallucinated one is a silent no-gate.
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

    private static IEnumerable<CardDefinition> NewCards => NewCardIds.Select(Card);

    private static IEnumerable<(string where, AbilityDefinition ability)> Abilities(CardDefinition c)
    {
        foreach (var a in c.Abilities) yield return ($"{c.Id}/ability {a.Id}", a);
        if (c.OnPlay != null) yield return ($"{c.Id}/onPlay", c.OnPlay);
    }

    [Fact]
    public void Batch_takes_the_undead_faction_from_42_to_74_exclusive_cards()
    {
        var before = Definition.Cards.Count(c => c.Faction == "undead" && !NewCardIds.Contains(c.Id));
        var after = Definition.Cards.Count(c => c.Faction == "undead");

        Assert.Equal(42, before); // the corrected baseline task 3951 was refined against
        Assert.Equal(32, NewCardIds.Distinct().Count());
        Assert.All(NewCards, c => Assert.Equal("undead", c.Faction));
        Assert.Equal(74, after);
        Assert.Equal(Definition.Cards.Count, Definition.Cards.Select(c => c.Id).Distinct().Count()); // no id collisions

        // every sub-type keeps growing, not just the common ones
        foreach (var type in new[] { "unit", "building", "spell", "equipment" })
            Assert.True(NewCards.Count(c => c.ObjectType == type) >= 2, $"batch adds too few '{type}' cards");
        Assert.Single(NewCards, c => c.ObjectType == "graveyard-hq");
        Assert.Single(NewCards, c => c.ObjectType == "necromancer-hero");
    }

    [Fact]
    public void Every_new_card_uses_only_effects_triggers_costs_and_conditions_the_engine_registers()
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
                foreach (var cond in a.Conditions)
                    if (!RegisteredConditionTypes.Contains(cond.Type)) problems.Add($"{where}: unregistered condition '{cond.Type}'");
                Assert.NotEmpty(a.Effects); // an ability with no effects is a silent no-op by construction
            }
            foreach (var t in c.Triggers)
            {
                if (!FiredTriggerEvents.ContainsKey(t.Event)) problems.Add($"{c.Id}/trigger: unfired event '{t.Event}'");
                foreach (var cond in t.Conditions)
                    if (!RegisteredConditionTypes.Contains(cond.Type)) problems.Add($"{c.Id}/trigger {t.Event}: unregistered condition '{cond.Type}'");
                Assert.NotEmpty(t.Effects);
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
            t.GetProperties().Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name)).ToHashSet();

        var cardKeys = Keys(typeof(CardDefinition));
        var abilityKeys = Keys(typeof(AbilityDefinition));
        var effectKeys = Keys(typeof(EffectDefinition));
        var costKeys = Keys(typeof(CostDefinition));
        var condKeys = Keys(typeof(ConditionDefinition));
        var choiceKeys = Keys(typeof(ChoiceDefinition));
        var triggerKeys = Keys(typeof(TriggerDefinition));
        var modKeys = Keys(typeof(AttachModifierDefinition));

        var problems = new List<string>();
        void Check(JsonElement el, HashSet<string> allowed, string where)
        {
            foreach (var p in el.EnumerateObject())
                if (!allowed.Contains(p.Name)) problems.Add($"{where}: unknown key '{p.Name}'");
        }
        void CheckList(JsonElement parent, string key, HashSet<string> allowed, string where)
        {
            if (parent.TryGetProperty(key, out var list))
                foreach (var item in list.EnumerateArray()) Check(item, allowed, where);
        }
        void CheckAbility(JsonElement a, string where)
        {
            Check(a, abilityKeys, where);
            CheckList(a, "costs", costKeys, where + " cost");
            CheckList(a, "conditions", condKeys, where + " condition");
            CheckList(a, "effects", effectKeys, where + " effect");
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
                    CheckList(t, "conditions", condKeys, id + "/trigger condition");
                    CheckList(t, "effects", effectKeys, id + "/trigger effect");
                }
            CheckList(c, "attachModifiers", modKeys, id + "/attachModifier");
        }
        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }

    [Fact]
    public void Target_scoped_effects_always_have_something_to_target()
    {
        // heal/direct_damage/freeze... with scope "target" and no ability "choice" resolve a null
        // target and do nothing: the silent no-op that hit many pre-existing cards.
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
    public void Every_new_card_is_referenced_by_an_undead_precon_deck()
    {
        // PR #52's cards shipped orphaned (referenced by no deck). Each of these must be reachable
        // through a precon deck of the undead faction, either in its card list or as its HQ/hero.
        var undeadDecks = Definition.Decks.Where(d => d.Faction == "undead").ToList();
        Assert.Contains(undeadDecks, d => d.Id == DeckId);
        var orphans = NewCardIds.Where(id => !undeadDecks.Any(d =>
            d.Cards.ContainsKey(id) || d.Hq == id || d.Hero == id || d.HqOptions.Contains(id) || d.HeroOptions.Contains(id))).ToList();
        Assert.True(orphans.Count == 0, "orphaned: " + string.Join(", ", orphans));
    }

    [Fact]
    public void Legion_deck_is_a_legal_60_card_deck_with_its_own_hq_and_hero()
    {
        var deck = Definition.Decks.First(d => d.Id == DeckId);
        Assert.Equal("undead", deck.Faction);
        Assert.Equal(60, deck.Cards.Values.Sum());
        Assert.Null(GameQueries.ValidateDeck(Definition, deck.Cards, isAdmin: false, enforceMinSize: true));

        var hq = Card(deck.Hq);
        var hero = Card(deck.Hero);
        Assert.True(GameQueries.IsObjectTypeOrSubtype(Definition, hq.ObjectType, "headquarters"));
        Assert.True(GameQueries.IsObjectTypeOrSubtype(Definition, hero.ObjectType, "hero"));
        Assert.Contains(deck.Hq, deck.HqOptions);
        Assert.Contains(deck.Hero, deck.HeroOptions);
        Assert.All(deck.Cards.Keys, id => Assert.Contains(Definition.Cards, c => c.Id == id));

        // units in this deck need housing; the HQ (plus buildings) must provide enough for every one of them
        var largest = deck.Cards.Keys.Select(Card).Where(c => c.HousingCost != null).Max(c => c.HousingCost!.Value);
        Assert.True(hq.HousingProvided >= largest, "the HQ must house the deck's biggest unit on its own");
    }

    [Fact]
    public void New_cards_are_deck_eligible_and_paid_in_corpses_the_resource_the_graveyard_produces()
    {
        // Undead has no gold income: a gold-priced Undead card is uncastable in a real match, so every
        // new castable card must be priced in corpses (an HQ-bank resource paid straight from the Citadel).
        foreach (var c in NewCards)
        {
            Assert.True(GameQueries.IsDeckEligible(Definition, c), $"{c.Id} is not deck-eligible");
            var costs = GameQueries.BasePlayCosts(c);
            if (c.ObjectType is "necromancer-hero" or "graveyard-hq") { Assert.Empty(costs); continue; }
            Assert.Equal(new[] { "corpses" }, costs.Keys.ToArray());
            Assert.True(costs["corpses"] >= 1, $"{c.Id} has no corpse cost");
            Assert.True(costs["corpses"] <= 5, $"{c.Id} costs more than the Citadel's income can reasonably pay for");
        }

        // and every new unit is housed like the existing Undead units, so housing still means something
        foreach (var c in NewCards.Where(c => c.ObjectType == "unit"))
            Assert.InRange(c.HousingCost ?? 0, 1, 2);
    }

    [Fact]
    public void New_cards_are_role_distinct_from_each_other_and_from_every_existing_undead_card()
    {
        // A structural fingerprint - object type, keyword tags, and which effect (type/scope/resource/
        // property) runs on which trigger/ability/cost/target - not stats. Two cards with the same
        // fingerprint would be the "reskinned clone under a new name" the task forbids; this fails the
        // moment a future batch copy-pastes an existing role.
        static string E(EffectDefinition e) =>
            string.Join(":", e.Type, e.Scope ?? "", e.ResourceId ?? "", e.PropertyId ?? "", e.Tag ?? "", e.Line ?? "");
        static string Effs(IEnumerable<EffectDefinition> effects) =>
            string.Join("+", effects.Select(E).OrderBy(x => x, StringComparer.Ordinal));
        static string Ab(AbilityDefinition a) =>
            "costs[" + string.Join("+", a.Costs.Select(x => $"{x.Type}:{x.Scope}:{x.ResourceId}").OrderBy(x => x, StringComparer.Ordinal)) + "]"
            + "cond[" + string.Join("+", a.Conditions.Select(x => x.Type).OrderBy(x => x, StringComparer.Ordinal)) + "]"
            + "choice[" + (a.Choice == null ? "-" : $"{a.Choice.Controller}/{a.Choice.ObjectType}/{a.Choice.Tag}") + "]"
            + "uses[" + a.UsesPerTurn + "]=" + Effs(a.Effects);

        var keywordTags = new[] { "guard", "blocking", "retaliate", "cleave", "splash", "ranged", "siege", "fortification", "builder", "worker" };
        string Fingerprint(CardDefinition c)
        {
            var parts = new List<string> { c.ObjectType };
            parts.AddRange(c.Tags.Where(keywordTags.Contains).OrderBy(x => x, StringComparer.Ordinal).Select(t => "tag:" + t));
            foreach (var a in c.Abilities) parts.Add("ability:" + Ab(a));
            if (c.OnPlay != null) parts.Add("onPlay:" + Ab(c.OnPlay));
            foreach (var t in c.Triggers)
                parts.Add($"trigger:{t.Event}:{(t.OncePerTurn ? "once" : "each")}:cond[{string.Join("+", t.Conditions.Select(x => x.Type).OrderBy(x => x, StringComparer.Ordinal))}]={Effs(t.Effects)}");
            if (c.Slots != null) parts.Add("slots:" + string.Join("+", c.Slots));
            if (c.Slot != null) parts.Add("slot:" + c.Slot);
            if (c.AttachTags.Count > 0) parts.Add("attachTags:" + string.Join("+", c.AttachTags.OrderBy(x => x, StringComparer.Ordinal)));
            if (c.AttachModifiers.Count > 0) parts.Add("mods:" + string.Join("+", c.AttachModifiers.Select(m => m.PropertyId).OrderBy(x => x, StringComparer.Ordinal)));
            if (c.Timing != "main") parts.Add("timing:" + c.Timing + ":" + string.Join("+", c.ReactionTo));
            if (c.IsSecret) parts.Add("secret");
            if (c.BonusAttackVsBuildings != null) parts.Add("bonusVsBuildings");
            return string.Join("|", parts.OrderBy(x => x, StringComparer.Ordinal));
        }

        // every card an Undead precon deck could already field (faction-exclusive or shared-pool)
        var existingIds = Definition.Cards.Where(c => c.Faction == "undead").Select(c => c.Id)
            .Concat(Definition.Decks.Where(d => d.Faction == "undead").SelectMany(d => d.Cards.Keys.Append(d.Hq).Append(d.Hero)))
            .Where(id => !NewCardIds.Contains(id))
            .Distinct().ToList();
        Assert.True(existingIds.Count > 90, "expected the full existing Undead roster, found " + existingIds.Count);
        var existing = existingIds.Select(Card).ToDictionary(c => c.Id, Fingerprint);

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

        // every non-vanilla new card really carries a mechanic beyond a stat line
        Assert.All(NewCards.Where(c => c.ObjectType == "unit"),
            c => Assert.True(c.Abilities.Count + c.Triggers.Count > 0 || c.OnPlay != null, $"{c.Id} is a vanilla body"));
    }

    // ------------------------------------------------------------------ economy premise

    [Fact]
    public void Opening_corpses_pay_for_a_two_drop_on_turn_one_and_the_citadel_provides_the_housing()
    {
        // No resource cheating here: the real starting economy of the new deck.
        var (game, engine, p1, _) = CreateMatch(richResources: false);
        var citadel = Hq(game, p1);
        Assert.Equal("undead-bone-citadel", citadel.DefinitionId);
        Assert.Equal(3, Corpses(game, p1));
        Assert.Equal(8, GameQueries.HousingCapacity(game, p1.Id));

        game.State = GameState.WaitingForAction;
        game.CurrentPhaseId = "main";
        Use(game, engine, p1, citadel, "bone-citadel-exhume");
        Assert.Equal(5, Corpses(game, p1)); // +2 per Exhume

        var ghoul = InHand(game, "undead-carrion-ghoul", p1);
        Play(game, engine, p1, ghoul);

        Assert.Equal("battlefield", ghoul.ZoneId);
        Assert.Equal(3, Corpses(game, p1)); // 5 - 2
        Assert.Equal(1, GameQueries.HousingUsed(game, p1.Id));
    }

    // ------------------------------------------------------------------ headquarters + hero

    [Fact]
    public void Citadel_exhume_banks_two_corpses_and_taps_and_the_bank_is_capped_at_ten()
    {
        var (game, engine, p1, _) = CreateMatch();
        var citadel = Hq(game, p1);
        citadel.Resources["corpses"] = 4;

        Use(game, engine, p1, citadel, "bone-citadel-exhume");

        Assert.Equal(6, Corpses(game, p1));
        Assert.True(citadel.IsTapped);

        citadel.IsTapped = false;
        citadel.Resources["corpses"] = 9;
        Use(game, engine, p1, citadel, "bone-citadel-exhume");
        Assert.Equal(10, Corpses(game, p1)); // Graveyard would stop at 8; the Citadel holds 10
    }

    [Fact]
    public void Citadel_ossify_mends_itself_for_two_corpses_once_per_turn()
    {
        var (game, engine, p1, _) = CreateMatch();
        var citadel = Hq(game, p1);
        citadel.Properties["currentHp"] = 5;
        citadel.Resources["corpses"] = 6;

        Use(game, engine, p1, citadel, "bone-citadel-ossify");

        Assert.Equal(8, Hp(citadel));
        Assert.Equal(4, Corpses(game, p1));
        Assert.False(citadel.IsTapped); // does not compete with Exhume for the tap

        var (again, error) = TryUse(game, engine, p1, citadel, "bone-citadel-ossify");
        Assert.False(again);
        Assert.Contains("Already used", error);

        citadel.Resources["corpses"] = 0;
        StartTurnOf(game, engine, p1);
        var (broke, _) = TryUse(game, engine, p1, citadel, "bone-citadel-ossify");
        Assert.False(broke); // and it cannot be paid without corpses
    }

    [Fact]
    public void Citadel_banks_a_corpse_the_first_time_a_unit_dies_each_turn_only()
    {
        var (game, engine, p1, p2) = CreateMatch();
        Hq(game, p1).Resources["corpses"] = 0;
        var a = Body(game, p1, 3, 3);
        var b = Body(game, p1, 3, 3);
        var v1 = Body(game, p2, 0, 1);
        var v2 = Body(game, p2, 0, 1);

        Attack(game, engine, a, v1);
        Attack(game, engine, b, v2);

        Assert.True(v1.IsDestroyed && v2.IsDestroyed);
        Assert.Equal(1, Corpses(game, p1));
    }

    [Fact]
    public void Regent_feeds_on_kills_and_on_every_turn_start()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var regent = Hero(game, p1);
        Assert.Equal("undead-grave-regent", regent.DefinitionId);
        regent.Line = "front";
        Assert.Equal(1, regent.Resources["dp"]); // the opening turn start already fed it once
        var victim = Body(game, p2, 0, 2);

        Attack(game, engine, regent, victim);
        Assert.True(victim.IsDestroyed);
        Assert.Equal(3, regent.Resources["dp"]); // onKill +2

        StartTurnOf(game, engine, p1);
        Assert.Equal(4, regent.Resources["dp"]); // onTurnStart +1

        regent.Resources["dp"] = 6;
        StartTurnOf(game, engine, p1);
        Assert.Equal(6, regent.Resources["dp"]); // capped at the card's own capacity
    }

    [Fact]
    public void Regents_command_spends_three_dp_to_rally_every_own_unit_by_two_attack()
    {
        var (game, engine, p1, _) = CreateMatch();
        var regent = Hero(game, p1);
        var a = Body(game, p1, 2, 3);
        var b = Body(game, p1, 3, 3);

        regent.Resources["dp"] = 2;
        Assert.False(TryUse(game, engine, p1, regent, "grave-regent-command").ok); // cannot pay 3 dp with 2

        regent.Resources["dp"] = 3;
        Use(game, engine, p1, regent, "grave-regent-command");

        Assert.Equal(4, Atk(game, a));
        Assert.Equal(5, Atk(game, b));
        Assert.Equal(0, regent.Resources["dp"]);
        Assert.True(regent.IsTapped);
    }

    // ------------------------------------------------------------------ units

    [Fact]
    public void Carrion_ghoul_banks_two_corpses_for_every_kill_it_makes()
    {
        var (game, engine, p1, p2) = CreateMatch();
        SilenceCitadel(game, p1);
        Hq(game, p1).Resources["corpses"] = 0;
        var ghoul = OnField(game, "undead-carrion-ghoul", p1);
        var victim = Body(game, p2, 0, 2);
        var survivor = Body(game, p2, 0, 9);

        Attack(game, engine, ghoul, survivor); // no kill, no corpses
        Assert.Equal(0, Corpses(game, p1));

        ghoul.IsTapped = false;
        Attack(game, engine, ghoul, victim);
        Assert.True(victim.IsDestroyed);
        Assert.Equal(2, Corpses(game, p1));
    }

    [Fact]
    public void Cairn_sentinel_leaves_a_skeleton_behind_when_it_is_cut_down()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var sentinel = OnField(game, "undead-cairn-sentinel", p1);
        var raider = Body(game, p2, 6, 6);
        Assert.Equal(0, Skeletons(game, p1));

        Attack(game, engine, raider, sentinel);

        Assert.True(sentinel.IsDestroyed);
        Assert.Equal(1, Skeletons(game, p1));
    }

    [Fact]
    public void Pyre_zealot_burns_itself_for_four_unblockable_damage_and_refunds_a_corpse()
    {
        var (game, engine, p1, p2) = CreateMatch();
        SilenceCitadel(game, p1);
        Hq(game, p1).Resources["corpses"] = 0;
        var zealot = OnField(game, "undead-pyre-zealot", p1);
        zealot.HasSummoningSickness = true; // no tap cost, so it works the turn it lands
        var tank = Body(game, p2, 1, 6, armor: 3);

        var valid = new TargetingService().GetValidTargets(game, Card("undead-pyre-zealot").Abilities[0].Choice!, p1.Id, zealot.Id);
        Assert.Contains(tank.Id, valid);
        Assert.DoesNotContain(Hero(game, p2).Id, valid); // units only

        Use(game, engine, p1, zealot, "pyre-zealot-immolate", tank);

        Assert.True(zealot.IsDestroyed);
        Assert.Equal(2, Hp(tank)); // 4 direct damage ignores the 3 armor
        Assert.Equal(1, Corpses(game, p1));
    }

    [Fact]
    public void Barrow_sentinel_guards_the_hero_and_freezes_whatever_kills_it()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var sentinel = OnField(game, "undead-barrow-sentinel", p1);
        var raider = Body(game, p2, 8, 9);
        raider.Tags.Add("ranged"); // reaches both lines, so only the guard rule can shield the back line

        var reach = new TargetingService().GetAttackTargets(game, raider);
        Assert.Contains(sentinel.Id, reach);
        Assert.DoesNotContain(Hero(game, p1).Id, reach); // an untapped guard shields hero and HQ
        Assert.DoesNotContain(Hq(game, p1).Id, reach);

        Attack(game, engine, raider, sentinel); // 8 - 2 armor = 6 = its hp

        Assert.True(sentinel.IsDestroyed);
        Assert.True(raider.IsTapped);
        Assert.True(raider.SkipNextUntap); // entombed in the barrow's chill: skips its next untap too
    }

    [Fact]
    public void Grave_vulture_draws_a_card_the_first_time_each_turn_an_ally_hits_the_enemy_hq()
    {
        var (game, engine, p1, p2) = CreateMatch();
        OnField(game, "undead-grave-vulture", p1, "back");
        var first = Body(game, p1, 4, 3);
        var second = Body(game, p1, 4, 3);
        var enemyHq = Hq(game, p2);
        var handBefore = HandCount(game, p1);

        Attack(game, engine, first, enemyHq);
        Assert.True(Hp(enemyHq) < enemyHq.Properties["maxHp"]);
        Assert.Equal(handBefore + 1, HandCount(game, p1));

        Attack(game, engine, second, enemyHq); // a second hit the same turn does not draw again
        Assert.Equal(handBefore + 1, HandCount(game, p1));
    }

    [Fact]
    public void Marrow_leech_heals_two_for_every_blow_it_lands()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var leech = OnField(game, "undead-marrow-leech", p1);
        leech.Properties["currentHp"] = 1;
        var dummy = Body(game, p2, 0, 9);

        Attack(game, engine, leech, dummy);

        Assert.Equal(3, Hp(leech)); // 1 + 2, capped at its 3 max
        Assert.Equal(7, Hp(dummy));
    }

    [Fact]
    public void Ravenous_revenant_untaps_after_a_kill_and_strikes_again_but_not_after_a_miss()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var revenant = OnField(game, "undead-ravenous-revenant", p1);
        var first = Body(game, p2, 0, 3);
        var second = Body(game, p2, 0, 3);
        var wall = Body(game, p2, 0, 9);

        Attack(game, engine, revenant, first);
        Assert.True(first.IsDestroyed);
        Assert.False(revenant.IsTapped);

        Attack(game, engine, revenant, second); // the second swing is only possible because of the untap
        Assert.True(second.IsDestroyed);
        Assert.False(revenant.IsTapped);

        Attack(game, engine, revenant, wall); // no kill: it stays tapped like any attacker
        Assert.False(wall.IsDestroyed);
        Assert.True(revenant.IsTapped);
    }

    [Fact]
    public void Withering_shade_permanently_shrinks_the_attack_of_whatever_it_hits()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var shade = OnField(game, "undead-withering-shade", p1);
        var brute = Body(game, p2, 3, 9);

        Attack(game, engine, shade, brute);
        Assert.Equal(2, Atk(game, brute));

        StartTurnOf(game, engine, p1); // untaps the shade; the shrink does not wear off with the turn
        Attack(game, engine, shade, brute);
        Assert.Equal(1, Atk(game, brute));

        StartTurnOf(game, engine, p1);
        Attack(game, engine, shade, brute);
        Assert.Equal(0, Atk(game, brute));

        StartTurnOf(game, engine, p1);
        Attack(game, engine, shade, brute);
        Assert.Equal(0, Atk(game, brute)); // never below zero
    }

    [Fact]
    public void Grave_broker_makes_the_next_corpse_priced_card_one_cheaper_for_that_one_card_only()
    {
        var (game, engine, p1, _) = CreateMatch();
        var broker = InHand(game, "undead-grave-broker", p1);
        var first = InHand(game, "undead-cairn-sentinel", p1);   // 3 corpses
        var second = InHand(game, "undead-cairn-sentinel", p1);  // 3 corpses

        Play(game, engine, p1, broker);
        Assert.Equal(8, Corpses(game, p1)); // the broker itself is full price
        Assert.Single(game.ActiveCostModifiers);

        Play(game, engine, p1, first);
        Assert.Equal(6, Corpses(game, p1)); // 3 - 1 discount = 2
        Assert.Empty(game.ActiveCostModifiers);

        Play(game, engine, p1, second);
        Assert.Equal(3, Corpses(game, p1)); // back to full price
    }

    [Fact]
    public void Bone_drover_arrives_with_two_skeletons_that_need_no_housing()
    {
        var (game, engine, p1, _) = CreateMatch();
        var drover = InHand(game, "undead-bone-drover", p1);

        Play(game, engine, p1, drover);

        Assert.Equal("battlefield", drover.ZoneId);
        Assert.Equal(2, Skeletons(game, p1));
        Assert.Equal(5, Corpses(game, p1)); // 10 - 5
        Assert.Equal(2, GameQueries.HousingUsed(game, p1.Id)); // the drover; skeleton tokens are free
    }

    [Fact]
    public void Hexbolt_acolyte_pings_the_enemy_hero_straight_through_guards_and_armor()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var acolyte = OnField(game, "undead-hexbolt-acolyte", p1, "back");
        var guard = Body(game, p2, 1, 9);
        guard.Tags.Add("guard");
        var enemyHero = Hero(game, p2);
        var before = Hp(enemyHero);

        var valid = new TargetingService().GetValidTargets(game, Card("undead-hexbolt-acolyte").Abilities[0].Choice!, p1.Id, acolyte.Id);
        Assert.Equal(new[] { enemyHero.Id }, valid); // heroes only, and guards do not stop an ability

        Use(game, engine, p1, acolyte, "hexbolt-acolyte-hexbolt", enemyHero);

        Assert.Equal(before - 2, Hp(enemyHero)); // direct damage ignores the hero's armor
        Assert.True(acolyte.IsTapped);
    }

    [Fact]
    public void Dirge_bearer_rallies_only_undead_tagged_units()
    {
        var (game, engine, p1, _) = CreateMatch();
        var bearer = OnField(game, "undead-dirge-bearer", p1);
        var skeleton = OnField(game, "skeleton", p1);
        var living = Body(game, p1, 2, 3); // town-watch: not undead
        var skeletonBefore = Atk(game, skeleton);

        Use(game, engine, p1, bearer, "dirge-bearer-dirge");

        Assert.Equal(skeletonBefore + 1, Atk(game, skeleton));
        Assert.Equal(2, Atk(game, bearer));
        Assert.Equal(2, Atk(game, living)); // unchanged
        Assert.True(bearer.IsTapped);
    }

    // ------------------------------------------------------------------ buildings

    [Fact]
    public void Thorn_ossuary_skewers_whatever_strikes_it_through_armor()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var ossuary = OnField(game, "undead-thorn-ossuary", p1);
        Assert.Contains("fortification", ossuary.Tags);
        var raider = Body(game, p2, 3, 6, armor: 2);

        Attack(game, engine, raider, ossuary); // 3 vs 3 armor: chips for 1

        Assert.Equal(8, Hp(ossuary));
        Assert.Equal(4, Hp(raider)); // 2 direct damage back, armor irrelevant
    }

    [Fact]
    public void Miasma_well_poisons_every_enemy_unit_at_the_start_of_its_owners_turn()
    {
        var (game, engine, p1, p2) = CreateMatch();
        OnField(game, "undead-miasma-well", p1);
        var front = Body(game, p2, 1, 5);
        var back = Body(game, p2, 1, 5, line: "back");
        var mine = Body(game, p1, 1, 5);

        StartTurnOf(game, engine, p1);

        Assert.Equal(1, front.Resources.GetValueOrDefault("poison"));
        Assert.Equal(1, back.Resources.GetValueOrDefault("poison"));
        Assert.Equal(0, mine.Resources.GetValueOrDefault("poison"));
        Assert.Equal(0, Hero(game, p2).Resources.GetValueOrDefault("poison")); // units only

        StartTurnOf(game, engine, p1);
        Assert.Equal(2, front.Resources["poison"]); // it stacks turn over turn
    }

    [Fact]
    public void Bone_cradle_knits_every_wounded_unit_each_turn_and_adds_four_housing()
    {
        var (game, engine, p1, _) = CreateMatch();
        var capacityBefore = GameQueries.HousingCapacity(game, p1.Id);
        OnField(game, "undead-bone-cradle", p1, "back");
        Assert.Equal(capacityBefore + 4, GameQueries.HousingCapacity(game, p1.Id));
        var a = Body(game, p1, 1, 5);
        var b = Body(game, p1, 1, 5);
        a.Properties["currentHp"] = 1;
        b.Properties["currentHp"] = 5;

        StartTurnOf(game, engine, p1);

        Assert.Equal(2, Hp(a));
        Assert.Equal(5, Hp(b)); // never over max
    }

    [Fact]
    public void Cursed_archive_trades_its_own_hp_for_a_card_each_turn()
    {
        var (game, engine, p1, _) = CreateMatch();
        var archive = OnField(game, "undead-cursed-archive", p1, "back");
        var handBefore = HandCount(game, p1);

        Use(game, engine, p1, archive, "cursed-archive-consult");

        Assert.Equal(handBefore + 1, HandCount(game, p1));
        Assert.Equal(4, Hp(archive));
        Assert.True(archive.IsTapped);

        archive.IsTapped = false;
        archive.Properties["currentHp"] = 1;
        Use(game, engine, p1, archive, "cursed-archive-consult");
        Assert.True(archive.IsDestroyed); // every consultation costs the archive itself: it eventually collapses
    }

    [Fact]
    public void Sepulchral_shrine_returns_a_dead_regent_and_a_skeleton_one_turn_after_the_rite()
    {
        var (game, engine, p1, _) = CreateMatch();
        var shrine = OnField(game, "undead-sepulchral-shrine", p1, "back");
        var regent = Hero(game, p1);

        Assert.False(TryUse(game, engine, p1, shrine, "sepulchral-shrine-begin-rite").ok); // hero is alive: nothing to return

        regent.IsDestroyed = true;
        regent.ZoneId = "discard";
        Use(game, engine, p1, shrine, "sepulchral-shrine-begin-rite");
        Assert.Equal(1, shrine.Resources["ritual"]);

        shrine.IsTapped = false;
        Assert.False(TryUse(game, engine, p1, shrine, "sepulchral-shrine-begin-rite").ok); // a rite is already under way
        Assert.Null(GameQueries.FindLivingHero(game, p1.Id));

        StartTurnOf(game, engine, p1);

        var returned = GameQueries.FindLivingHero(game, p1.Id);
        Assert.NotNull(returned);
        Assert.Equal("undead-grave-regent", returned!.DefinitionId);
        Assert.Equal(7, Hp(returned));
        Assert.Equal(1, Skeletons(game, p1));
        Assert.Equal(0, shrine.Resources["ritual"]); // the shrine resets for the next time
    }

    // ------------------------------------------------------------------ spells

    [Fact]
    public void Skeletal_warband_raises_three_skeletons_and_costs_four_corpses()
    {
        var (game, engine, p1, _) = CreateMatch();
        var warband = InHand(game, "undead-skeletal-warband", p1);

        Play(game, engine, p1, warband);

        Assert.Equal(3, Skeletons(game, p1));
        Assert.Equal(6, Corpses(game, p1));
        Assert.Equal("discard", warband.ZoneId);
    }

    [Fact]
    public void Grave_hush_taps_every_enemy_unit_but_not_the_hero()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var a = Body(game, p2, 2, 3);
        var b = Body(game, p2, 2, 3, line: "back");
        var mine = Body(game, p1, 2, 3);
        var hush = InHand(game, "undead-grave-hush", p1);

        Play(game, engine, p1, hush);

        Assert.True(a.IsTapped);
        Assert.True(b.IsTapped);
        Assert.False(mine.IsTapped);
        Assert.False(Hero(game, p2).IsTapped);
    }

    [Fact]
    public void Cascade_of_bone_hits_every_unit_on_both_sides_through_armor()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var mine = Body(game, p1, 1, 3);
        var armored = Body(game, p2, 1, 3, armor: 1);
        var frail = Body(game, p2, 1, 2);
        var cascade = InHand(game, "undead-cascade-of-bone", p1);

        Play(game, engine, p1, cascade);

        Assert.Equal(1, Hp(mine));    // 2 damage, own side is not spared
        Assert.Equal(2, Hp(armored)); // 2 - 1 armor
        Assert.True(frail.IsDestroyed);
        Assert.True(Hp(Hero(game, p2)) == Hero(game, p2).Properties["maxHp"]); // units only
    }

    [Fact]
    public void Unquiet_grave_untaps_an_exhausted_undead_and_lends_it_two_attack()
    {
        var (game, engine, p1, _) = CreateMatch();
        var skeleton = OnField(game, "skeleton", p1);
        var living = Body(game, p1, 2, 3);
        skeleton.IsTapped = true;
        var grave = InHand(game, "undead-unquiet-grave", p1);
        var before = Atk(game, skeleton);

        var valid = new TargetingService().GetValidTargets(game, Card("undead-unquiet-grave").OnPlay!.Choice!, p1.Id, grave.Id);
        Assert.Contains(skeleton.Id, valid);
        Assert.DoesNotContain(living.Id, valid); // undead only

        Play(game, engine, p1, grave, skeleton);

        Assert.False(skeleton.IsTapped);
        Assert.Equal(before + 2, Atk(game, skeleton));
    }

    [Fact]
    public void Spirit_ward_counters_an_enemy_spell_and_cannot_be_cast_on_your_own_turn()
    {
        var (game, engine, p1, p2) = CreateMatch(deckBId: DeckId);
        var ward = InHand(game, "undead-spirit-ward", p1);
        var warband = InHand(game, "undead-skeletal-warband", p2);

        // a reaction is not a main-phase play
        var (ownTurn, message) = TryPlay(game, engine, p1, ward);
        Assert.False(ownTurn);
        Assert.Contains("Reaction", message);

        game.ActivePlayerId = p2.Id;
        Play(game, engine, p2, warband);
        Assert.Equal(GameState.WaitingForReaction, game.State);
        Assert.Equal(p1.Id, game.ReactionPlayerId);
        Assert.Equal("stack", warband.ZoneId);

        Play(game, engine, p1, ward);
        Assert.Equal(7, Corpses(game, p1)); // 10 - 3
        var (resolved, _) = engine.ExecuteAction(game, p1.Id, new ActionRequest { Type = "pass" });
        Assert.True(resolved);

        Assert.Equal(GameState.WaitingForAction, game.State);
        Assert.Equal(0, Skeletons(game, p2)); // the Warband fizzled
        Assert.Equal("discard", warband.ZoneId);
        Assert.Equal("discard", ward.ZoneId);
    }

    [Fact]
    public void Without_a_ward_in_hand_the_same_spell_resolves_normally()
    {
        var (game, engine, p1, p2) = CreateMatch(deckBId: DeckId);
        var warband = InHand(game, "undead-skeletal-warband", p2);

        game.ActivePlayerId = p2.Id;
        Play(game, engine, p2, warband);

        Assert.Equal(GameState.WaitingForAction, game.State);
        Assert.Equal(3, Skeletons(game, p2));
    }

    [Fact]
    public void Plague_snare_lies_face_down_and_poisons_whoever_attacks_the_hero()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var snare = InHand(game, "undead-plague-snare", p1);
        Play(game, engine, p1, snare);
        Assert.Equal("secrets", snare.ZoneId);
        Assert.True(snare.FaceDown);
        Assert.Equal(8, Corpses(game, p1));

        var raider = Body(game, p2, 2, 9);
        Attack(game, engine, raider, Hero(game, p1));

        Assert.Equal(4, raider.Resources["poison"]);
        Assert.Equal("discard", snare.ZoneId); // sprung
        Assert.False(snare.FaceDown);
    }

    [Fact]
    public void Plague_snare_ignores_attacks_on_anything_but_the_hero()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var snare = InHand(game, "undead-plague-snare", p1);
        Play(game, engine, p1, snare);
        var raider = Body(game, p2, 2, 9);

        Attack(game, engine, raider, Hq(game, p1));

        Assert.Equal(0, raider.Resources.GetValueOrDefault("poison"));
        Assert.Equal("secrets", snare.ZoneId); // still armed
    }

    [Fact]
    public void Blood_pact_pays_two_hero_hp_for_two_cards_and_two_death_power()
    {
        var (game, engine, p1, _) = CreateMatch();
        var regent = Hero(game, p1);
        regent.Resources["dp"] = 0;
        var pact = InHand(game, "undead-blood-pact", p1);

        var valid = new TargetingService().GetValidTargets(game, Card("undead-blood-pact").OnPlay!.Choice!, p1.Id, pact.Id);
        Assert.Equal(new[] { regent.Id }, valid); // your own hero and nothing else

        Play(game, engine, p1, pact, regent);

        Assert.Equal(5, Hp(regent));
        Assert.Equal(2, HandCount(game, p1)); // the pact left the hand, two cards arrived
        Assert.Equal(2, regent.Resources["dp"]);
        Assert.Equal(9, Corpses(game, p1));
    }

    [Fact]
    public void Crumbling_curse_batters_an_enemy_building_through_its_armor_and_only_buildings()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var enemyHq = Hq(game, p2);
        var wall = OnField(game, "wooden-palisade", p2);
        var unit = Body(game, p2, 1, 5);
        var curse = InHand(game, "undead-crumbling-curse", p1);
        var hqBefore = Hp(enemyHq);
        var wallArmor = wall.Properties["armor"];
        var wallBefore = Hp(wall);

        var valid = new TargetingService().GetValidTargets(game, Card("undead-crumbling-curse").OnPlay!.Choice!, p1.Id, curse.Id);
        Assert.Contains(enemyHq.Id, valid);
        Assert.Contains(wall.Id, valid);
        Assert.DoesNotContain(unit.Id, valid);
        Assert.DoesNotContain(Hq(game, p1).Id, valid);

        Play(game, engine, p1, curse, enemyHq);
        Assert.Equal(hqBefore - (5 - enemyHq.Properties["armor"]), Hp(enemyHq));

        var curse2 = InHand(game, "undead-crumbling-curse", p1);
        Play(game, engine, p1, curse2, wall);
        Assert.Equal(wallBefore - (5 - wallArmor), Hp(wall));
    }

    [Fact]
    public void Mend_the_crypt_heals_six_on_one_own_building_or_hq()
    {
        var (game, engine, p1, _) = CreateMatch();
        var citadel = Hq(game, p1);
        citadel.Properties["currentHp"] = 3;
        var archive = OnField(game, "undead-cursed-archive", p1, "back");
        archive.Properties["currentHp"] = 1;
        var mend = InHand(game, "undead-mend-the-crypt", p1);
        var mend2 = InHand(game, "undead-mend-the-crypt", p1);

        Play(game, engine, p1, mend, citadel);
        Assert.Equal(9, Hp(citadel)); // +6
        Assert.Equal(1, Hp(archive)); // a single target

        Play(game, engine, p1, mend2, archive);
        Assert.Equal(5, Hp(archive)); // capped at its max
    }

    // ------------------------------------------------------------------ equipment

    private static ObjectInstance Equip(GameInstance game, RuleEngine engine, PlayerInstance p, string cardId, ObjectInstance bearer)
    {
        var gear = InHand(game, cardId, p);
        Play(game, engine, p, gear, bearer);
        Assert.Equal(bearer.Id, gear.AttachedToId);
        Assert.Equal("battlefield", gear.ZoneId);
        return gear;
    }

    [Fact]
    public void Soulreaper_scythe_adds_an_attack_and_heals_its_bearer_three_on_every_kill()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var bearer = Body(game, p1, 2, 6);
        bearer.Properties["currentHp"] = 1;
        Equip(game, engine, p1, "undead-soulreaper-scythe", bearer);
        Assert.Equal(3, Atk(game, bearer));
        var victim = Body(game, p2, 0, 3);

        Attack(game, engine, bearer, victim);

        Assert.True(victim.IsDestroyed);
        Assert.Equal(4, Hp(bearer)); // 1 + 3
    }

    [Fact]
    public void Wraithmail_adds_armor_and_poisons_whatever_hits_the_wearer()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var wearer = Body(game, p1, 1, 9);
        Equip(game, engine, p1, "undead-wraithmail", wearer);
        Assert.Equal(1, GameQueries.GetEffectiveProperty(game, wearer, "armor"));
        var raider = Body(game, p2, 3, 9);

        Attack(game, engine, raider, wearer);

        Assert.Equal(7, Hp(wearer)); // 3 - 1 armor = 2
        Assert.Equal(2, raider.Resources["poison"]);
    }

    [Fact]
    public void Coffin_lid_bulwark_turns_its_bearer_into_the_only_legal_target_and_the_tag_leaves_with_it()
    {
        var (game, engine, p1, p2) = CreateMatch();
        var bearer = Body(game, p1, 1, 9);
        var other = Body(game, p1, 1, 9);
        var raider = Body(game, p2, 2, 9);
        var targeting = new TargetingService();
        Assert.Contains(other.Id, targeting.GetAttackTargets(game, raider));

        var shield = Equip(game, engine, p1, "undead-coffin-lid-bulwark", bearer);

        Assert.Contains("blocking", bearer.Tags);
        Assert.Equal(1, GameQueries.GetEffectiveProperty(game, bearer, "armor"));
        Assert.Equal(new[] { bearer.Id }, targeting.GetAttackTargets(game, raider));

        // destroying the shield takes the taunt with it
        var mutator = new GameMutator(new EventBus());
        mutator.DestroyObject(game, shield);
        Assert.DoesNotContain("blocking", bearer.Tags);
        Assert.Contains(other.Id, targeting.GetAttackTargets(game, raider));
    }

    [Fact]
    public void Phylactery_shard_raises_max_hp_and_shatters_into_a_full_heal()
    {
        var (game, engine, p1, _) = CreateMatch();
        var wearer = Body(game, p1, 1, 4);
        wearer.Properties["currentHp"] = 1;
        var shard = Equip(game, engine, p1, "undead-phylactery-shard", wearer);
        Assert.Equal(6, GameQueries.GetEffectiveProperty(game, wearer, "maxHp"));

        Use(game, engine, p1, shard, "phylactery-shard-shatter");

        Assert.True(shard.IsDestroyed);
        Assert.Equal(4, Hp(wearer)); // healed to the wearer's own max once the shard's bonus is gone
        Assert.Equal(4, GameQueries.GetEffectiveProperty(game, wearer, "maxHp"));
    }

    // ------------------------------------------------------------------ live bot-vs-bot smoke

    [Theory]
    [InlineData("town")]
    [InlineData("undead")]
    [InlineData("raiders")]
    [InlineData("machine")]
    public void Legion_deck_plays_real_bot_matches_without_errors_and_actually_casts_its_new_cards(string opponentDeck)
    {
        // Real economy (no free resources), real bots, alternating first player - same loop the
        // /api/simulate endpoint runs. Proves the deck is not merely legal but playable: the new
        // cards must be castable from the Citadel's own corpse income.
        var castNewCards = new HashSet<string>();
        int games = 6;
        for (int i = 0; i < games; i++)
        {
            bool legionFirst = i % 2 == 0;
            var deckIds = legionFirst ? (DeckId, opponentDeck) : (opponentDeck, DeckId);
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

            var legionPlayer = legionFirst ? p1 : p2;
            foreach (var o in game.Objects.Where(o => o.OwnerId == legionPlayer.Id
                         && NewCardIds.Contains(o.DefinitionId) && o.ZoneId is "battlefield" or "discard"))
                castNewCards.Add(o.DefinitionId);
            Assert.DoesNotContain(game.Log, l => l.Contains("Exception", StringComparison.OrdinalIgnoreCase));
        }

        Assert.True(castNewCards.Count >= 8,
            $"only {castNewCards.Count} distinct new cards were ever cast across {games} games vs {opponentDeck}: {string.Join(", ", castNewCards)}");
    }
}
