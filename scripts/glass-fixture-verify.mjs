// Glass 3D designer — dev visual/numerical verification harness.
//
// Loads a deterministic, backend-free fixture scene in the /dev/glass-fixture route and reports the
// AUTHORITATIVE scene geometry from window.__CAD_SCENE__() (exact mm — arc radius/sweep/arcLength,
// panels, shapes). This is the reliable, autonomous "numerical" layer of the verification pyramid.
//
// It ALSO attempts a canvas screenshot. NOTE: capturing the WebGL canvas through Playwright
// automation is unreliable for this app — the R3F renderer emits "THREE.WebGLRenderer: Context
// Lost." under Playwright's browser (independent of headless/headed/GPU/quality), so the PNG is
// usually blank. The /dev/glass-fixture route renders CORRECTLY in a normal browser; open it there
// (backend/auth-free) to eyeball a scene. The scene-data JSON below is the trustworthy signal.
//
// Prereqs: `npm run dev` running on :5273, and `npm run e2e:install` (Playwright chromium).
// Usage:   node scripts/glass-fixture-verify.mjs [sceneKey] [outPng]
//          sceneKey defaults to arc-holefill-triangle; fixtures: src/features/glass-enclosure/dev/fixtures.ts

import { chromium } from '@playwright/test';
import { writeFileSync } from 'node:fs';

const sceneKey = process.argv[2] ?? 'arc-holefill-triangle';
const outPng = process.argv[3] ?? null;
const url = `http://localhost:5273/dev/glass-fixture?scene=${encodeURIComponent(sceneKey)}`;

const browser = await chromium.launch({
  args: ['--ignore-gpu-blocklist', '--enable-unsafe-swiftshader'],
});
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
let contextLost = false;
page.on('console', (m) => {
  if (m.text().includes('Context Lost')) contextLost = true;
});
await page.addInitScript(() => {
  window.__E2E__ = true;
});
await page.goto(url, { waitUntil: 'load', timeout: 30000 }).catch((e) => console.error('goto:', e.message));
await page.waitForSelector('canvas', { timeout: 20000 }).catch(() => console.error('no canvas'));
await page.waitForTimeout(3500);

const scene = await page.evaluate(() => (window.__CAD_SCENE__ ? window.__CAD_SCENE__() : null));
console.log('=== __CAD_SCENE__ (' + sceneKey + ') ===');
console.log(JSON.stringify(scene, null, 2));

if (outPng) {
  const dataUrl = await page.evaluate(() => {
    const c = [...document.querySelectorAll('canvas')].sort((a, b) => b.width * b.height - a.width * a.height)[0];
    return c ? c.toDataURL('image/png') : null;
  });
  if (dataUrl) {
    writeFileSync(outPng, Buffer.from(dataUrl.split(',')[1], 'base64'));
    console.log('screenshot ->', outPng, contextLost ? '(WARNING: WebGL context lost — likely blank)' : '');
  }
}

await browser.close();
