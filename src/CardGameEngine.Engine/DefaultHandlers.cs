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
            if (cardDef.HousingCost is int housing &&
                !GameQueries.HasHousingFor(ctx.Game, ctx.Player.Id, housing))
            {
                ctx.Game.Log.Add($"{ctx.Player.Name} has no housing left for a {cardDef.Name}!");
                return;
            }
            var obj = s.Factory.CreateObjectInstance(ctx.Game, cardDef, ctx.Player.Id, "battlefield");
            obj.HasSummoningSickness = true;
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
            var resId = ctx.Effect.ResourceId ?? "gold";
            var storeId = ctx.Effect.Tag ?? "loot";
            var taken = Math.Min(ctx.Effect.Amount ?? 0, opponent.Resources.GetValueOrDefault(resId));
            if (taken <= 0)
            {
                ctx.Game.Log.Add($"{ctx.Source.Name} finds nothing to plunder from {opponent.Name}.");
                return;
            }
            opponent.Resources[resId] = opponent.Resources.GetValueOrDefault(resId) - taken;
            ctx.Source.Resources[storeId] = ctx.Source.Resources.GetValueOrDefault(storeId) + taken;
            ctx.Game.Log.Add($"{ctx.Source.Name} plunders {taken} {resId} from {opponent.Name}! ({taken} tokens stored)");
        });

        e.Register("revive_hero", ctx =>
        {
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
    }
}
