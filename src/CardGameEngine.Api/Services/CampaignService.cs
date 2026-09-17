using System.Text.Json;
using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;

namespace CardGameEngine.Api.Services;

// ---- Mission definitions (loaded from definitions/<game>/encounters.json) ----

public class MissionDefinition
{
    public string Id { get; set; } = "";
    public string Campaign { get; set; } = "town"; // "town" or "raiders"
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Requires { get; set; } // previous mission id (may cross campaigns)
    public MissionSide Player { get; set; } = new();
    public MissionSide Enemy { get; set; } = new();
    public string Victory { get; set; } = "default"; // "cleared" = defeat all spawns
    public List<MissionSpawn> Spawns { get; set; } = new();
    public List<string> Rewards { get; set; } = new();
}

public class MissionSide
{
    public string? Hq { get; set; }
    public string? Hero { get; set; }
    public List<string> Cards { get; set; } = new();   // starting cards (drawn as the opening hand)
    public Dictionary<string, int>? Deck { get; set; } // optional full deck (mission 5 style)
}

public class MissionSpawn
{
    public int Turn { get; set; }
    public string CardId { get; set; } = "";
    public int Count { get; set; } = 1;
}

// ---- Player profile: collection + campaign progress, persisted as JSON ----

public class PlayerProfile
{
    public string Name { get; set; } = "";
    public List<string> CompletedMissions { get; set; } = new();
    public Dictionary<string, int> Collection { get; set; } = new();
}

public class CampaignService
{
    private readonly MatchService _matchService;
    private readonly GameDefinitionService _definitions;
    private readonly ILogger<CampaignService> _logger;
    private readonly string _profilesDir;
    private List<MissionDefinition>? _missions;

    public CampaignService(MatchService matchService, GameDefinitionService definitions,
        ILogger<CampaignService> logger)
    {
        _matchService = matchService;
        _definitions = definitions;
        _logger = logger;
        var defPath = definitions.DefinitionsPath ?? AppContext.BaseDirectory;
        _profilesDir = Path.GetFullPath(Path.Combine(defPath, "..", "profiles"));
        Directory.CreateDirectory(_profilesDir);
    }

    public List<MissionDefinition> GetMissions(string gameId)
    {
        if (_missions != null) return _missions;
        var path = Path.Combine(_definitions.DefinitionsPath ?? "", gameId, "encounters.json");
        if (!File.Exists(path))
        {
            _logger.LogWarning("No encounters.json found at {Path}", path);
            return _missions = new();
        }
        var json = File.ReadAllText(path);
        _missions = JsonSerializer.Deserialize<List<MissionDefinition>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        return _missions;
    }

    // ---- profiles (keyed by account id — never a client-typed name) ----

    private string ProfilePath(string userId)
    {
        var safe = string.Concat(userId.Where(char.IsLetterOrDigit)).ToLowerInvariant();
        if (safe.Length == 0) safe = "player";
        return Path.Combine(_profilesDir, safe + ".json");
    }

    public PlayerProfile GetProfile(string userId, string displayName = "")
    {
        var path = ProfilePath(userId);
        var profile = File.Exists(path)
            ? JsonSerializer.Deserialize<PlayerProfile>(File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new PlayerProfile()
            : new PlayerProfile();
        if (!string.IsNullOrEmpty(displayName))
            profile.Name = displayName; // always reflect the account's current display name
        return profile;
    }

    public void SaveProfile(string userId, PlayerProfile profile)
    {
        File.WriteAllText(ProfilePath(userId),
            JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true }));
    }

    public bool IsUnlocked(PlayerProfile profile, MissionDefinition mission) =>
        mission.Requires == null || profile.CompletedMissions.Contains(mission.Requires);

    public static int CollectionCount(PlayerProfile profile) =>
        profile.Collection.Values.Sum();

    public int RequiredCards(string gameId) =>
        _definitions.GetById(gameId)?.DeckRules?.MinDeckSize ?? 40;

    /// <summary>
    /// Multiplayer opens once the collection can field a minimum-size deck.
    /// Completing the whole campaign (playset rewards) gets you there exactly.
    /// </summary>
    public bool IsMultiplayerUnlocked(string gameId, PlayerProfile profile)
    {
        var minDeck = _definitions.GetById(gameId)?.DeckRules?.MinDeckSize ?? 40;
        return CollectionCount(profile) >= minDeck;
    }

    // ---- encounter match construction ----

    public (GameInstance? game, string? error) StartMission(string gameId, string missionId, string userId, string displayName,
        Dictionary<string, int>? customDeck = null, string? customHq = null, string? customHero = null)
    {
        var mission = GetMissions(gameId).FirstOrDefault(m => m.Id == missionId);
        if (mission == null) return (null, $"Unknown mission '{missionId}'");

        var profile = GetProfile(userId, displayName);
        if (!IsUnlocked(profile, mission))
            return (null, "Complete the previous mission first");

        // Use the player's custom deck if provided, otherwise fall back to the mission default
        var playerDeck = (customDeck != null && customDeck.Count > 0)
            ? customDeck
            : mission.Player.Deck
              ?? mission.Player.Cards.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());

        var players = new List<PlayerSetup>
        {
            new()
            {
                Id = "p1", Name = profile.Name,
                IsAdmin = true,
                Deck = playerDeck,
            },
            new()
            {
                Id = "p2", Name = "Enemy", IsBot = true, IsAdmin = true,
                Deck = mission.Enemy.Deck
                    ?? mission.Enemy.Cards.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count()),
            },
        };

        var (game, error) = _matchService.CreateMatch(gameId, players, userId, setupOverride: g =>
        {
            g.Players[0].HqCardId = !string.IsNullOrEmpty(customHq) ? customHq : mission.Player.Hq;
            g.Players[0].HeroCardId = !string.IsNullOrEmpty(customHero) ? customHero : mission.Player.Hero;
            g.Players[1].HqCardId = mission.Enemy.Hq;
            g.Players[1].HeroCardId = mission.Enemy.Hero;
            g.Encounter = new EncounterState
            {
                MissionId = mission.Id,
                ProfileUserId = userId,
                PlayerId = "p1",
                EnemyPlayerId = "p2",
                VictoryOnCleared = mission.Victory == "cleared",
                RewardCards = mission.Rewards,
                PendingSpawns = mission.Spawns
                    .SelectMany(s => Enumerable.Repeat(new SpawnEntry { Turn = s.Turn, CardId = s.CardId }, s.Count))
                    .ToList(),
            };
        });

        return (game, error);
    }

    /// <summary>
    /// Wires a lightweight, non-campaign encounter onto a Lobby "vs Computer" quick match so a
    /// win grants exactly one random card (never the full playset a scripted mission gives),
    /// while still reusing the existing mission victory-screen reward display and completion
    /// endpoint. Picks the reward up front (like a mission's fixed rewards) so it's already
    /// known by the time the game ends.
    /// </summary>
    public void AttachQuickMatchReward(GameInstance game, string humanUserId, string humanPlayerId, string botPlayerId)
    {
        var pool = game.Definition.Cards.Where(c => GameQueries.IsDeckEligible(game.Definition, c)).ToList();
        if (pool.Count == 0) return;

        var rewardCard = pool[Random.Shared.Next(pool.Count)].Id;
        game.Encounter = new EncounterState
        {
            ProfileUserId = humanUserId,
            PlayerId = humanPlayerId,
            EnemyPlayerId = botPlayerId,
            IsQuickMatch = true,
            RewardCards = new List<string> { rewardCard },
        };
    }

    /// <summary>Called when the player reports a finished encounter; verifies and grants rewards.
    /// callerUserId must match the account that started the mission — otherwise anyone who learns
    /// a matchId could claim another player's campaign rewards.</summary>
    public (bool granted, string message, PlayerProfile? profile) CompleteMission(GameInstance game, string callerUserId)
    {
        var enc = game.Encounter;
        if (enc == null) return (false, "Not a campaign match", null);
        if (enc.ProfileUserId != callerUserId) return (false, "Not authorized to complete this mission", null);
        if (game.State != GameState.GameEnded) return (false, "The battle is not over", null);
        if (enc.RewardsGranted) return (false, "Rewards already collected", GetProfile(enc.ProfileUserId));

        var player = game.Players.First(p => p.Id == enc.PlayerId);
        if (!player.IsWinner) return (false, "The town has fallen — try again", null);

        var profile = GetProfile(enc.ProfileUserId);
        var playset = game.Definition.DeckRules?.MaxCopies ?? 4;

        if (enc.IsQuickMatch)
        {
            // A quick vs-computer win adds one copy of the (already-chosen) random reward
            // card, capped at the playset limit — not the full-playset grant a scripted
            // campaign mission gives, and it never touches CompletedMissions.
            foreach (var cardId in enc.RewardCards)
                profile.Collection[cardId] =
                    Math.Min(profile.Collection.GetValueOrDefault(cardId) + 1, playset);
        }
        else
        {
            if (!profile.CompletedMissions.Contains(enc.MissionId))
                profile.CompletedMissions.Add(enc.MissionId);
            // Rewards come as a full playset, capped so replays cannot farm past it —
            // finishing all missions yields exactly a minimum multiplayer deck
            foreach (var cardId in enc.RewardCards)
                profile.Collection[cardId] =
                    Math.Min(profile.Collection.GetValueOrDefault(cardId) + playset, playset);
        }

        SaveProfile(enc.ProfileUserId, profile);
        enc.RewardsGranted = true;

        _logger.LogInformation("Profile {Name} completed {Mission}, rewards: {Rewards}",
            profile.Name, enc.MissionId, string.Join(",", enc.RewardCards));
        return (true, "Victory!", profile);
    }
}