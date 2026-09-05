using System.Text.Json;
using CardGameEngine.Core.Definitions;
using Xunit;

namespace CardGameEngine.Engine.Tests;

/// <summary>
/// Task 1524: the deck-builder faction filter groups precon decks by an explicit canonical
/// faction id instead of treating every precon deck as its own faction (PR #42's alternative
/// decks and PR #49's meta deck had ballooned the dropdown to 19 buckets). These tests pin
/// the data contract: every deck carries a faction, and the distinct factions are exactly
/// the 9 real ones — so the filter renders 9 options + Unaffiliated.
/// </summary>
public class DeckFactionTests
{
    private static readonly string[] RealFactions =
        { "town", "raiders", "machine", "conclave", "undead", "brood", "shadow", "alchemists", "hunks" };

    private static readonly GameDefinition Definition = LoadDefinition();

    private static GameDefinition LoadDefinition()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CardGameEngine.slnx")))
            dir = dir.Parent;
        if (dir == null)
            throw new InvalidOperationException("Could not locate repo root (CardGameEngine.slnx) from " + AppContext.BaseDirectory);

        var path = Path.Combine(dir.FullName, "definitions", "town-tcg", "game.json");
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<GameDefinition>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize " + path);
    }

    [Fact]
    public void EveryDeck_HasANonEmptyFaction()
    {
        var missing = Definition.Decks.Where(d => string.IsNullOrWhiteSpace(d.Faction)).Select(d => d.Id).ToList();
        Assert.True(missing.Count == 0, "Decks without a faction: " + string.Join(", ", missing));
    }

    [Fact]
    public void DistinctFactions_AreExactlyTheNineRealOnes()
    {
        var distinct = Definition.Decks.Select(d => d.Faction).Distinct().OrderBy(f => f).ToArray();
        Assert.Equal(RealFactions.OrderBy(f => f).ToArray(), distinct);
    }

    [Fact]
    public void DeckIds_MatchTheirFaction()
    {
        // Convention from PR #42: alternative deck ids are "{faction}-{subname}". The one
        // exception is the Bloodfang's Wrath meta deck (PR #49), which is a raiders build.
        foreach (var deck in Definition.Decks)
        {
            if (deck.Id == "bloodfangs-wrath")
            {
                Assert.Equal("raiders", deck.Faction);
                continue;
            }
            Assert.True(
                deck.Id == deck.Faction || deck.Id.StartsWith(deck.Faction + "-", StringComparison.Ordinal),
                $"Deck '{deck.Id}' claims faction '{deck.Faction}' which doesn't match its id prefix");
        }
    }

    [Fact]
    public void EachRealFaction_HasAtLeastTwoDecks()
    {
        // The point of task 1524: base + alternative deck(s) must merge into one bucket.
        foreach (var faction in RealFactions)
        {
            var count = Definition.Decks.Count(d => d.Faction == faction);
            Assert.True(count >= 2, $"Faction '{faction}' has only {count} deck(s)");
        }
    }
}
