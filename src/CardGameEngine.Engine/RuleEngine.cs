using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

public class RuleEngine
{
    private int _objectCounter = 0;
    private int _choiceCounter = 0;
    private int _modifierCounter = 0;

    // -------------------------------------------------------------------------
    // Setup
    // -------------------------------------------------------------------------

    public void ExecuteSetup(GameInstance game)
    {
        // Players with a chosen deck bring their own HQ + hero; otherwise fall back to setup actions
        bool perPlayerSetup = game.Players.All(p => p.HqCardId != null && p.HeroCardId != null);
        if (perPlayerSetup)
        {
            foreach (var player in game.Players)
            {
                PlaceCard(game, player.HqCardId!, "battlefield", player.Id);
                PlaceCard(game, player.HeroCardId!, "battlefield", player.Id);
            }
        }
        else
        {
            foreach (var action in game.Definition.Setup.Actions)
            {
                switch (action.Type)
                {
                    case "place_card":
                        if (action.Scope == "eachPlayer")
                        {
                            foreach (var player in game.Players)
                                PlaceCard(game, action.CardId!, action.Zone!, player.Id);
                        }
                        else
                        {
                            PlaceCard(game, action.CardId!, action.Zone!, game.Players[0].Id);
                        }
                        break;

                    case "set_resource":
                        if (action.Scope == "eachPlayer")
                        {
                            foreach (var player in game.Players)
                                player.Resources[action.ResourceId!] = action.Amount ?? 0;
                        }
                        break;
                }
            }
        }

        // Initialize player resources to defaults
        foreach (var player in game.Players)
        {
            foreach (var resDef in game.Definition.Resources.Where(r => r.Scope == "player"))
            {
                if (!player.Resources.ContainsKey(resDef.Id))
                    player.Resources[resDef.Id] = resDef.DefaultValue;
            }
        }

        // Build and shuffle decks
        foreach (var player in game.Players)
        {
            var order = new List<string>();
            foreach (var cardId in player.DeckList)
            {
                var cardDef = game.Definition.Cards.FirstOrDefault(c => c.Id == cardId);
                if (cardDef == null) continue;
                var obj = CreateObjectInstance(game, cardDef, player.Id, "deck");
                game.Objects.Add(obj);
                order.Add(obj.Id);
            }
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Shared.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            game.DeckOrder[player.Id] = order;
        }

        // Draw starting hands
        var startingHand = game.Definition.DeckRules?.StartingHandSize ?? 0;
        foreach (var player in game.Players)
            for (int i = 0; i < startingHand; i++)
                DrawCard(game, player);

        // Set initial phase
        var firstPhase = game.Definition.Flow.Phases.First();
        game.CurrentPhaseId = firstPhase.Id;
        game.ActivePlayerId = game.Players[0].Id;
        game.State = GameState.WaitingForAction;

        game.Log.Add($"Game started! {game.Players[0].Name} goes first.");

        // Run auto-actions for first phase
        RunAutoActions(game, firstPhase);
    }

    private ObjectInstance PlaceCard(GameInstance game, string cardId, string zoneId, string playerId)
    {
        var cardDef = game.Definition.Cards.First(c => c.Id == cardId);
        var obj = CreateObjectInstance(game, cardDef, playerId, zoneId);
        game.Objects.Add(obj);
        return obj;
    }

    private ObjectInstance CreateObjectInstance(GameInstance game, CardDefinition cardDef, string playerId, string zoneId)
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

        // Copy properties from definition
        foreach (var propDef in game.Definition.Properties)
        {
            if (cardDef.Properties.TryGetValue(propDef.Id, out var val))
                obj.Properties[propDef.Id] = val;
            else
                obj.Properties[propDef.Id] = propDef.DefaultValue;
        }

        // Copy entity resources — only for resources the object type (or its ancestors) declare
        foreach (var resDef in game.Definition.Resources.Where(r => r.Scope == "entity"))
        {
            if (!ObjectTypeHasResource(game, cardDef.ObjectType, resDef.Id)) continue;
            if (cardDef.Resources.TryGetValue(resDef.Id, out var val))
                obj.Resources[resDef.Id] = val;
            else
                obj.Resources[resDef.Id] = resDef.DefaultValue;
        }

        return obj;
    }

    private bool ObjectTypeHasResource(GameInstance game, string objectType, string resourceId)
    {
        var typeDef = game.Definition.ObjectTypes.FirstOrDefault(t => t.Id == objectType);
        while (typeDef != null)
        {
            if (typeDef.Resources.Contains(resourceId)) return true;
            typeDef = game.Definition.ObjectTypes.FirstOrDefault(t => t.Id == typeDef.ParentType);
        }
        return false;
    }

    // -------------------------------------------------------------------------
    // Auto-actions per phase
    // -------------------------------------------------------------------------

    private void RunAutoActions(GameInstance game, TurnPhaseDefinition phase)
    {
        foreach (var action in phase.AutoActions)
        {
            switch (action)
            {
                case "untap_all":
                    UntapAll(game);
                    break;
                case "gain_hero_ap":
                    GainHeroAp(game);
                    break;
                case "expire_end_of_turn":
                    ExpireModifiers(game, "endOfTurn");
                    break;
                case "draw_card":
                    var activePlayer = game.Players.First(p => p.Id == game.ActivePlayerId);
                    var drawCount = game.Definition.DeckRules?.DrawPerTurn ?? 1;
                    for (int i = 0; i < drawCount; i++)
                        DrawCard(game, activePlayer);
                    break;
            }
        }
    }

    private void DrawCard(GameInstance game, PlayerInstance player)
    {
        if (!game.DeckOrder.TryGetValue(player.Id, out var order) || order.Count == 0)
        {
            game.Log.Add($"{player.Name}'s deck is empty — no card drawn.");
            return;
        }
        var objId = order[0];
        order.RemoveAt(0);
        var obj = game.Objects.First(o => o.Id == objId);
        obj.ZoneId = "hand";
        game.Log.Add($"{player.Name} draws a card. ({order.Count} left in deck)");
    }

    private void UntapAll(GameInstance game)
    {
        var myObjects = game.Objects.Where(o => o.ControllerId == game.ActivePlayerId && !o.IsDestroyed);
        foreach (var obj in myObjects)
        {
            if (obj.IsTapped)
            {
                obj.IsTapped = false;
                game.Log.Add($"{obj.Name} untapped.");
            }
            obj.HasMovedThisTurn = false;
            obj.HasSummoningSickness = false;
        }
    }

    private void GainHeroAp(GameInstance game)
    {
        var heroes = game.Objects.Where(o =>
            o.ControllerId == game.ActivePlayerId &&
            !o.IsDestroyed &&
            IsObjectTypeOrSubtype(game, o.ObjectType, "hero"));

        foreach (var hero in heroes)
        {
            var apResDef = game.Definition.Resources.FirstOrDefault(r => r.Id == "ap");
            if (apResDef != null && hero.Resources.ContainsKey("ap"))
            {
                hero.Resources["ap"] += 1;
                if (apResDef.MaxValue.HasValue)
                    hero.Resources["ap"] = Math.Min(hero.Resources["ap"], apResDef.MaxValue.Value);
                game.Log.Add($"{hero.Name} gains 1 AP (now {hero.Resources["ap"]} AP).");
            }
        }
    }

    // -------------------------------------------------------------------------
    // Phase management
    // -------------------------------------------------------------------------

    public void EndPhase(GameInstance game, string playerId)
    {
        if (game.State == GameState.GameEnded) return;
        if (game.ActivePlayerId != playerId) return;

        var phases = game.Definition.Flow.Phases;
        var currentIdx = phases.FindIndex(p => p.Id == game.CurrentPhaseId);

        if (currentIdx < phases.Count - 1)
        {
            // Move to next phase
            var nextPhase = phases[currentIdx + 1];
            game.CurrentPhaseId = nextPhase.Id;
            game.Log.Add($"--- {nextPhase.Name} ---");
            RunAutoActions(game, nextPhase);
        }
        else
        {
            // End of last phase: switch to next player's turn
            var currentPlayerIdx = game.Players.FindIndex(p => p.Id == game.ActivePlayerId);
            var nextPlayerIdx = (currentPlayerIdx + 1) % game.Players.Count;
            game.ActivePlayerId = game.Players[nextPlayerIdx].Id;
            game.TurnNumber++;
            game.FiredOncePerTurn.Clear();

            var firstPhase = phases[0];
            game.CurrentPhaseId = firstPhase.Id;
            game.Log.Add($"=== {game.Players[nextPlayerIdx].Name}'s Turn (Turn {game.TurnNumber}) ===");
            game.Log.Add($"--- {firstPhase.Name} ---");
            RunAutoActions(game, firstPhase);
        }

        game.State = GameState.WaitingForAction;
    }

    // -------------------------------------------------------------------------
    // Available actions
    // -------------------------------------------------------------------------

    public List<AvailableAction> GetAvailableActions(GameInstance game, string playerId)
    {
        var actions = new List<AvailableAction>();

        if (game.State == GameState.GameEnded)
            return actions;

        bool isActivePlayer = game.ActivePlayerId == playerId;
        var player = game.Players.First(p => p.Id == playerId);

        if (!isActivePlayer)
            return actions;

        // Card abilities
        var myObjects = game.Objects.Where(o => o.ControllerId == playerId && !o.IsDestroyed && o.ZoneId == "battlefield");
        foreach (var obj in myObjects)
        {
            var cardDef = game.Definition.Cards.FirstOrDefault(c => c.Id == obj.DefinitionId);
            if (cardDef == null) continue;

            foreach (var ability in cardDef.Abilities)
            {
                var canUse = CanUseAbility(game, obj, ability, player, out string? reason);
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
                    var choiceTargets = GetValidTargets(game, ability.Choice, playerId);
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

            // Attack action: only for characters, in combat phase
            if (game.CurrentPhaseId == "combat" &&
                IsObjectTypeOrSubtype(game, obj.ObjectType, "character") &&
                !obj.IsTapped)
            {
                string? attackReason = null;
                if (obj.HasSummoningSickness) attackReason = "Cannot attack the turn it was summoned";
                else if (obj.HasMovedThisTurn) attackReason = "Already moved this turn";

                var validTargets = attackReason == null ? GetAttackTargets(game, obj) : new List<string>();
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

            // Move action: characters can switch lines once per turn (main or combat phase)
            if (game.Definition.BattlefieldLines != null &&
                IsObjectTypeOrSubtype(game, obj.ObjectType, "character") &&
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
            var cardDef = game.Definition.Cards.FirstOrDefault(c => c.Id == obj.DefinitionId);
            if (cardDef == null) continue;

            var cost = cardDef.PlayCost ?? 0;
            string? reason = null;
            if (game.CurrentPhaseId != "main")
                reason = "Can only play cards during Main Phase";
            else if (player.Resources.GetValueOrDefault("gold") < cost)
                reason = $"Requires {cost} gold";

            var playAction = new AvailableAction
            {
                Type = "playCard",
                SourceObjectId = obj.Id,
                Label = $"{obj.Name}: Play ({cost}g)",
                Available = reason == null,
                UnavailableReason = reason
            };

            if (playAction.Available && cardDef.OnPlay?.Choice != null)
            {
                var validTargets = GetValidTargets(game, cardDef.OnPlay.Choice, playerId);
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

    private bool CanUseAbility(GameInstance game, ObjectInstance obj, AbilityDefinition ability, PlayerInstance player, out string? reason)
    {
        reason = null;

        if (obj.HasMovedThisTurn)
        {
            reason = "Already moved this turn";
            return false;
        }

        // Check costs
        foreach (var cost in ability.Costs)
        {
            if (!CanPayCost(game, cost, obj, player))
            {
                reason = $"Cannot pay cost: {cost.Type}";
                return false;
            }
        }

        // Check conditions
        foreach (var cond in ability.Conditions)
        {
            if (!CheckCondition(game, cond, obj, player))
            {
                reason = $"Condition not met: {cond.Type}";
                return false;
            }
        }

        return true;
    }

    // Line-aware attack reach. With lines disabled, every enemy on the battlefield is in reach.
    // Melee: front line hits enemy front line (or back line if front is empty); back line hits nothing.
    // Ranged: front line hits both enemy lines; back line hits enemy front line only.
    private List<string> GetAttackTargets(GameInstance game, ObjectInstance attacker)
    {
        var enemies = game.Objects
            .Where(o => o.ControllerId != attacker.ControllerId
                && !o.IsDestroyed
                && o.ZoneId == "battlefield")
            .ToList();

        if (game.Definition.BattlefieldLines == null)
            return enemies.Select(o => o.Id).ToList();

        bool ranged = attacker.Tags.Contains("ranged");
        var enemyFront = enemies.Where(o => o.Line == "front").ToList();
        var enemyBack = enemies.Where(o => o.Line != "front").ToList();

        if (attacker.Line == "front")
        {
            if (ranged)
                return enemies.Select(o => o.Id).ToList();
            return (enemyFront.Count > 0 ? enemyFront : enemyBack).Select(o => o.Id).ToList();
        }

        // back line
        if (ranged)
            return enemyFront.Select(o => o.Id).ToList();
        return new List<string>();
    }

    private List<string> GetValidTargets(GameInstance game, ChoiceDefinition choice, string playerId)
    {
        return game.Objects
            .Where(o =>
            {
                if (o.IsDestroyed || o.ZoneId != "battlefield") return false;
                if (choice.Controller == "self" && o.ControllerId != playerId) return false;
                if (choice.Controller == "opponent" && o.ControllerId == playerId) return false;
                if (choice.ObjectType != null && !IsObjectTypeOrSubtype(game, o.ObjectType, choice.ObjectType)) return false;
                if (choice.Tag != null && !o.Tags.Contains(choice.Tag)) return false;
                return true;
            })
            .Select(o => o.Id)
            .ToList();
    }

    // -------------------------------------------------------------------------
    // Execute action
    // -------------------------------------------------------------------------

    public (bool success, string? error) ExecuteAction(GameInstance game, string playerId, ActionRequest action)
    {
        if (game.State == GameState.GameEnded)
            return (false, "Game has ended");
        if (game.ActivePlayerId != playerId)
            return (false, "Not your turn");

        switch (action.Type)
        {
            case "activateAbility":
                return ActivateAbility(game, playerId, action);
            case "attack":
                return ExecuteAttack(game, playerId, action);
            case "move":
                return ExecuteMove(game, playerId, action);
            case "playCard":
                return ExecutePlayCard(game, playerId, action);
            case "endPhase":
                EndPhase(game, playerId);
                return (true, null);
            default:
                return (false, $"Unknown action type: {action.Type}");
        }
    }

    private (bool, string?) ExecutePlayCard(GameInstance game, string playerId, ActionRequest action)
    {
        if (game.CurrentPhaseId != "main")
            return (false, "Can only play cards during Main Phase");

        var player = game.Players.First(p => p.Id == playerId);
        var obj = game.Objects.FirstOrDefault(o => o.Id == action.SourceObjectId);
        if (obj == null) return (false, "Card not found");
        if (obj.OwnerId != playerId) return (false, "Not your card");
        if (obj.ZoneId != "hand") return (false, "Card is not in your hand");

        var cardDef = game.Definition.Cards.FirstOrDefault(c => c.Id == obj.DefinitionId);
        if (cardDef == null) return (false, "Card definition not found");

        var cost = cardDef.PlayCost ?? 0;
        if (player.Resources.GetValueOrDefault("gold") < cost)
            return (false, $"Requires {cost} gold");

        var targetIds = action.TargetIds ?? new List<string>();
        if (cardDef.OnPlay?.Choice != null)
        {
            var choice = cardDef.OnPlay.Choice;
            if (targetIds.Count < choice.Min || targetIds.Count > choice.Max)
                return (false, $"Must select between {choice.Min} and {choice.Max} target(s)");
            var valid = GetValidTargets(game, choice, playerId);
            foreach (var t in targetIds)
                if (!valid.Contains(t))
                    return (false, "Invalid target");
        }

        if (cost > 0)
        {
            player.Resources["gold"] = player.Resources.GetValueOrDefault("gold") - cost;
            game.Log.Add($"{player.Name} spends {cost} gold. (Remaining: {player.Resources["gold"]})");
        }

        if (IsObjectTypeOrSubtype(game, obj.ObjectType, "spell"))
        {
            game.Log.Add($"{player.Name} casts {obj.Name}!");
            if (cardDef.OnPlay != null)
                ApplyAbilityEffects(game, cardDef.OnPlay, obj, player, targetIds);
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
                ApplyAbilityEffects(game, cardDef.OnPlay, obj, player, targetIds);
        }

        CheckEndConditions(game);
        return (true, null);
    }

    private (bool, string?) ActivateAbility(GameInstance game, string playerId, ActionRequest action)
    {
        var player = game.Players.First(p => p.Id == playerId);
        var obj = game.Objects.FirstOrDefault(o => o.Id == action.SourceObjectId);
        if (obj == null) return (false, "Object not found");

        var cardDef = game.Definition.Cards.FirstOrDefault(c => c.Id == obj.DefinitionId);
        if (cardDef == null) return (false, "Card definition not found");

        var ability = cardDef.Abilities.FirstOrDefault(a => a.Id == action.AbilityId);
        if (ability == null) return (false, "Ability not found");

        if (!CanUseAbility(game, obj, ability, player, out string? reason))
            return (false, reason);

        // Validate provided targets before paying any cost
        if (ability.Choice != null && action.TargetIds.Count > 0)
        {
            var valid = GetValidTargets(game, ability.Choice, playerId);
            if (action.TargetIds.Count < ability.Choice.Min || action.TargetIds.Count > ability.Choice.Max)
                return (false, $"Must select between {ability.Choice.Min} and {ability.Choice.Max} target(s)");
            foreach (var t in action.TargetIds)
                if (!valid.Contains(t))
                    return (false, "Invalid target");
        }
        else if (ability.Choice != null && ability.Choice.Min > 0)
        {
            var valid = GetValidTargets(game, ability.Choice, playerId);
            if (valid.Count == 0)
                return (false, "No valid targets");
        }

        // Pay costs
        foreach (var cost in ability.Costs)
            PayCost(game, cost, obj, player);

        game.Log.Add($"{player.Name}: {obj.Name} uses '{ability.Name}'.");

        // Handle choice if ability requires it and targets were provided
        if (ability.Choice != null && action.TargetIds.Count > 0)
        {
            // Targets provided directly in the action
            ApplyAbilityEffects(game, ability, obj, player, action.TargetIds);
        }
        else if (ability.Choice != null)
        {
            // Need to wait for choice
            _choiceCounter++;
            var stackItemId = $"stack_{_choiceCounter}";
            var stackItem = new StackItem
            {
                Id = stackItemId,
                ControllerId = playerId,
                SourceObjectId = obj.Id,
                AbilityId = ability.Id,
                TargetIds = new List<string>()
            };
            game.Stack.Push(stackItem);

            var validOptions = GetValidTargets(game, ability.Choice, playerId);
            var pending = new PendingChoice
            {
                Id = $"choice_{_choiceCounter}",
                PlayerId = playerId,
                StackItemId = stackItemId,
                Definition = ability.Choice,
                ValidOptions = validOptions
            };
            game.PendingChoices.Add(pending);
            game.State = GameState.WaitingForChoice;
        }
        else
        {
            ApplyAbilityEffects(game, ability, obj, player, new List<string>());
        }

        CheckEndConditions(game);
        return (true, null);
    }

    private void ApplyAbilityEffects(GameInstance game, AbilityDefinition ability, ObjectInstance source, PlayerInstance player, List<string> targetIds)
    {
        var targets = targetIds
            .Select(id => game.Objects.FirstOrDefault(o => o.Id == id))
            .Where(o => o != null)
            .Cast<ObjectInstance>()
            .ToList();

        foreach (var effect in ability.Effects)
        {
            if (targets.Count > 0)
            {
                foreach (var target in targets)
                    ProcessEffect(game, effect, source, target, player);
            }
            else
            {
                ProcessEffect(game, effect, source, null, player);
            }
        }
    }

    private (bool, string?) ExecuteAttack(GameInstance game, string playerId, ActionRequest action)
    {
        if (game.CurrentPhaseId != "combat")
            return (false, "Can only attack during combat phase");

        var attacker = game.Objects.FirstOrDefault(o => o.Id == action.SourceObjectId);
        if (attacker == null) return (false, "Attacker not found");
        if (attacker.ControllerId != playerId) return (false, "Not your unit");
        if (attacker.IsTapped) return (false, "Unit is tapped");
        if (!IsObjectTypeOrSubtype(game, attacker.ObjectType, "character")) return (false, "Only characters can attack");

        if (attacker.HasSummoningSickness) return (false, "Cannot attack the turn it was summoned");
        if (attacker.HasMovedThisTurn) return (false, "Already moved this turn");

        if (action.TargetIds.Count == 0) return (false, "No target specified");
        var defender = game.Objects.FirstOrDefault(o => o.Id == action.TargetIds[0]);
        if (defender == null) return (false, "Defender not found");
        if (defender.ControllerId == playerId) return (false, "Cannot attack your own units");
        if (defender.IsDestroyed) return (false, "Target is already destroyed");
        if (!GetAttackTargets(game, attacker).Contains(defender.Id))
            return (false, "Target is out of reach");

        int attackerAttack = GetEffectiveProperty(game, attacker, "attack");
        int defenderAttack = GetEffectiveProperty(game, defender, "attack");
        int attackerArmor = GetEffectiveProperty(game, attacker, "armor");
        int defenderArmor = GetEffectiveProperty(game, defender, "armor");

        // Anti-building bonus (e.g. Pillager)
        var attackerDef = game.Definition.Cards.FirstOrDefault(c => c.Id == attacker.DefinitionId);
        bool defenderIsBuilding = IsObjectTypeOrSubtype(game, defender.ObjectType, "building");
        if (defenderIsBuilding && attackerDef?.BonusAttackVsBuildings is int bonus && bonus > 0)
        {
            attackerAttack += bonus;
            game.Log.Add($"{attacker.Name} gains +{bonus} attack against buildings!");
        }

        int damageToDefender = Math.Max(0, attackerAttack - defenderArmor);

        // Defender only counterattacks when the attacker is within its own reach
        bool defenderCanCounter = GetAttackTargets(game, defender).Contains(attacker.Id);
        int damageToAttacker = defenderCanCounter ? Math.Max(0, defenderAttack - attackerArmor) : 0;

        game.Log.Add($"{attacker.Name} attacks {defender.Name}! ({attackerAttack} ATK vs {defenderArmor} ARM = {damageToDefender} dmg{(defenderCanCounter ? $"; counter: {defenderAttack} ATK vs {attackerArmor} ARM = {damageToAttacker} dmg" : "; no counterattack")})");

        // Apply damage simultaneously
        ApplyDamage(game, attacker, damageToAttacker);
        ApplyDamage(game, defender, damageToDefender);

        // Tap attacker
        attacker.IsTapped = true;

        // Combat triggers
        var attackerController = game.Players.First(p => p.Id == attacker.ControllerId);
        if (defender.IsDestroyed && !attacker.IsDestroyed)
        {
            FireTriggers(game, attacker, "onKill", attackerController);
            if (defenderIsBuilding)
                FireTriggers(game, attacker, "onDestroyBuilding", attackerController);
        }

        bool defenderIsHqOrHero = IsObjectTypeOrSubtype(game, defender.ObjectType, "headquarters")
            || IsObjectTypeOrSubtype(game, defender.ObjectType, "hero");
        if (damageToDefender > 0 && defenderIsHqOrHero)
        {
            var friendlies = game.Objects
                .Where(o => o.ControllerId == attacker.ControllerId && !o.IsDestroyed && o.ZoneId == "battlefield")
                .ToList();
            foreach (var obj in friendlies)
                FireTriggers(game, obj, "onFriendlyDamageHqOrHero", attackerController);
        }

        CheckEndConditions(game);
        return (true, null);
    }

    private void FireTriggers(GameInstance game, ObjectInstance obj, string eventName, PlayerInstance controller)
    {
        var cardDef = game.Definition.Cards.FirstOrDefault(c => c.Id == obj.DefinitionId);
        if (cardDef == null) return;

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
                ProcessEffect(game, effect, obj, null, controller);
        }
    }

    private (bool, string?) ExecuteMove(GameInstance game, string playerId, ActionRequest action)
    {
        if (game.Definition.BattlefieldLines == null)
            return (false, "This game has no battlefield lines");
        if (game.CurrentPhaseId != "main" && game.CurrentPhaseId != "combat")
            return (false, "Can only move during Main or Combat Phase");

        var obj = game.Objects.FirstOrDefault(o => o.Id == action.SourceObjectId);
        if (obj == null) return (false, "Unit not found");
        if (obj.ControllerId != playerId) return (false, "Not your unit");
        if (obj.ZoneId != "battlefield") return (false, "Unit is not on the battlefield");
        if (!IsObjectTypeOrSubtype(game, obj.ObjectType, "character")) return (false, "Only characters can move");
        if (obj.HasSummoningSickness) return (false, "Cannot move the turn it was summoned");
        if (obj.HasMovedThisTurn) return (false, "Already moved this turn");
        if (obj.IsTapped) return (false, "Tapped units cannot move");

        var from = obj.Line == "front" ? "Front" : "Back";
        obj.Line = obj.Line == "front" ? "back" : "front";
        obj.HasMovedThisTurn = true;
        var to = obj.Line == "front" ? "Front" : "Back";
        game.Log.Add($"{obj.Name} moves from the {from} Line to the {to} Line.");

        return (true, null);
    }

    // -------------------------------------------------------------------------
    // Choice resolution
    // -------------------------------------------------------------------------

    public (bool success, string? error) ResolveChoice(GameInstance game, string playerId, string choiceId, List<string> selectedIds)
    {
        var choice = game.PendingChoices.FirstOrDefault(c => c.Id == choiceId);
        if (choice == null) return (false, "Choice not found");
        if (choice.PlayerId != playerId) return (false, "Not your choice");

        // Validate selection count
        if (selectedIds.Count < choice.Definition.Min || selectedIds.Count > choice.Definition.Max)
            return (false, $"Must select between {choice.Definition.Min} and {choice.Definition.Max} targets");

        // Validate each selected target
        foreach (var id in selectedIds)
        {
            if (!choice.ValidOptions.Contains(id))
                return (false, $"Invalid target: {id}");
        }

        game.PendingChoices.Remove(choice);

        // Find the stack item
        var stackItem = game.Stack.Items.FirstOrDefault(s => s.Id == choice.StackItemId);
        if (stackItem != null)
        {
            stackItem.TargetIds = selectedIds;
            game.Stack.Items.Remove(stackItem);

            // Execute the ability now with the chosen targets
            var source = game.Objects.FirstOrDefault(o => o.Id == stackItem.SourceObjectId);
            var player = game.Players.First(p => p.Id == playerId);
            if (source != null)
            {
                var cardDef = game.Definition.Cards.FirstOrDefault(c => c.Id == source.DefinitionId);
                var ability = cardDef?.Abilities.FirstOrDefault(a => a.Id == stackItem.AbilityId);
                if (ability != null)
                    ApplyAbilityEffects(game, ability, source, player, selectedIds);
            }
        }

        if (game.PendingChoices.Count == 0)
            game.State = GameState.WaitingForAction;

        CheckEndConditions(game);
        return (true, null);
    }

    // -------------------------------------------------------------------------
    // Effect processing
    // -------------------------------------------------------------------------

    private void ProcessEffect(GameInstance game, EffectDefinition effect, ObjectInstance? source, ObjectInstance? target, PlayerInstance player)
    {
        switch (effect.Type)
        {
            case "gain_resource":
                ApplyGainResource(game, effect, source, target, player);
                break;
            case "set_resource":
                ApplySetResource(game, effect, source, target, player);
                break;
            case "set_property":
                ApplySetProperty(game, effect, source, target, player);
                break;
            case "modify_property":
                ApplyModifyProperty(game, effect, source, target, player);
                break;
            case "tap":
                ApplyTap(game, effect, source, target, player);
                break;
            case "untap":
                ApplyUntap(game, effect, source, target, player);
                break;
            case "summon":
                ApplySummon(game, effect, source, player);
                break;
            case "destroy":
                ApplyDestroy(game, effect, source, target, player);
                break;
            case "heal":
                ApplyHeal(game, effect, source, target, player);
                break;
            case "damage":
                ApplyDamageEffect(game, effect, source, target, player);
                break;
            case "buff_tag_until_end_of_turn":
                ApplyBuffTagUntilEndOfTurn(game, effect, player);
                break;
            case "buff_target_until_end_of_turn":
                ApplyBuffTargetUntilEndOfTurn(game, effect, source, target);
                break;
            case "transform":
                ApplyTransform(game, effect, target);
                break;
            case "steal_resource":
                ApplyStealResource(game, effect, player);
                break;
            case "plunder_resource":
                ApplyPlunderResource(game, effect, source, player);
                break;
        }
    }

    private void ApplyBuffTargetUntilEndOfTurn(GameInstance game, EffectDefinition effect, ObjectInstance? source, ObjectInstance? target)
    {
        var obj = ResolveScope(effect.Scope ?? "target", source, target);
        if (obj == null) return;

        _modifierCounter++;
        game.ActiveModifiers.Add(new ModifierInstance
        {
            Id = $"mod_{_modifierCounter}",
            TargetObjectId = obj.Id,
            PropertyId = effect.PropertyId ?? "attack",
            Amount = effect.Amount ?? 1,
            ExpiresOn = "endOfTurn"
        });
        game.Log.Add($"{obj.Name} gains +{effect.Amount ?? 1} {effect.PropertyId ?? "attack"} until end of turn.");
    }

    private void ApplyTransform(GameInstance game, EffectDefinition effect, ObjectInstance? target)
    {
        if (target == null || effect.CardId == null) return;
        var newDef = game.Definition.Cards.FirstOrDefault(c => c.Id == effect.CardId);
        if (newDef == null) return;

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
                ? val
                : propDef.DefaultValue;
        }

        target.Resources.Clear();
        foreach (var resDef in game.Definition.Resources.Where(r => r.Scope == "entity"))
        {
            if (!ObjectTypeHasResource(game, newDef.ObjectType, resDef.Id)) continue;
            target.Resources[resDef.Id] = newDef.Resources.TryGetValue(resDef.Id, out var val)
                ? val
                : resDef.DefaultValue;
        }

        game.Log.Add($"{oldName} is trained into a {newDef.Name}!");
    }

    private void ApplyStealResource(GameInstance game, EffectDefinition effect, PlayerInstance player)
    {
        var opponent = game.Players.FirstOrDefault(p => p.Id != player.Id);
        if (opponent == null) return;

        var resId = effect.ResourceId ?? "gold";
        var stolen = Math.Min(effect.Amount ?? 0, opponent.Resources.GetValueOrDefault(resId));
        if (stolen <= 0)
        {
            game.Log.Add($"{player.Name} tries to steal {resId} from {opponent.Name}, but there is nothing to take!");
            return;
        }

        opponent.Resources[resId] = opponent.Resources.GetValueOrDefault(resId) - stolen;
        player.Resources[resId] = player.Resources.GetValueOrDefault(resId) + stolen;
        game.Log.Add($"{player.Name} steals {stolen} {resId} from {opponent.Name}!");
    }

    private void ApplyPlunderResource(GameInstance game, EffectDefinition effect, ObjectInstance? source, PlayerInstance player)
    {
        if (source == null) return;
        var opponent = game.Players.FirstOrDefault(p => p.Id != player.Id);
        if (opponent == null) return;

        var resId = effect.ResourceId ?? "gold";
        var storeId = effect.Tag ?? "loot"; // token resource stored on the source object
        var taken = Math.Min(effect.Amount ?? 0, opponent.Resources.GetValueOrDefault(resId));
        if (taken <= 0)
        {
            game.Log.Add($"{source.Name} finds nothing to plunder from {opponent.Name}.");
            return;
        }

        opponent.Resources[resId] = opponent.Resources.GetValueOrDefault(resId) - taken;
        source.Resources[storeId] = source.Resources.GetValueOrDefault(storeId) + taken;
        game.Log.Add($"{source.Name} plunders {taken} {resId} from {opponent.Name}! ({taken} tokens stored)");
    }

    private void ApplyGainResource(GameInstance game, EffectDefinition effect, ObjectInstance? source, ObjectInstance? target, PlayerInstance player)
    {
        var amount = effect.Amount ?? 0;
        var resId = effect.ResourceId!;

        if (effect.Scope == "player")
        {
            player.Resources[resId] = player.Resources.GetValueOrDefault(resId) + amount;
            game.Log.Add($"{player.Name} gains {amount} {resId}. (Total: {player.Resources[resId]})");
        }
        else if (effect.Scope == "self" && source != null)
        {
            source.Resources[resId] = source.Resources.GetValueOrDefault(resId) + amount;
            game.Log.Add($"{source.Name} gains {amount} {resId}. (Total: {source.Resources[resId]})");
        }
        else if (effect.Scope == "target" && target != null)
        {
            target.Resources[resId] = target.Resources.GetValueOrDefault(resId) + amount;
        }
    }

    private void ApplySetResource(GameInstance game, EffectDefinition effect, ObjectInstance? source, ObjectInstance? target, PlayerInstance player)
    {
        var amount = effect.Amount ?? 0;
        var resId = effect.ResourceId!;

        if (effect.Scope == "player")
            player.Resources[resId] = amount;
        else if (effect.Scope == "self" && source != null)
            source.Resources[resId] = amount;
        else if (effect.Scope == "target" && target != null)
            target.Resources[resId] = amount;
    }

    private void ApplySetProperty(GameInstance game, EffectDefinition effect, ObjectInstance? source, ObjectInstance? target, PlayerInstance player)
    {
        var obj = ResolveScope(effect.Scope, source, target);
        if (obj == null) return;
        obj.Properties[effect.PropertyId!] = effect.Amount ?? 0;
    }

    private void ApplyModifyProperty(GameInstance game, EffectDefinition effect, ObjectInstance? source, ObjectInstance? target, PlayerInstance player)
    {
        var obj = ResolveScope(effect.Scope, source, target);
        if (obj == null) return;
        var propId = effect.PropertyId!;
        obj.Properties[propId] = obj.Properties.GetValueOrDefault(propId) + (effect.Amount ?? 0);
        ClampProperty(game, obj, propId);
    }

    private void ApplyTap(GameInstance game, EffectDefinition effect, ObjectInstance? source, ObjectInstance? target, PlayerInstance player)
    {
        var obj = ResolveScope(effect.Scope, source, target);
        if (obj != null) obj.IsTapped = true;
    }

    private void ApplyUntap(GameInstance game, EffectDefinition effect, ObjectInstance? source, ObjectInstance? target, PlayerInstance player)
    {
        var obj = ResolveScope(effect.Scope, source, target);
        if (obj != null) obj.IsTapped = false;
    }

    private void ApplySummon(GameInstance game, EffectDefinition effect, ObjectInstance? source, PlayerInstance player)
    {
        var cardDef = game.Definition.Cards.FirstOrDefault(c => c.Id == effect.CardId);
        if (cardDef == null) return;

        var obj = CreateObjectInstance(game, cardDef, player.Id, "battlefield");
        obj.HasSummoningSickness = true;
        game.Objects.Add(obj);
        game.Log.Add($"{player.Name} summons {cardDef.Name}!");
    }

    private void ApplyDestroy(GameInstance game, EffectDefinition effect, ObjectInstance? source, ObjectInstance? target, PlayerInstance player)
    {
        var obj = ResolveScope(effect.Scope, source, target);
        if (obj != null) DestroyObject(game, obj);
    }

    private void ApplyHeal(GameInstance game, EffectDefinition effect, ObjectInstance? source, ObjectInstance? target, PlayerInstance player)
    {
        var obj = ResolveScope(effect.Scope, source, target);
        if (obj == null) return;

        var amount = effect.Amount ?? 0;
        var currentHp = obj.Properties.GetValueOrDefault("currentHp");
        var maxHp = obj.Properties.GetValueOrDefault("maxHp");
        var newHp = Math.Min(currentHp + amount, maxHp);
        obj.Properties["currentHp"] = newHp;
        game.Log.Add($"{obj.Name} heals {amount} HP. (HP: {currentHp} → {newHp}/{maxHp})");
    }

    private void ApplyDamageEffect(GameInstance game, EffectDefinition effect, ObjectInstance? source, ObjectInstance? target, PlayerInstance player)
    {
        var obj = ResolveScope(effect.Scope, source, target);
        if (obj == null) return;

        var rawDamage = effect.Amount ?? 0;
        var armor = GetEffectiveProperty(game, obj, "armor");
        var damage = Math.Max(0, rawDamage - armor);
        ApplyDamage(game, obj, damage);
    }

    private void ApplyBuffTagUntilEndOfTurn(GameInstance game, EffectDefinition effect, PlayerInstance player)
    {
        var tag = effect.Tag;
        var propId = effect.PropertyId ?? "attack";
        var amount = effect.Amount ?? 1;

        var taggedObjects = game.Objects.Where(o =>
            o.ControllerId == player.Id &&
            !o.IsDestroyed &&
            o.Tags.Contains(tag!));

        foreach (var obj in taggedObjects)
        {
            _modifierCounter++;
            var modifier = new ModifierInstance
            {
                Id = $"mod_{_modifierCounter}",
                TargetObjectId = obj.Id,
                PropertyId = propId,
                Amount = amount,
                ExpiresOn = "endOfTurn"
            };
            game.ActiveModifiers.Add(modifier);
            game.Log.Add($"{obj.Name} gains +{amount} {propId} until end of turn.");
        }
    }

    private void ApplyDamage(GameInstance game, ObjectInstance obj, int damage)
    {
        if (damage <= 0) return;
        var current = obj.Properties.GetValueOrDefault("currentHp");
        var newHp = Math.Max(0, current - damage);
        obj.Properties["currentHp"] = newHp;
        game.Log.Add($"{obj.Name} takes {damage} damage. (HP: {current} → {newHp})");

        if (newHp <= 0)
            DestroyObject(game, obj);
    }

    private void DestroyObject(GameInstance game, ObjectInstance obj)
    {
        if (obj.IsDestroyed) return;
        obj.IsDestroyed = true;
        obj.ZoneId = "discard";
        game.Log.Add($"{obj.Name} is destroyed!");
    }

    private void ClampProperty(GameInstance game, ObjectInstance obj, string propId)
    {
        var propDef = game.Definition.Properties.FirstOrDefault(p => p.Id == propId);
        if (propDef == null) return;

        var val = obj.Properties.GetValueOrDefault(propId);
        if (propDef.MinValue.HasValue && propDef.ClampToMin)
            val = Math.Max(val, propDef.MinValue.Value);
        if (propDef.MaxValue.HasValue)
            val = Math.Min(val, propDef.MaxValue.Value);
        // If MaxValue is null, max is maxHp for currentHp
        if (propId == "currentHp" && propDef.MaxValue == null)
        {
            var maxHp = obj.Properties.GetValueOrDefault("maxHp");
            val = Math.Min(val, maxHp);
        }
        obj.Properties[propId] = val;
    }

    // -------------------------------------------------------------------------
    // Conditions & costs
    // -------------------------------------------------------------------------

    private bool CheckCondition(GameInstance game, ConditionDefinition cond, ObjectInstance obj, PlayerInstance player)
    {
        return cond.Type switch
        {
            "not_tapped" => !obj.IsTapped,
            "is_tapped" => obj.IsTapped,
            "resource_gte" => GetResourceValue(game, cond, obj, player) >= (cond.Amount ?? 0),
            "resource_lte" => GetResourceValue(game, cond, obj, player) <= (cond.Amount ?? 0),
            "has_tag" => obj.Tags.Contains(cond.Tag ?? ""),
            "is_phase" => game.CurrentPhaseId == cond.Phase,
            _ => true
        };
    }

    private int GetResourceValue(GameInstance game, ConditionDefinition cond, ObjectInstance obj, PlayerInstance player)
    {
        if (cond.Scope == "player")
            return player.Resources.GetValueOrDefault(cond.ResourceId ?? "");
        return obj.Resources.GetValueOrDefault(cond.ResourceId ?? "");
    }

    private bool CanPayCost(GameInstance game, CostDefinition cost, ObjectInstance obj, PlayerInstance player)
    {
        return cost.Type switch
        {
            "tap" => !obj.IsTapped,
            "resource" => GetCostResourceValue(cost, obj, player) >= (cost.Amount ?? 0),
            "sacrifice" => !obj.IsDestroyed,
            _ => true
        };
    }

    private int GetCostResourceValue(CostDefinition cost, ObjectInstance obj, PlayerInstance player)
    {
        if (cost.Scope == "player")
            return player.Resources.GetValueOrDefault(cost.ResourceId ?? "");
        return obj.Resources.GetValueOrDefault(cost.ResourceId ?? "");
    }

    private void PayCost(GameInstance game, CostDefinition cost, ObjectInstance obj, PlayerInstance player)
    {
        switch (cost.Type)
        {
            case "tap":
                obj.IsTapped = true;
                break;
            case "resource":
                var amount = cost.Amount ?? 0;
                if (cost.Scope == "player")
                {
                    player.Resources[cost.ResourceId!] -= amount;
                    game.Log.Add($"{player.Name} spends {amount} {cost.ResourceId}. (Remaining: {player.Resources[cost.ResourceId!]})");
                }
                else // "self" or entity scope
                {
                    obj.Resources[cost.ResourceId!] -= amount;
                    game.Log.Add($"{obj.Name} spends {amount} {cost.ResourceId}. (Remaining: {obj.Resources[cost.ResourceId!]})");
                }
                break;
            case "sacrifice":
                DestroyObject(game, obj);
                break;
        }
    }

    // -------------------------------------------------------------------------
    // End conditions
    // -------------------------------------------------------------------------

    public void CheckEndConditions(GameInstance game)
    {
        if (game.State == GameState.GameEnded) return;

        foreach (var endCond in game.Definition.EndConditions)
        {
            if (endCond.Type == "all_destroyed" && endCond.Scope == "player")
            {
                foreach (var player in game.Players)
                {
                    // Check if all objects of any of the target types belonging to this player are destroyed
                    bool allDestroyed = endCond.Targets.All(targetType =>
                    {
                        var playerObjects = game.Objects.Where(o =>
                            o.OwnerId == player.Id &&
                            IsObjectTypeOrSubtype(game, o.ObjectType, targetType));

                        // If the player has no objects of this type at all, that type is not "all destroyed" in a meaningful sense
                        // But if they had some and all are destroyed, condition is met per type
                        return playerObjects.Any() && playerObjects.All(o => o.IsDestroyed);
                    });

                    if (allDestroyed && endCond.Result == "lose")
                    {
                        player.IsLoser = true;
                        game.Log.Add($"{player.Name} has lost all their {string.Join(" and ", endCond.Targets)}!");
                    }
                }
            }
        }

        // Determine winners
        var losers = game.Players.Where(p => p.IsLoser).ToList();
        if (losers.Count > 0)
        {
            var winners = game.Players.Where(p => !p.IsLoser).ToList();
            foreach (var winner in winners)
            {
                winner.IsWinner = true;
                game.Log.Add($"{winner.Name} wins!");
            }
            game.State = GameState.GameEnded;
        }
    }

    // -------------------------------------------------------------------------
    // Modifiers
    // -------------------------------------------------------------------------

    private void ExpireModifiers(GameInstance game, string eventType)
    {
        var expired = game.ActiveModifiers.Where(m => m.ExpiresOn == eventType).ToList();
        foreach (var mod in expired)
        {
            game.ActiveModifiers.Remove(mod);
            var obj = game.Objects.FirstOrDefault(o => o.Id == mod.TargetObjectId);
            if (obj != null)
                game.Log.Add($"Buff on {obj.Name} ({mod.PropertyId} +{mod.Amount}) expires.");
        }
    }

    public int GetEffectiveProperty(GameInstance game, ObjectInstance obj, string propertyId)
    {
        var base_val = obj.Properties.GetValueOrDefault(propertyId);
        var bonus = game.ActiveModifiers
            .Where(m => m.TargetObjectId == obj.Id && m.PropertyId == propertyId)
            .Sum(m => m.Amount);
        return base_val + bonus;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private ObjectInstance? ResolveScope(string? scope, ObjectInstance? source, ObjectInstance? target)
    {
        return scope switch
        {
            "self" => source,
            "target" => target,
            _ => source
        };
    }

    public bool IsObjectTypeOrSubtype(GameInstance game, string objectType, string targetType)
    {
        if (objectType == targetType) return true;

        // Walk up the parent chain
        var typeDef = game.Definition.ObjectTypes.FirstOrDefault(t => t.Id == objectType);
        while (typeDef?.ParentType != null)
        {
            if (typeDef.ParentType == targetType) return true;
            typeDef = game.Definition.ObjectTypes.FirstOrDefault(t => t.Id == typeDef.ParentType);
        }
        return false;
    }
}
