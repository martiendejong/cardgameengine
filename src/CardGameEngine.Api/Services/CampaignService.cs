using System.Text.Json;
using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;

namespace CardGameEngine.Api.Services;

// ---- Mission definitions (loaded from definitions/<game>/encounters.json) ----

public class MissionDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Requires { get; set; } // previous mission id
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

    // ---- profiles ----

    private string ProfilePath(string name)
    {
        var safe = string.Concat(name.Where(char.IsLetterOrDigit)).ToLowerInvariant();
        if (safe.Length == 0) safe = "player";
        return Path.Combine(_profilesDir, safe + ".json");
    }

    public PlayerProfile GetProfile(string name)
    {
        var path = ProfilePath(name);
        if (File.Exists(path))
            return JsonSerializer.Deserialize<PlayerProfile>(File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new PlayerProfile { Name = name };
        return new PlayerProfile { Name = name };
    }

    public void SaveProfile(PlayerProfile profile)
    {
        File.WriteAllText(ProfilePath(profile.Name),
            JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true }));
    }

    public bool IsUnlocked(PlayerProfile profile, MissionDefinition mission) =>
        mission.Requires == null || profile.CompletedMissions.Contains(mission.Requires);

    // ---- encounter match construction ----

    public (GameInstance? game, string? error) StartMission(string gameId, string missionId, string profileName)
    {
        var mission = GetMissions(gameId).FirstOrDefault(m => m.Id == missionId);
        if (mission == null) return (null, $"Unknown mission '{missionId}'");

        var profile = GetProfile(profileName);
        if (!IsUnlocked(profile, mission))
            return (null, "Complete the previous mission first");

        var players = new List<PlayerSetup>
        {
            new()
            {
                Id = "p1", Name = profile.Name,
                // encounter sides bypass precon validation via admin flag; starting
                // cards become a tiny deck that is drawn as the opening hand
                IsAdmin = true,
                Deck = mission.Player.Deck
                    ?? mission.Player.Cards.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count()),
            },
            new()
            {
                Id = "p2", Name = "Enemy", IsBot = true, IsAdmin = true,
                Deck = mission.Enemy.Deck
                    ?? mission.Enemy.Cards.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count()),
            },
        };

        var (game, error) = _matchService.CreateMatch(gameId, players, setupOverride: g =>
        {
            g.Players[0].HqCardId = mission.Player.Hq;
            g.Players[0].HeroCardId = mission.Player.Hero;
            g.Players[1].HqCardId = mission.Enemy.Hq;
            g.Players[1].HeroCardId = mission.Enemy.Hero;
            g.Encounter = new EncounterState
            {
                MissionId = mission.Id,
                ProfileName = profile.Name,
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

    /// <summary>Called when the player reports a finished encounter; verifies and grants rewards.</summary>
    public (bool granted, string message, PlayerProfile? profile) CompleteMission(GameInstance game)
    {
        var enc = game.Encounter;
        if (enc == null) return (false, "Not a campaign match", null);
        if (game.State != GameState.GameEnded) return (false, "The battle is not over", null);
        if (enc.RewardsGranted) return (false, "Rewards already collected", GetProfile(enc.ProfileName));

        var player = game.Players.First(p => p.Id == enc.PlayerId);
        if (!player.IsWinner) return (false, "The town has fallen — try again", null);

        var profile = GetProfile(enc.ProfileName);
        if (!profile.CompletedMissions.Contains(enc.MissionId))
            profile.CompletedMissions.Add(enc.MissionId);
        foreach (var cardId in enc.RewardCards)
            profile.Collection[cardId] = profile.Collection.GetValueOrDefault(cardId) + 1;
        SaveProfile(profile);
        enc.RewardsGranted = true;

        _logger.LogInformation("Profile {Name} completed {Mission}, rewards: {Rewards}",
            profile.Name, enc.MissionId, string.Join(",", enc.RewardCards));
        return (true, "Victory!", profile);
    }
}