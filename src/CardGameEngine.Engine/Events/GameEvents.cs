using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>
/// Every meaningful state change flows through the event bus as one of these.
/// Triggers, logging extensions and future reaction windows all hang off events
/// instead of being hardcoded into action execution.
/// </summary>
public static class GameEventTypes
{
    public const string TurnStarted = "turnStarted";
    public const string PhaseEntered = "phaseEntered";
    public const string CardDrawn = "cardDrawn";
    public const string CardDiscarded = "cardDiscarded"; // Player = who discarded, Target = the card
    public const string CardPlayed = "cardPlayed";
    public const string ObjectSummoned = "objectSummoned";
    public const string ObjectDestroyed = "objectDestroyed";
    public const string UnitKilled = "unitKilled"; // Source = killer, Target = victim (combat kills)
    public const string DamageDealt = "damageDealt";
    public const string Healed = "healed";
    public const string ResourceGained = "resourceGained";
    public const string ResourceSpent = "resourceSpent";
    public const string AttackResolved = "attackResolved";
    public const string ModuleInstalled = "moduleInstalled";
    public const string ObjectTransformed = "objectTransformed";
    public const string HeroRevived = "heroRevived";
    public const string ObjectMovedLine = "objectMovedLine";
    public const string ModifierExpired = "modifierExpired";
}

public class GameEvent
{
    public string Type { get; init; } = "";
    public ObjectInstance? Source { get; init; }
    public ObjectInstance? Target { get; init; }
    public PlayerInstance? Player { get; init; }
    public int Amount { get; init; }
    public string? ResourceId { get; init; }
}

public class EventBus
{
    private readonly List<Action<GameInstance, GameEvent>> _subscribers = new();
    private int _depth;
    private const int MaxDepth = 32; // cascading events are fine; infinite loops are not

    public void Subscribe(Action<GameInstance, GameEvent> handler) => _subscribers.Add(handler);

    public void Publish(GameInstance game, GameEvent evt)
    {
        if (_depth >= MaxDepth)
        {
            game.Log.Add($"[engine] Event cascade limit reached; '{evt.Type}' not dispatched.");
            return;
        }

        _depth++;
        try
        {
            foreach (var handler in _subscribers)
                handler(game, evt);
        }
        finally
        {
            _depth--;
        }
    }
}
