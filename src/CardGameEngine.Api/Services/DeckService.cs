using System.Text.Json;
using CardGameEngine.Engine;

namespace CardGameEngine.Api.Services;

// ---- A player-built, named deck saved for reuse across matches ----

public class SavedDeck
{
    public string Id { get; set; } = "";
    public string GameId { get; set; } = "town-tcg";
    public string Name { get; set; } = "";
    public string? HqId { get; set; }
    public string? HeroId { get; set; }
    public Dictionary<string, int> Cards { get; set; } = new();
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}

/// <summary>All decks saved under one player's profile name, persisted as JSON.</summary>
public class PlayerDecks
{
    public string Name { get; set; } = "";
    public List<SavedDeck> Decks { get; set; } = new();
}

/// <summary>
/// Per-profile saved-deck store. No DB — mirrors CampaignService's
/// ProfilePath/GetProfile/SaveProfile JSON-file pattern in its own "decks" folder next to
/// "profiles", so saved decks stay independent of campaign progress/collection but persist
/// the same way (survives reloads, sessions, and redeploys — see deploy.ps1, which only
/// overwrites the definitions/ folder and never touches sibling runtime data folders).
/// </summary>
public class DeckService
{
    private readonly GameDefinitionService _definitions;
    private readonly ILogger<DeckService> _logger;
    private readonly string _decksDir;

    public DeckService(GameDefinitionService definitions, ILogger<DeckService> logger)
    {
        _definitions = definitions;
        _logger = logger;
        var defPath = definitions.DefinitionsPath ?? AppContext.BaseDirectory;
        _decksDir = Path.GetFullPath(Path.Combine(defPath, "..", "decks"));
        Directory.CreateDirectory(_decksDir);
    }

    private string DecksPath(string profileName)
    {
        var safe = string.Concat(profileName.Where(char.IsLetterOrDigit)).ToLowerInvariant();
        if (safe.Length == 0) safe = "player";
        return Path.Combine(_decksDir, safe + ".json");
    }

    public PlayerDecks GetPlayerDecks(string profileName)
    {
        var path = DecksPath(profileName);
        if (File.Exists(path))
            return JsonSerializer.Deserialize<PlayerDecks>(File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new PlayerDecks { Name = profileName };
        return new PlayerDecks { Name = profileName };
    }

    private void SavePlayerDecks(PlayerDecks decks)
    {
        File.WriteAllText(DecksPath(decks.Name),
            JsonSerializer.Serialize(decks, new JsonSerializerOptions { WriteIndented = true }));
    }

    public List<SavedDeck> ListDecks(string profileName, string gameId) =>
        GetPlayerDecks(profileName).Decks.Where(d => d.GameId == gameId).ToList();

    /// <summary>Creates a new deck (id == null) or updates an existing one (id given).</summary>
    public (SavedDeck? deck, string? error) SaveDeck(string profileName, string gameId, string? id, string name,
        string? hqId, string? heroId, Dictionary<string, int> cards)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            return (null, "Profile name is required");
        name = name.Trim();
        if (name.Length == 0)
            return (null, "Deck name is required");

        var definition = _definitions.GetById(gameId);
        if (definition == null)
            return (null, $"Game definition '{gameId}' not found");
        if (hqId != null && definition.Cards.All(c => c.Id != hqId))
            return (null, $"Unknown HQ card '{hqId}'");
        if (heroId != null && definition.Cards.All(c => c.Id != heroId))
            return (null, $"Unknown hero card '{heroId}'");

        // Drop zero/blank counts before validating — the deck builder UI never sends these,
        // but a stale client payload shouldn't be able to smuggle a bogus card id in as 0.
        var cleanCards = cards.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
        var error = GameQueries.ValidateDeck(definition, cleanCards, isAdmin: false, enforceMinSize: true);
        if (error != null) return (null, error);

        var store = GetPlayerDecks(profileName);
        store.Name = profileName;
        var now = DateTime.UtcNow.ToString("o");

        SavedDeck? deck = id == null ? null : store.Decks.FirstOrDefault(d => d.Id == id);
        if (id != null && deck == null)
            return (null, $"Deck '{id}' not found");

        if (deck == null)
        {
            deck = new SavedDeck { Id = Guid.NewGuid().ToString("N"), GameId = gameId, CreatedAt = now };
            store.Decks.Add(deck);
        }

        deck.Name = name;
        deck.HqId = hqId;
        deck.HeroId = heroId;
        deck.Cards = cleanCards;
        deck.UpdatedAt = now;

        SavePlayerDecks(store);
        _logger.LogInformation("Profile {Name} saved deck {DeckId} ({DeckName})", profileName, deck.Id, deck.Name);
        return (deck, null);
    }

    public bool DeleteDeck(string profileName, string deckId)
    {
        var store = GetPlayerDecks(profileName);
        if (store.Decks.RemoveAll(d => d.Id == deckId) == 0) return false;
        SavePlayerDecks(store);
        return true;
    }
}
