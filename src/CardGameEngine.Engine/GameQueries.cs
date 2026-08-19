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

    // ---- Play costs ----

    /// <summary>Base multi-resource play cost of a card (PlayCosts wins over PlayCost).</summary>
    public static Dictionary<string, int> BasePlayCosts(CardDefinition cardDef)
    {
        if (cardDef.PlayCosts != null) return new Dictionary<string, int>(cardDef.PlayCosts);
        if (cardDef.PlayCost != null) return new Dictionary<string, int> { [cardDef.PlayCostResource] = cardDef.PlayCost.Value };
        return new Dictionary<string, int>();
    }

    public static bool IsDeckEligible(CardDefinition cardDef) =>
        cardDef.PlayCost != null || cardDef.PlayCosts != null;

    /// <summary>Play cost after active cost discounts (e.g. Archery Range). Never below 0.</summary>
    public static Dictionary<string, int> EffectivePlayCosts(GameInstance game, string playerId, CardDefinition cardDef)
    {
        var costs = BasePlayCosts(cardDef);
        foreach (var mod in game.ActiveCostModifiers)
        {
            if (mod.PlayerId != playerId) continue;
            if (mod.TagFilter != null && !cardDef.Tags.Contains(mod.TagFilter)) continue;
            if (!costs.ContainsKey(mod.ResourceId)) continue;
            costs[mod.ResourceId] = Math.Max(0, costs[mod.ResourceId] - mod.Amount);
        }
        return costs;
    }

    public static string FormatCosts(Dictionary<string, int> costs)
    {
        if (costs.Count == 0) return "free";
        return string.Join(" + ", costs.Select(kv =>
            kv.Key == "gold" ? $"{kv.Value}g" : $"{kv.Value} {kv.Key}"));
    }

    // ---- Housing ----

    public static int HousingUsed(GameInstance game, string playerId) =>
        BattlefieldObjects(game, playerId)
            .Sum(o => GetCardDefinition(game, o)?.HousingCost ?? 0);

    // Buildings under construction provide nothing yet
    public static int HousingCapacity(GameInstance game, string playerId) =>
        BattlefieldObjects(game, playerId)
            .Where(o => !o.UnderConstruction)
            .Sum(o => GetCardDefinition(game, o)?.HousingProvided ?? 0);

    /// <summary>Free living space; effectively unlimited for decks that never use housing.</summary>
    public static bool HasHousingFor(GameInstance game, string playerId, int cost)
    {
        if (cost <= 0) return true;
        return HousingUsed(game, playerId) + cost <= HousingCapacity(game, playerId);
    }
}
