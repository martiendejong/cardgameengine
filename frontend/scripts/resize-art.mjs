// Generates the scaled art variants the app actually ships.
// Source of truth: full-size PNGs in src/assets (cards/*.png, bg-*.png, card.png, card-back.png).
// Run after adding or changing any source art: npm run art
//
// Outputs (webp, committed):
//   src/assets/cards/small/<name>.webp  360px wide  — card face on the board/hand (renders at 130px)
//   src/assets/cards/large/<name>.webp  800px wide  — card detail modal (renders at 360px)
//   src/assets/frame/card-small.webp / card-large.webp — card front frame, same two contexts
//   src/assets/frame/card-back-small.webp — opponent hand card backs (renders at ~60px)
//   src/assets/bg/<name>.webp — player-area faction backgrounds, recompressed at source resolution
import { readdir, mkdir } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import sharp from 'sharp';

const assets = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../src/assets');

const SMALL_CARD_WIDTH = 360;
const LARGE_CARD_WIDTH = 800;
const FRAME_SMALL_WIDTH = 320;
const FRAME_LARGE_WIDTH = 760;
const CARD_BACK_WIDTH = 160;
const QUALITY = 80;
const BG_QUALITY = 72;

async function emit(srcFile, destFile, width, quality) {
  let img = sharp(srcFile);
  if (width) img = img.resize({ width, withoutEnlargement: true });
  await img.webp({ quality }).toFile(destFile);
}

const cardsDir = path.join(assets, 'cards');
const smallDir = path.join(cardsDir, 'small');
const largeDir = path.join(cardsDir, 'large');
const frameDir = path.join(assets, 'frame');
const bgDir = path.join(assets, 'bg');
await Promise.all([smallDir, largeDir, frameDir, bgDir].map(d => mkdir(d, { recursive: true })));

const jobs = [];

for (const file of await readdir(cardsDir)) {
  if (!file.endsWith('.png')) continue;
  const name = file.replace(/\.png$/, '');
  jobs.push(emit(path.join(cardsDir, file), path.join(smallDir, `${name}.webp`), SMALL_CARD_WIDTH, QUALITY));
  jobs.push(emit(path.join(cardsDir, file), path.join(largeDir, `${name}.webp`), LARGE_CARD_WIDTH, QUALITY));
}

jobs.push(emit(path.join(assets, 'card.png'), path.join(frameDir, 'card-small.webp'), FRAME_SMALL_WIDTH, QUALITY));
jobs.push(emit(path.join(assets, 'card.png'), path.join(frameDir, 'card-large.webp'), FRAME_LARGE_WIDTH, QUALITY));
jobs.push(emit(path.join(assets, 'card-back.png'), path.join(frameDir, 'card-back-small.webp'), CARD_BACK_WIDTH, QUALITY));

for (const file of await readdir(assets)) {
  if (!/^bg-.*\.png$/.test(file)) continue;
  jobs.push(emit(path.join(assets, file), path.join(bgDir, file.replace(/\.png$/, '.webp')), null, BG_QUALITY));
}

await Promise.all(jobs);
console.log(`Generated ${jobs.length} scaled art files.`);
