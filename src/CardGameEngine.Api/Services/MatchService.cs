using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;

namespace CardGameEngine.Api.Services;

public class PlayerSetup
{
    public string Name { get; set; } = "";
    public string? Id { get; set; }
}

public class CreateMatchRequest
{
    public string GameId { get; set; } = "";
    public List<PlayerSetup> Players { get; set; } = new();
}

public class MatchService
{
    private readonly Dictionary<string, GameInstance> _matches = new();
    private readonly GameDefinitionService _definitionService;
    private readonly RuleEngine _ruleEngine;
    private readonly ILogger<MatchService> _logger;
    private int _matchCounter = 0;

    public MatchService(GameDefinitionService definitionService, RuleEngine ruleEngine, ILogger<MatchService> logger)
    {
        _definitionService = definitionService;
        _ruleEngine = ruleEngine;
        _logger = logger;
    }

    public (GameInstance? game, string? error) CreateMatch(string gameId, List<PlayerSetup> players)
    {
        var definition = _definitionService.GetById(gameId);
        if (definition == null)
            return (null, $"Game definition '{gameId}' not found");

        if (players.Count < 2)
            return (null, "At least 2 players required");

        _matchCounter++;
        var matchId = $"match_{_matchCounter}_{DateTime.UtcNow:yyyyMMddHHmmss}";

        var game = new GameInstance
        {
            Id = matchId,
            Definition = definition
        };

        foreach (var setup in players)
        {
            game.Players.Add(new PlayerInstance
            {
                Id = setup.Id ?? Guid.NewGuid().ToString("N")[..8],
                Name = setup.Name
            });
        }

        _ruleEngine.ExecuteSetup(game);
        _matches[matchId] = game;

        _logger.LogInformation("Created match {MatchId} for game {GameId}", matchId, gameId);
        return (game, null);
    }

    public GameInstance? GetMatch(string matchId) =>
        _matches.TryGetValue(matchId, out var game) ? game : null;

    public IEnumerable<GameInstance> GetAllMatches() => _matches.Values;
}
