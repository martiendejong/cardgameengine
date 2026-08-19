using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>
/// Attack resolution. Publishes DamageDealt/UnitKilled/AttackResolved events —
/// triggers (Spoils of War, Scavenger bounty, ...) react via the bus, not here.
/// </summary>
public class CombatService
{
    private readonly EngineServices _s;

    public CombatService(EngineServices services) => _s = services;

    public (bool, string?) ExecuteAttack(GameInstance game, string playerId, ActionRequest action)
    {
        if (game.CurrentPhaseId != "combat")
            return (false, "Can only attack during combat phase");

        var attacker = game.Objects.FirstOrDefault(o => o.Id == action.SourceObjectId);
        if (attacker == null) return (false, "Attacker not found");
        if (attacker.ControllerId != playerId) return (false, "Not your unit");
        if (attacker.IsTapped) return (false, "Unit is tapped");
        if (!GameQueries.IsObjectTypeOrSubtype(game, attacker.ObjectType, "character")) return (false, "Only characters can attack");
        if (attacker.HasSummoningSickness) return (false, "Cannot attack the turn it was summoned");
        if (attacker.HasMovedThisTurn) return (false, "Already moved this turn");

        if (action.TargetIds.Count == 0) return (false, "No target specified");
        var defender = game.Objects.FirstOrDefault(o => o.Id == action.TargetIds[0]);
        if (defender == null) return (false, "Defender not found");
        if (defender.ControllerId == playerId) return (false, "Cannot attack your own units");
        if (defender.IsDestroyed) return (false, "Target is already destroyed");
        if (!_s.Targeting.GetAttackTargets(game, attacker).Contains(defender.Id))
            return (false, "Target is out of reach");

        int attackerAttack = GameQueries.GetEffectiveProperty(game, attacker, "attack");
        int defenderAttack = GameQueries.GetEffectiveProperty(game, defender, "attack");
        int attackerArmor = GameQueries.GetEffectiveProperty(game, attacker, "armor");
        int defenderArmor = GameQueries.GetEffectiveProperty(game, defender, "armor");

        // Anti-building bonus (e.g. Pillager)
        var attackerDef = GameQueries.GetCardDefinition(game, attacker);
        bool defenderIsBuilding = GameQueries.IsObjectTypeOrSubtype(game, defender.ObjectType, "building");
        if (defenderIsBuilding && attackerDef?.BonusAttackVsBuildings is int bonus && bonus > 0)
        {
            attackerAttack += bonus;
            game.Log.Add($"{attacker.Name} gains +{bonus} attack against buildings!");
        }

        int damageToDefender = Math.Max(0, attackerAttack - defenderArmor);

        // Defenders do not strike back by default. Only the Retaliate keyword lets a
        // defender hit back, and even then only when the attacker is within its reach.
        bool defenderRetaliates = defender.Tags.Contains("retaliate")
            && _s.Targeting.GetAttackTargets(game, defender).Contains(attacker.Id);
        int damageToAttacker = defenderRetaliates ? Math.Max(0, defenderAttack - attackerArmor) : 0;

        game.Log.Add($"{attacker.Name} attacks {defender.Name}! ({attackerAttack} ATK vs {defenderArmor} ARM = {damageToDefender} dmg{(defenderRetaliates ? $"; retaliation: {defenderAttack} ATK vs {attackerArmor} ARM = {damageToAttacker} dmg" : "")})");

        // Apply damage simultaneously (events published by the mutator)
        _s.Mutator.ApplyDamage(game, attacker, damageToAttacker, defender);
        _s.Mutator.ApplyDamage(game, defender, damageToDefender, attacker);

        _s.Mutator.Tap(game, attacker);

        if (defender.IsDestroyed)
            _s.Bus.Publish(game, new GameEvent { Type = GameEventTypes.UnitKilled, Source = attacker, Target = defender });
        if (attacker.IsDestroyed)
            _s.Bus.Publish(game, new GameEvent { Type = GameEventTypes.UnitKilled, Source = defender, Target = attacker });

        _s.Bus.Publish(game, new GameEvent { Type = GameEventTypes.AttackResolved, Source = attacker, Target = defender });

        return (true, null);
    }
}
