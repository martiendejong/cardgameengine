import { CardDefinitionDto } from '../types/game';

// Hero-lineage types have no play cost (they enter via the lobby picker or onPlay effects,
// not from hand) but must still appear in the deck builder pool so players can browse all
// available heroes and add them to their deck selection. All hero subtypes in this game
// carry "hero" in their objectType id (hero, caster-hero, necromancer-hero).
const HERO_OBJECT_TYPES = new Set(['hero', 'caster-hero', 'necromancer-hero']);

// Mirrors GameQueries.IsDeckEligible on the server: a card belongs in deck-builder
// pools when it has any play cost — single-resource (playCost, e.g. Peasant) or
// multi-resource (playCosts, e.g. Soldier's gold+training). Filtering on playCost
// alone silently hides every playCosts-priced card (task 972).
// Heroes are always eligible regardless of play cost (task 1406 follow-up).
export function isDeckEligible(card: CardDefinitionDto): boolean {
  return HERO_OBJECT_TYPES.has(card.objectType)
    || (card.playCost !== null && card.playCost !== undefined)
    || !!(card.playCosts && Object.keys(card.playCosts).length > 0);
}
