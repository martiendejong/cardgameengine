using CardGameEngine.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardGameEngine.Api.Controllers;

[ApiController]
[Route("api/matches")]
public class MatchesController : ControllerBase
{
    private readonly MatchService _matchService;

    public MatchesController(MatchService matchService)
    {
        _matchService = matchService;
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateMatchRequest request)
    {
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
