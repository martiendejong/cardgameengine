using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;

namespace CardGameEngine.Api.Services;

public class PlayerSetup
{
    public string Name { get; set; } = "";
    public string? Id { get; set; }
    public string? DeckId { get; set; }                // preconstructed deck (brings HQ + hero)
    public string? HqId { get; set; }                  // chosen HQ (must be in the deck's hqOptions)
    public string? HeroId { get; set; }                // chosen starting hero (must be in heroOptions)
    public Dictionary<string, int>? Deck { get; set; } // cardId -> copies (overrides precon card list)
    public bool IsAdmin { get; set; }
    public bool IsBot { get; set; }                    // seat is played by the server-side bot
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

    public (GameInstance? game, string? error) CreateMatch(string gameId, List<PlayerSetup> players,
        Action<GameInstance>? setupOverride = null)
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
            // Resolve preconstructed deck (fall back to the first one so every player has an HQ + hero)
            var precon = definition.Decks.FirstOrDefault(d => d.Id == setup.DeckId)
                ?? definition.Decks.FirstOrDefault();
            if (setup.DeckId != null && definition.Decks.All(d => d.Id != setup.DeckId))
                return (null, $"{setup.Name}: unknown deck '{setup.DeckId}'");

            var deck = setup.Deck
                ?? precon?.Cards
                ?? definition.DeckRules?.DefaultDeck
                ?? new Dictionary<string, int>();

            // Validate deck contents
            foreach (var (cardId, count) in deck)
            {
                if (count < 0)
                    return (null, $"{setup.Name}: negative count for card '{cardId}'");
                var cardDef = definition.Cards.FirstOrDefault(c => c.Id == cardId);
                if (cardDef == null)
                    return (null, $"{setup.Name}: unknown card '{cardId}'");
                if (!setup.IsAdmin && !CardGameEngine.Engine.GameQueries.IsDeckEligible(cardDef))
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
                    var cardDef = definition.Cards.First(c => c.Id == cardId);
                    if (cardDef.DeckLimit == "unlimited") continue;
                    var limit = int.TryParse(cardDef.DeckLimit, out var perCard) ? perCard : rules.MaxCopies;
                    if (count > limit)
                        return (null, $"{setup.Name}: at most {limit} copies of '{cardId}' allowed");
                }
            }

            var deckList = new List<string>();
            foreach (var (cardId, count) in deck)
                for (int i = 0; i < count; i++)
                    deckList.Add(cardId);

            // HQ / hero choice within the faction's options
            var hqId = precon?.Hq;
            var heroId = precon?.Hero;
            if (setup.HqId != null && precon != null)
            {
                var hqOptions = precon.HqOptions.Count > 0 ? precon.HqOptions : new List<string> { precon.Hq };
                if (!setup.IsAdmin && !hqOptions.Contains(setup.HqId))
                    return (null, $"{setup.Name}: HQ '{setup.HqId}' is not available for this faction");
                hqId = setup.HqId;
            }
            if (setup.HeroId != null && precon != null)
            {
                var heroOptions = precon.HeroOptions.Count > 0 ? precon.HeroOptions : new List<string> { precon.Hero };
                if (!setup.IsAdmin && !heroOptions.Contains(setup.HeroId))
                    return (null, $"{setup.Name}: hero '{setup.HeroId}' is not available for this faction");
                heroId = setup.HeroId;
            }

            var player = new PlayerInstance
            {
                Id = setup.Id ?? Guid.NewGuid().ToString("N")[..8],
                Name = setup.Name,
                IsAdmin = setup.IsAdmin,
                IsBot = setup.IsBot,
                DeckList = deckList,
                HqCardId = hqId,
                HeroCardId = heroId
            };
            player.RelevantResources = ComputeRelevantResources(definition, player);
            game.Players.Add(player);
        }

        // Campaign encounters adjust HQ/hero/scripting before the board is set up
        setupOverride?.Invoke(game);

        _ruleEngine.ExecuteSetup(game);
        _matches[matchId] = game;

        _logger.LogInformation("Created match {MatchId} for game {GameId}", matchId, gameId);
        return (game, null);
    }

    /// <summary>
    /// Which player-scoped resources this player's deck actually touches: play costs,
    /// ability costs/effects, triggers — following summon/transform chains. Drives
    /// which resource chips the UI shows for this player.
    /// </summary>
    private static List<string> ComputeRelevantResources(Core.Definitions.GameDefinition def, PlayerInstance player)
    {
        var relevant = new HashSet<string>();
        var seen = new HashSet<string>(player.DeckList);
        if (player.HqCardId != null) seen.Add(player.HqCardId);
        if (player.HeroCardId != null) seen.Add(player.HeroCardId);

        var queue = new Queue<string>(seen);
        while (queue.Count > 0)
        {
            var cardId = queue.Dequeue();
            var card = def.Cards.FirstOrDefault(c => c.Id == cardId);
            if (card == null) continue;

            if (card.PlayCost != null)
                relevant.Add(card.PlayCostResource);
            if (card.PlayCosts != null)
                foreach (var res in card.PlayCosts.Keys)
                    relevant.Add(res);

            var abilities = card.Abilities.ToList();
            if (card.OnPlay != null) abilities.Add(card.OnPlay);

            var effects = abilities.SelectMany(a => a.Effects)
                .Concat(card.Triggers.SelectMany(t => t.Effects))
                .ToList();

            foreach (var cost in abilities.SelectMany(a => a.Costs))
                if (cost.Scope == "player" && cost.ResourceId != null)
                    relevant.Add(cost.ResourceId);

            foreach (var eff in effects)
            {
                if (eff.ResourceId != null &&
                    (eff.Scope == "player" || eff.Type is "steal_resource" or "plunder_resource"))
                    relevant.Add(eff.ResourceId);
                if (eff.CardId != null && (eff.Type == "summon" || eff.Type == "transform") && seen.Add(eff.CardId))
                    queue.Enqueue(eff.CardId);
            }
        }

        if (relevant.Count == 0) relevant.Add("gold");
        return def.Resources
            .Where(r => r.Scope == "player" && relevant.Contains(r.Id))
            .Select(r => r.Id)
            .ToList();
    }

    public GameInstance? GetMatch(string matchId) =>
        _matches.TryGetValue(matchId, out var game) ? game : null;

    public IEnumerable<GameInstance> GetAllMatches() => _matches.Values;
}
