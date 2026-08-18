using System.Text.Json;
using CardGameEngine.Core.Definitions;

namespace CardGameEngine.Api.Services;

public class GameDefinitionService
{
    private readonly Dictionary<string, GameDefinition> _definitions = new();
    private readonly ILogger<GameDefinitionService> _logger;

    public GameDefinitionService(ILogger<GameDefinitionService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        LoadDefinitions(env.ContentRootPath);
    }

    private void LoadDefinitions(string contentRoot)
    {
        // Look for definitions folder relative to content root or project root
        var searchPaths = new[]
        {
            Path.Combine(contentRoot, "definitions"),
            Path.Combine(contentRoot, "..", "..", "..", "definitions"),
            Path.Combine(contentRoot, "..", "..", "definitions"),
            Path.Combine(AppContext.BaseDirectory, "definitions"),
        };

        string? definitionsPath = null;
        foreach (var path in searchPaths)
        {
            var resolved = Path.GetFullPath(path);
            if (Directory.Exists(resolved))
            {
                definitionsPath = resolved;
                break;
            }
        }

        if (definitionsPath == null)
        {
            _logger.LogWarning("Definitions directory not found. Checked: {Paths}", string.Join(", ", searchPaths));
            return;
        }

        _logger.LogInformation("Loading definitions from: {Path}", definitionsPath);

        var jsonFiles = Directory.GetFiles(definitionsPath, "game.json", SearchOption.AllDirectories);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        foreach (var file in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var def = JsonSerializer.Deserialize<GameDefinition>(json, options);
                if (def != null)
                {
                    _definitions[def.Id] = def;
                    _logger.LogInformation("Loaded game definition: {Id} ({Name})", def.Id, def.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load game definition from {File}", file);
            }
        }
    }

    public IEnumerable<GameDefinition> GetAll() => _definitions.Values;

    public GameDefinition? GetById(string id) =>
        _definitions.TryGetValue(id, out var def) ? def : null;
}
