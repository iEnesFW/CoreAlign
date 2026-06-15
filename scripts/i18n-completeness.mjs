#!/usr/bin/env node
import { readFileSync, statSync, writeFileSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');

const DEFAULT_THRESHOLD = 0.8;
const EXTRA_LOCALES = ['ar', 'de', 'ru'];

const APPS = [
  {
    name: 'admin',
    enPath: resolve(repoRoot, 'src/app/i18n/locales/en.json'),
    localeDir: resolve(repoRoot, 'src/app/i18n/locales'),
  },
  {
    name: 'customer-portal',
    enPath: resolve(repoRoot, 'apps/customer-portal/src/app/locales/en.json'),
    localeDir: resolve(repoRoot, 'apps/customer-portal/src/app/locales'),
  },
  {
    name: 'b2b',
    enPath: resolve(repoRoot, 'apps/b2b/src/app/locales/en.json'),
    localeDir: resolve(repoRoot, 'apps/b2b/src/app/locales'),
  },
];

function safeExists(p) {
  try {
    statSync(p);
    return true;
  } catch {
    return false;
  }
}

function flattenKeys(value, prefix = '') {
  const out = new Set();
  if (value === null || value === undefined) {
    if (prefix) out.add(prefix);
    return out;
  }
  if (typeof value !== 'object' || Array.isArray(value)) {
    if (prefix) out.add(prefix);
    return out;
  }
  for (const [k, v] of Object.entries(value)) {
    const next = prefix ? `${prefix}.${k}` : k;
    if (typeof v === 'object' && v !== null && !Array.isArray(v)) {
      for (const child of flattenKeys(v, next)) out.add(child);
    } else {
      out.add(next);
    }
  }
  return out;
}

function loadLocale(path) {
  if (!safeExists(path)) return null;
  try {
    return JSON.parse(readFileSync(path, 'utf8'));
  } catch (err) {
    process.stderr.write(`[i18n-completeness] cannot read ${path}: ${err.message}\n`);
    return null;
  }
}

function buildReport(app, threshold) {
  const en = loadLocale(app.enPath);
  if (!en) {
    return { app: app.name, enKeyCount: 0, locales: [], supported: ['en'] };
  }
  const enKeys = flattenKeys(en);
  const trData = loadLocale(resolve(app.localeDir, 'tr.json'));
  const baseSupported = ['en'];
  if (trData) baseSupported.push('tr');

  const locales = [];
  const supported = [...baseSupported];
  for (const code of EXTRA_LOCALES) {
    const path = resolve(app.localeDir, `${code}.json`);
    const data = loadLocale(path);
    if (!data) {
      locales.push({ code, exists: false, parity: 0, keyCount: 0, included: false });
      continue;
    }
    const keys = flattenKeys(data);
    const intersect = [...enKeys].filter((k) => keys.has(k)).length;
    const parity = enKeys.size === 0 ? 0 : intersect / enKeys.size;
    const included = parity >= threshold;
    locales.push({
      code,
      exists: true,
      parity: Math.round(parity * 1000) / 1000,
      keyCount: keys.size,
      included,
    });
    if (included) supported.push(code);
  }
  return { app: app.name, enKeyCount: enKeys.size, locales, supported };
}

const args = process.argv.slice(2);
const thresholdArg = args.find((a) => a.startsWith('--threshold='));
const threshold = thresholdArg ? Number.parseFloat(thresholdArg.split('=')[1]) : DEFAULT_THRESHOLD;
const writeManifest = args.includes('--write-manifest');
const failBelow = args.includes('--strict');

const reports = APPS.map((app) => buildReport(app, threshold));

for (const report of reports) {
  process.stdout.write(`\n[i18n-completeness] ${report.app} (en=${report.enKeyCount} keys)\n`);
  for (const loc of report.locales) {
    if (!loc.exists) {
      process.stdout.write(`  ${loc.code}: missing (skipped)\n`);
      continue;
    }
    const pct = (loc.parity * 100).toFixed(1);
    const tag = loc.included ? 'included' : 'hidden';
    process.stdout.write(
      `  ${loc.code}: ${pct}% (${loc.keyCount}/${report.enKeyCount}) [${tag}]\n`,
    );
  }
  process.stdout.write(`  supported: ${report.supported.join(', ')}\n`);
}

if (writeManifest) {
  const manifest = Object.fromEntries(reports.map((r) => [r.app, r.supported]));
  const out = resolve(repoRoot, 'src/app/i18n/supportedLocales.generated.json');
  writeFileSync(out, JSON.stringify(manifest, null, 2) + '\n', 'utf8');
  process.stdout.write(`\n[i18n-completeness] wrote manifest -> ${out}\n`);
}

let strictFailed = false;
if (failBelow) {
  for (const report of reports) {
    for (const loc of report.locales) {
      if (loc.exists && !loc.included) {
        process.stderr.write(
          `\n[i18n-completeness] FAILED (strict): ${report.app}/${loc.code} parity ${(loc.parity * 100).toFixed(1)}% < ${(threshold * 100).toFixed(0)}%\n`,
        );
        strictFailed = true;
      }
    }
  }
}

process.exit(strictFailed ? 1 : 0);
