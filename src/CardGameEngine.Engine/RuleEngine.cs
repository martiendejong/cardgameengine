using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>
/// Facade over the modular engine. Wires the event bus, registries and services,
/// dispatches player actions, and checks end conditions after every action.
///
/// Architecture: PLAYER INTENTION → ACTION SERVICE → MUTATOR → EVENT → TRIGGERS → EFFECTS → ...
/// - GameMutator is the only state writer; every mutation publishes an event.
/// - EffectProcessor / CostProcessor / ConditionEvaluator are extensible registries.
/// - TriggerService turns card trigger definitions into event subscribers.
/// </summary>
public class RuleEngine
{
    private readonly EngineServices _services;
    private readonly SetupService _setup;
    private readonly TurnService _turns;
    private readonly CombatService _combat;
    private readonly CardPlayService _cardPlay;
    private readonly AbilityService _abilities;
    private readonly ActionCatalog _catalog;

    public RuleEngine()
    {
        var bus = new EventBus();
        var mutator = new GameMutator(bus);
        _services = new EngineServices
        {
            Bus = bus,
            Mutator = mutator,
            Factory = new ObjectFactory(),
            Targeting = new TargetingService(),
            Conditions = new ConditionEvaluator(),
            Costs = new CostProcessor(),
            Effects = new EffectProcessor()
        };

        DefaultHandlers.RegisterAll(_services);
        _ = new TriggerService(_services); // subscribes itself to the bus

        _turns = new TurnService(_services);
        _setup = new SetupService(_services, _turns);
        _combat = new CombatService(_services);
        _cardPlay = new CardPlayService(_services);
        _abilities = new AbilityService(_services);
        _catalog = new ActionCatalog(_services, _abilities);
    }

    // ---- Public API (unchanged shape for the API layer) ----

    public void ExecuteSetup(GameInstance game) => _setup.ExecuteSetup(game);

    public List<AvailableAction> GetAvailableActions(GameInstance game, string playerId) =>
        _catalog.GetAvailableActions(game, playerId);

    public (bool success, string? error) ExecuteAction(GameInstance game, string playerId, ActionRequest action)
    {
        if (game.State == GameState.GameEnded)
            return (false, "Game has ended");

        // Reaction window: only the reacting player may act, with reactions or a pass
        if (game.State == GameState.WaitingForReaction)
        {
            if (playerId != game.ReactionPlayerId)
                return (false, "Waiting for the opponent's reaction");

            if (action.Type == "pass")
            {
                game.Log.Add($"{game.Players.First(p => p.Id == playerId).Name} passes.");
                ResolveStack(game);
                CheckEndConditions(game);
                return (true, null);
            }
            if (action.Type == "playCard")
            {
                var r = _cardPlay.ExecutePlayCard(game, playerId, action);
                if (r.Item1) CheckEndConditions(game);
                return r;
            }
            return (false, "Respond with a reaction card or pass");
        }

        if (game.ActivePlayerId != playerId)
            return (false, "Not your turn");

        var result = action.Type switch
        {
            "activateAbility" => _abilities.ActivateAbility(game, playerId, action),
            "attack" => _combat.ExecuteAttack(game, playerId, action),
            "move" => _turns.ExecuteMove(game, playerId, action),
            "build" => _turns.ExecuteBuild(game, playerId, action),
            "playCard" => _cardPlay.ExecutePlayCard(game, playerId, action),
            "endPhase" => EndPhaseAction(game, playerId),
            _ => (false, $"Unknown action type: {action.Type}")
        };

        if (result.Item1)
            CheckEndConditions(game);
        return result;
    }

    private (bool, string?) EndPhaseAction(GameInstance game, string playerId)
    {
        _turns.EndPhase(game, playerId);
        return (true, null);
    }

    /// <summary>Resolve everything on the stack, latest first (LIFO).</summary>
    public void ResolveStack(GameInstance game)
    {
        while (!game.Stack.IsEmpty)
        {
            var item = game.Stack.Pop();
            if (item.Kind == "attack")
            {
                var attacker = game.Objects.FirstOrDefault(o => o.Id == item.SourceObjectId);
                var defender = game.Objects.FirstOrDefault(o => o.Id == item.TargetIds.FirstOrDefault());
                if (attacker != null && defender != null)
                    _combat.ResolveDeclaredAttack(game, attacker, defender);
            }
            else if (item.Kind == "spell")
            {
                _cardPlay.ResolveSpellItem(game, item);
            }
        }

        game.ReactionPlayerId = null;
        game.ReactionWindowEvent = null;
        if (game.State != GameState.GameEnded)
            game.State = GameState.WaitingForAction;
    }

    public (bool success, string? error) ResolveChoice(GameInstance game, string playerId, string choiceId, List<string> selectedIds)
    {
        var result = _abilities.ResolveChoice(game, playerId, choiceId, selectedIds);
        if (result.Item1)
            CheckEndConditions(game);
        return result;
    }

    public void EndPhase(GameInstance game, string playerId)
    {
        _turns.EndPhase(game, playerId);
        CheckEndConditions(game);
    }

    public int GetEffectiveProperty(GameInstance game, ObjectInstance obj, string propertyId) =>
        GameQueries.GetEffectiveProperty(game, obj, propertyId);

    public bool IsObjectTypeOrSubtype(GameInstance game, string objectType, string targetType) =>
        GameQueries.IsObjectTypeOrSubtype(game, objectType, targetType);

    // ---- End conditions ----

    public void CheckEndConditions(GameInstance game)
    {
        if (game.State == GameState.GameEnded) return;

        foreach (var endCond in game.Definition.EndConditions)
        {
            if (endCond.Type != "all_destroyed" || endCond.Scope != "player") continue;

            foreach (var player in game.Players)
            {
                bool allDestroyed = endCond.Targets.All(targetType =>
                {
                    var playerObjects = game.Objects.Where(o =>
                        o.OwnerId == player.Id &&
                        GameQueries.IsObjectTypeOrSubtype(game, o.ObjectType, targetType)).ToList();
                    return playerObjects.Count > 0 && playerObjects.All(o => o.IsDestroyed);
                });

                if (allDestroyed && endCond.Result == "lose" && !player.IsLoser)
                {
                    player.IsLoser = true;
                    game.Log.Add($"{player.Name} has lost all their {string.Join(" and ", endCond.Targets)}!");
                }
            }
        }

        var losers = game.Players.Where(p => p.IsLoser).ToList();
        if (losers.Count > 0)
        {
            foreach (var winner in game.Players.Where(p => !p.IsLoser))
            {
                winner.IsWinner = true;
                game.Log.Add($"{winner.Name} wins!");
            }
            game.State = GameState.GameEnded;
        }
    }
}
