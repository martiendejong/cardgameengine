import { CardDefinitionDto } from '../types/game';

// Mirrors GameQueries.IsDeckEligible on the server: a card belongs in deck-builder
// pools when it has any play cost — single-resource (playCost, e.g. Peasant) or
// multi-resource (playCosts, e.g. Soldier's gold+training). Filtering on playCost
// alone silently hides every playCosts-priced card (task 972).
//
// playCosts must only be checked for presence, not non-emptiness: The Hive is priced
// entirely via playCostsExtra (sacrifice 5 units, no resource cost at all) and carries
// playCosts: {} for exactly that reason — requiring Object.keys(...).length > 0 here
// silently dropped it from every deck-builder/lobby pool in the game (task 1327 round 2).
export function isDeckEligible(card: CardDefinitionDto): boolean {
  return (card.playCost !== null && card.playCost !== undefined)
    || (card.playCosts !== null && card.playCosts !== undefined);
}
