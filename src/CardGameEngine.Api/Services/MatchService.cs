using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;

namespace CardGameEngine.Api.Services;

public class PlayerSetup
{
    public string Name { get; set; } = "";
    public string? Id { get; set; }
    public Dictionary<string, int>? Deck { get; set; } // cardId -> copies
    public bool IsAdmin { get; set; }
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
            var deck = setup.Deck ?? definition.DeckRules?.DefaultDeck ?? new Dictionary<string, int>();

            // Validate deck contents
            foreach (var (cardId, count) in deck)
            {
                if (count < 0)
                    return (null, $"{setup.Name}: negative count for card '{cardId}'");
                var cardDef = definition.Cards.FirstOrDefault(c => c.Id == cardId);
                if (cardDef == null)
                    return (null, $"{setup.Name}: unknown card '{cardId}'");
                if (!setup.IsAdmin && cardDef.PlayCost == null)
                    return (null, $"{setup.Name}: card '{cardDef.Name}' is not deck-eligible");
            }

            // Enforce deck rules for non-admins
            if (!setup.IsAdmin && definition.DeckRules != null)
            {
                var rules = definition.DeckRules;
                var total = deck.Values.Sum();
                if (total > rules.MaxDeckSize)
                    return (null, $"{setup.Name}: deck has {total} cards, maximum is {rules.MaxDeckSize}");
                foreach (var (cardId, count) in deck)
                {
                    if (count > rules.MaxCopies)
                        return (null, $"{setup.Name}: at most {rules.MaxCopies} copies of '{cardId}' allowed");
                }
            }

            var deckList = new List<string>();
            foreach (var (cardId, count) in deck)
                for (int i = 0; i < count; i++)
                    deckList.Add(cardId);

            game.Players.Add(new PlayerInstance
            {
                Id = setup.Id ?? Guid.NewGuid().ToString("N")[..8],
                Name = setup.Name,
                IsAdmin = setup.IsAdmin,
                DeckList = deckList
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
