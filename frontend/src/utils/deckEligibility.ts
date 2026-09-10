import { CardDefinitionDto } from '../types/game';

// Hero-lineage types have no play cost (they enter via the lobby picker or onPlay effects,
// not from hand) but must still appear in the deck builder pool so players can browse all
// available heroes and add them to their deck selection. All hero subtypes in this game
// carry "hero" in their objectType id (hero, caster-hero, necromancer-hero).
const HERO_OBJECT_TYPES = new Set(['hero', 'caster-hero', 'necromancer-hero']);

// Headquarters-lineage types also have no play cost (they're placed at game setup, not
// played from hand) but a player can carry a spare HQ as a "reserve copy" in their deck
// (task 906) — so they must be deck-eligible too (task 1604).
const HEADQUARTERS_OBJECT_TYPES = new Set([
  'headquarters', 'nexus', 'raider-hq', 'graveyard-hq', 'hive-hq', 'laboratory-hq', 'homestead-hq',
]);

// Mirrors GameQueries.IsDeckEligible on the server: a card belongs in deck-builder
// pools when it has any play cost — single-resource (playCost, e.g. Peasant) or
// multi-resource (playCosts, e.g. Soldier's gold+training). Filtering on playCost
// alone silently hides every playCosts-priced card (task 972).
// Heroes are always eligible regardless of play cost (task 1406 follow-up), and so are
// headquarters (task 1604 follow-up).
export function isDeckEligible(card: CardDefinitionDto): boolean {
  return HERO_OBJECT_TYPES.has(card.objectType)
    || HEADQUARTERS_OBJECT_TYPES.has(card.objectType)
    || (card.playCost !== null && card.playCost !== undefined)
    || !!(card.playCosts && Object.keys(card.playCosts).length > 0);
}
