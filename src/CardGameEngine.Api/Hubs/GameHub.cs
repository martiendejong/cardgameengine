using CardGameEngine.Api.Services;
using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;
using Microsoft.AspNetCore.SignalR;

namespace CardGameEngine.Api.Hubs;

public class GameHub : Hub
{
    private readonly MatchService _matchService;
    private readonly RuleEngine _ruleEngine;
    private readonly StateProjector _projector;
    private readonly MatchConnectionRegistry _registry;
    private readonly ILogger<GameHub> _logger;

    public GameHub(MatchService matchService, RuleEngine ruleEngine, StateProjector projector,
        MatchConnectionRegistry registry, ILogger<GameHub> logger)
    {
        _matchService = matchService;
        _ruleEngine = ruleEngine;
        _projector = projector;
        _registry = registry;
        _logger = logger;
    }

    /// <summary>Join a match in a seat. playerId = "" joins as omniscient hotseat/spectator.</summary>
    public async Task JoinMatch(string matchId, string playerId)
    {
        var game = _matchService.GetMatch(matchId);
        if (game == null)
        {
            await Clients.Caller.SendAsync("ActionError", $"Match '{matchId}' not found");
            return;
        }

        _registry.Add(Context.ConnectionId, matchId, playerId);
        _logger.LogInformation("Connection {Conn} joined match {MatchId} as '{PlayerId}'",
            Context.ConnectionId, matchId, playerId);

        await Clients.Caller.SendAsync("GameStateUpdate", _projector.Build(game, playerId));
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

        await BroadcastStateUpdate(matchId, game);
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

        await BroadcastStateUpdate(matchId, game);
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
        await BroadcastStateUpdate(matchId, game);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _registry.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>Each connection receives its own projection — hidden information stays server-side.</summary>
    private async Task BroadcastStateUpdate(string matchId, GameInstance game)
    {
        foreach (var (connectionId, playerId) in _registry.GetForMatch(matchId))
        {
            await Clients.Client(connectionId)
                .SendAsync("GameStateUpdate", _projector.Build(game, playerId));
        }
    }
}
