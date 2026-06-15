#!/usr/bin/env node
import { readdirSync, statSync, existsSync } from 'node:fs';
import { join, resolve, basename } from 'node:path';
import { argv, exit, env, cwd } from 'node:process';

const KB = 1024;
const DEFAULTS = {
  vendor3dMaxKb: 1500,
  mainMaxKb: 800,
  chunkMaxKb: 600,
};

function parseArgs(args) {
  const opts = {
    dist: null,
    vendor3dMaxKb: DEFAULTS.vendor3dMaxKb,
    mainMaxKb: DEFAULTS.mainMaxKb,
    chunkMaxKb: DEFAULTS.chunkMaxKb,
    allowChunkPrefixes: [],
  };
  for (let i = 0; i < args.length; i += 1) {
    const a = args[i];
    if (a === '--dist' || a === '-d') {
      opts.dist = args[i + 1];
      i += 1;
    } else if (a === '--vendor-3d-max-kb') {
      opts.vendor3dMaxKb = Number(args[i + 1]);
      i += 1;
    } else if (a === '--main-max-kb') {
      opts.mainMaxKb = Number(args[i + 1]);
      i += 1;
    } else if (a === '--chunk-max-kb') {
      opts.chunkMaxKb = Number(args[i + 1]);
      i += 1;
    } else if (a === '--allow-chunk') {
      opts.allowChunkPrefixes.push(String(args[i + 1] ?? ''));
      i += 1;
    } else if (a === '--help' || a === '-h') {
      printHelp();
      exit(0);
    }
  }
  return opts;
}

function printHelp() {
  console.log(`Usage: node scripts/check-bundle-size.mjs [options]

Options:
  -d, --dist <dir>           Path to a built SPA root containing assets/
                             (default: ./dist).
  --vendor-3d-max-kb <n>     Max size for vendor-3d chunk (default ${DEFAULTS.vendor3dMaxKb}).
  --main-max-kb <n>          Max size for main entry chunk (default ${DEFAULTS.mainMaxKb}).
  --chunk-max-kb <n>         Max size for any other non-vendor chunk (default ${DEFAULTS.chunkMaxKb}).
  --allow-chunk <prefix>     Exempt chunks whose hashless name starts with this
                             prefix from the budget. Repeatable. Use for
                             documented known-large chunks awaiting split.
                             (e.g. --allow-chunk AddressRegionFields)
  -h, --help                 Show this help text.

Environment overrides:
  BUNDLE_DIST                Same as --dist.
  BUNDLE_VENDOR_3D_MAX_KB    Same as --vendor-3d-max-kb.
  BUNDLE_MAIN_MAX_KB         Same as --main-max-kb.
  BUNDLE_CHUNK_MAX_KB        Same as --chunk-max-kb.
  BUNDLE_ALLOW_CHUNKS        Comma-separated chunk-name prefixes to exempt.

The script scans <dist>/assets/*.js and asserts:
  - vendor-3d-*.js          <= vendor-3d-max-kb
  - index-*.js              <= main-max-kb
  - everything else (non-vendor) <= chunk-max-kb
  - vendor-*-*.js other than vendor-3d uses chunk-max-kb as a soft cap.

Exit codes:
  0  All chunks within budget.
  1  At least one chunk exceeds its budget.
  2  Misconfiguration (no dist found, bad args).
`);
}

function classify(name) {
  if (name.startsWith('vendor-3d')) return 'vendor-3d';
  if (name.startsWith('index')) return 'main';
  if (name.startsWith('vendor-')) return 'vendor-other';
  return 'chunk';
}

function loadEnvOverrides(opts) {
  if (env.BUNDLE_DIST) opts.dist = env.BUNDLE_DIST;
  if (env.BUNDLE_VENDOR_3D_MAX_KB) opts.vendor3dMaxKb = Number(env.BUNDLE_VENDOR_3D_MAX_KB);
  if (env.BUNDLE_MAIN_MAX_KB) opts.mainMaxKb = Number(env.BUNDLE_MAIN_MAX_KB);
  if (env.BUNDLE_CHUNK_MAX_KB) opts.chunkMaxKb = Number(env.BUNDLE_CHUNK_MAX_KB);
  if (env.BUNDLE_ALLOW_CHUNKS) {
    const extra = env.BUNDLE_ALLOW_CHUNKS.split(',').map((s) => s.trim()).filter(Boolean);
    opts.allowChunkPrefixes = [...opts.allowChunkPrefixes, ...extra];
  }
  return opts;
}

function isExempt(fileName, allowChunkPrefixes) {
  if (!allowChunkPrefixes.length) return false;
  return allowChunkPrefixes.some((prefix) => fileName.startsWith(prefix));
}

function gatherJsAssets(distRoot) {
  const assets = join(distRoot, 'assets');
  if (!existsSync(assets)) {
    return { assets, files: null };
  }
  const files = readdirSync(assets)
    .filter((f) => f.endsWith('.js'))
    .map((f) => ({ name: f, path: join(assets, f), size: statSync(join(assets, f)).size }));
  return { assets, files };
}

function budgetFor(category, opts) {
  if (category === 'vendor-3d') return opts.vendor3dMaxKb * KB;
  if (category === 'main') return opts.mainMaxKb * KB;
  return opts.chunkMaxKb * KB;
}

function formatKb(bytes) {
  return `${(bytes / KB).toFixed(1)} KB`;
}

function main() {
  let opts = parseArgs(argv.slice(2));
  opts = loadEnvOverrides(opts);
  const distRoot = resolve(opts.dist ?? join(cwd(), 'dist'));

  if (![opts.vendor3dMaxKb, opts.mainMaxKb, opts.chunkMaxKb].every((n) => Number.isFinite(n) && n > 0)) {
    console.error('error: budget values must be positive numbers');
    exit(2);
  }

  const { assets, files } = gatherJsAssets(distRoot);
  if (files === null) {
    console.error(`error: no assets directory under ${distRoot}. Did the build run?`);
    exit(2);
  }
  if (files.length === 0) {
    console.error(`error: no .js assets found in ${assets}`);
    exit(2);
  }

  const failures = [];
  const summary = [];
  const exemptions = [];
  for (const file of files) {
    const category = classify(file.name);
    const budget = budgetFor(category, opts);
    const exempt = isExempt(file.name, opts.allowChunkPrefixes);
    let status;
    if (file.size <= budget) {
      status = 'OK';
    } else if (exempt) {
      status = 'WARN';
      exemptions.push({ file: file.name, category, size: file.size, budget });
    } else {
      status = 'FAIL';
      failures.push({ file: file.name, category, size: file.size, budget });
    }
    summary.push({ file: file.name, category, size: file.size, budget, status });
  }

  summary.sort((a, b) => b.size - a.size);
  const label = basename(distRoot.replace(/[\\/]+dist$/i, '')) || distRoot;
  console.log(`bundle-size report: ${label} (${files.length} chunks)`);
  for (const row of summary.slice(0, 15)) {
    console.log(
      `  ${row.status.padEnd(4)} [${row.category.padEnd(12)}] ${formatKb(row.size).padStart(10)} / ${formatKb(row.budget).padStart(10)}  ${row.file}`,
    );
  }
  if (summary.length > 15) {
    console.log(`  ... ${summary.length - 15} smaller chunk(s) elided`);
  }

  if (exemptions.length > 0) {
    console.warn(`\nWARN: ${exemptions.length} chunk(s) over budget but exempted via --allow-chunk:`);
    for (const e of exemptions) {
      console.warn(
        `  - ${e.file}: ${formatKb(e.size)} > ${formatKb(e.budget)} (${e.category})`,
      );
    }
  }

  if (failures.length > 0) {
    console.error(`\nFAIL: ${failures.length} chunk(s) exceeded the bundle-size budget:`);
    for (const f of failures) {
      console.error(
        `  - ${f.file}: ${formatKb(f.size)} > ${formatKb(f.budget)} (${f.category})`,
      );
    }
    console.error(
      '\nSee docs/performance-budget.md for raising the threshold and the justification ritual.',
    );
    exit(1);
  }

  console.log('\nPASS: all chunks within the configured budget.');
  exit(0);
}

main();
