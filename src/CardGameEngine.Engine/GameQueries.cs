using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>Read-only helpers over game state. No mutations, no events.</summary>
public static class GameQueries
{
    public static bool IsObjectTypeOrSubtype(GameInstance game, string objectType, string targetType)
    {
        if (objectType == targetType) return true;
        var typeDef = game.Definition.ObjectTypes.FirstOrDefault(t => t.Id == objectType);
        while (typeDef?.ParentType != null)
        {
            if (typeDef.ParentType == targetType) return true;
            typeDef = game.Definition.ObjectTypes.FirstOrDefault(t => t.Id == typeDef.ParentType);
        }
        return false;
    }

    public static bool ObjectTypeHasResource(GameInstance game, string objectType, string resourceId)
    {
        var typeDef = game.Definition.ObjectTypes.FirstOrDefault(t => t.Id == objectType);
        while (typeDef != null)
        {
            if (typeDef.Resources.Contains(resourceId)) return true;
            typeDef = game.Definition.ObjectTypes.FirstOrDefault(t => t.Id == typeDef.ParentType);
        }
        return false;
    }

    /// <summary>Base property value plus all active modifiers.</summary>
    public static int GetEffectiveProperty(GameInstance game, ObjectInstance obj, string propertyId)
    {
        var baseVal = obj.Properties.GetValueOrDefault(propertyId);
        var bonus = game.ActiveModifiers
            .Where(m => m.TargetObjectId == obj.Id && m.PropertyId == propertyId)
            .Sum(m => m.Amount);
        return baseVal + bonus;
    }

    public static CardDefinition? GetCardDefinition(GameInstance game, ObjectInstance obj) =>
        game.Definition.Cards.FirstOrDefault(c => c.Id == obj.DefinitionId);

    public static ObjectInstance? FindLivingHero(GameInstance game, string playerId) =>
        game.Objects.FirstOrDefault(o =>
            o.OwnerId == playerId && !o.IsDestroyed && o.ZoneId == "battlefield" &&
            IsObjectTypeOrSubtype(game, o.ObjectType, "hero"));

    public static PlayerInstance? GetOpponent(GameInstance game, PlayerInstance player) =>
        game.Players.FirstOrDefault(p => p.Id != player.Id);

    public static IEnumerable<ObjectInstance> BattlefieldObjects(GameInstance game, string? controllerId = null) =>
        game.Objects.Where(o =>
            !o.IsDestroyed && o.ZoneId == "battlefield" &&
            (controllerId == null || o.ControllerId == controllerId));

    public static bool IsAttachable(ObjectInstance o) => o.AttachedToId != null;
}
