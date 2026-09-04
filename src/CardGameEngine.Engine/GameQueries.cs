using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>Read-only helpers over game state. No mutations, no events.</summary>
public static class GameQueries
{
    public static bool IsObjectTypeOrSubtype(GameInstance game, string objectType, string targetType) =>
        IsObjectTypeOrSubtype(game.Definition, objectType, targetType);

    /// <summary>Same type-hierarchy walk as the GameInstance overload, for call sites that only
    /// have the static GameDefinition (no live match) — e.g. deck-eligibility checks.</summary>
    public static bool IsObjectTypeOrSubtype(GameDefinition definition, string objectType, string targetType)
    {
        if (objectType == targetType) return true;
        var typeDef = definition.ObjectTypes.FirstOrDefault(t => t.Id == objectType);
        while (typeDef?.ParentType != null)
        {
            if (typeDef.ParentType == targetType) return true;
            typeDef = definition.ObjectTypes.FirstOrDefault(t => t.Id == typeDef.ParentType);
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

    // ---- Resource banks ----

    /// <summary>Entity-scoped play costs (mana, corpses, biomass) are paid from your HQ.</summary>
    public static ObjectInstance? FindResourceBank(GameInstance game, string playerId) =>
        game.Objects.FirstOrDefault(o =>
            o.OwnerId == playerId && !o.IsDestroyed && o.ZoneId == "battlefield" &&
            IsObjectTypeOrSubtype(game, o.ObjectType, "headquarters"));

    public static bool IsEntityScopedResource(GameInstance game, string resourceId) =>
        game.Definition.Resources.FirstOrDefault(r => r.Id == resourceId)?.Scope == "entity";

    /// <summary>How much of a play-cost resource the player can spend (own pool or HQ bank).</summary>
    public static int AvailableForPlayCost(GameInstance game, PlayerInstance player, string resourceId)
    {
        if (!IsEntityScopedResource(game, resourceId))
            return player.Resources.GetValueOrDefault(resourceId);
        return FindResourceBank(game, player.Id)?.Resources.GetValueOrDefault(resourceId) ?? 0;
    }

    public static int ResourceCapacity(GameInstance game, ObjectInstance obj, string resourceId)
    {
        var def = GetCardDefinition(game, obj);
        return def?.ResourceCapacities?.GetValueOrDefault(resourceId, int.MaxValue) ?? int.MaxValue;
    }

    // ---- Play costs ----

    /// <summary>Base multi-resource play cost of a card (PlayCosts wins over PlayCost).</summary>
    public static Dictionary<string, int> BasePlayCosts(CardDefinition cardDef)
    {
        if (cardDef.PlayCosts != null) return new Dictionary<string, int>(cardDef.PlayCosts);
        if (cardDef.PlayCost != null) return new Dictionary<string, int> { [cardDef.PlayCostResource] = cardDef.PlayCost.Value };
        return new Dictionary<string, int>();
    }

    /// <summary>
    /// A card belongs in deck-builder pools when it has any play cost — single-resource
    /// (playCost, e.g. Peasant) or multi-resource (playCosts, e.g. Soldier's gold+training).
    /// Hero-lineage cards are always eligible regardless of play cost: most heroes enter via
    /// the lobby/deck-builder hero picker (no playCost at all, e.g. ax-01), not from hand, but
    /// still need to be selectable in a custom deck's card pool (task 1421). Mirrors the
    /// frontend's isDeckEligible (frontend/src/utils/deckEligibility.ts, task 1421 follow-up
    /// to PR #36) so both sides agree on what "deck-eligible" means.
    /// </summary>
    public static bool IsDeckEligible(GameDefinition definition, CardDefinition cardDef) =>
        IsObjectTypeOrSubtype(definition, cardDef.ObjectType, "hero")
        || cardDef.PlayCost != null || cardDef.PlayCosts != null;

    /// <summary>
    /// Validates a deck's card ids/counts against a game definition's card pool and, for
    /// non-admins, its deck rules (max size, max copies, deck-eligibility). Returns null when
    /// valid, otherwise a human-readable error. Shared by match creation and the saved-deck
    /// store so every deck builder (Lobby, Campaign, My Decks) enforces the same limits.
    /// enforceMinSize additionally requires the deck to meet DeckRules.MinDeckSize — off by
    /// default so match creation keeps its existing (no minimum) behavior; a deck saved for
    /// reuse must be a legally playable size, so the deck store turns it on.
    /// </summary>
    public static string? ValidateDeck(GameDefinition definition, Dictionary<string, int> deck, bool isAdmin,
        bool enforceMinSize = false)
    {
        foreach (var (cardId, count) in deck)
        {
            if (count < 0)
                return $"negative count for card '{cardId}'";
            var cardDef = definition.Cards.FirstOrDefault(c => c.Id == cardId);
            if (cardDef == null)
                return $"unknown card '{cardId}'";
            if (!isAdmin && !IsDeckEligible(definition, cardDef))
                return $"card '{cardDef.Name}' is not deck-eligible";
        }

        if (isAdmin || definition.DeckRules == null) return null;

        var rules = definition.DeckRules;
        var total = deck.Values.Sum();
        if (total > rules.MaxDeckSize)
            return $"deck has {total} cards, maximum is {rules.MaxDeckSize}";
        if (enforceMinSize && total < rules.MinDeckSize)
            return $"deck has {total} cards, minimum is {rules.MinDeckSize}";
        foreach (var (cardId, count) in deck)
        {
            var cardDef = definition.Cards.First(c => c.Id == cardId);
            if (cardDef.DeckLimit == "unlimited") continue;
            var limit = int.TryParse(cardDef.DeckLimit, out var perCard) ? perCard : rules.MaxCopies;
            if (count > limit)
                return $"at most {limit} copies of '{cardId}' allowed";
        }
        return null;
    }

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
