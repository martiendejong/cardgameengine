using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>Creates and reshapes object instances from card definitions.</summary>
public class ObjectFactory
{
    private int _objectCounter;

    public ObjectInstance CreateObjectInstance(GameInstance game, CardDefinition cardDef, string playerId, string zoneId)
    {
        _objectCounter++;
        var obj = new ObjectInstance
        {
            Id = $"obj_{_objectCounter}",
            DefinitionId = cardDef.Id,
            Name = cardDef.Name,
            ObjectType = cardDef.ObjectType,
            OwnerId = playerId,
            ControllerId = playerId,
            ZoneId = zoneId,
            Tags = new List<string>(cardDef.Tags)
        };

        if (zoneId == "battlefield" && game.Definition.BattlefieldLines != null)
            obj.Line = game.Definition.BattlefieldLines.SpawnLine;

        foreach (var propDef in game.Definition.Properties)
        {
            obj.Properties[propDef.Id] = cardDef.Properties.TryGetValue(propDef.Id, out var val)
                ? val : propDef.DefaultValue;
        }

        foreach (var resDef in game.Definition.Resources.Where(r => r.Scope == "entity"))
        {
            if (!GameQueries.ObjectTypeHasResource(game, cardDef.ObjectType, resDef.Id)) continue;
            obj.Resources[resDef.Id] = cardDef.Resources.TryGetValue(resDef.Id, out var val)
                ? val : resDef.DefaultValue;
        }

        return obj;
    }

    public ObjectInstance PlaceCard(GameInstance game, string cardId, string zoneId, string playerId)
    {
        var cardDef = game.Definition.Cards.First(c => c.Id == cardId);
        var obj = CreateObjectInstance(game, cardDef, playerId, zoneId);
        game.Objects.Add(obj);
        return obj;
    }

    /// <summary>Rebuild an existing instance as another card (e.g. Peasant trained into Soldier).</summary>
    public void Transform(GameInstance game, ObjectInstance target, CardDefinition newDef)
    {
        var oldName = target.Name;
        target.DefinitionId = newDef.Id;
        target.Name = newDef.Name;
        target.ObjectType = newDef.ObjectType;
        target.Tags = new List<string>(newDef.Tags);
        target.IsTapped = false;
        target.HasSummoningSickness = true;

        target.Properties.Clear();
        foreach (var propDef in game.Definition.Properties)
        {
            target.Properties[propDef.Id] = newDef.Properties.TryGetValue(propDef.Id, out var val)
                ? val : propDef.DefaultValue;
        }

        target.Resources.Clear();
        foreach (var resDef in game.Definition.Resources.Where(r => r.Scope == "entity"))
        {
            if (!GameQueries.ObjectTypeHasResource(game, newDef.ObjectType, resDef.Id)) continue;
            target.Resources[resDef.Id] = newDef.Resources.TryGetValue(resDef.Id, out var val)
                ? val : resDef.DefaultValue;
        }

        game.Log.Add($"{oldName} is trained into a {newDef.Name}!");
    }
}
