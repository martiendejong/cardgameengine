namespace CardGameEngine.Core.Definitions;

public class GameDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public List<ObjectTypeDefinition> ObjectTypes { get; set; } = new();
    public List<PropertyDefinition> Properties { get; set; } = new();
    public List<ResourceDefinition> Resources { get; set; } = new();
    public List<TagDefinition> Tags { get; set; } = new();
    public List<ZoneDefinition> Zones { get; set; } = new();
    public FlowDefinition Flow { get; set; } = new();
    public List<CardDefinition> Cards { get; set; } = new();
    public SetupDefinition Setup { get; set; } = new();
    public List<EndConditionDefinition> EndConditions { get; set; } = new();
    public ResolutionPolicyDefinition ResolutionPolicy { get; set; } = new();
    public DeckRulesDefinition? DeckRules { get; set; }
    public BattlefieldLinesDefinition? BattlefieldLines { get; set; }
    public List<PreconDeckDefinition> Decks { get; set; } = new();
    // Equipment slots for characters that don't declare their own (mainHand/offHand/body/head)
    public Dictionary<string, int>? DefaultEquipmentSlots { get; set; }
}

public class PreconDeckDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Hq { get; set; } = "";   // default headquarters card id
    public string Hero { get; set; } = ""; // default hero card id
    public List<string> HqOptions { get; set; } = new();   // selectable HQs for this faction
    public List<string> HeroOptions { get; set; } = new(); // selectable starting heroes
    public Dictionary<string, int> Cards { get; set; } = new();
}

public class BattlefieldLinesDefinition
{
    public List<string> Lines { get; set; } = new(); // e.g. ["front", "back"]
    public string SpawnLine { get; set; } = "back";  // where new cards enter the battlefield
}

public class DeckRulesDefinition
{
    public int MaxCopies { get; set; } = 2;
    public int MaxDeckSize { get; set; } = 15;
    // Multiplayer unlocks once a profile's collection can field a deck this big
    public int MinDeckSize { get; set; } = 40;
    public int StartingHandSize { get; set; } = 3;
    public int DrawPerTurn { get; set; } = 1;
    public Dictionary<string, int> DefaultDeck { get; set; } = new();
}

public class ObjectTypeDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? ParentType { get; set; }
    public List<string> Properties { get; set; } = new();
    public List<string> Resources { get; set; } = new();
}

public class PropertyDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "int"; // "int", "bool"
    public int DefaultValue { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; } // null = maxHP
    public bool ClampToMin { get; set; } = true;
}

public class ResourceDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Scope { get; set; } = "player"; // "player" or "entity"
    public int DefaultValue { get; set; }
    public int? MaxValue { get; set; }
}

public class TagDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public class ZoneDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Visibility { get; set; } = "all"; // "owner", "all", "none"
    public bool Ordered { get; set; }
}

public class FlowDefinition
{
    public List<TurnPhaseDefinition> Phases { get; set; } = new();
}

public class TurnPhaseDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string> AutoActions { get; set; } = new(); // auto-executed rule ids
}

public class CardDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string ObjectType { get; set; } = "";
    public Dictionary<string, int> Properties { get; set; } = new();
    public Dictionary<string, int> Resources { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<AbilityDefinition> Abilities { get; set; } = new();
    public PlayTimingDefinition? PlayTiming { get; set; }
    public string? ArtworkDescription { get; set; }
    public string? Icon { get; set; } // emoji shown in the card's art area
    public int? PlayCost { get; set; } // cost to play from hand; null = not deck-eligible
    public string PlayCostResource { get; set; } = "gold"; // which player resource pays the play cost
    public Dictionary<string, int>? PlayCosts { get; set; } // multi-resource play cost (e.g. gold + training); overrides PlayCost
    public string? DeckLimit { get; set; } // "unlimited" or a number as string; null = deck rule default
    public int? ConstructionRequirement { get; set; } // buildings enter play under construction until this much progress
    public List<string>? Slots { get; set; } // multi-slot equipment (e.g. two-handed: mainHand+offHand); overrides Slot
    public string AttachTo { get; set; } = "hero"; // "hero" (auto-attach, robot modules) or "chooseCharacter" (equipment)
    public List<string> AttachTags { get; set; } = new(); // tags granted to the bearer while attached (e.g. longbow -> ranged)
    public int? Duration { get; set; } // lifetime in own turns; at 0 the card expires (destroyed or transforms)
    public string? ExpiresInto { get; set; } // card id this transforms into when Duration runs out (Cocoon -> Behemoth)
    public Dictionary<string, int>? ResourceCapacities { get; set; } // per-card entity resource caps (mana 6, corpses 8, ...)
    public string Timing { get; set; } = "main"; // "main", "reaction" (instant), or "both"
    public List<string> ReactionTo { get; set; } = new(); // events this reaction answers: "attackDeclared", "spellCast"
    public bool IsSecret { get; set; } // played face-down; auto-reveals when its trigger event occurs
    public AbilityDefinition? OnPlay { get; set; } // resolved when the card is played from hand
    public List<TriggerDefinition> Triggers { get; set; } = new();
    public int? BonusAttackVsBuildings { get; set; } // extra attack when the target is a building
    public string? Slot { get; set; } // module slot ("arm", "core", ...); modules attach to your hero
    public List<AttachModifierDefinition> AttachModifiers { get; set; } = new(); // stat bonuses while attached
    public Dictionary<string, int>? EquipmentSlots { get; set; } // on heroes: slot id -> capacity
    public int? HousingCost { get; set; }     // living space this unit occupies while on the battlefield
    public int? HousingProvided { get; set; } // living space this card supplies while on the battlefield
}

public class AttachModifierDefinition
{
    public string PropertyId { get; set; } = "";
    public int Amount { get; set; }
}

public class TriggerDefinition
{
    // "onKill"                    - this card destroyed an enemy in combat
    // "onDestroyBuilding"         - this card destroyed an enemy building in combat
    // "onFriendlyDamageHqOrHero"  - any friendly card damaged an enemy hero or HQ in combat
    // "onTurnStart"               - at the start of its controller's turn
    public string Event { get; set; } = "";
    public bool OncePerTurn { get; set; }
    public List<ConditionDefinition> Conditions { get; set; } = new();
    public List<EffectDefinition> Effects { get; set; } = new();
}

public class AbilityDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<CostDefinition> Costs { get; set; } = new();
    public List<ConditionDefinition> Conditions { get; set; } = new();
    public ChoiceDefinition? Choice { get; set; }
    public List<EffectDefinition> Effects { get; set; } = new();
    public int? UsesPerTurn { get; set; } // cap without tapping (Settlement: 3 workers per turn)
}

public class CostDefinition
{
    public string Type { get; set; } = ""; // "tap", "resource", "sacrifice"
    public string? ResourceId { get; set; }
    public int? Amount { get; set; }
    public string? Scope { get; set; } // "self", "player"
}

public class ConditionDefinition
{
    public string Type { get; set; } = ""; // "not_tapped", "resource_gte", "has_tag", "is_phase"
    public string? ResourceId { get; set; }
    public int? Amount { get; set; }
    public string? Tag { get; set; }
    public string? Phase { get; set; }
    public string? Scope { get; set; }
}

public class ChoiceDefinition
{
    public string Type { get; set; } = "entity"; // "entity"
    public string Controller { get; set; } = "any"; // "self", "opponent", "any"
    public string? ObjectType { get; set; }
    public string? Tag { get; set; }
    public int Min { get; set; } = 1;
    public int Max { get; set; } = 1;
    public bool RequireUntapped { get; set; } // e.g. harvest needs an untapped Worker
    public bool RequireUnderConstruction { get; set; } // e.g. builders target unfinished buildings
    public string? RequireResourceId { get; set; } // target must hold at least RequireResourceAmount of this
    public int RequireResourceAmount { get; set; }
    public bool ExcludeSelf { get; set; } // target may not be the ability's source (sacrifice another unit)
    public bool AttachmentsOnly { get; set; } // target equipment/modules installed on cards (Dissolve)
}

public class EffectDefinition
{
    public string Type { get; set; } = "";
    // gain_resource, set_resource, set_property, modify_property, tap, untap,
    // summon, destroy, heal, damage, buff_tag_until_end_of_turn
    public string? Scope { get; set; } // "self", "player", "target", "controller", "all_tagged"
    public string? ResourceId { get; set; }
    public string? PropertyId { get; set; }
    public int? Amount { get; set; }
    public string? CardId { get; set; } // for summon
    public string? Tag { get; set; }
    public string? UntilEvent { get; set; }
}

public class PlayTimingDefinition
{
    public List<string> AllowedDuring { get; set; } = new(); // "ownTurn", "anyTurn", "reaction"
    public List<string> ReactionTo { get; set; } = new(); // event types
}

public class SetupDefinition
{
    public List<SetupActionDefinition> Actions { get; set; } = new();
}

public class SetupActionDefinition
{
    public string Type { get; set; } = ""; // "place_card", "set_resource"
    public string? CardId { get; set; }
    public string? Zone { get; set; }
    public string? Scope { get; set; } // "eachPlayer"
    public string? ResourceId { get; set; }
    public int? Amount { get; set; }
}

public class EndConditionDefinition
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = ""; // "all_destroyed"
    public List<string> Targets { get; set; } = new(); // object types e.g. ["headquarters", "hero"]
    public string Result { get; set; } = "lose"; // "lose"
    public string Scope { get; set; } = "player"; // "player"
}

public class ResolutionPolicyDefinition
{
    public string Type { get; set; } = "stack"; // "stack", "immediate"
    public string? Order { get; set; } = "LIFO";
}
