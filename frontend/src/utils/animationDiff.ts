import { GameStateDto } from '../types/game';

/**
 * How long a triggered card animation (summon/attack/hit) plays for, in ms.
 * Shared by GameBoard (clears the CSS animation class after this long) and
 * GamePage (pauses user input for this long so a follow-up click can't cut
 * the animation short or race ahead of what's shown).
 */
export const ANIMATION_PAUSE_MS = 1500;

/**
 * Diffs two consecutive game states and returns the animation class each
 * newly-affected battlefield object should get (summon / hit / attack).
 */
export function computeCardAnimations(
  prev: GameStateDto | null,
  curr: GameStateDto
): Record<string, string> {
  const anims: Record<string, string> = {};
  if (!prev) return anims;

  const prevMap = new Map(prev.objects.map(o => [o.id, o]));

  for (const obj of curr.objects) {
    if (obj.isDestroyed || obj.zoneId !== 'battlefield') continue;
    const p = prevMap.get(obj.id);

    if (!p) {
      anims[obj.id] = 'anim-summon';
    } else {
      const prevHp = p.properties['currentHp'] ?? 0;
      const currHp = obj.properties['currentHp'] ?? 0;
      if (currHp < prevHp) {
        anims[obj.id] = 'anim-hit';
      } else if (!p.isTapped && obj.isTapped) {
        anims[obj.id] = 'anim-attack';
      }
    }
  }

  return anims;
}

/** True if the new state has any change worth animating (and thus worth pausing input for). */
export function hasAnimatableChange(prev: GameStateDto | null, curr: GameStateDto): boolean {
  return Object.keys(computeCardAnimations(prev, curr)).length > 0;
}
