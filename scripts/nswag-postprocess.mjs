#!/usr/bin/env node
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');

const TARGETS = [
  resolve(repoRoot, 'src/shared/api/EMCM.Client.ts'),
  resolve(repoRoot, 'apps/customer-portal/src/shared/api/EMCM.Client.ts'),
  resolve(repoRoot, 'apps/b2b/src/shared/api/EMCM.Client.ts'),
];

const HEADER = '// @ts-nocheck\n/* eslint-disable */\n';

for (const target of TARGETS) {
  try {
    const raw = readFileSync(target, 'utf8');
    if (raw.startsWith('// @ts-nocheck')) continue;
    writeFileSync(target, HEADER + raw, 'utf8');
    process.stdout.write(`[nswag-postprocess] prepended @ts-nocheck to ${target}\n`);
  } catch (err) {
    process.stderr.write(`[nswag-postprocess] WARN: skipping ${target}: ${err.message}\n`);
  }
}
