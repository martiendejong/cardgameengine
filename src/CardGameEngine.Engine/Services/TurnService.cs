using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>Phase flow, per-phase auto actions, drawing, and line movement.</summary>
public class TurnService
{
    private readonly EngineServices _s;

    public TurnService(EngineServices services)
    {
        _s = services;
        // draw_cards effects route through the bus so effect handlers stay decoupled
        services.Bus.Subscribe((game, evt) =>
        {
            if (evt.Type == "requestDraw" && evt.Player != null)
                DrawCard(game, evt.Player);
        });
    }

    public void EndPhase(GameInstance game, string playerId)
    {
        if (game.State == GameState.GameEnded) return;
        if (game.ActivePlayerId != playerId) return;

        var phases = game.Definition.Flow.Phases;
        var currentIdx = phases.FindIndex(p => p.Id == game.CurrentPhaseId);

        if (currentIdx < phases.Count - 1)
        {
            var nextPhase = phases[currentIdx + 1];
            game.CurrentPhaseId = nextPhase.Id;
            game.Log.Add($"--- {nextPhase.Name} ---");
            _s.Bus.Publish(game, new GameEvent { Type = GameEventTypes.PhaseEntered });
            RunAutoActions(game, nextPhase);
        }
        else
        {
            var currentPlayerIdx = game.Players.FindIndex(p => p.Id == game.ActivePlayerId);
            var nextPlayer = game.Players[(currentPlayerIdx + 1) % game.Players.Count];
            game.ActivePlayerId = nextPlayer.Id;
            game.TurnNumber++;
            game.FiredOncePerTurn.Clear();
            game.ActiveCostModifiers.Clear(); // "this turn" discounts do not carry over

            // Modifiers that last "until the owner's next turn" (e.g. Divine Shield) expire now
            var expiring = game.ActiveModifiers
                .Where(m => m.ExpiresOn == "ownerNextTurnStart" && m.OwnerPlayerId == nextPlayer.Id)
                .ToList();
            foreach (var mod in expiring)
            {
                game.ActiveModifiers.Remove(mod);
                var target = game.Objects.FirstOrDefault(o => o.Id == mod.TargetObjectId);
                if (target != null)
                    game.Log.Add($"Buff on {target.Name} ({mod.PropertyId} +{mod.Amount}) expires.");
            }

            var firstPhase = phases[0];
            game.CurrentPhaseId = firstPhase.Id;
            game.Log.Add($"=== {nextPlayer.Name}'s Turn (Turn {game.TurnNumber}) ===");
            game.Log.Add($"--- {firstPhase.Name} ---");
            _s.Bus.Publish(game, new GameEvent { Type = GameEventTypes.TurnStarted, Player = nextPlayer });
            RunAutoActions(game, firstPhase);
        }

        game.State = GameState.WaitingForAction;
    }

    public void RunAutoActions(GameInstance game, TurnPhaseDefinition phase)
    {
        foreach (var action in phase.AutoActions)
        {
            switch (action)
            {
                case "untap_all":
                    foreach (var obj in game.Objects.Where(o => o.ControllerId == game.ActivePlayerId && !o.IsDestroyed))
                    {
                        if (obj.IsTapped)
                        {
                            if (obj.SkipNextUntap)
                            {
                                obj.SkipNextUntap = false;
                                game.Log.Add($"{obj.Name} is frozen and stays tapped.");
                            }
                            else
                            {
                                obj.IsTapped = false;
                                game.Log.Add($"{obj.Name} untapped.");
                            }
                        }
                        obj.HasMovedThisTurn = false;
                        obj.HasSummoningSickness = false;
                    }
                    break;

                case "tick_lifetimes":
                    foreach (var obj in game.Objects
                        .Where(o => o.ControllerId == game.ActivePlayerId && !o.IsDestroyed &&
                            o.ZoneId == "battlefield" && o.Lifetime.HasValue).ToList())
                    {
                        obj.Lifetime--;
                        if (obj.Lifetime > 0)
                        {
                            game.Log.Add($"{obj.Name} has {obj.Lifetime} turn(s) remaining.");
                            continue;
                        }
                        var def = GameQueries.GetCardDefinition(game, obj);
                        if (def?.ExpiresInto != null)
                        {
                            var newDef = game.Definition.Cards.FirstOrDefault(c => c.Id == def.ExpiresInto);
                            if (newDef != null)
                            {
                                obj.Lifetime = newDef.Duration;
                                _s.Factory.Transform(game, obj, newDef);
                                continue;
                            }
                        }
                        game.Log.Add($"{obj.Name} expires!");
                        _s.Mutator.DestroyObject(game, obj);
                    }
                    break;

                case "tick_poison":
                    foreach (var obj in game.Objects
                        .Where(o => o.ControllerId == game.ActivePlayerId && !o.IsDestroyed &&
                            o.ZoneId == "battlefield" && o.Resources.GetValueOrDefault("poison") > 0).ToList())
                    {
                        var stacks = obj.Resources["poison"];
                        game.Log.Add($"{obj.Name} suffers from poison ({stacks} stack{(stacks > 1 ? "s" : "")}).");
                        _s.Mutator.ApplyDirectDamage(game, obj, stacks);
                        if (!obj.IsDestroyed)
                            obj.Resources["poison"] = stacks - 1;
                    }
                    break;

                case "gain_hero_ap":
                    var apResDef = game.Definition.Resources.FirstOrDefault(r => r.Id == "ap");
                    if (apResDef == null) break;
                    var heroes = game.Objects.Where(o =>
                        o.ControllerId == game.ActivePlayerId && !o.IsDestroyed &&
                        GameQueries.IsObjectTypeOrSubtype(game, o.ObjectType, "hero") &&
                        o.Resources.ContainsKey("ap"));
                    foreach (var hero in heroes)
                    {
                        hero.Resources["ap"] += 1;
                        if (apResDef.MaxValue.HasValue)
                            hero.Resources["ap"] = Math.Min(hero.Resources["ap"], apResDef.MaxValue.Value);
                        game.Log.Add($"{hero.Name} gains 1 AP (now {hero.Resources["ap"]} AP).");
                    }
                    break;

                case "expire_end_of_turn":
                    _s.Mutator.ExpireModifiers(game, "endOfTurn");
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

    public void DrawCard(GameInstance game, PlayerInstance player)
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
        _s.Bus.Publish(game, new GameEvent { Type = GameEventTypes.CardDrawn, Player = player, Target = obj });
    }

    public (bool, string?) ExecuteMove(GameInstance game, string playerId, ActionRequest action)
    {
        if (game.Definition.BattlefieldLines == null)
            return (false, "This game has no battlefield lines");
        if (game.CurrentPhaseId != "main" && game.CurrentPhaseId != "combat")
            return (false, "Can only move during Main or Combat Phase");

        var obj = game.Objects.FirstOrDefault(o => o.Id == action.SourceObjectId);
        if (obj == null) return (false, "Unit not found");
        if (obj.ControllerId != playerId) return (false, "Not your unit");
        if (obj.ZoneId != "battlefield") return (false, "Unit is not on the battlefield");
        if (!GameQueries.IsObjectTypeOrSubtype(game, obj.ObjectType, "character")) return (false, "Only characters can move");
        if (obj.HasSummoningSickness) return (false, "Cannot move the turn it was summoned");
        if (obj.HasMovedThisTurn) return (false, "Already moved this turn");
        if (obj.IsTapped) return (false, "Tapped units cannot move");

        var from = obj.Line == "front" ? "Front" : "Back";
        obj.Line = obj.Line == "front" ? "back" : "front";
        obj.HasMovedThisTurn = true;
        var to = obj.Line == "front" ? "Front" : "Back";
        game.Log.Add($"{obj.Name} moves from the {from} Line to the {to} Line.");
        _s.Bus.Publish(game, new GameEvent { Type = GameEventTypes.ObjectMovedLine, Target = obj });

        return (true, null);
    }

    /// <summary>Builder taps to add 1 construction progress to an unfinished building.</summary>
    public (bool, string?) ExecuteBuild(GameInstance game, string playerId, ActionRequest action)
    {
        if (game.CurrentPhaseId != "main")
            return (false, "Can only build during Main Phase");

        var builder = game.Objects.FirstOrDefault(o => o.Id == action.SourceObjectId);
        if (builder == null) return (false, "Builder not found");
        if (builder.ControllerId != playerId) return (false, "Not your unit");
        if (builder.ZoneId != "battlefield" || builder.IsDestroyed) return (false, "Builder is not on the battlefield");
        if (!builder.Tags.Contains("builder")) return (false, "This unit is not a Builder");
        if (builder.IsTapped) return (false, "Builder is tapped");
        if (builder.HasSummoningSickness) return (false, "Cannot work the turn it arrived");
        if (builder.HasMovedThisTurn) return (false, "Already moved this turn");

        if (action.TargetIds.Count != 1) return (false, "Choose a building under construction");
        var building = game.Objects.FirstOrDefault(o => o.Id == action.TargetIds[0]);
        if (building == null || building.IsDestroyed || building.ZoneId != "battlefield" ||
            building.ControllerId != playerId || !building.UnderConstruction)
            return (false, "Invalid construction target");

        _s.Mutator.Tap(game, builder);
        game.Log.Add($"{builder.Name} works on {building.Name}.");
        _s.Effects.Process(new EffectContext
        {
            Game = game,
            Effect = new EffectDefinition { Type = "add_progress", Scope = "target", Amount = 1 },
            Source = builder,
            Target = building,
            Player = game.Players.First(p => p.Id == playerId)
        });

        return (true, null);
    }
}
