using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>Ability activation, cost/condition checks, and pending choice resolution.</summary>
public class AbilityService
{
    private readonly EngineServices _s;
    private int _choiceCounter;

    public AbilityService(EngineServices services) => _s = services;

    public bool CanUseAbility(GameInstance game, ObjectInstance obj, AbilityDefinition ability,
        PlayerInstance player, out string? reason)
    {
        reason = null;

        if (obj.UnderConstruction)
        {
            reason = "Still under construction";
            return false;
        }

        if (obj.HasMovedThisTurn)
        {
            reason = "Already moved this turn";
            return false;
        }

        foreach (var cost in ability.Costs)
        {
            if (!_s.Costs.CanPay(new CostContext { Game = game, Cost = cost, Object = obj, Player = player }))
            {
                reason = $"Cannot pay cost: {cost.Type}";
                return false;
            }
        }

        foreach (var cond in ability.Conditions)
        {
            if (!_s.Conditions.Check(new ConditionContext { Game = game, Condition = cond, Object = obj, Player = player }))
            {
                reason = $"Condition not met: {cond.Type}";
                return false;
            }
        }

        // Summoning abilities need housing for what they summon
        foreach (var effect in ability.Effects.Where(e => e.Type == "summon" && e.CardId != null))
        {
            var summonDef = game.Definition.Cards.FirstOrDefault(c => c.Id == effect.CardId);
            if (summonDef?.HousingCost is int housing &&
                !GameQueries.HasHousingFor(game, player.Id, housing))
            {
                reason = $"Not enough housing ({GameQueries.HousingUsed(game, player.Id)}/{GameQueries.HousingCapacity(game, player.Id)})";
                return false;
            }
        }

        return true;
    }

    public (bool, string?) ActivateAbility(GameInstance game, string playerId, ActionRequest action)
    {
        var player = game.Players.First(p => p.Id == playerId);
        var obj = game.Objects.FirstOrDefault(o => o.Id == action.SourceObjectId);
        if (obj == null) return (false, "Object not found");

        var cardDef = GameQueries.GetCardDefinition(game, obj);
        if (cardDef == null) return (false, "Card definition not found");

        var ability = cardDef.Abilities.FirstOrDefault(a => a.Id == action.AbilityId);
        if (ability == null) return (false, "Ability not found");

        if (!CanUseAbility(game, obj, ability, player, out string? reason))
            return (false, reason);

        // Validate provided targets before paying any cost
        if (ability.Choice != null && action.TargetIds.Count > 0)
        {
            var valid = _s.Targeting.GetValidTargets(game, ability.Choice, playerId);
            if (action.TargetIds.Count < ability.Choice.Min || action.TargetIds.Count > ability.Choice.Max)
                return (false, $"Must select between {ability.Choice.Min} and {ability.Choice.Max} target(s)");
            foreach (var t in action.TargetIds)
                if (!valid.Contains(t))
                    return (false, "Invalid target");
        }
        else if (ability.Choice != null && ability.Choice.Min > 0)
        {
            var valid = _s.Targeting.GetValidTargets(game, ability.Choice, playerId);
            if (valid.Count == 0)
                return (false, "No valid targets");
        }

        foreach (var cost in ability.Costs)
            _s.Costs.Pay(new CostContext { Game = game, Cost = cost, Object = obj, Player = player });

        game.Log.Add($"{player.Name}: {obj.Name} uses '{ability.Name}'.");

        if (ability.Choice != null && action.TargetIds.Count > 0)
        {
            _s.Effects.ApplyAbility(game, ability, obj, player, action.TargetIds);
        }
        else if (ability.Choice != null)
        {
            // Costs paid, targets still needed: park on the stack and ask the player
            _choiceCounter++;
            var stackItem = new StackItem
            {
                Id = $"stack_{_choiceCounter}",
                ControllerId = playerId,
                SourceObjectId = obj.Id,
                AbilityId = ability.Id,
                TargetIds = new List<string>()
            };
            game.Stack.Push(stackItem);

            game.PendingChoices.Add(new PendingChoice
            {
                Id = $"choice_{_choiceCounter}",
                PlayerId = playerId,
                StackItemId = stackItem.Id,
                Definition = ability.Choice,
                ValidOptions = _s.Targeting.GetValidTargets(game, ability.Choice, playerId)
            });
            game.State = GameState.WaitingForChoice;
        }
        else
        {
            _s.Effects.ApplyAbility(game, ability, obj, player, new List<string>());
        }

        return (true, null);
    }

    public (bool, string?) ResolveChoice(GameInstance game, string playerId, string choiceId, List<string> selectedIds)
    {
        var choice = game.PendingChoices.FirstOrDefault(c => c.Id == choiceId);
        if (choice == null) return (false, "Choice not found");
        if (choice.PlayerId != playerId) return (false, "Not your choice");

        if (selectedIds.Count < choice.Definition.Min || selectedIds.Count > choice.Definition.Max)
            return (false, $"Must select between {choice.Definition.Min} and {choice.Definition.Max} targets");

        foreach (var id in selectedIds)
            if (!choice.ValidOptions.Contains(id))
                return (false, $"Invalid target: {id}");

        game.PendingChoices.Remove(choice);

        var stackItem = game.Stack.Items.FirstOrDefault(s => s.Id == choice.StackItemId);
        if (stackItem != null)
        {
            stackItem.TargetIds = selectedIds;
            game.Stack.Items.Remove(stackItem);

            var source = game.Objects.FirstOrDefault(o => o.Id == stackItem.SourceObjectId);
            var player = game.Players.First(p => p.Id == playerId);
            if (source != null)
            {
                var cardDef = GameQueries.GetCardDefinition(game, source);
                var ability = cardDef?.Abilities.FirstOrDefault(a => a.Id == stackItem.AbilityId);
                if (ability != null)
                    _s.Effects.ApplyAbility(game, ability, source, player, selectedIds);
            }
        }

        if (game.PendingChoices.Count == 0)
            game.State = GameState.WaitingForAction;

        return (true, null);
    }
}
