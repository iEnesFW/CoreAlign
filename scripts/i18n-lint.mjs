#!/usr/bin/env node
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { resolve, dirname, join, extname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');

const APPS = [
  {
    name: 'admin',
    sourceRoot: resolve(repoRoot, 'src'),
    locales: {
      en: resolve(repoRoot, 'src/app/i18n/locales/en.json'),
      tr: resolve(repoRoot, 'src/app/i18n/locales/tr.json'),
    },
    extraLocales: ['ar', 'de', 'ru'].map((l) => ({
      code: l,
      path: resolve(repoRoot, `src/app/i18n/locales/${l}.json`),
    })),
  },
  {
    name: 'customer-portal',
    sourceRoot: resolve(repoRoot, 'apps/customer-portal/src'),
    locales: {
      en: resolve(repoRoot, 'apps/customer-portal/src/app/locales/en.json'),
      tr: resolve(repoRoot, 'apps/customer-portal/src/app/locales/tr.json'),
    },
    extraLocales: ['ar', 'de', 'ru']
      .map((l) => ({
        code: l,
        path: resolve(repoRoot, `apps/customer-portal/src/app/locales/${l}.json`),
      }))
      .filter((l) => safeExists(l.path)),
  },
  {
    name: 'b2b',
    sourceRoot: resolve(repoRoot, 'apps/b2b/src'),
    locales: {
      en: resolve(repoRoot, 'apps/b2b/src/app/locales/en.json'),
      tr: resolve(repoRoot, 'apps/b2b/src/app/locales/tr.json'),
    },
    extraLocales: ['ar', 'de', 'ru']
      .map((l) => ({
        code: l,
        path: resolve(repoRoot, `apps/b2b/src/app/locales/${l}.json`),
      }))
      .filter((l) => safeExists(l.path)),
  },
];

const SKIP_DIRS = new Set(['node_modules', 'dist', 'build', '.vite', '.next', 'coverage']);
const SKIP_FILE_PATTERNS = [/EMCM\.Client\.ts$/];

function safeExists(p) {
  try {
    statSync(p);
    return true;
  } catch {
    return false;
  }
}

function* walk(dir) {
  let entries;
  try {
    entries = readdirSync(dir);
  } catch {
    return;
  }
  for (const entry of entries) {
    if (SKIP_DIRS.has(entry)) continue;
    const full = join(dir, entry);
    let stat;
    try {
      stat = statSync(full);
    } catch {
      continue;
    }
    if (stat.isDirectory()) {
      yield* walk(full);
    } else if (stat.isFile()) {
      const ext = extname(entry);
      if (ext === '.ts' || ext === '.tsx' || ext === '.js' || ext === '.jsx') {
        if (SKIP_FILE_PATTERNS.some((re) => re.test(full))) continue;
        yield full;
      }
    }
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
    const nextKey = prefix ? `${prefix}.${k}` : k;
    if (typeof v === 'object' && v !== null && !Array.isArray(v)) {
      for (const child of flattenKeys(v, nextKey)) out.add(child);
    } else {
      out.add(nextKey);
    }
  }
  return out;
}

function loadLocale(path) {
  try {
    const raw = readFileSync(path, 'utf8');
    return JSON.parse(raw);
  } catch (err) {
    process.stderr.write(`[i18n-lint] cannot read locale ${path}: ${err.message}\n`);
    return {};
  }
}

const KEY_REGEX = /\bt\(\s*['"]([^'"]+)['"]/g;
const KEY_REGEX_BT = /\bt\(\s*`([^`$]+)`/g;

function extractKeys(source) {
  const keys = new Set();
  let m;
  while ((m = KEY_REGEX.exec(source)) !== null) {
    keys.add(m[1]);
  }
  while ((m = KEY_REGEX_BT.exec(source)) !== null) {
    keys.add(m[1]);
  }
  return keys;
}

function lintApp(app) {
  const en = loadLocale(app.locales.en);
  const tr = loadLocale(app.locales.tr);
  const enKeys = flattenKeys(en);
  const trKeys = flattenKeys(tr);

  const usedKeys = new Set();
  for (const file of walk(app.sourceRoot)) {
    const src = readFileSync(file, 'utf8');
    for (const k of extractKeys(src)) usedKeys.add(k);
  }

  const missingInEn = [];
  const missingInTr = [];
  const usedButUndefined = [];
  const definedButUnused = [];

  for (const k of [...enKeys, ...trKeys]) {
    if (!enKeys.has(k)) missingInEn.push(k);
    if (!trKeys.has(k)) missingInTr.push(k);
  }
  for (const k of usedKeys) {
    if (!enKeys.has(k) && !trKeys.has(k)) usedButUndefined.push(k);
  }
  for (const k of enKeys) {
    if (usedKeys.has(k)) continue;
    let prefixHit = false;
    for (const used of usedKeys) {
      if (used.startsWith(`${k}.`) || k.startsWith(`${used}.`)) {
        prefixHit = true;
        break;
      }
    }
    if (!prefixHit) definedButUnused.push(k);
  }

  const extraLocaleParity = app.extraLocales.map((entry) => {
    const data = loadLocale(entry.path);
    const keys = flattenKeys(data);
    const intersect = [...enKeys].filter((k) => keys.has(k));
    const parity = enKeys.size === 0 ? 0 : intersect.length / enKeys.size;
    return { code: entry.code, parity: Math.round(parity * 1000) / 1000, keyCount: keys.size };
  });

  return {
    app: app.name,
    enKeyCount: enKeys.size,
    trKeyCount: trKeys.size,
    usedKeyCount: usedKeys.size,
    missingInEn: [...new Set(missingInEn)].sort(),
    missingInTr: [...new Set(missingInTr)].sort(),
    usedButUndefined: [...new Set(usedButUndefined)].sort(),
    definedButUnused: [...new Set(definedButUnused)].sort(),
    extraLocaleParity,
  };
}

function format(report) {
  const lines = [];
  lines.push(`\n[i18n-lint] ${report.app}`);
  lines.push(`  keys: en=${report.enKeyCount} tr=${report.trKeyCount} used=${report.usedKeyCount}`);
  if (report.extraLocaleParity.length > 0) {
    for (const entry of report.extraLocaleParity) {
      lines.push(
        `  extra: ${entry.code} parity=${(entry.parity * 100).toFixed(1)}% (${entry.keyCount}/${report.enKeyCount})`,
      );
    }
  }
  if (report.missingInEn.length > 0) {
    lines.push(`  missing-in-en (${report.missingInEn.length}):`);
    for (const k of report.missingInEn) lines.push(`    - ${k}`);
  }
  if (report.missingInTr.length > 0) {
    lines.push(`  missing-in-tr (${report.missingInTr.length}):`);
    for (const k of report.missingInTr) lines.push(`    - ${k}`);
  }
  if (report.usedButUndefined.length > 0) {
    lines.push(`  used-but-undefined (${report.usedButUndefined.length}):`);
    for (const k of report.usedButUndefined) lines.push(`    - ${k}`);
  }
  if (report.definedButUnused.length > 0) {
    lines.push(`  defined-but-unused (${report.definedButUnused.length}, informational only):`);
    const preview = report.definedButUnused.slice(0, 20);
    for (const k of preview) lines.push(`    - ${k}`);
    if (report.definedButUnused.length > preview.length) {
      lines.push(`    ... +${report.definedButUnused.length - preview.length} more`);
    }
  }
  return lines.join('\n');
}

const args = new Set(process.argv.slice(2));
const strict = args.has('--strict');

let parityFailed = false;
let usageFailed = false;
let unusedFound = false;
for (const app of APPS) {
  const report = lintApp(app);
  process.stdout.write(format(report) + '\n');
  if (report.missingInEn.length > 0 || report.missingInTr.length > 0) {
    parityFailed = true;
  }
  if (report.usedButUndefined.length > 0) {
    usageFailed = true;
  }
  if (report.definedButUnused.length > 0) {
    unusedFound = true;
  }
}

if (parityFailed) {
  process.stderr.write('\n[i18n-lint] FAILED: en/tr parity broken.\n');
  process.exit(1);
}
if (usageFailed && strict) {
  process.stderr.write('\n[i18n-lint] FAILED (strict): used-but-undefined keys found.\n');
  process.exit(1);
}
if (usageFailed) {
  process.stdout.write(
    '\n[i18n-lint] WARN: used-but-undefined keys found. Run with --strict to fail.\n',
  );
}
if (unusedFound) {
  process.stdout.write(
    '\n[i18n-lint] INFO: defined-but-unused keys found (informational only, never fails).\n',
  );
}
if (!parityFailed && !usageFailed && !unusedFound) {
  process.stdout.write('\n[i18n-lint] OK (no missing or stale keys)\n');
}
process.exit(0);
