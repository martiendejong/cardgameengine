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
    /// Melee: front hits enemy front (or back when front is empty); back hits nothing.
    /// Ranged: front hits both enemy lines; back hits enemy front only.
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

        if (ranged) return enemyFront.Select(o => o.Id).ToList();
        return new List<string>();
    }
}
