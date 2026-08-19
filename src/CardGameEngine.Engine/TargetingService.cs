using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>Answers "what can this act on": ability choices and attack reach (line rules).</summary>
public class TargetingService
{
    public List<string> GetValidTargets(GameInstance game, ChoiceDefinition choice, string playerId)
    {
        return game.Objects
            .Where(o =>
            {
                if (o.IsDestroyed || o.ZoneId != "battlefield") return false;
                if (o.AttachedToId != null) return false;
                if (choice.Controller == "self" && o.ControllerId != playerId) return false;
                if (choice.Controller == "opponent" && o.ControllerId == playerId) return false;
                if (choice.ObjectType != null && !GameQueries.IsObjectTypeOrSubtype(game, o.ObjectType, choice.ObjectType)) return false;
                if (choice.Tag != null && !o.Tags.Contains(choice.Tag)) return false;
                if (choice.RequireUntapped && o.IsTapped) return false;
                if (choice.RequireUnderConstruction && !o.UnderConstruction) return false;
                return true;
            })
            .Select(o => o.Id)
            .ToList();
    }

    /// <summary>
    /// Line-aware attack reach. Without lines, every enemy battlefield object is in reach.
    ///
    /// Front line:
    ///   Melee:  enemy front line; reaches the enemy back line only when their front is empty.
    ///   Ranged: both enemy lines.
    /// Back line:
    ///   Melee:  when your OWN front line is empty, strikes into the enemy front line
    ///           (defensive stand); otherwise nothing. Never reaches the enemy back line.
    ///   Ranged: always the enemy front line; when the enemy front is empty AND your own
    ///           front line is held, it can bombard the enemy back line. With your front
    ///           empty, archers stay defensive: enemy front line only.
    /// </summary>
    public List<string> GetAttackTargets(GameInstance game, ObjectInstance attacker)
    {
        var enemies = game.Objects
            .Where(o => o.ControllerId != attacker.ControllerId
                && !o.IsDestroyed
                && o.ZoneId == "battlefield"
                && o.AttachedToId == null)
            .ToList();

        List<ObjectInstance> reachable;

        if (game.Definition.BattlefieldLines == null)
        {
            reachable = enemies;
        }
        else
        {
            bool ranged = attacker.Tags.Contains("ranged");
            var enemyFront = enemies.Where(o => o.Line == "front").ToList();
            var enemyBack = enemies.Where(o => o.Line != "front").ToList();

            if (attacker.Line == "front")
            {
                reachable = ranged
                    ? enemies
                    : (enemyFront.Count > 0 ? enemyFront : enemyBack);
            }
            else
            {
                bool ownFrontEmpty = !game.Objects.Any(o =>
                    o.ControllerId == attacker.ControllerId
                    && !o.IsDestroyed
                    && o.ZoneId == "battlefield"
                    && o.AttachedToId == null
                    && o.Line == "front");

                if (ranged)
                {
                    // Always the enemy front; bombard their back only from behind a held front
                    reachable = enemyFront.Count > 0
                        ? enemyFront
                        : (ownFrontEmpty ? new List<ObjectInstance>() : enemyBack);
                }
                else
                {
                    // Melee defensive stand while your own front is empty
                    reachable = ownFrontEmpty ? enemyFront : new List<ObjectInstance>();
                }
            }
        }

        // Guard: while an untapped Guard is in reach, the Hero and HQ cannot be attacked
        bool guardInReach = reachable.Any(o => o.Tags.Contains("guard") && !o.IsTapped);
        if (guardInReach)
        {
            reachable = reachable.Where(o =>
                !GameQueries.IsObjectTypeOrSubtype(game, o.ObjectType, "hero") &&
                !GameQueries.IsObjectTypeOrSubtype(game, o.ObjectType, "headquarters")).ToList();
        }

        return reachable.Select(o => o.Id).ToList();
    }
}
