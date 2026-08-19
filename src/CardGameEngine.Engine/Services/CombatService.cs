using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>
/// Attack resolution with a declaration step: declaring an attack publishes
/// attackDeclared (revealing Ambush-style secrets) and — when the defender holds a
/// playable reaction — opens a reaction window before damage resolves.
/// </summary>
public class CombatService
{
    private readonly EngineServices _s;

    public CombatService(EngineServices services) => _s = services;

    /// <summary>Does this player hold a playable reaction card answering the given event?</summary>
    public static bool HasPlayableReaction(GameInstance game, string playerId, string windowEvent)
    {
        var player = game.Players.First(p => p.Id == playerId);
        return game.Objects
            .Where(o => o.OwnerId == playerId && o.ZoneId == "hand" && !o.IsDestroyed)
            .Select(o => GameQueries.GetCardDefinition(game, o))
            .Any(def => def != null
                && (def.Timing == "reaction" || def.Timing == "both")
                && def.ReactionTo.Contains(windowEvent)
                && GameQueries.EffectivePlayCosts(game, playerId, def)
                    .All(kv => GameQueries.AvailableForPlayCost(game, player, kv.Key) >= kv.Value));
    }

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

        // Declaration: commit the attacker, reveal secrets, maybe open a reaction window
        _s.Mutator.Tap(game, attacker);
        game.Log.Add($"{attacker.Name} declares an attack on {defender.Name}!");
        _s.Bus.Publish(game, new GameEvent { Type = "attackDeclared", Source = attacker, Target = defender });

        if (attacker.IsDestroyed)
        {
            game.Log.Add($"{attacker.Name} never gets to strike!");
            return (true, null);
        }
        if (defender.IsDestroyed)
            return (true, null);

        if (HasPlayableReaction(game, defender.ControllerId, "attackDeclared"))
        {
            game.Stack.Push(new StackItem
            {
                Id = $"stk_{game.Stack.Items.Count + 1}_{game.TurnNumber}",
                Kind = "attack",
                ControllerId = playerId,
                SourceObjectId = attacker.Id,
                TargetIds = new List<string> { defender.Id }
            });
            game.State = GameState.WaitingForReaction;
            game.ReactionPlayerId = defender.ControllerId;
            game.ReactionWindowEvent = "attackDeclared";
            game.Log.Add($"{game.Players.First(p => p.Id == defender.ControllerId).Name} may respond...");
            return (true, null);
        }

        ResolveDeclaredAttack(game, attacker, defender);
        return (true, null);
    }

    /// <summary>Damage step — runs after any reaction window closes.</summary>
    public void ResolveDeclaredAttack(GameInstance game, ObjectInstance attacker, ObjectInstance defender)
    {
        if (attacker.IsDestroyed)
        {
            game.Log.Add($"{attacker.Name} was destroyed before the attack landed!");
            return;
        }
        if (defender.IsDestroyed)
        {
            game.Log.Add($"{attacker.Name}'s target is already gone.");
            return;
        }

        int attackerAttack = GameQueries.GetEffectiveProperty(game, attacker, "attack");
        int defenderAttack = GameQueries.GetEffectiveProperty(game, defender, "attack");
        int attackerArmor = GameQueries.GetEffectiveProperty(game, attacker, "armor");
        int defenderArmor = GameQueries.GetEffectiveProperty(game, defender, "armor");

        var attackerDef = GameQueries.GetCardDefinition(game, attacker);
        bool defenderIsBuilding = GameQueries.IsObjectTypeOrSubtype(game, defender.ObjectType, "building");
        if (defenderIsBuilding && attackerDef?.BonusAttackVsBuildings is int bonus && bonus > 0)
        {
            attackerAttack += bonus;
            game.Log.Add($"{attacker.Name} gains +{bonus} attack against buildings!");
        }

        int damageToDefender = Math.Max(0, attackerAttack - defenderArmor);

        bool defenderRetaliates = defender.Tags.Contains("retaliate")
            && _s.Targeting.GetAttackTargets(game, defender).Contains(attacker.Id);
        int damageToAttacker = defenderRetaliates ? Math.Max(0, defenderAttack - attackerArmor) : 0;

        game.Log.Add($"{attacker.Name} attacks {defender.Name}! ({attackerAttack} ATK vs {defenderArmor} ARM = {damageToDefender} dmg{(defenderRetaliates ? $"; retaliation: {defenderAttack} ATK vs {attackerArmor} ARM = {damageToAttacker} dmg" : "")})");

        _s.Mutator.ApplyDamage(game, attacker, damageToAttacker, defender);
        _s.Mutator.ApplyDamage(game, defender, damageToDefender, attacker);

        if (defender.IsDestroyed)
            _s.Bus.Publish(game, new GameEvent { Type = GameEventTypes.UnitKilled, Source = attacker, Target = defender });
        if (attacker.IsDestroyed)
            _s.Bus.Publish(game, new GameEvent { Type = GameEventTypes.UnitKilled, Source = defender, Target = attacker });

        _s.Bus.Publish(game, new GameEvent { Type = GameEventTypes.AttackResolved, Source = attacker, Target = defender });
    }
}