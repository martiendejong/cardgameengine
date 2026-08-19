using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;
using CardGameEngine.Engine;

namespace CardGameEngine.Api.Services;

/// <summary>
/// A heuristic computer player. Runs server-side: whenever it is a bot's turn,
/// it plays the whole turn through the same RuleEngine API a human client uses.
///
/// Policy per phase: use economy/utility abilities and play affordable cards in
/// Main; attack with everything in Combat (prefer lethal targets, then the enemy
/// HQ/hero), then advance idle melee to the front line for next turn.
/// </summary>
public class BotService
{
    private readonly RuleEngine _engine;

    public BotService(RuleEngine engine) => _engine = engine;

    /// <summary>Plays for as long as the active player is a bot. Safe against loops.</summary>
    public void PlayBotTurns(GameInstance game)
    {
        int safety = 0;
        while (game.State != GameState.GameEnded && safety++ < 300)
        {
            var bot = game.Players.FirstOrDefault(p => p.Id == game.ActivePlayerId && p.IsBot);
            if (bot == null) break;

            var actions = _engine.GetAvailableActions(game, bot.Id)
                .Where(a => a.Available)
                .ToList();

            var request = ChooseAction(game, bot, actions);
            if (request == null)
            {
                _engine.EndPhase(game, bot.Id);
                continue;
            }

            var (ok, _) = _engine.ExecuteAction(game, bot.Id, request);
            if (!ok)
                _engine.EndPhase(game, bot.Id); // never get stuck on a misjudged action
        }
    }

    private ActionRequest? ChooseAction(GameInstance game, PlayerInstance bot, List<AvailableAction> actions)
    {
        // 1. Attack with everything that can attack
        var attack = actions.FirstOrDefault(a => a.Type == "attack" && a.ValidTargets is { Count: > 0 });
        if (attack != null)
        {
            var attacker = game.Objects.First(o => o.Id == attack.SourceObjectId);
            return new ActionRequest
            {
                Type = "attack",
                SourceObjectId = attack.SourceObjectId,
                TargetIds = new List<string> { PickAttackTarget(game, attacker, attack.ValidTargets!) }
            };
        }

        // 2. Useful abilities (skip heals with nothing to heal)
        foreach (var ability in actions.Where(a => a.Type == "activateAbility"))
        {
            if (!AbilityIsWorthUsing(game, bot, ability, out var targets)) continue;
            return new ActionRequest
            {
                Type = "activateAbility",
                SourceObjectId = ability.SourceObjectId,
                AbilityId = ability.AbilityId,
                TargetIds = targets
            };
        }

        // 3. Play affordable cards from hand
        var play = actions.FirstOrDefault(a => a.Type == "playCard");
        if (play != null)
        {
            var targets = play.RequiresChoice != null && play.ValidTargets is { Count: > 0 }
                ? new List<string> { PickEffectTarget(game, play.SourceObjectId!, play.ValidTargets!) }
                : new List<string>();
            return new ActionRequest
            {
                Type = "playCard",
                SourceObjectId = play.SourceObjectId,
                TargetIds = targets
            };
        }

        // 4. In combat, advance idle melee attackers from the back line to the front
        if (game.CurrentPhaseId == "combat")
        {
            foreach (var move in actions.Where(a => a.Type == "move" && a.Label.Contains("Front")))
            {
                var obj = game.Objects.FirstOrDefault(o => o.Id == move.SourceObjectId);
                if (obj == null) continue;
                if (obj.Tags.Contains("ranged")) continue; // archers stay behind
                if (_engine.GetEffectiveProperty(game, obj, "attack") <= 0) continue;
                return new ActionRequest { Type = "move", SourceObjectId = move.SourceObjectId, TargetIds = new List<string>() };
            }
        }

        // 5. Nothing useful left in this phase
        return null;
    }

    private string PickAttackTarget(GameInstance game, ObjectInstance attacker, List<string> targetIds)
    {
        var targets = targetIds
            .Select(id => game.Objects.First(o => o.Id == id))
            .ToList();

        int DamageTo(ObjectInstance t)
        {
            var atk = _engine.GetEffectiveProperty(game, attacker, "attack");
            var def = game.Definition.Cards.FirstOrDefault(c => c.Id == attacker.DefinitionId);
            if (def?.BonusAttackVsBuildings is int bonus &&
                _engine.IsObjectTypeOrSubtype(game, t.ObjectType, "building"))
                atk += bonus;
            return Math.Max(0, atk - _engine.GetEffectiveProperty(game, t, "armor"));
        }

        // Lethal first, then the HQ/hero, then whatever takes the most damage
        var lethal = targets.FirstOrDefault(t => DamageTo(t) >= t.Properties.GetValueOrDefault("currentHp"));
        if (lethal != null) return lethal.Id;

        var vital = targets.FirstOrDefault(t =>
            _engine.IsObjectTypeOrSubtype(game, t.ObjectType, "headquarters") ||
            _engine.IsObjectTypeOrSubtype(game, t.ObjectType, "hero"));
        if (vital != null) return vital.Id;

        return targets.OrderByDescending(DamageTo).First().Id;
    }

    private bool AbilityIsWorthUsing(GameInstance game, PlayerInstance bot, AvailableAction action, out List<string> targets)
    {
        targets = new List<string>();
        var source = game.Objects.FirstOrDefault(o => o.Id == action.SourceObjectId);
        var cardDef = source != null ? game.Definition.Cards.FirstOrDefault(c => c.Id == source.DefinitionId) : null;
        var ability = cardDef?.Abilities.FirstOrDefault(a => a.Id == action.AbilityId);
        if (source == null || ability == null) return false;

        bool heals = ability.Effects.Any(e => e.Type == "heal");
        if (heals)
        {
            if (action.RequiresChoice != null && action.ValidTargets is { Count: > 0 })
            {
                // Only heal something that is actually damaged; prefer the most wounded
                var damaged = action.ValidTargets
                    .Select(id => game.Objects.First(o => o.Id == id))
                    .Where(o => o.Properties.GetValueOrDefault("currentHp") < o.Properties.GetValueOrDefault("maxHp"))
                    .OrderBy(o => o.Properties.GetValueOrDefault("currentHp"))
                    .ToList();
                if (damaged.Count == 0) return false;
                targets.Add(damaged[0].Id);
                return true;
            }
            // Self-heal (e.g. Regenerate): only when damaged
            return source.Properties.GetValueOrDefault("currentHp") < source.Properties.GetValueOrDefault("maxHp");
        }

        if (action.RequiresChoice != null)
        {
            if (action.ValidTargets is not { Count: > 0 }) return false;
            targets.Add(PickEffectTarget(game, action.SourceObjectId!, action.ValidTargets));
        }

        return true;
    }

    /// <summary>Target picker for damage/buff/transform style choices: kill if possible, then vital, then first.</summary>
    private string PickEffectTarget(GameInstance game, string sourceObjectId, List<string> validTargets)
    {
        var source = game.Objects.FirstOrDefault(o => o.Id == sourceObjectId);
        var cardDef = source != null ? game.Definition.Cards.FirstOrDefault(c => c.Id == source.DefinitionId) : null;

        // Find the damage amount if this is a damage effect (ability or onPlay)
        var effects = (cardDef?.Abilities.SelectMany(a => a.Effects) ?? Enumerable.Empty<EffectDefinition>())
            .Concat(cardDef?.OnPlay?.Effects ?? new List<EffectDefinition>());
        var damageEffect = effects.FirstOrDefault(e => e.Type == "damage");

        var targets = validTargets.Select(id => game.Objects.First(o => o.Id == id)).ToList();

        if (damageEffect != null)
        {
            var lethal = targets.FirstOrDefault(t =>
                Math.Max(0, (damageEffect.Amount ?? 0) - _engine.GetEffectiveProperty(game, t, "armor"))
                    >= t.Properties.GetValueOrDefault("currentHp"));
            if (lethal != null) return lethal.Id;

            var vital = targets.FirstOrDefault(t =>
                _engine.IsObjectTypeOrSubtype(game, t.ObjectType, "headquarters") ||
                _engine.IsObjectTypeOrSubtype(game, t.ObjectType, "hero"));
            if (vital != null) return vital.Id;
        }

        return targets[0].Id;
    }
}
