using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>
/// Derives the currently legal actions for a player from the game definition —
/// the server tells clients what is possible, clients never compute rules.
/// </summary>
public class ActionCatalog
{
    private readonly EngineServices _s;
    private readonly AbilityService _abilities;

    public ActionCatalog(EngineServices services, AbilityService abilities)
    {
        _s = services;
        _abilities = abilities;
    }

    public List<AvailableAction> GetAvailableActions(GameInstance game, string playerId)
    {
        var actions = new List<AvailableAction>();
        if (game.State == GameState.GameEnded) return actions;
        if (game.ActivePlayerId != playerId) return actions;

        var player = game.Players.First(p => p.Id == playerId);

        // Battlefield objects: abilities, attack, move
        var myObjects = game.Objects
            .Where(o => o.ControllerId == playerId && !o.IsDestroyed && o.ZoneId == "battlefield")
            .ToList();

        foreach (var obj in myObjects)
        {
            var cardDef = GameQueries.GetCardDefinition(game, obj);
            if (cardDef == null) continue;

            // Builder work action
            if (obj.Tags.Contains("builder") && game.CurrentPhaseId == "main" && obj.AttachedToId == null)
            {
                var sites = game.Objects
                    .Where(o => o.ControllerId == playerId && !o.IsDestroyed &&
                        o.ZoneId == "battlefield" && o.UnderConstruction)
                    .Select(o => o.Id)
                    .ToList();
                if (sites.Count > 0)
                {
                    string? buildReason = null;
                    if (obj.IsTapped) buildReason = "Tapped";
                    else if (obj.HasSummoningSickness) buildReason = "Cannot work the turn it arrived";
                    else if (obj.HasMovedThisTurn) buildReason = "Already moved this turn";
                    else if (obj.UnderConstruction) buildReason = "Still under construction";

                    actions.Add(new AvailableAction
                    {
                        Type = "build",
                        SourceObjectId = obj.Id,
                        Label = $"{obj.Name}: Build",
                        Available = buildReason == null,
                        UnavailableReason = buildReason,
                        ValidTargets = buildReason == null ? sites : null,
                        RequiresChoice = buildReason == null
                            ? new ChoiceDefinition { Type = "entity", Controller = "self", Min = 1, Max = 1, RequireUnderConstruction = true }
                            : null
                    });
                }
            }

            foreach (var ability in cardDef.Abilities)
            {
                var canUse = _abilities.CanUseAbility(game, obj, ability, player, out string? reason);
                var action = new AvailableAction
                {
                    Type = "activateAbility",
                    SourceObjectId = obj.Id,
                    AbilityId = ability.Id,
                    Label = $"{obj.Name}: {ability.Name}",
                    Available = canUse,
                    UnavailableReason = reason
                };

                if (canUse && ability.Choice != null)
                {
                    var choiceTargets = _s.Targeting.GetValidTargets(game, ability.Choice, playerId);
                    if (choiceTargets.Count == 0)
                    {
                        action.Available = false;
                        action.UnavailableReason = "No valid targets";
                    }
                    else
                    {
                        action.RequiresChoice = ability.Choice;
                        action.ValidTargets = choiceTargets;
                    }
                }

                actions.Add(action);
            }

            bool isCharacter = GameQueries.IsObjectTypeOrSubtype(game, obj.ObjectType, "character");
            bool isAttached = obj.AttachedToId != null;

            // Attack (combat phase, characters only)
            if (game.CurrentPhaseId == "combat" && isCharacter && !isAttached && !obj.IsTapped)
            {
                string? attackReason = null;
                if (obj.HasSummoningSickness) attackReason = "Cannot attack the turn it was summoned";
                else if (obj.HasMovedThisTurn) attackReason = "Already moved this turn";

                var validTargets = attackReason == null
                    ? _s.Targeting.GetAttackTargets(game, obj)
                    : new List<string>();
                if (attackReason == null && validTargets.Count == 0)
                    attackReason = "No targets in reach";

                actions.Add(new AvailableAction
                {
                    Type = "attack",
                    SourceObjectId = obj.Id,
                    Label = $"{obj.Name}: Attack",
                    Available = attackReason == null,
                    UnavailableReason = attackReason,
                    ValidTargets = validTargets,
                    RequiresChoice = attackReason == null
                        ? new ChoiceDefinition { Type = "entity", Controller = "opponent", Min = 1, Max = 1 }
                        : null
                });
            }

            // Move between lines
            if (game.Definition.BattlefieldLines != null && isCharacter && !isAttached &&
                (game.CurrentPhaseId == "main" || game.CurrentPhaseId == "combat"))
            {
                string? moveReason = null;
                if (obj.HasSummoningSickness) moveReason = "Cannot move the turn it was summoned";
                else if (obj.HasMovedThisTurn) moveReason = "Already moved this turn";
                else if (obj.IsTapped) moveReason = "Tapped";

                var targetLine = obj.Line == "front" ? "back" : "front";
                actions.Add(new AvailableAction
                {
                    Type = "move",
                    SourceObjectId = obj.Id,
                    Label = $"{obj.Name}: Move to {(targetLine == "front" ? "Front" : "Back")} Line",
                    Available = moveReason == null,
                    UnavailableReason = moveReason
                });
            }
        }

        // Hand cards: play actions
        var handCards = game.Objects.Where(o => o.OwnerId == playerId && o.ZoneId == "hand" && !o.IsDestroyed);
        foreach (var obj in handCards)
        {
            var cardDef = GameQueries.GetCardDefinition(game, obj);
            if (cardDef == null) continue;

            var costs = GameQueries.EffectivePlayCosts(game, playerId, cardDef);
            var attachSlots = CardPlayService.GetAttachSlots(cardDef);
            bool needsEquipTarget = attachSlots.Count > 0 && cardDef.AttachTo == "chooseCharacter";

            string? reason = null;
            if (game.CurrentPhaseId != "main")
                reason = "Can only play cards during Main Phase";
            else if (costs.Any(kv => player.Resources.GetValueOrDefault(kv.Key) < kv.Value))
                reason = $"Requires {GameQueries.FormatCosts(costs)}";
            else if (attachSlots.Count > 0 && cardDef.AttachTo != "chooseCharacter" &&
                     GameQueries.FindLivingHero(game, playerId) == null)
                reason = "No hero to install this module on";
            else if (cardDef.HousingCost is int housing && !GameQueries.HasHousingFor(game, playerId, housing))
                reason = $"Not enough housing ({GameQueries.HousingUsed(game, playerId)}/{GameQueries.HousingCapacity(game, playerId)})";
            else if (GameQueries.IsObjectTypeOrSubtype(game, obj.ObjectType, "hero") &&
                     GameQueries.FindLivingHero(game, playerId) != null)
                reason = "You already control a Hero";

            var playAction = new AvailableAction
            {
                Type = "playCard",
                SourceObjectId = obj.Id,
                Label = $"{obj.Name}: Play ({GameQueries.FormatCosts(costs)})",
                Available = reason == null,
                UnavailableReason = reason
            };

            if (playAction.Available && needsEquipTarget)
            {
                var equipChoice = new ChoiceDefinition
                {
                    Type = "entity", Controller = "self", ObjectType = "character", Min = 1, Max = 1
                };
                var bearers = _s.Targeting.GetValidTargets(game, equipChoice, playerId);
                if (bearers.Count == 0)
                {
                    playAction.Available = false;
                    playAction.UnavailableReason = "No character to equip";
                }
                else
                {
                    playAction.RequiresChoice = equipChoice;
                    playAction.ValidTargets = bearers;
                }
            }
            else if (playAction.Available && cardDef.OnPlay?.Choice != null)
            {
                var validTargets = _s.Targeting.GetValidTargets(game, cardDef.OnPlay.Choice, playerId);
                if (validTargets.Count == 0)
                {
                    playAction.Available = false;
                    playAction.UnavailableReason = "No valid targets";
                }
                else
                {
                    playAction.RequiresChoice = cardDef.OnPlay.Choice;
                    playAction.ValidTargets = validTargets;
                }
            }

            actions.Add(playAction);
        }

        // End phase
        if (game.State == GameState.WaitingForAction)
        {
            actions.Add(new AvailableAction
            {
                Type = "endPhase",
                Label = $"End {game.Definition.Flow.Phases.First(p => p.Id == game.CurrentPhaseId).Name}",
                Available = true
            });
        }

        return actions;
    }
}
