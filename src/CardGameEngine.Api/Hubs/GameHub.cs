using System.Security.Claims;
using CardGameEngine.Api.Services;
using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CardGameEngine.Api.Hubs;

[Authorize]
public class GameHub : Hub
{
    private readonly MatchService _matchService;
    private readonly RuleEngine _ruleEngine;
    private readonly StateProjector _projector;
    private readonly MatchConnectionRegistry _registry;
    private readonly BotService _bot;
    private readonly ILogger<GameHub> _logger;

    public GameHub(MatchService matchService, RuleEngine ruleEngine, StateProjector projector,
        MatchConnectionRegistry registry, BotService bot, ILogger<GameHub> logger)
    {
        _matchService = matchService;
        _ruleEngine = ruleEngine;
        _projector = projector;
        _registry = registry;
        _bot = bot;
        _logger = logger;
    }

    private string? UserId => Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>Join a match in a seat. playerId = "" joins as omniscient hotseat/spectator
    /// (only the match's creator account may do this — it can act as every non-bot seat).
    /// A non-empty playerId is bound to whichever account first joins it (invite-link flow);
    /// once bound, only that same account may join or act as that seat again.</summary>
    public async Task JoinMatch(string matchId, string playerId)
    {
        var userId = UserId;
        if (userId == null)
        {
            await Clients.Caller.SendAsync("ActionError", "Not authenticated");
            return;
        }

        var game = _matchService.GetMatch(matchId);
        if (game == null)
        {
            await Clients.Caller.SendAsync("ActionError", $"Match '{matchId}' not found");
            return;
        }

        if (playerId == "")
        {
            if (game.CreatorUserId != userId)
            {
                await Clients.Caller.SendAsync("ActionError", "Not authorized to join this match");
                return;
            }
        }
        else
        {
            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null || player.IsBot)
            {
                await Clients.Caller.SendAsync("ActionError", $"Seat '{playerId}' not found");
                return;
            }
            if (player.OwnerUserId == null)
                player.OwnerUserId = userId; // first claim wins (invite-link flow)
            else if (player.OwnerUserId != userId)
            {
                await Clients.Caller.SendAsync("ActionError", "This seat belongs to another player");
                return;
            }
        }

        _registry.Add(Context.ConnectionId, matchId, playerId);
        _logger.LogInformation("Connection {Conn} (user {User}) joined match {MatchId} as '{PlayerId}'",
            Context.ConnectionId, userId, matchId, playerId);

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
        if (!IsAuthorizedForSeat(game, playerId))
        {
            await Clients.Caller.SendAsync("ActionError", "Not authorized to act as this player");
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
        if (!IsAuthorizedForSeat(game, playerId))
        {
            await Clients.Caller.SendAsync("ActionError", "Not authorized to act as this player");
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
        if (!IsAuthorizedForSeat(game, playerId))
        {
            await Clients.Caller.SendAsync("ActionError", "Not authorized to act as this player");
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

    /// <summary>
    /// True when THIS connection is allowed to act as requestedPlayerId — derived from what it
    /// actually joined as (verified in JoinMatch), never from the caller-supplied id alone. A
    /// connection that joined as "" (hotseat) was already proven to be the match creator and may
    /// act as any non-bot seat; a connection that joined a specific seat may only act as that seat.
    /// </summary>
    private bool IsAuthorizedForSeat(GameInstance game, string requestedPlayerId)
    {
        var reg = _registry.Get(Context.ConnectionId);
        if (reg == null || reg.Value.matchId != game.Id) return false;
        var joinedAs = reg.Value.playerId;
        return joinedAs == "" || joinedAs == requestedPlayerId;
    }

    /// <summary>Each connection receives its own projection — hidden information stays server-side.</summary>
    private async Task BroadcastStateUpdate(string matchId, GameInstance game)
    {
        // If the turn passed to a computer player, let it play before broadcasting
        _bot.PlayBotTurns(game);

        foreach (var (connectionId, playerId) in _registry.GetForMatch(matchId))
        {
            await Clients.Client(connectionId)
                .SendAsync("GameStateUpdate", _projector.Build(game, playerId));
        }
    }
}
