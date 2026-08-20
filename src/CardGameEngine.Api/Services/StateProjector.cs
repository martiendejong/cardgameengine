using CardGameEngine.Core.Dtos;
using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;

namespace CardGameEngine.Api.Services;

/// <summary>
/// Projects the authoritative game state into what a specific viewer may see.
/// Opponent hands and all decks are redacted server-side — hidden information
/// never leaves the server. An empty viewerId means omniscient (hotseat mode).
/// </summary>
public class StateProjector
{
    private readonly RuleEngine _ruleEngine;

    public StateProjector(RuleEngine ruleEngine) => _ruleEngine = ruleEngine;

    public GameStateDto Build(GameInstance game, string viewerId)
    {
        bool omniscient = string.IsNullOrEmpty(viewerId);
        var winner = game.Players.FirstOrDefault(p => p.IsWinner);
        var actionsFor = omniscient
            ? (game.State == GameState.WaitingForReaction ? game.ReactionPlayerId ?? game.ActivePlayerId : game.ActivePlayerId)
            : viewerId;

        return new GameStateDto
        {
            MatchId = game.Id,
            GameId = game.Definition.Id,
            CurrentPhaseId = game.CurrentPhaseId,
            ActivePlayerId = game.ActivePlayerId,
            State = game.State,
            TurnNumber = game.TurnNumber,
            Winner = winner?.Name,
            Log = game.Log.TakeLast(60).ToList(),
            Players = game.Players.Select(p => new PlayerStateDto
            {
                Id = p.Id,
                Name = p.Name,
                Resources = new Dictionary<string, int>(p.Resources),
                RelevantResources = new List<string>(p.RelevantResources),
                IsWinner = p.IsWinner,
                IsLoser = p.IsLoser,
                IsBot = p.IsBot,
                UsesHousing = PlayerUsesHousing(game, p.Id),
                HousingUsed = GameQueries.HousingUsed(game, p.Id),
                HousingCapacity = GameQueries.HousingCapacity(game, p.Id)
            }).ToList(),
            Objects = game.Objects.Select(o => ProjectObject(game, o, viewerId, omniscient)).ToList(),
            AvailableActions = _ruleEngine.GetAvailableActions(game, actionsFor), // catalog self-gates
            PendingChoice = game.PendingChoices.FirstOrDefault(c => omniscient || c.PlayerId == viewerId),
            ReactionPlayerId = game.ReactionPlayerId,
            ReactionWindowEvent = game.ReactionWindowEvent,
            Encounter = game.Encounter == null ? null : new EncounterDto
            {
                MissionId = game.Encounter.MissionId,
                PlayerId = game.Encounter.PlayerId,
                RewardCards = game.Encounter.RewardCards,
                RewardCopies = game.Definition.DeckRules?.MaxCopies ?? 1,
                PendingSpawnCount = game.Encounter.PendingSpawns.Count
            }
        };
    }

    private ObjectStateDto ProjectObject(GameInstance game, ObjectInstance o, string viewerId, bool omniscient)
    {
        // Secrets and infiltrated spies are private state; hand/deck as before
        bool hiddenZone = o.ZoneId == "hand" || o.ZoneId == "deck" || o.ZoneId == "secrets" || o.FaceDown;
        bool visible = omniscient || !hiddenZone || o.OwnerId == viewerId;

        // Own deck contents are hidden even from the owner — only counts are known
        if (!omniscient && o.ZoneId == "deck")
            visible = false;

        if (!visible)
        {
            return new ObjectStateDto
            {
                Id = o.Id,
                DefinitionId = "hidden",
                Name = "Hidden Card",
                ObjectType = "hidden",
                OwnerId = o.OwnerId,
                ControllerId = o.ControllerId,
                ZoneId = o.ZoneId,
                // "something is lurking there" is public information
                AttachedToId = o.AttachedToId,
                Slot = o.Slot
            };
        }

        return new ObjectStateDto
        {
            Id = o.Id,
            DefinitionId = o.DefinitionId,
            Name = o.Name,
            ObjectType = o.ObjectType,
            OwnerId = o.OwnerId,
            ControllerId = o.ControllerId,
            ZoneId = o.ZoneId,
            Properties = BuildEffectiveProperties(game, o),
            Resources = new Dictionary<string, int>(o.Resources),
            Tags = new List<string>(o.Tags),
            IsTapped = o.IsTapped,
            IsDestroyed = o.IsDestroyed,
            Line = o.Line,
            HasMovedThisTurn = o.HasMovedThisTurn,
            HasSummoningSickness = o.HasSummoningSickness,
            AttachedToId = o.AttachedToId,
            Slot = o.Slot,
            Icon = game.Definition.Cards.FirstOrDefault(c => c.Id == o.DefinitionId)?.Icon,
            HousingCost = game.Definition.Cards.FirstOrDefault(c => c.Id == o.DefinitionId)?.HousingCost,
            HousingProvided = game.Definition.Cards.FirstOrDefault(c => c.Id == o.DefinitionId)?.HousingProvided,
            UnderConstruction = o.UnderConstruction,
            ConstructionProgress = o.ConstructionProgress,
            ConstructionRequirement = game.Definition.Cards.FirstOrDefault(c => c.Id == o.DefinitionId)?.ConstructionRequirement,
            Lifetime = o.Lifetime,
            FaceDown = o.FaceDown
        };
    }

    /// <summary>Housing only matters (and only shows) for players whose cards reference it.</summary>
    private static bool PlayerUsesHousing(GameInstance game, string playerId)
    {
        return game.Objects
            .Where(o => o.OwnerId == playerId)
            .Select(o => game.Definition.Cards.FirstOrDefault(c => c.Id == o.DefinitionId))
            .Any(def => def?.HousingCost != null || def?.HousingProvided != null);
    }

    private Dictionary<string, int> BuildEffectiveProperties(GameInstance game, ObjectInstance obj)
    {
        var result = new Dictionary<string, int>(obj.Properties);
        foreach (var propId in result.Keys.ToList())
            result[propId] = _ruleEngine.GetEffectiveProperty(game, obj, propId);
        return result;
    }
}

/// <summary>Tracks which SignalR connection sits in which seat of which match.</summary>
public class MatchConnectionRegistry
{
    private readonly object _lock = new();
    private readonly Dictionary<string, (string matchId, string playerId)> _connections = new();

    public void Add(string connectionId, string matchId, string playerId)
    {
        lock (_lock) _connections[connectionId] = (matchId, playerId);
    }

    public void Remove(string connectionId)
    {
        lock (_lock) _connections.Remove(connectionId);
    }

    public List<(string connectionId, string playerId)> GetForMatch(string matchId)
    {
        lock (_lock)
            return _connections
                .Where(kv => kv.Value.matchId == matchId)
                .Select(kv => (kv.Key, kv.Value.playerId))
                .ToList();
    }
}
