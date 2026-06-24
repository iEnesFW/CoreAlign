import { Resvg } from '@resvg/resvg-js';
import { readFileSync, writeFileSync } from 'node:fs';

const svg = readFileSync('public/og-image.svg', 'utf8');
const resvg = new Resvg(svg, {
  fitTo: { mode: 'width', value: 1200 },
  font: { loadSystemFonts: true, defaultFontFamily: 'Arial' },
  background: '#1e1b4b',
});
const png = resvg.render().asPng();
writeFileSync('public/og-image.png', png);
console.log(`public/og-image.png written: ${(png.length / 1024).toFixed(1)} KB`);
