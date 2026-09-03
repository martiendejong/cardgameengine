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
    public EncounterDto? Encounter { get; set; }
}

/// <summary>Campaign context shown in the winner overlay (mission + rewards).</summary>
public class EncounterDto
{
    public string MissionId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public List<string> RewardCards { get; set; } = new();
    public int RewardCopies { get; set; } = 1; // playset size granted per reward card
    public int PendingSpawnCount { get; set; }
    // A Lobby "vs Computer" quick match rather than a scripted campaign mission —
    // the victory screen reuses the same reward display, but a loss falls back to
    // the plain "Game Over" screen instead of the mission "town has fallen" one.
    public bool IsQuickMatch { get; set; }
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
