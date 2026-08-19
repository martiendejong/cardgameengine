namespace CardGameEngine.Core.Runtime;

public class GameInstance
{
    public string Id { get; set; } = "";
    public Definitions.GameDefinition Definition { get; set; } = new();
    public List<PlayerInstance> Players { get; set; } = new();
    public List<ObjectInstance> Objects { get; set; } = new();
    public ResolutionStack Stack { get; set; } = new();
    public string CurrentPhaseId { get; set; } = "";
    public string ActivePlayerId { get; set; } = "";
    public GameState State { get; set; }
    public List<string> Log { get; set; } = new();
    public List<ModifierInstance> ActiveModifiers { get; set; } = new();
    public List<PendingChoice> PendingChoices { get; set; } = new();
    public int TurnNumber { get; set; } = 1;
    // Ordered object ids per player: index 0 is the top of the deck
    public Dictionary<string, List<string>> DeckOrder { get; set; } = new();
    // "objectId:event" keys for once-per-turn triggers; cleared when the turn passes
    public HashSet<string> FiredOncePerTurn { get; set; } = new();
}

public enum GameState
{
    WaitingForAction,
    WaitingForChoice,
    WaitingForReaction,
    ResolvingStack,
    GameEnded
}

public class PlayerInstance
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public Dictionary<string, int> Resources { get; set; } = new();
    public bool IsWinner { get; set; }
    public bool IsLoser { get; set; }
    public bool IsAdmin { get; set; }
    public List<string> DeckList { get; set; } = new(); // expanded card definition ids
    public string? HqCardId { get; set; }   // headquarters placed at setup
    public string? HeroCardId { get; set; } // hero placed at setup
}

public class ObjectInstance
{
    public string Id { get; set; } = "";
    public string DefinitionId { get; set; } = "";
    public string Name { get; set; } = "";
    public string ObjectType { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string ControllerId { get; set; } = "";
    public string ZoneId { get; set; } = "";
    public Dictionary<string, int> Properties { get; set; } = new();
    public Dictionary<string, int> Resources { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public bool IsTapped { get; set; }
    public bool IsDestroyed { get; set; }
    public string Line { get; set; } = ""; // battlefield line ("front"/"back") when lines are enabled
    public bool HasMovedThisTurn { get; set; }
    public bool HasSummoningSickness { get; set; } // cannot move or attack the turn it arrived
    public string? AttachedToId { get; set; } // host object when this is an installed module
    public string? Slot { get; set; }         // which slot this module occupies on its host
}

public class ResolutionStack
{
    public List<StackItem> Items { get; set; } = new();
    public bool IsEmpty => Items.Count == 0;
    public StackItem? Top => Items.Count > 0 ? Items[^1] : null;
    public void Push(StackItem item) => Items.Add(item);
    public StackItem Pop()
    {
        var item = Items[^1];
        Items.RemoveAt(Items.Count - 1);
        return item;
    }
}

public class StackItem
{
    public string Id { get; set; } = "";
    public string ControllerId { get; set; } = "";
    public string SourceObjectId { get; set; } = "";
    public string AbilityId { get; set; } = "";
    public List<string> TargetIds { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class ModifierInstance
{
    public string Id { get; set; } = "";
    public string TargetObjectId { get; set; } = "";
    public string PropertyId { get; set; } = "";
    public int Amount { get; set; }
    public string ExpiresOn { get; set; } = "endOfTurn"; // event type; "never" for permanent
    public string? SourceObjectId { get; set; } // removed when this object leaves the battlefield
}

public class PendingChoice
{
    public string Id { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string StackItemId { get; set; } = "";
    public Definitions.ChoiceDefinition Definition { get; set; } = new();
    public List<string> ValidOptions { get; set; } = new();
}

public class AvailableAction
{
    public string Type { get; set; } = ""; // "activateAbility", "attack", "endPhase"
    public string? SourceObjectId { get; set; }
    public string? AbilityId { get; set; }
    public string Label { get; set; } = "";
    public bool Available { get; set; }
    public string? UnavailableReason { get; set; }
    public Definitions.ChoiceDefinition? RequiresChoice { get; set; }
    public List<string>? ValidTargets { get; set; }
}

public class ActionRequest
{
    public string Type { get; set; } = ""; // "activateAbility", "attack", "endPhase"
    public string? SourceObjectId { get; set; }
    public string? AbilityId { get; set; }
    public List<string> TargetIds { get; set; } = new();
}
