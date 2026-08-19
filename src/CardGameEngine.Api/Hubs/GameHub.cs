using CardGameEngine.Api.Services;
using CardGameEngine.Core.Dtos;
using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;
using Microsoft.AspNetCore.SignalR;

namespace CardGameEngine.Api.Hubs;

public class GameHub : Hub
{
    private readonly MatchService _matchService;
    private readonly RuleEngine _ruleEngine;
    private readonly ILogger<GameHub> _logger;

    public GameHub(MatchService matchService, RuleEngine ruleEngine, ILogger<GameHub> logger)
    {
        _matchService = matchService;
        _ruleEngine = ruleEngine;
        _logger = logger;
    }

    public async Task JoinMatch(string matchId, string playerId)
    {
        var game = _matchService.GetMatch(matchId);
        if (game == null)
        {
            await Clients.Caller.SendAsync("ActionError", $"Match '{matchId}' not found");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, matchId);
        _logger.LogInformation("Player {PlayerId} joined match {MatchId}", playerId, matchId);

        // Send current state to the joining player
        var stateDto = BuildStateDto(game, playerId);
        await Clients.Caller.SendAsync("GameStateUpdate", stateDto);
    }

    public async Task SendAction(string matchId, string playerId, ActionRequest action)
    {
        var game = _matchService.GetMatch(matchId);
        if (game == null)
        {
            await Clients.Caller.SendAsync("ActionError", $"Match '{matchId}' not found");
            return;
        }

        var (success, error) = _ruleEngine.ExecuteAction(game, playerId, action);
        if (!success)
        {
            await Clients.Caller.SendAsync("ActionError", error ?? "Unknown error");
            return;
        }

        await BroadcastStateUpdate(matchId, game, playerId);
    }

    public async Task ResolveChoice(string matchId, string playerId, string choiceId, List<string> selectedIds)
    {
        var game = _matchService.GetMatch(matchId);
        if (game == null)
        {
            await Clients.Caller.SendAsync("ActionError", $"Match '{matchId}' not found");
            return;
        }

        var (success, error) = _ruleEngine.ResolveChoice(game, playerId, choiceId, selectedIds);
        if (!success)
        {
            await Clients.Caller.SendAsync("ActionError", error ?? "Unknown error");
            return;
        }

        await BroadcastStateUpdate(matchId, game, playerId);
    }

    public async Task EndPhase(string matchId, string playerId)
    {
        var game = _matchService.GetMatch(matchId);
        if (game == null)
        {
            await Clients.Caller.SendAsync("ActionError", $"Match '{matchId}' not found");
            return;
        }

        _ruleEngine.EndPhase(game, playerId);
        await BroadcastStateUpdate(matchId, game, playerId);
    }

    private async Task BroadcastStateUpdate(string matchId, GameInstance game, string requestingPlayerId)
    {
        // Send state to each player with their personalized available actions
        foreach (var player in game.Players)
        {
            var stateDto = BuildStateDto(game, player.Id);
            // We broadcast to the group; clients will filter by their playerId
            // For simplicity, send full state to everyone (they share the same browser in 2-player local)
        }

        // Send combined state (with actions for the active player)
        var dto = BuildStateDto(game, game.ActivePlayerId);
        await Clients.Group(matchId).SendAsync("GameStateUpdate", dto);
    }

    private GameStateDto BuildStateDto(GameInstance game, string forPlayerId)
    {
        var winner = game.Players.FirstOrDefault(p => p.IsWinner);

        var dto = new GameStateDto
        {
            MatchId = game.Id,
            CurrentPhaseId = game.CurrentPhaseId,
            ActivePlayerId = game.ActivePlayerId,
            State = game.State,
            TurnNumber = game.TurnNumber,
            Winner = winner?.Name,
            Log = game.Log.TakeLast(50).ToList(),
            Players = game.Players.Select(p => new PlayerStateDto
            {
                Id = p.Id,
                Name = p.Name,
                Resources = new Dictionary<string, int>(p.Resources),
                IsWinner = p.IsWinner,
                IsLoser = p.IsLoser
            }).ToList(),
            Objects = game.Objects.Select(o => new ObjectStateDto
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
                Slot = o.Slot
            }).ToList(),
            AvailableActions = game.ActivePlayerId == forPlayerId
                ? _ruleEngine.GetAvailableActions(game, forPlayerId)
                : new List<AvailableAction>(),
            PendingChoice = game.PendingChoices.FirstOrDefault(c => c.PlayerId == forPlayerId)
        };

        return dto;
    }

    private Dictionary<string, int> BuildEffectiveProperties(GameInstance game, ObjectInstance obj)
    {
        var result = new Dictionary<string, int>(obj.Properties);
        foreach (var propId in result.Keys.ToList())
        {
            result[propId] = _ruleEngine.GetEffectiveProperty(game, obj, propId);
        }
        return result;
    }
}
