using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>Playing cards from hand: spells, units/buildings, and module installation.</summary>
public class CardPlayService
{
    private readonly EngineServices _s;

    public CardPlayService(EngineServices services) => _s = services;

    public (bool, string?) ExecutePlayCard(GameInstance game, string playerId, ActionRequest action)
    {
        if (game.CurrentPhaseId != "main")
            return (false, "Can only play cards during Main Phase");

        var player = game.Players.First(p => p.Id == playerId);
        var obj = game.Objects.FirstOrDefault(o => o.Id == action.SourceObjectId);
        if (obj == null) return (false, "Card not found");
        if (obj.OwnerId != playerId) return (false, "Not your card");
        if (obj.ZoneId != "hand") return (false, "Card is not in your hand");

        var cardDef = GameQueries.GetCardDefinition(game, obj);
        if (cardDef == null) return (false, "Card definition not found");

        var cost = cardDef.PlayCost ?? 0;
        var costRes = cardDef.PlayCostResource;
        if (player.Resources.GetValueOrDefault(costRes) < cost)
            return (false, $"Requires {cost} {costRes}");

        // Validate targets before paying anything
        var targetIds = action.TargetIds ?? new List<string>();
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

        // Modules need a living hero to be installed on
        ObjectInstance? moduleHost = null;
        if (cardDef.Slot != null)
        {
            moduleHost = GameQueries.FindLivingHero(game, playerId);
            if (moduleHost == null) return (false, "No hero to install this module on");
        }

        if (cost > 0)
            _s.Mutator.SpendResource(game, player, costRes, cost);

        if (cardDef.Slot != null && moduleHost != null)
        {
            InstallModule(game, obj, cardDef, moduleHost, player);
        }
        else if (GameQueries.IsObjectTypeOrSubtype(game, obj.ObjectType, "spell"))
        {
            game.Log.Add($"{player.Name} casts {obj.Name}!");
            if (cardDef.OnPlay != null)
                _s.Effects.ApplyAbility(game, cardDef.OnPlay, obj, player, targetIds);
            obj.ZoneId = "discard";
        }
        else
        {
            obj.ZoneId = "battlefield";
            obj.HasSummoningSickness = true;
            if (game.Definition.BattlefieldLines != null)
                obj.Line = game.Definition.BattlefieldLines.SpawnLine;
            game.Log.Add($"{player.Name} plays {obj.Name}!");
            if (cardDef.OnPlay != null)
                _s.Effects.ApplyAbility(game, cardDef.OnPlay, obj, player, targetIds);
        }

        _s.Bus.Publish(game, new GameEvent { Type = GameEventTypes.CardPlayed, Target = obj, Player = player });
        return (true, null);
    }

    private void InstallModule(GameInstance game, ObjectInstance module, CardDefinition moduleDef,
        ObjectInstance host, PlayerInstance player)
    {
        var slot = moduleDef.Slot!;
        var hostDef = GameQueries.GetCardDefinition(game, host);
        var capacity = hostDef?.EquipmentSlots?.GetValueOrDefault(slot, 1) ?? 1;

        // Slot full: dismantle the oldest module in that slot
        var occupying = game.Objects
            .Where(o => o.AttachedToId == host.Id && o.Slot == slot && !o.IsDestroyed)
            .ToList();
        while (occupying.Count >= capacity)
        {
            var oldest = occupying[0];
            occupying.RemoveAt(0);
            oldest.AttachedToId = null;
            oldest.ZoneId = "discard";
            game.ActiveModifiers.RemoveAll(m => m.SourceObjectId == oldest.Id);
            game.Log.Add($"{oldest.Name} is dismantled to make room.");
        }

        module.ZoneId = "battlefield";
        module.AttachedToId = host.Id;
        module.Slot = slot;
        module.HasSummoningSickness = false;
        game.Log.Add($"{player.Name} installs {module.Name} on {host.Name} ({slot} slot).");

        foreach (var attachMod in moduleDef.AttachModifiers)
        {
            _s.Mutator.AddModifier(game, host, attachMod.PropertyId, attachMod.Amount, "never", module);
            game.Log.Add($"{host.Name} gains +{attachMod.Amount} {attachMod.PropertyId} from {module.Name}.");
        }

        _s.Bus.Publish(game, new GameEvent { Type = GameEventTypes.ModuleInstalled, Source = module, Target = host, Player = player });
    }
}
