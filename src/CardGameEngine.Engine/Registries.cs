using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

// ============================================================================
// Extensible registries: a new mechanic is a new registered handler, not an
// edit to a switch statement buried in the engine.
// ============================================================================

public class ConditionContext
{
    public required GameInstance Game { get; init; }
    public required ConditionDefinition Condition { get; init; }
    public required ObjectInstance Object { get; init; }
    public required PlayerInstance Player { get; init; }
}

public class ConditionEvaluator
{
    private readonly Dictionary<string, Func<ConditionContext, bool>> _handlers = new();

    public void Register(string type, Func<ConditionContext, bool> handler) => _handlers[type] = handler;

    public bool Check(ConditionContext ctx) =>
        !_handlers.TryGetValue(ctx.Condition.Type, out var handler) || handler(ctx);
}

public class CostContext
{
    public required GameInstance Game { get; init; }
    public required CostDefinition Cost { get; init; }
    public required ObjectInstance Object { get; init; }
    public required PlayerInstance Player { get; init; }
}

public class CostProcessor
{
    private readonly Dictionary<string, (Func<CostContext, bool> canPay, Action<CostContext> pay)> _handlers = new();

    public void Register(string type, Func<CostContext, bool> canPay, Action<CostContext> pay) =>
        _handlers[type] = (canPay, pay);

    public bool CanPay(CostContext ctx) =>
        !_handlers.TryGetValue(ctx.Cost.Type, out var h) || h.canPay(ctx);

    public void Pay(CostContext ctx)
    {
        if (_handlers.TryGetValue(ctx.Cost.Type, out var h)) h.pay(ctx);
    }
}

public class EffectContext
{
    public required GameInstance Game { get; init; }
    public required EffectDefinition Effect { get; init; }
    public required PlayerInstance Player { get; init; }
    public ObjectInstance? Source { get; init; }
    public ObjectInstance? Target { get; init; }
    public int? ChosenAmount { get; init; }

    public ObjectInstance? ResolveScope(string? fallback = null)
    {
        return (Effect.Scope ?? fallback) switch
        {
            "self" => Source,
            "target" => Target,
            // the object this card is attached to / infiltrated into
            "host" => Source?.AttachedToId != null
                ? Game.Objects.FirstOrDefault(o => o.Id == Source.AttachedToId)
                : null,
            _ => Source
        };
    }
}

public class EffectProcessor
{
    private readonly Dictionary<string, Action<EffectContext>> _handlers = new();

    public void Register(string type, Action<EffectContext> handler) => _handlers[type] = handler;

    public void Process(EffectContext ctx)
    {
        if (_handlers.TryGetValue(ctx.Effect.Type, out var handler))
            handler(ctx);
    }

    /// <summary>Apply an ability's effect list: once per target, or once with no target.</summary>
    public void ApplyAbility(GameInstance game, AbilityDefinition ability, ObjectInstance? source,
        PlayerInstance player, List<string> targetIds, int? chosenAmount = null)
    {
        var targets = targetIds
            .Select(id => game.Objects.FirstOrDefault(o => o.Id == id))
            .Where(o => o != null)
            .Cast<ObjectInstance>()
            .ToList();

        foreach (var effect in ability.Effects)
        {
            if (targets.Count > 0)
            {
                foreach (var target in targets)
                    Process(new EffectContext { Game = game, Effect = effect, Source = source, Target = target, Player = player, ChosenAmount = chosenAmount });
            }
            else
            {
                Process(new EffectContext { Game = game, Effect = effect, Source = source, Target = null, Player = player, ChosenAmount = chosenAmount });
            }
        }
    }
}
