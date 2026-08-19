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
            case GameEventTypes.UnitKilled:
                // Source = killer, Target = victim
                if (evt.Source != null && !evt.Source.IsDestroyed)
                {
                    Fire(game, evt.Source, "onKill");
                    if (evt.Target != null && GameQueries.IsObjectTypeOrSubtype(game, evt.Target.ObjectType, "building"))
                        Fire(game, evt.Source, "onDestroyBuilding");
                }
                break;

            case GameEventTypes.DamageDealt:
                if (evt.Source == null || evt.Target == null || evt.Amount <= 0) break;
                if (evt.Source.ControllerId == evt.Target.ControllerId) break;

                bool hqOrHero = GameQueries.IsObjectTypeOrSubtype(game, evt.Target.ObjectType, "headquarters")
                    || GameQueries.IsObjectTypeOrSubtype(game, evt.Target.ObjectType, "hero");
                if (!hqOrHero) break;

                foreach (var obj in GameQueries.BattlefieldObjects(game, evt.Source.ControllerId).ToList())
                    Fire(game, obj, "onFriendlyDamageHqOrHero");
                break;
        }
    }

    private void Fire(GameInstance game, ObjectInstance obj, string eventName)
    {
        var cardDef = GameQueries.GetCardDefinition(game, obj);
        if (cardDef == null) return;

        var controller = game.Players.FirstOrDefault(p => p.Id == obj.ControllerId);
        if (controller == null) return;

        foreach (var trigger in cardDef.Triggers.Where(t => t.Event == eventName))
        {
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
                    Target = null,
                    Player = controller
                });
            }
        }
    }
}
