using CardGameEngine.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardGameEngine.Api.Controllers;

public class SimulationRequest
{
    public string GameId { get; set; } = "town-tcg";
    public string DeckA { get; set; } = "town";
    public string DeckB { get; set; } = "raiders";
    public int Games { get; set; } = 20;
}

/// <summary>
/// Automated balance mode: bot-vs-bot matchups producing win rates and game-length
/// telemetry, as designed in the spec's simulation section.
/// </summary>
[ApiController]
[Route("api/simulate")]
public class SimulationController : ControllerBase
{
    private readonly MatchService _matchService;
    private readonly BotService _bot;

    public SimulationController(MatchService matchService, BotService bot)
    {
        _matchService = matchService;
        _bot = bot;
    }

    [HttpPost]
    public IActionResult Simulate([FromBody] SimulationRequest request)
    {
        var games = Math.Clamp(request.Games, 1, 100);
        int winsA = 0, winsB = 0, draws = 0;
        var turns = new List<int>();

        for (int i = 0; i < games; i++)
        {
            // Alternate who goes first to cancel first-player advantage
            bool aFirst = i % 2 == 0;
            var players = new List<PlayerSetup>
            {
                new() { Id = "p1", Name = aFirst ? "A" : "B", DeckId = aFirst ? request.DeckA : request.DeckB, IsBot = true },
                new() { Id = "p2", Name = aFirst ? "B" : "A", DeckId = aFirst ? request.DeckB : request.DeckA, IsBot = true },
            };

            var (game, error) = _matchService.CreateMatch(request.GameId, players);
            if (game == null) return BadRequest(error);

            // Both seats are bots: bounded by a hard 60-turn cap (unfinished = draw)
            int safety = 0;
            while (game.State != Core.Runtime.GameState.GameEnded
                   && game.TurnNumber < 60 && safety++ < 100)
            {
                var before = game.TurnNumber;
                _bot.PlayBotTurns(game);
                if (game.TurnNumber == before) break; // stalled
            }

            var winner = game.Players.FirstOrDefault(p => p.IsWinner);
            if (winner == null) draws++;
            else if (winner.Name == "A") winsA++;
            else winsB++;
            turns.Add(game.TurnNumber);
        }

        return Ok(new
        {
            games,
            deckA = request.DeckA,
            deckB = request.DeckB,
            winsA,
            winsB,
            draws,
            winRateA = Math.Round((double)winsA / games, 3),
            avgTurns = Math.Round(turns.Average(), 1),
            minTurns = turns.Min(),
            maxTurns = turns.Max()
        });
    }
}