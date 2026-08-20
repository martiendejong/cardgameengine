using CardGameEngine.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardGameEngine.Api.Controllers;

[ApiController]
[Route("api/matches")]
public class MatchesController : ControllerBase
{
    private readonly MatchService _matchService;
    private readonly CampaignService _campaign;

    public MatchesController(MatchService matchService, CampaignService campaign)
    {
        _matchService = matchService;
        _campaign = campaign;
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateMatchRequest request)
    {
        // Multiplayer gate: non-admin human seats require a campaign profile
        // whose collection can field a minimum-size deck. Admin mode bypasses.
        var needsUnlock = request.Players.Any(p => !p.IsAdmin && !p.IsBot);
        if (needsUnlock)
        {
            var profile = _campaign.GetProfile(request.Profile ?? "");
            if (!_campaign.IsMultiplayerUnlocked(request.GameId, profile))
            {
                var needed = _campaign.RequiredCards(request.GameId);
                var have = CampaignService.CollectionCount(profile);
                return BadRequest(
                    $"Multiplayer unlocks once your collection holds {needed} cards. " +
                    $"You have {have} — complete campaign missions to earn more.");
            }
        }

        var (game, error) = _matchService.CreateMatch(request.GameId, request.Players);
        if (game == null)
            return BadRequest(error);

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
