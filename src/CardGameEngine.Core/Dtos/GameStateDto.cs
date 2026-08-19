using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Core.Dtos;

public class GameStateDto
{
    public string MatchId { get; set; } = "";
    public string GameId { get; set; } = "";
    public string CurrentPhaseId { get; set; } = "";
    public string ActivePlayerId { get; set; } = "";
    public GameState State { get; set; }
    public List<PlayerStateDto> Players { get; set; } = new();
    public List<ObjectStateDto> Objects { get; set; } = new();
    public List<AvailableAction> AvailableActions { get; set; } = new();
    public PendingChoice? PendingChoice { get; set; }
    public List<string> Log { get; set; } = new();
    public string? Winner { get; set; }
    public int TurnNumber { get; set; }
    public string? ReactionPlayerId { get; set; }
    public string? ReactionWindowEvent { get; set; }
}

public class PlayerStateDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public Dictionary<string, int> Resources { get; set; } = new();
    public List<string> RelevantResources { get; set; } = new();
    public bool IsWinner { get; set; }
    public bool IsLoser { get; set; }
    public bool IsBot { get; set; }
    public bool UsesHousing { get; set; }
    public int HousingUsed { get; set; }
    public int HousingCapacity { get; set; }
}

public class ObjectStateDto
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
    public string Line { get; set; } = "";
    public bool HasMovedThisTurn { get; set; }
    public bool HasSummoningSickness { get; set; }
    public string? AttachedToId { get; set; }
    public string? Slot { get; set; }
    public string? Icon { get; set; }
    public int? HousingCost { get; set; }
    public int? HousingProvided { get; set; }
    public bool UnderConstruction { get; set; }
    public int ConstructionProgress { get; set; }
    public int? ConstructionRequirement { get; set; }
    public int? Lifetime { get; set; }
    public bool FaceDown { get; set; }
}
