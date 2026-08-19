using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>
/// Listens to game events and fires card triggers. Cards declare triggers in data
/// ("onKill", "onDestroyBuilding", "onFriendlyDamageHqOrHero"); this service maps
/// engine events onto them. New trigger kinds = new mapping here, no action-code changes.
/// </summary>
public class TriggerService
{
    private readonly EngineServices _services;

    public TriggerService(EngineServices services)
    {
        _services = services;
        services.Bus.Subscribe(OnEvent);
    }

    private void OnEvent(GameInstance game, GameEvent evt)
    {
        switch (evt.Type)
        {
            case GameEventTypes.TurnStarted:
                if (evt.Player == null) break;
                foreach (var obj in GameQueries.BattlefieldObjects(game, evt.Player.Id).ToList())
                    Fire(game, obj, "onTurnStart");
                break;

            case GameEventTypes.UnitKilled:
                // Source = killer, Target = victim
                if (evt.Source != null && !evt.Source.IsDestroyed)
                {
                    Fire(game, evt.Source, "onKill", evt.Target);
                    if (evt.Target != null && GameQueries.IsObjectTypeOrSubtype(game, evt.Target.ObjectType, "building"))
                        Fire(game, evt.Source, "onDestroyBuilding", evt.Target);
                }
                // Dying units get their last word (Plague Zombie, Bloated Spawn)
                if (evt.Target != null)
                    Fire(game, evt.Target, "onDeath", evt.Source);
                break;

            case GameEventTypes.ObjectDestroyed:
                // Any unit dying feeds necromancy and scavengers everywhere
                if (evt.Target == null) break;
                if (!GameQueries.IsObjectTypeOrSubtype(game, evt.Target.ObjectType, "unit") &&
                    !GameQueries.IsObjectTypeOrSubtype(game, evt.Target.ObjectType, "hero")) break;
                foreach (var player in game.Players)
                    foreach (var obj in GameQueries.BattlefieldObjects(game, player.Id).ToList())
                        if (obj.Id != evt.Target.Id)
                            Fire(game, obj, "onUnitDied", evt.Target);
                break;

            case GameEventTypes.DamageDealt:
                if (evt.Source == null || evt.Target == null || evt.Amount <= 0) break;
                if (evt.Source.ControllerId == evt.Target.ControllerId) break;

                // Venom-style: attacker's own on-damage triggers, victim passed as target
                Fire(game, evt.Source, "onDealCombatDamage", evt.Target);

                bool hqOrHero = GameQueries.IsObjectTypeOrSubtype(game, evt.Target.ObjectType, "headquarters")
                    || GameQueries.IsObjectTypeOrSubtype(game, evt.Target.ObjectType, "hero");
                if (!hqOrHero) break;

                foreach (var obj in GameQueries.BattlefieldObjects(game, evt.Source.ControllerId).ToList())
                    Fire(game, obj, "onFriendlyDamageHqOrHero");
                break;

            case "attackDeclared":
                // Reveal defender's matching secrets: Source = attacker, Target = defender
                if (evt.Source == null || evt.Target == null) break;
                var defenderOwner = evt.Target.ControllerId;
                var secrets = game.Objects
                    .Where(o => o.OwnerId == defenderOwner && o.ZoneId == "secrets" && !o.IsDestroyed)
                    .ToList();
                foreach (var secret in secrets)
                {
                    var def = GameQueries.GetCardDefinition(game, secret);
                    if (def == null || !def.Triggers.Any(t => t.Event == "onEnemyAttack")) continue;
                    // Ambush guards the hero: only fires when the hero is the target
                    bool heroOnly = def.Triggers.Any(t => t.Event == "onEnemyAttack");
                    if (heroOnly && !GameQueries.IsObjectTypeOrSubtype(game, evt.Target.ObjectType, "hero")) continue;

                    secret.FaceDown = false;
                    secret.ZoneId = "discard";
                    game.Log.Add($"Secret revealed: {secret.Name}!");
                    Fire(game, secret, "onEnemyAttack", evt.Source);
                }
                break;
        }
    }

    private void Fire(GameInstance game, ObjectInstance obj, string eventName, ObjectInstance? eventTarget = null)
    {
        var cardDef = GameQueries.GetCardDefinition(game, obj);
        if (cardDef == null) return;

        var controller = game.Players.FirstOrDefault(p => p.Id == obj.ControllerId);
        if (controller == null) return;

        foreach (var trigger in cardDef.Triggers.Where(t => t.Event == eventName))
        {
            if (obj.UnderConstruction) continue; // unfinished buildings provide nothing

            if (trigger.Conditions.Any(cond => !_services.Conditions.Check(new ConditionContext
                { Game = game, Condition = cond, Object = obj, Player = controller })))
                continue;

            if (trigger.OncePerTurn)
            {
                var key = $"{obj.Id}:{eventName}";
                if (game.FiredOncePerTurn.Contains(key)) continue;
                game.FiredOncePerTurn.Add(key);
            }

            game.Log.Add($"{obj.Name} triggers!");
            foreach (var effect in trigger.Effects)
            {
                _services.Effects.Process(new EffectContext
                {
                    Game = game,
                    Effect = effect,
                    Source = obj,
                    Target = eventTarget,
                    Player = controller
                });
            }
        }
    }
}
