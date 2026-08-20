using CardGameEngine.Core.Runtime;

namespace CardGameEngine.Engine;

public class SetupService
{
    private readonly EngineServices _s;
    private readonly TurnService _turns;

    public SetupService(EngineServices services, TurnService turns)
    {
        _s = services;
        _turns = turns;
    }

    public void ExecuteSetup(GameInstance game)
    {
        // Players with chosen starting cards bring them; heroes (and even HQs, for
        // scripted encounter enemies) are optional. Legacy setup actions only run
        // when nobody declares anything.
        bool perPlayerSetup = game.Players.Any(p => p.HqCardId != null || p.HeroCardId != null);
        if (perPlayerSetup)
        {
            foreach (var player in game.Players)
            {
                if (player.HqCardId != null) PlaceStartingCard(game, player.HqCardId, player);
                if (player.HeroCardId != null) PlaceStartingCard(game, player.HeroCardId, player);
            }
        }
        else
        {
            foreach (var action in game.Definition.Setup.Actions)
            {
                switch (action.Type)
                {
                    case "place_card":
                        var targets = action.Scope == "eachPlayer" ? game.Players : game.Players.Take(1);
                        foreach (var player in targets)
                            _s.Factory.PlaceCard(game, action.CardId!, action.Zone!, player.Id);
                        break;

                    case "set_resource":
                        if (action.Scope == "eachPlayer")
                            foreach (var player in game.Players)
                                player.Resources[action.ResourceId!] = action.Amount ?? 0;
                        break;
                }
            }
        }

        // Initialize player resources to defaults
        foreach (var player in game.Players)
            foreach (var resDef in game.Definition.Resources.Where(r => r.Scope == "player"))
                if (!player.Resources.ContainsKey(resDef.Id))
                    player.Resources[resDef.Id] = resDef.DefaultValue;

        // Build and shuffle decks
        foreach (var player in game.Players)
        {
            var order = new List<string>();
            foreach (var cardId in player.DeckList)
            {
                var cardDef = game.Definition.Cards.FirstOrDefault(c => c.Id == cardId);
                if (cardDef == null) continue;
                var obj = _s.Factory.CreateObjectInstance(game, cardDef, player.Id, "deck");
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
                _turns.DrawCard(game, player);

        // Set initial phase
        var firstPhase = game.Definition.Flow.Phases.First();
        game.CurrentPhaseId = firstPhase.Id;
        game.ActivePlayerId = game.Players[0].Id;
        game.State = GameState.WaitingForAction;

        game.Log.Add($"Game started! {game.Players[0].Name} goes first.");
        _s.Bus.Publish(game, new GameEvent { Type = GameEventTypes.TurnStarted, Player = game.Players[0] });

        _turns.RunAutoActions(game, firstPhase);
    }

    /// <summary>Starting HQ/hero cards fire their onPlay when placed (e.g. the Hive hatches a Larva).</summary>
    private void PlaceStartingCard(GameInstance game, string cardId, Core.Runtime.PlayerInstance player)
    {
        var obj = _s.Factory.PlaceCard(game, cardId, "battlefield", player.Id);
        var cardDef = GameQueries.GetCardDefinition(game, obj);
        if (cardDef?.OnPlay != null && cardDef.OnPlay.Choice == null)
            _s.Effects.ApplyAbility(game, cardDef.OnPlay, obj, player, new List<string>());
    }
}
