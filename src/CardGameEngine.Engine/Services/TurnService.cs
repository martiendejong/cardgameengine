using CardGameEngine.Core.Definitions;
using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

/// <summary>Phase flow, per-phase auto actions, drawing, and line movement.</summary>
public class TurnService
{
    private readonly EngineServices _s;

    public TurnService(EngineServices services) => _s = services;

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
                            obj.IsTapped = false;
                            game.Log.Add($"{obj.Name} untapped.");
                        }
                        obj.HasMovedThisTurn = false;
                        obj.HasSummoningSickness = false;
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
}
