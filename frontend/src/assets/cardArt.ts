// Single source for card artwork lookups. The webp variants are generated
// from the full-size PNGs by `npm run art` (scripts/resize-art.mjs):
// small = card face on the board/hand, large = card detail modal.
const smallModules = import.meta.glob('./cards/small/*.webp', { eager: true, import: 'default' }) as Record<string, string>;
const largeModules = import.meta.glob('./cards/large/*.webp', { eager: true, import: 'default' }) as Record<string, string>;

// Definition ids that reuse another card's art.
const ALIASES: Record<string, string> = {
  'raider-brigand': 'raider',
  'conjurer': 'conjuror',
  'apprentice-mage': 'mage-apprentice',
  'hive-warden': 'hive-guardian',
};

function buildMap(modules: Record<string, string>): Record<string, string> {
  const map: Record<string, string> = {};
  for (const [file, url] of Object.entries(modules)) {
    const name = file.split('/').pop()!.replace(/\.webp$/, '');
    map[name] = url;
  }
  for (const [alias, target] of Object.entries(ALIASES)) {
    if (map[target]) map[alias] = map[target];
  }
  return map;
}

export const CARD_ART_SMALL = buildMap(smallModules);
export const CARD_ART_LARGE = buildMap(largeModules);

export { default as cardFrameSmall } from './frame/card-small.webp';
export { default as cardFrameLarge } from './frame/card-large.webp';
export { default as cardBackSmall } from './frame/card-back-small.webp';
