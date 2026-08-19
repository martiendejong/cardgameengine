using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>
/// The only place game state is mutated. Every mutation logs and publishes an event,
/// so triggers and future reaction systems see everything without actions knowing about them.
/// </summary>
public class GameMutator
{
    private readonly EventBus _bus;
    private int _modifierCounter;

    public GameMutator(EventBus bus) => _bus = bus;

    // ---- Resources ----

    public void GainResource(GameInstance game, PlayerInstance player, string resourceId, int amount)
    {
        if (amount == 0) return;
        player.Resources[resourceId] = player.Resources.GetValueOrDefault(resourceId) + amount;
        game.Log.Add($"{player.Name} gains {amount} {resourceId}. (Total: {player.Resources[resourceId]})");
        _bus.Publish(game, new GameEvent { Type = GameEventTypes.ResourceGained, Player = player, ResourceId = resourceId, Amount = amount });
    }

    public void SpendResource(GameInstance game, PlayerInstance player, string resourceId, int amount)
    {
        if (amount == 0) return;
        player.Resources[resourceId] = player.Resources.GetValueOrDefault(resourceId) - amount;
        game.Log.Add($"{player.Name} spends {amount} {resourceId}. (Remaining: {player.Resources[resourceId]})");
        _bus.Publish(game, new GameEvent { Type = GameEventTypes.ResourceSpent, Player = player, ResourceId = resourceId, Amount = amount });
    }

    public void GainEntityResource(GameInstance game, ObjectInstance obj, string resourceId, int amount)
    {
        if (amount == 0) return;
        obj.Resources[resourceId] = obj.Resources.GetValueOrDefault(resourceId) + amount;
        if (amount > 0)
            game.Log.Add($"{obj.Name} gains {amount} {resourceId}. (Total: {obj.Resources[resourceId]})");
        else
            game.Log.Add($"{obj.Name} spends {-amount} {resourceId}. (Remaining: {obj.Resources[resourceId]})");
    }

    // ---- Damage / healing / destruction ----

    /// <param name="source">what caused the damage (attacker, spell object) — flows into events for triggers</param>
    public void ApplyDamage(GameInstance game, ObjectInstance obj, int damage, ObjectInstance? source = null)
    {
        if (damage <= 0) return;
        var current = obj.Properties.GetValueOrDefault("currentHp");
        var newHp = Math.Max(0, current - damage);
        obj.Properties["currentHp"] = newHp;
        game.Log.Add($"{obj.Name} takes {damage} damage. (HP: {current} → {newHp})");

        var controller = game.Players.FirstOrDefault(p => p.Id == (source?.ControllerId ?? ""));
        _bus.Publish(game, new GameEvent { Type = GameEventTypes.DamageDealt, Source = source, Target = obj, Player = controller, Amount = damage });

        if (newHp <= 0)
            DestroyObject(game, obj, source);
    }

    public void Heal(GameInstance game, ObjectInstance obj, int amount)
    {
        if (amount <= 0) return;
        var currentHp = obj.Properties.GetValueOrDefault("currentHp");
        var maxHp = obj.Properties.GetValueOrDefault("maxHp");
        var newHp = Math.Min(currentHp + amount, maxHp);
        obj.Properties["currentHp"] = newHp;
        game.Log.Add($"{obj.Name} heals {amount} HP. (HP: {currentHp} → {newHp}/{maxHp})");
        _bus.Publish(game, new GameEvent { Type = GameEventTypes.Healed, Target = obj, Amount = newHp - currentHp });
    }

    public void DestroyObject(GameInstance game, ObjectInstance obj, ObjectInstance? source = null)
    {
        if (obj.IsDestroyed) return;
        obj.IsDestroyed = true;
        obj.ZoneId = "discard";
        game.Log.Add($"{obj.Name} is destroyed!");

        // Bonuses granted by this object disappear with it
        game.ActiveModifiers.RemoveAll(m => m.SourceObjectId == obj.Id);

        // Modules go down with their host
        var attached = game.Objects.Where(o => o.AttachedToId == obj.Id && !o.IsDestroyed).ToList();
        foreach (var module in attached)
        {
            game.Log.Add($"{module.Name} is destroyed along with {obj.Name}!");
            DestroyObject(game, module, source);
        }

        _bus.Publish(game, new GameEvent { Type = GameEventTypes.ObjectDestroyed, Source = source, Target = obj });
    }

    // ---- Tap state ----

    public void Tap(GameInstance game, ObjectInstance obj) => obj.IsTapped = true;
    public void Untap(GameInstance game, ObjectInstance obj) => obj.IsTapped = false;

    // ---- Modifiers ----

    public ModifierInstance AddModifier(GameInstance game, ObjectInstance target, string propertyId, int amount,
        string expiresOn, ObjectInstance? source = null)
    {
        _modifierCounter++;
        var modifier = new ModifierInstance
        {
            Id = $"mod_{_modifierCounter}",
            TargetObjectId = target.Id,
            PropertyId = propertyId,
            Amount = amount,
            ExpiresOn = expiresOn,
            SourceObjectId = source?.Id
        };
        game.ActiveModifiers.Add(modifier);
        return modifier;
    }

    public void ExpireModifiers(GameInstance game, string eventType)
    {
        var expired = game.ActiveModifiers.Where(m => m.ExpiresOn == eventType).ToList();
        foreach (var mod in expired)
        {
            game.ActiveModifiers.Remove(mod);
            var obj = game.Objects.FirstOrDefault(o => o.Id == mod.TargetObjectId);
            if (obj != null)
            {
                game.Log.Add($"Buff on {obj.Name} ({mod.PropertyId} +{mod.Amount}) expires.");
                _bus.Publish(game, new GameEvent { Type = GameEventTypes.ModifierExpired, Target = obj });
            }
        }
    }

    // ---- Properties ----

    public void SetProperty(GameInstance game, ObjectInstance obj, string propertyId, int value)
    {
        obj.Properties[propertyId] = value;
        ClampProperty(game, obj, propertyId);
    }

    public void ModifyProperty(GameInstance game, ObjectInstance obj, string propertyId, int delta)
    {
        obj.Properties[propertyId] = obj.Properties.GetValueOrDefault(propertyId) + delta;
        ClampProperty(game, obj, propertyId);
    }

    private static void ClampProperty(GameInstance game, ObjectInstance obj, string propId)
    {
        var propDef = game.Definition.Properties.FirstOrDefault(p => p.Id == propId);
        if (propDef == null) return;

        var val = obj.Properties.GetValueOrDefault(propId);
        if (propDef.MinValue.HasValue && propDef.ClampToMin)
            val = Math.Max(val, propDef.MinValue.Value);
        if (propDef.MaxValue.HasValue)
            val = Math.Min(val, propDef.MaxValue.Value);
        if (propId == "currentHp" && propDef.MaxValue == null)
            val = Math.Min(val, obj.Properties.GetValueOrDefault("maxHp"));
        obj.Properties[propId] = val;
    }
}
