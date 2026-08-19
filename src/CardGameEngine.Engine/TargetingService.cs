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

        if (game.Definition.BattlefieldLines == null)
            return enemies.Select(o => o.Id).ToList();

        bool ranged = attacker.Tags.Contains("ranged");
        var enemyFront = enemies.Where(o => o.Line == "front").ToList();
        var enemyBack = enemies.Where(o => o.Line != "front").ToList();

        if (attacker.Line == "front")
        {
            if (ranged) return enemies.Select(o => o.Id).ToList();
            return (enemyFront.Count > 0 ? enemyFront : enemyBack).Select(o => o.Id).ToList();
        }

        // Back line
        bool ownFrontEmpty = !game.Objects.Any(o =>
            o.ControllerId == attacker.ControllerId
            && !o.IsDestroyed
            && o.ZoneId == "battlefield"
            && o.AttachedToId == null
            && o.Line == "front");

        if (ranged)
        {
            if (enemyFront.Count > 0)
                return enemyFront.Select(o => o.Id).ToList();
            // Enemy front is empty: bombard their back line, but only from behind a held front
            return ownFrontEmpty ? new List<string>() : enemyBack.Select(o => o.Id).ToList();
        }

        // Melee: defensive stand — strike the enemy front line while your own front is empty
        if (ownFrontEmpty)
            return enemyFront.Select(o => o.Id).ToList();
        return new List<string>();
    }
}
