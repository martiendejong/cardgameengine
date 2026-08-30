using CardGameEngine.Api.Data;
using CardGameEngine.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CardGameEngine.Api.Controllers;

[ApiController]
[Route("api/matches")]
[Authorize]
public class MatchesController : ControllerBase
{
    private readonly MatchService _matchService;
    private readonly CampaignService _campaign;
    private readonly UserManager<ApplicationUser> _userManager;

    public MatchesController(MatchService matchService, CampaignService campaign, UserManager<ApplicationUser> userManager)
    {
        _matchService = matchService;
        _campaign = campaign;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMatchRequest request)
    {
        var userId = this.UserId();

        // Admin lobby seats (no deck limits, full card pool) are an account privilege,
        // not a checkbox anyone may tick: the caller must hold the Admin role.
        if (request.Players.Any(p => p.IsAdmin))
        {
            var caller = await _userManager.GetUserAsync(User);
            bool callerIsAdmin = caller != null && await _userManager.IsInRoleAsync(caller, "Admin");
            if (!callerIsAdmin)
                return StatusCode(403, "Admin mode requires an admin account — log in as an admin or disable the Admin toggle.");
        }

        // Multiplayer gate: real (non-bot) non-admin human seats require a campaign profile
        // whose collection can field a minimum-size deck. Admin mode bypasses. A "vs Computer"
        // match never touches a player's collection state, so it's exempt regardless of the
        // human seat's admin flag — only a genuine human-vs-human match needs the unlock.
        var hasBot = request.Players.Any(p => p.IsBot);
        var needsUnlock = !hasBot && request.Players.Any(p => !p.IsAdmin && !p.IsBot);
        if (needsUnlock)
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = _campaign.GetProfile(userId, user?.DisplayName ?? "");
            if (!_campaign.IsMultiplayerUnlocked(request.GameId, profile))
            {
                var needed = _campaign.RequiredCards(request.GameId);
                var have = CampaignService.CollectionCount(profile);
                return BadRequest(
                    $"Multiplayer unlocks once your collection holds {needed} cards. " +
                    $"You have {have} — complete campaign missions to earn more.");
            }
        }

        var (game, error) = _matchService.CreateMatch(request.GameId, request.Players, userId);
        if (game == null)
            return BadRequest(error);

        // Winning a vs-Computer quick match grants a random card reward, same as a campaign
        // mission — wire up a lightweight encounter so the existing victory-screen reward
        // display and /api/campaign/complete flow can be reused as-is.
        if (hasBot)
        {
            var botPlayer = game.Players.FirstOrDefault(p => p.IsBot);
            var humanPlayer = game.Players.FirstOrDefault(p => !p.IsBot);
            if (botPlayer != null && humanPlayer != null)
                _campaign.AttachQuickMatchReward(game, userId, humanPlayer.Id, botPlayer.Id);
        }

        return Ok(new
        {
            matchId = game.Id,
            players = game.Players.Select(p => new { p.Id, p.Name })
        });
    }

    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var game = _matchService.GetMatch(id);
        if (game == null)
            return NotFound($"Match '{id}' not found");

        return Ok(new
        {
            matchId = game.Id,
            currentPhaseId = game.CurrentPhaseId,
            activePlayerId = game.ActivePlayerId,
            state = game.State.ToString(),
            players = game.Players.Select(p => new { p.Id, p.Name, p.Resources, p.IsWinner, p.IsLoser }),
            objectCount = game.Objects.Count
        });
    }
}
