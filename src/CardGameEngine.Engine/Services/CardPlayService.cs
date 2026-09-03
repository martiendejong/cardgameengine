using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>
/// Playing cards from hand: spells, units, buildings (possibly under construction),
/// hero limit, and attachments (robot modules auto-attach to your hero; equipment
/// attaches to a chosen character and can occupy multiple slots, e.g. two-handed).
/// </summary>
public class CardPlayService
{
    private readonly EngineServices _s;

    public CardPlayService(EngineServices services) => _s = services;

    public (bool, string?) ExecutePlayCard(GameInstance game, string playerId, ActionRequest action)
    {
        bool inReactionWindow = game.State == GameState.WaitingForReaction;

        if (!inReactionWindow && game.CurrentPhaseId != "main")
            return (false, "Can only play cards during Main Phase");

        var player = game.Players.First(p => p.Id == playerId);
        var obj = game.Objects.FirstOrDefault(o => o.Id == action.SourceObjectId);
        if (obj == null) return (false, "Card not found");
        if (obj.OwnerId != playerId) return (false, "Not your card");
        if (obj.ZoneId != "hand") return (false, "Card is not in your hand");

        var cardDef = GameQueries.GetCardDefinition(game, obj);
        if (cardDef == null) return (false, "Card definition not found");

        if (inReactionWindow)
        {
            if (cardDef.Timing != "reaction" && cardDef.Timing != "both")
                return (false, "Only reaction cards can be played now");
            if (game.ReactionWindowEvent != null && !cardDef.ReactionTo.Contains(game.ReactionWindowEvent))
                return (false, "This reaction does not answer the current event");
        }
        else if (cardDef.Timing == "reaction")
        {
            return (false, "Reaction — play it when an enemy acts");
        }

        // Multi-resource cost after discounts; entity-scoped resources come from your HQ bank
        var costs = GameQueries.EffectivePlayCosts(game, playerId, cardDef);
        foreach (var (resId, amount) in costs)
            if (GameQueries.AvailableForPlayCost(game, player, resId) < amount)
                return (false, $"Requires {GameQueries.FormatCosts(costs)}");

        // Additional non-resource costs (e.g. sacrifice units you control)
        var extraCosts = cardDef.PlayCostsExtra ?? new List<CostDefinition>();
        foreach (var cost in extraCosts)
            if (!_s.Costs.CanPay(new CostContext { Game = game, Cost = cost, Object = obj, Player = player }))
                return (false, $"Cannot pay cost: {cost.Type}");

        if (cardDef.HousingCost is int housing &&
            !GameQueries.HasHousingFor(game, playerId, housing))
            return (false, $"Not enough housing ({GameQueries.HousingUsed(game, playerId)}/{GameQueries.HousingCapacity(game, playerId)})");

        // One active hero at a time
        bool isHero = GameQueries.IsObjectTypeOrSubtype(game, obj.ObjectType, "hero");
        if (isHero && GameQueries.FindLivingHero(game, playerId) != null)
            return (false, "You already control a Hero");

        // Validate onPlay targets before paying anything
        var targetIds = action.TargetIds ?? new List<string>();
        var attachSlots = GetAttachSlots(cardDef);

        if (cardDef.OnPlay?.Choice != null)
        {
            var choice = cardDef.OnPlay.Choice;
            if (targetIds.Count < choice.Min || targetIds.Count > choice.Max)
                return (false, $"Must select between {choice.Min} and {choice.Max} target(s)");
            var valid = _s.Targeting.GetValidTargets(game, choice, playerId);
            foreach (var t in targetIds)
                if (!valid.Contains(t))
                    return (false, "Invalid target");
        }

        // Resolve attachment host
        ObjectInstance? attachHost = null;
        if (attachSlots.Count > 0)
        {
            if (cardDef.AttachTo == "chooseCharacter")
            {
                if (targetIds.Count != 1) return (false, "Choose a character to equip");
                attachHost = game.Objects.FirstOrDefault(o => o.Id == targetIds[0]);
                if (attachHost == null || attachHost.IsDestroyed || attachHost.ZoneId != "battlefield" ||
                    attachHost.ControllerId != playerId ||
                    !GameQueries.IsObjectTypeOrSubtype(game, attachHost.ObjectType, "character"))
                    return (false, "Invalid equip target");
            }
            else
            {
                attachHost = GameQueries.FindLivingHero(game, playerId);
                if (attachHost == null) return (false, "No hero to install this module on");
            }

            var slotError = CheckSlots(game, attachHost, attachSlots);
            if (slotError != null) return (false, slotError);
        }

        // Pay (entity-scoped costs drain the HQ bank)
        foreach (var (resId, amount) in costs)
        {
            if (amount <= 0) continue;
            if (GameQueries.IsEntityScopedResource(game, resId))
            {
                var bank = GameQueries.FindResourceBank(game, playerId)!;
                _s.Mutator.GainEntityResource(game, bank, resId, -amount);
            }
            else
            {
                _s.Mutator.SpendResource(game, player, resId, amount);
            }
        }
        foreach (var cost in extraCosts)
            _s.Costs.Pay(new CostContext { Game = game, Cost = cost, Object = obj, Player = player });
        ConsumeCostModifiers(game, playerId, cardDef);

        // Resolve
        if (cardDef.IsSecret)
        {
            obj.ZoneId = "secrets";
            obj.FaceDown = true;
            game.Log.Add($"{player.Name} plays a card face-down.");
        }
        else if (attachHost != null)
        {
            Attach(game, obj, cardDef, attachHost, attachSlots, player);
        }
        else if (GameQueries.IsObjectTypeOrSubtype(game, obj.ObjectType, "spell"))
        {
            // Sorceries can be countered: open a window when the opponent holds an answer
            var opponent = GameQueries.GetOpponent(game, player);
            if (!inReactionWindow && opponent != null &&
                HasReactionTo(game, opponent.Id, "spellCast"))
            {
                obj.ZoneId = "stack";
                game.Stack.Push(new StackItem
                {
                    Id = $"stk_{game.Stack.Items.Count + 1}_{game.TurnNumber}",
                    Kind = "spell",
                    ControllerId = playerId,
                    SourceObjectId = obj.Id,
                    TargetIds = targetIds
                });
                game.State = GameState.WaitingForReaction;
                game.ReactionPlayerId = opponent.Id;
                game.ReactionWindowEvent = "spellCast";
                game.Log.Add($"{player.Name} begins casting {obj.Name}... {opponent.Name} may respond.");
                return (true, null);
            }

            game.Log.Add($"{player.Name} casts {obj.Name}!");
            if (cardDef.OnPlay != null)
                _s.Effects.ApplyAbility(game, cardDef.OnPlay, obj, player, targetIds);
            obj.ZoneId = "discard";
        }
        else
        {
            obj.ZoneId = "battlefield";
            obj.HasSummoningSickness = true;
            if (cardDef.Duration is int lifetime) obj.Lifetime = lifetime;
            if (game.Definition.BattlefieldLines != null)
                obj.Line = game.Definition.BattlefieldLines.SpawnLine;

            if (cardDef.ConstructionRequirement is int req && req > 0)
            {
                obj.UnderConstruction = true;
                obj.ConstructionProgress = 0;
                game.Log.Add($"{player.Name} starts constructing {obj.Name} (0/{req}).");
            }
            else
            {
                game.Log.Add($"{player.Name} plays {obj.Name}!");
            }

            if (cardDef.OnPlay != null)
                _s.Effects.ApplyAbility(game, cardDef.OnPlay, obj, player, targetIds);
        }

        _s.Bus.Publish(game, new GameEvent { Type = GameEventTypes.CardPlayed, Target = obj, Player = player });
        return (true, null);
    }

    private static bool HasReactionTo(GameInstance game, string playerId, string evt) =>
        CombatService.HasPlayableReaction(game, playerId, evt);

    /// <summary>Resolve a spell that sat on the stack (unless it was countered).</summary>
    public void ResolveSpellItem(GameInstance game, StackItem item)
    {
        var obj = game.Objects.FirstOrDefault(o => o.Id == item.SourceObjectId);
        if (obj == null) return;
        var player = game.Players.First(p => p.Id == item.ControllerId);
        var cardDef = GameQueries.GetCardDefinition(game, obj);

        if (item.Cancelled)
        {
            game.Log.Add($"{obj.Name} fizzles.");
            obj.ZoneId = "discard";
            return;
        }

        game.Log.Add($"{player.Name} casts {obj.Name}!");
        if (cardDef?.OnPlay != null)
            _s.Effects.ApplyAbility(game, cardDef.OnPlay, obj, player, item.TargetIds);
        obj.ZoneId = "discard";
    }

    public static List<string> GetAttachSlots(CardDefinition cardDef)
    {
        if (cardDef.Slots is { Count: > 0 }) return cardDef.Slots;
        if (cardDef.Slot != null) return new List<string> { cardDef.Slot };
        return new List<string>();
    }

    /// <summary>Slot capacities for a host: its own declaration, or the game default for characters.</summary>
    public static Dictionary<string, int> HostSlotCapacities(GameInstance game, ObjectInstance host)
    {
        var hostDef = GameQueries.GetCardDefinition(game, host);
        if (hostDef?.EquipmentSlots != null) return hostDef.EquipmentSlots;
        if (GameQueries.IsObjectTypeOrSubtype(game, host.ObjectType, "character"))
            return game.Definition.DefaultEquipmentSlots ?? new Dictionary<string, int>();
        return new Dictionary<string, int>();
    }

    private string? CheckSlots(GameInstance game, ObjectInstance host, List<string> slots)
    {
        var capacities = HostSlotCapacities(game, host);
        foreach (var slot in slots.Distinct())
        {
            var needed = slots.Count(s => s == slot);
            if (capacities.GetValueOrDefault(slot, 0) < needed)
                return $"{host.Name} has no {slot} slot";
        }
        return null;
    }

    private void ConsumeCostModifiers(GameInstance game, string playerId, CardDefinition cardDef)
    {
        var applicable = game.ActiveCostModifiers
            .Where(m => m.PlayerId == playerId
                && (m.TagFilter == null || cardDef.Tags.Contains(m.TagFilter))
                && GameQueries.BasePlayCosts(cardDef).ContainsKey(m.ResourceId))
            .ToList();
        foreach (var mod in applicable)
        {
            mod.Uses--;
            if (mod.Uses <= 0) game.ActiveCostModifiers.Remove(mod);
        }
    }

    private void Attach(GameInstance game, ObjectInstance attachment, CardDefinition attachDef,
        ObjectInstance host, List<string> slots, PlayerInstance player)
    {
        var capacities = HostSlotCapacities(game, host);

        // Evict whatever occupies the required slots
        foreach (var slot in slots.Distinct())
        {
            var capacity = capacities.GetValueOrDefault(slot, 0);
            var needed = slots.Count(s => s == slot);
            var occupying = game.Objects
                .Where(o => o.AttachedToId == host.Id && !o.IsDestroyed && o.OccupiedSlots.Contains(slot))
                .ToList();
            while (occupying.Count > capacity - needed)
            {
                var oldest = occupying[0];
                occupying.RemoveAt(0);
                Unattach(game, oldest);
                game.Log.Add($"{oldest.Name} is unequipped to make room.");
            }
        }

        attachment.ZoneId = "battlefield";
        attachment.AttachedToId = host.Id;
        attachment.Slot = slots[0];
        attachment.OccupiedSlots = new List<string>(slots);
        attachment.HasSummoningSickness = false;
        if (attachDef.Duration is int lifetime) attachment.Lifetime = lifetime; // magic equipment fades
        game.Log.Add($"{player.Name} equips {attachment.Name} on {host.Name} ({string.Join("+", slots)}).");

        foreach (var attachMod in attachDef.AttachModifiers)
        {
            _s.Mutator.AddModifier(game, host, attachMod.PropertyId, attachMod.Amount, "never", attachment);
            game.Log.Add($"{host.Name} gains +{attachMod.Amount} {attachMod.PropertyId} from {attachment.Name}.");
        }

        foreach (var tag in attachDef.AttachTags)
        {
            if (!host.Tags.Contains(tag))
            {
                host.Tags.Add(tag);
                game.Log.Add($"{host.Name} gains {tag} from {attachment.Name}.");
            }
        }

        _s.Bus.Publish(game, new GameEvent { Type = GameEventTypes.ModuleInstalled, Source = attachment, Target = host, Player = player });
    }

    private void Unattach(GameInstance game, ObjectInstance attachment)
    {
        var host = game.Objects.FirstOrDefault(o => o.Id == attachment.AttachedToId);
        var attachDef = GameQueries.GetCardDefinition(game, attachment);

        if (host != null && attachDef != null)
            foreach (var tag in attachDef.AttachTags)
                host.Tags.Remove(tag);

        game.ActiveModifiers.RemoveAll(m => m.SourceObjectId == attachment.Id);
        attachment.AttachedToId = null;
        attachment.OccupiedSlots = new List<string>();
        attachment.ZoneId = "discard";
    }
}