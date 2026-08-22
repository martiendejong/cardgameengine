using CardGameEngine.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardGameEngine.Api.Controllers;

public class StartMissionRequest
{
    public string GameId { get; set; } = "town-tcg";
    public string MissionId { get; set; } = "";
    public string Profile { get; set; } = "";
    public Dictionary<string, int>? CustomDeck { get; set; }
    public string? CustomHq { get; set; }
    public string? CustomHero { get; set; }
}

public class CompleteMissionRequest
{
    public string MatchId { get; set; } = "";
}

[ApiController]
[Route("api/campaign")]
public class CampaignController : ControllerBase
{
    private readonly CampaignService _campaign;
    private readonly MatchService _matches;

    public CampaignController(CampaignService campaign, MatchService matches)
    {
        _campaign = campaign;
        _matches = matches;
    }

    /// <summary>Mission list with unlock/completion status for a profile.</summary>
    [HttpGet]
    public IActionResult GetCampaign([FromQuery] string gameId = "town-tcg", [FromQuery] string profile = "")
    {
        var missions = _campaign.GetMissions(gameId);
        var prof = _campaign.GetProfile(profile);
        return Ok(new
        {
            profile = new { prof.Name, prof.CompletedMissions, prof.Collection },
            collectionCount = CampaignService.CollectionCount(prof),
            requiredCards = _campaign.RequiredCards(gameId),
            multiplayerUnlocked = _campaign.IsMultiplayerUnlocked(gameId, prof),
            missions = missions.Select(m => new
            {
                m.Id,
                m.Campaign,
                m.Name,
                m.Description,
                m.Rewards,
                completed = prof.CompletedMissions.Contains(m.Id),
                unlocked = _campaign.IsUnlocked(prof, m),
            })
        });
    }

    [HttpPost("start")]
    public IActionResult Start([FromBody] StartMissionRequest request)
    {
        var (game, error) = _campaign.StartMission(request.GameId, request.MissionId, request.Profile, request.CustomDeck, request.CustomHq, request.CustomHero);
        if (game == null)
            return BadRequest(error);

        return Ok(new
        {
            matchId = game.Id,
            playerId = game.Encounter!.PlayerId,
            missionId = game.Encounter.MissionId,
        });
    }

    [HttpPost("complete")]
    public IActionResult Complete([FromBody] CompleteMissionRequest request)
    {
        var game = _matches.GetMatch(request.MatchId);
        if (game == null)
            return NotFound($"Match '{request.MatchId}' not found");

        var (granted, message, profile) = _campaign.CompleteMission(game);
        return Ok(new
        {
            granted,
            message,
            rewards = granted ? game.Encounter!.RewardCards : new List<string>(),
            profile = profile == null ? null : new { profile.Name, profile.CompletedMissions, profile.Collection }
        });
    }
}
