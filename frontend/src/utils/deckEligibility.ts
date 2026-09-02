import { CardDefinitionDto } from '../types/game';

// Mirrors GameQueries.IsDeckEligible on the server: a card belongs in deck-builder
// pools when it has any play cost — single-resource (playCost, e.g. Peasant) or
// multi-resource (playCosts, e.g. Soldier's gold+training). Filtering on playCost
// alone silently hides every playCosts-priced card (task 972).
export function isDeckEligible(card: CardDefinitionDto): boolean {
  return (card.playCost !== null && card.playCost !== undefined)
    || !!(card.playCosts && Object.keys(card.playCosts).length > 0);
}
