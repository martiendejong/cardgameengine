using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>Bundle of engine services handed to handlers and action services.</summary>
public class EngineServices
{
    public required EventBus Bus { get; init; }
    public required GameMutator Mutator { get; init; }
    public required ObjectFactory Factory { get; init; }
    public required TargetingService Targeting { get; init; }
    public required ConditionEvaluator Conditions { get; init; }
    public required CostProcessor Costs { get; init; }
    public required EffectProcessor Effects { get; init; }
}

/// <summary>Registers the standard condition, cost and effect vocabulary.</summary>
public static class DefaultHandlers
{
    public static void RegisterAll(EngineServices s)
    {
        RegisterConditions(s);
        RegisterCosts(s);
        RegisterEffects(s);
    }

    // ------------------------------------------------------------------ conditions

    private static void RegisterConditions(EngineServices s)
    {
        var c = s.Conditions;

        c.Register("not_tapped", ctx => !ctx.Object.IsTapped);
        c.Register("is_tapped", ctx => ctx.Object.IsTapped);
        c.Register("resource_gte", ctx => GetResource(ctx) >= (ctx.Condition.Amount ?? 0));
        c.Register("resource_lte", ctx => GetResource(ctx) <= (ctx.Condition.Amount ?? 0));
        c.Register("has_tag", ctx => ctx.Object.Tags.Contains(ctx.Condition.Tag ?? ""));
        c.Register("is_phase", ctx => ctx.Game.CurrentPhaseId == ctx.Condition.Phase);
        c.Register("own_hero_destroyed", ctx => ctx.Game.Objects.Any(o =>
            o.OwnerId == ctx.Player.Id && o.IsDestroyed &&
            GameQueries.IsObjectTypeOrSubtype(ctx.Game, o.ObjectType, "hero")));
        c.Register("controls_no_tagged", ctx => !GameQueries.BattlefieldObjects(ctx.Game, ctx.Player.Id)
            .Any(o => o.Tags.Contains(ctx.Condition.Tag ?? "")));
        // Infiltration state: gates a spy's abilities to inside/outside a building
        c.Register("is_attached", ctx => ctx.Object.AttachedToId != null);
        c.Register("not_attached", ctx => ctx.Object.AttachedToId == null);

        static int GetResource(ConditionContext ctx) =>
            ctx.Condition.Scope == "player"
                ? ctx.Player.Resources.GetValueOrDefault(ctx.Condition.ResourceId ?? "")
                : ctx.Object.Resources.GetValueOrDefault(ctx.Condition.ResourceId ?? "");
    }

    // ------------------------------------------------------------------ costs

    private static void RegisterCosts(EngineServices s)
    {
        s.Costs.Register("tap",
            canPay: ctx => !ctx.Object.IsTapped,
            pay: ctx => s.Mutator.Tap(ctx.Game, ctx.Object));

        s.Costs.Register("resource",
            canPay: ctx => GetResource(ctx) >= (ctx.Cost.Amount ?? 0),
            pay: ctx =>
            {
                var amount = ctx.Cost.Amount ?? 0;
                if (ctx.Cost.Scope == "player")
                    s.Mutator.SpendResource(ctx.Game, ctx.Player, ctx.Cost.ResourceId!, amount);
                else
                    s.Mutator.GainEntityResource(ctx.Game, ctx.Object, ctx.Cost.ResourceId!, -amount);
            });

        s.Costs.Register("sacrifice",
            canPay: ctx => !ctx.Object.IsDestroyed,
            pay: ctx => s.Mutator.DestroyObject(ctx.Game, ctx.Object));

        // Crew: tap Amount untapped friendly units with Tag (the operators of a machine).
        // Crew members are auto-selected; excludes the source object itself.
        s.Costs.Register("crew",
            canPay: ctx => GameQueries.BattlefieldObjects(ctx.Game, ctx.Player.Id)
                .Count(o => o.Id != ctx.Object.Id && !o.IsTapped && !o.HasSummoningSickness
                    && o.Tags.Contains(ctx.Cost.Tag ?? "")) >= (ctx.Cost.Amount ?? 1),
            pay: ctx =>
            {
                var crew = GameQueries.BattlefieldObjects(ctx.Game, ctx.Player.Id)
                    .Where(o => o.Id != ctx.Object.Id && !o.IsTapped && !o.HasSummoningSickness
                        && o.Tags.Contains(ctx.Cost.Tag ?? ""))
                    .Take(ctx.Cost.Amount ?? 1)
                    .ToList();
                foreach (var member in crew)
                {
                    s.Mutator.Tap(ctx.Game, member);
                    ctx.Game.Log.Add($"{member.Name} crews {ctx.Object.Name}.");
                }
            });

        static int GetResource(CostContext ctx) =>
            ctx.Cost.Scope == "player"
                ? ctx.Player.Resources.GetValueOrDefault(ctx.Cost.ResourceId ?? "")
                : ctx.Object.Resources.GetValueOrDefault(ctx.Cost.ResourceId ?? "");
    }

    // ------------------------------------------------------------------ effects

    private static void RegisterEffects(EngineServices s)
    {
        var e = s.Effects;
        var m = s.Mutator;

        e.Register("gain_resource", ctx =>
        {
            var amount = ctx.Effect.Amount ?? 0;
            var resId = ctx.Effect.ResourceId!;
            if (ctx.Effect.Scope == "player")
                m.GainResource(ctx.Game, ctx.Player, resId, amount);
            else
            {
                var obj = ctx.ResolveScope();
                if (obj != null) m.GainEntityResource(ctx.Game, obj, resId, amount);
            }
        });

        e.Register("set_resource", ctx =>
        {
            var amount = ctx.Effect.Amount ?? 0;
            var resId = ctx.Effect.ResourceId!;
            if (ctx.Effect.Scope == "player")
                ctx.Player.Resources[resId] = amount;
            else
            {
                var obj = ctx.ResolveScope();
                if (obj != null) obj.Resources[resId] = amount;
            }
        });

        e.Register("set_property", ctx =>
        {
            var obj = ctx.ResolveScope();
            if (obj != null) m.SetProperty(ctx.Game, obj, ctx.Effect.PropertyId!, ctx.Effect.Amount ?? 0);
        });

        e.Register("modify_property", ctx =>
        {
            var obj = ctx.ResolveScope();
            if (obj != null) m.ModifyProperty(ctx.Game, obj, ctx.Effect.PropertyId!, ctx.Effect.Amount ?? 0);
        });

        e.Register("tap", ctx =>
        {
            var obj = ctx.ResolveScope();
            if (obj != null) m.Tap(ctx.Game, obj);
        });

        e.Register("untap", ctx =>
        {
            var obj = ctx.ResolveScope();
            if (obj != null) m.Untap(ctx.Game, obj);
        });

        e.Register("summon", ctx =>
        {
            var cardDef = ctx.Game.Definition.Cards.FirstOrDefault(c => c.Id == ctx.Effect.CardId);
            if (cardDef == null) return;
            if (!ctx.Effect.IgnoreHousing && cardDef.HousingCost is int housing &&
                !GameQueries.HasHousingFor(ctx.Game, ctx.Player.Id, housing))
            {
                ctx.Game.Log.Add($"{ctx.Player.Name} has no housing left for a {cardDef.Name}!");
                return;
            }
            var obj = s.Factory.CreateObjectInstance(ctx.Game, cardDef, ctx.Player.Id, "battlefield");
            obj.HasSummoningSickness = true;
            if (cardDef.Duration is int lifetime) obj.Lifetime = lifetime;
            ctx.Game.Objects.Add(obj);
            ctx.Game.Log.Add($"{ctx.Player.Name} summons {cardDef.Name}!");
            s.Bus.Publish(ctx.Game, new GameEvent { Type = GameEventTypes.ObjectSummoned, Target = obj, Player = ctx.Player });
        });

        e.Register("destroy", ctx =>
        {
            var obj = ctx.ResolveScope();
            if (obj != null) m.DestroyObject(ctx.Game, obj, ctx.Source);
        });

        e.Register("heal", ctx =>
        {
            var obj = ctx.ResolveScope();
            if (obj != null) m.Heal(ctx.Game, obj, ctx.Effect.Amount ?? 0);
        });

        e.Register("damage", ctx =>
        {
            var obj = ctx.ResolveScope();
            if (obj == null) return;
            var armor = GameQueries.GetEffectiveProperty(ctx.Game, obj, "armor");
            m.ApplyDamage(ctx.Game, obj, Math.Max(0, (ctx.Effect.Amount ?? 0) - armor), ctx.Source);
        });

        e.Register("buff_tag_until_end_of_turn", ctx =>
        {
            var propId = ctx.Effect.PropertyId ?? "attack";
            var amount = ctx.Effect.Amount ?? 1;
            var tagged = ctx.Game.Objects.Where(o =>
                o.ControllerId == ctx.Player.Id && !o.IsDestroyed && o.Tags.Contains(ctx.Effect.Tag ?? ""));
            foreach (var obj in tagged)
            {
                m.AddModifier(ctx.Game, obj, propId, amount, "endOfTurn");
                ctx.Game.Log.Add($"{obj.Name} gains +{amount} {propId} until end of turn.");
            }
        });

        e.Register("buff_target_until_end_of_turn", ctx =>
        {
            var obj = ctx.ResolveScope("target");
            if (obj == null) return;
            var propId = ctx.Effect.PropertyId ?? "attack";
            var amount = ctx.Effect.Amount ?? 1;
            // untilEvent "ownerNextTurnStart" makes the buff survive the opponent's turn (Divine Shield)
            var expiry = ctx.Effect.UntilEvent ?? "endOfTurn";
            var mod = m.AddModifier(ctx.Game, obj, propId, amount, expiry);
            if (expiry == "ownerNextTurnStart") mod.OwnerPlayerId = ctx.Player.Id;
            ctx.Game.Log.Add($"{obj.Name} gains +{amount} {propId} {(expiry == "ownerNextTurnStart" ? "until your next turn" : "until end of turn")}.");
        });

        e.Register("add_progress", ctx =>
        {
            var obj = ctx.ResolveScope("target");
            if (obj == null || !obj.UnderConstruction) return;
            var cardDef = GameQueries.GetCardDefinition(ctx.Game, obj);
            var req = cardDef?.ConstructionRequirement ?? 0;
            obj.ConstructionProgress += ctx.Effect.Amount ?? 1;
            ctx.Game.Log.Add($"{obj.Name} construction: {Math.Min(obj.ConstructionProgress, req)}/{req}.");
            if (obj.ConstructionProgress >= req)
            {
                obj.UnderConstruction = false;
                obj.ConstructionProgress = req;
                ctx.Game.Log.Add($"{obj.Name} is completed!");
            }
        });

        e.Register("cost_discount", ctx =>
        {
            ctx.Game.ActiveCostModifiers.Add(new CostModifierInstance
            {
                PlayerId = ctx.Player.Id,
                ResourceId = ctx.Effect.ResourceId ?? "gold",
                Amount = ctx.Effect.Amount ?? 1,
                TagFilter = ctx.Effect.Tag,
                Uses = 1
            });
            ctx.Game.Log.Add($"Next {ctx.Effect.Tag ?? "card"} costs {ctx.Effect.Amount ?? 1} less {ctx.Effect.ResourceId ?? "gold"} this turn.");
        });

        e.Register("transform", ctx =>
        {
            if (ctx.Target == null || ctx.Effect.CardId == null) return;
            var newDef = ctx.Game.Definition.Cards.FirstOrDefault(c => c.Id == ctx.Effect.CardId);
            if (newDef == null) return;

            // If the new form needs more living space than the old one, the delta must fit
            var oldDef = GameQueries.GetCardDefinition(ctx.Game, ctx.Target);
            var delta = (newDef.HousingCost ?? 0) - (oldDef?.HousingCost ?? 0);
            if (delta > 0 && !GameQueries.HasHousingFor(ctx.Game, ctx.Player.Id, delta))
            {
                ctx.Game.Log.Add($"{ctx.Player.Name} has no housing left to train a {newDef.Name}!");
                return;
            }

            s.Factory.Transform(ctx.Game, ctx.Target, newDef);
            s.Bus.Publish(ctx.Game, new GameEvent { Type = GameEventTypes.ObjectTransformed, Target = ctx.Target, Player = ctx.Player });
        });

        // Raider looting: drain whatever the enemy has (gold, training, or their HQ
        // stockpile - mana/corpses/biomass/reagents/energy) and convert it to gold.
        // Never fully dependent on the enemy holding gold; scraps guarantee 1.
        e.Register("drain_resource", ctx =>
        {
            var opponent = GameQueries.GetOpponent(ctx.Game, ctx.Player);
            if (opponent == null) return;
            var want = ctx.Effect.Amount ?? 1;
            var drained = 0;

            // player-scoped valuables first
            foreach (var resId in new[] { "gold", "training" })
            {
                if (drained >= want) break;
                var have = opponent.Resources.GetValueOrDefault(resId);
                var take = Math.Min(want - drained, have);
                if (take <= 0) continue;
                opponent.Resources[resId] = have - take;
                drained += take;
                ctx.Game.Log.Add($"{ctx.Player.Name} drains {take} {resId} from {opponent.Name}!");
            }

            // then their HQ stockpile
            var bank = GameQueries.FindResourceBank(ctx.Game, opponent.Id);
            if (bank != null)
            {
                foreach (var (resId, have) in bank.Resources.ToList())
                {
                    if (drained >= want) break;
                    var take = Math.Min(want - drained, have);
                    if (take <= 0) continue;
                    bank.Resources[resId] = have - take;
                    drained += take;
                    ctx.Game.Log.Add($"{ctx.Player.Name} plunders {take} {resId} from {bank.Name}!");
                }
            }

            var gained = Math.Max(1, drained); // scraps: raiding always pays something
            m.GainResource(ctx.Game, ctx.Player, "gold", gained);
        });

        e.Register("steal_resource", ctx =>
        {
            var opponent = GameQueries.GetOpponent(ctx.Game, ctx.Player);
            if (opponent == null) return;
            var resId = ctx.Effect.ResourceId ?? "gold";
            var stolen = Math.Min(ctx.Effect.Amount ?? 0, opponent.Resources.GetValueOrDefault(resId));
            if (stolen <= 0)
            {
                ctx.Game.Log.Add($"{ctx.Player.Name} tries to steal {resId} from {opponent.Name}, but there is nothing to take!");
                return;
            }
            opponent.Resources[resId] = opponent.Resources.GetValueOrDefault(resId) - stolen;
            ctx.Player.Resources[resId] = ctx.Player.Resources.GetValueOrDefault(resId) + stolen;
            ctx.Game.Log.Add($"{ctx.Player.Name} steals {stolen} {resId} from {opponent.Name}!");
        });

        e.Register("plunder_resource", ctx =>
        {
            if (ctx.Source == null) return;
            var opponent = GameQueries.GetOpponent(ctx.Game, ctx.Player);
            if (opponent == null) return;
            var storeId = ctx.Effect.Tag ?? "loot";
            var want = ctx.Effect.Amount ?? 0;
            var taken = 0;

            // Gold first, then whatever their HQ has stockpiled
            var gold = opponent.Resources.GetValueOrDefault("gold");
            var g = Math.Min(want, gold);
            if (g > 0) { opponent.Resources["gold"] = gold - g; taken += g; }

            var bank = GameQueries.FindResourceBank(ctx.Game, opponent.Id);
            if (bank != null && taken < want)
            {
                foreach (var (resId, have) in bank.Resources.ToList())
                {
                    if (taken >= want) break;
                    var t = Math.Min(want - taken, have);
                    if (t <= 0) continue;
                    bank.Resources[resId] = have - t;
                    taken += t;
                }
            }

            if (taken <= 0)
            {
                ctx.Game.Log.Add($"{ctx.Source.Name} finds nothing to plunder from {opponent.Name}.");
                return;
            }
            ctx.Source.Resources[storeId] = ctx.Source.Resources.GetValueOrDefault(storeId) + taken;
            ctx.Game.Log.Add($"{ctx.Source.Name} plunders {taken} resources from {opponent.Name}! ({taken} tokens stored)");
        });

        e.Register("direct_damage", ctx =>
        {
            var obj = ctx.ResolveScope("target");
            if (obj != null) m.ApplyDirectDamage(ctx.Game, obj, ctx.Effect.Amount ?? 0, ctx.Source);
        });

        e.Register("draw_cards", ctx =>
        {
            // Draw is a turn concern; publish an event the RuleEngine wires to TurnService
            for (int i = 0; i < (ctx.Effect.Amount ?? 1); i++)
                s.Bus.Publish(ctx.Game, new GameEvent { Type = "requestDraw", Player = ctx.Player });
        });

        // Move an entity resource between the ability's source and its target
        e.Register("transfer_resource", ctx =>
        {
            if (ctx.Source == null || ctx.Target == null) return;
            var resId = ctx.Effect.ResourceId ?? "mana";
            bool toTarget = (ctx.Effect.Scope ?? "target") != "self";
            var from = toTarget ? ctx.Source : ctx.Target;
            var to = toTarget ? ctx.Target : ctx.Source;

            var amount = Math.Min(ctx.ChosenAmount ?? ctx.Effect.Amount ?? int.MaxValue, from.Resources.GetValueOrDefault(resId));
            var room = GameQueries.ResourceCapacity(ctx.Game, to, resId) - to.Resources.GetValueOrDefault(resId);
            amount = Math.Max(0, Math.Min(amount, room));
            if (amount == 0) return;

            from.Resources[resId] = from.Resources.GetValueOrDefault(resId) - amount;
            to.Resources[resId] = to.Resources.GetValueOrDefault(resId) + amount;
            ctx.Game.Log.Add($"{amount} {resId} flows from {from.Name} to {to.Name}.");
        });

        e.Register("gain_resource_all_tagged", ctx =>
        {
            var resId = ctx.Effect.ResourceId ?? "mana";
            var amount = ctx.Effect.Amount ?? 1;
            foreach (var obj in GameQueries.BattlefieldObjects(ctx.Game, ctx.Player.Id)
                         .Where(o => o.Tags.Contains(ctx.Effect.Tag ?? "")).ToList())
                m.GainEntityResource(ctx.Game, obj, resId, amount);
        });

        // A spy leaves your battlefield and burrows under an enemy building
        e.Register("infiltrate", ctx =>
        {
            if (ctx.Source == null || ctx.Target == null) return;
            ctx.Source.AttachedToId = ctx.Target.Id;
            ctx.Source.Slot = "infiltrated";
            ctx.Source.OccupiedSlots = new List<string>();
            ctx.Source.FaceDown = true;
            ctx.Game.Log.Add($"{ctx.Source.Name} infiltrates {ctx.Target.Name}!");
        });

        // Counterspell: cancel the spell currently on the stack
        e.Register("counter_spell", ctx =>
        {
            var item = ctx.Game.Stack.Items.LastOrDefault(i => i.Kind == "spell" && !i.Cancelled);
            if (item == null)
            {
                ctx.Game.Log.Add("...but there is no spell to counter.");
                return;
            }
            item.Cancelled = true;
            var spellObj = ctx.Game.Objects.FirstOrDefault(o => o.Id == item.SourceObjectId);
            ctx.Game.Log.Add($"{spellObj?.Name ?? "The spell"} is countered!");
        });

        // Freeze: tap and skip the next untap step
        e.Register("freeze", ctx =>
        {
            var obj = ctx.ResolveScope("target");
            if (obj == null) return;
            m.Tap(ctx.Game, obj);
            obj.SkipNextUntap = true;
            ctx.Game.Log.Add($"{obj.Name} is frozen solid!");
        });

        e.Register("revive_hero", ctx =>
        {
            if (GameQueries.FindLivingHero(ctx.Game, ctx.Player.Id) != null) return; // hero limit 1
            var deadHero = ctx.Game.Objects.FirstOrDefault(o =>
                o.OwnerId == ctx.Player.Id && o.IsDestroyed &&
                GameQueries.IsObjectTypeOrSubtype(ctx.Game, o.ObjectType, "hero"));
            if (deadHero == null) return;

            deadHero.IsDestroyed = false;
            deadHero.ZoneId = "battlefield";
            deadHero.IsTapped = false;
            deadHero.HasSummoningSickness = true;
            deadHero.HasMovedThisTurn = false;
            deadHero.Properties["currentHp"] = deadHero.Properties.GetValueOrDefault("maxHp", 1);
            if (ctx.Game.Definition.BattlefieldLines != null)
                deadHero.Line = ctx.Game.Definition.BattlefieldLines.SpawnLine;

            ctx.Game.Log.Add($"{deadHero.Name} is reconstructed and returns to the battlefield!");
            s.Bus.Publish(ctx.Game, new GameEvent { Type = GameEventTypes.HeroRevived, Target = deadHero, Player = ctx.Player });
        });

        // Counter-intelligence: flip one face-down card face-up
        e.Register("reveal", ctx =>
        {
            var obj = ctx.ResolveScope("target");
            if (obj == null || !obj.FaceDown) return;
            obj.FaceDown = false;
            ctx.Game.Log.Add($"{obj.Name} is revealed!");
        });

        // Inspect a building: everything hiding inside is flipped face-up
        e.Register("reveal_attachments", ctx =>
        {
            var host = ctx.ResolveScope("target");
            if (host == null) return;
            var hidden = ctx.Game.Objects
                .Where(o => o.AttachedToId == host.Id && !o.IsDestroyed && o.FaceDown)
                .ToList();
            if (hidden.Count == 0)
            {
                ctx.Game.Log.Add($"{host.Name} is searched — nothing hides inside.");
                return;
            }
            foreach (var obj in hidden)
            {
                obj.FaceDown = false;
                ctx.Game.Log.Add($"{obj.Name} is discovered inside {host.Name}!");
            }
        });

        // Purge a building: every enemy card attached to it (hidden or not) is destroyed
        e.Register("destroy_infiltrators", ctx =>
        {
            var host = ctx.ResolveScope("target");
            if (host == null) return;
            var intruders = ctx.Game.Objects
                .Where(o => o.AttachedToId == host.Id && !o.IsDestroyed && o.ControllerId != ctx.Player.Id)
                .ToList();
            if (intruders.Count == 0)
            {
                ctx.Game.Log.Add($"No intruders found in {host.Name}.");
                return;
            }
            foreach (var obj in intruders)
            {
                obj.FaceDown = false;
                ctx.Game.Log.Add($"{obj.Name} is flushed out of {host.Name}!");
                m.DestroyObject(ctx.Game, obj, ctx.Source);
            }
        });

        // Sweep a building: enemy cards attached to it are revealed and take direct damage
        e.Register("damage_infiltrators", ctx =>
        {
            var host = ctx.ResolveScope("target");
            if (host == null) return;
            var intruders = ctx.Game.Objects
                .Where(o => o.AttachedToId == host.Id && !o.IsDestroyed && o.ControllerId != ctx.Player.Id)
                .ToList();
            if (intruders.Count == 0)
            {
                ctx.Game.Log.Add($"No intruders found in {host.Name}.");
                return;
            }
            foreach (var obj in intruders)
            {
                obj.FaceDown = false;
                ctx.Game.Log.Add($"{obj.Name} is caught in the sweep of {host.Name}!");
                m.ApplyDirectDamage(ctx.Game, obj, ctx.Effect.Amount ?? 0, ctx.Source);
            }
        });

        // Deposit an entity-scoped resource into your HQ stockpile (corpses, biomass, ...)
        e.Register("gain_bank_resource", ctx =>
        {
            var bank = GameQueries.FindResourceBank(ctx.Game, ctx.Player.Id);
            if (bank == null)
            {
                ctx.Game.Log.Add($"{ctx.Player.Name} has no stockpile to store {ctx.Effect.ResourceId} in.");
                return;
            }
            m.GainEntityResource(ctx.Game, bank, ctx.Effect.ResourceId!, ctx.Effect.Amount ?? 0);
        });

        // Consume a module installed on your hero and convert it to energy
        e.Register("salvage_module", ctx =>
        {
            var obj = ctx.ResolveScope("target");
            if (obj == null || obj.AttachedToId == null) return;

            var def = GameQueries.GetCardDefinition(ctx.Game, obj);
            var energyGained = def?.PlayCost ?? 2;

            m.DestroyObject(ctx.Game, obj, ctx.Source);
            m.GainResource(ctx.Game, ctx.Player, "energy", energyGained);
            ctx.Game.Log.Add($"{ctx.Player.Name} salvages {obj.Name} for {energyGained} energy!");
        });
    }
}
