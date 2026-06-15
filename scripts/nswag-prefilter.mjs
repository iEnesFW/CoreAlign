#!/usr/bin/env node
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');
const sourceSpec = resolve(repoRoot, 'openapi', 'v1.json');

const TARGETS = [
  {
    name: 'admin',
    output: resolve(repoRoot, 'openapi', 'v1.admin.json'),
    keep: (path) =>
      !path.startsWith('/api/v1/customer-portal') && !path.startsWith('/api/v1/dealer-portal'),
  },
  {
    name: 'customer-portal',
    output: resolve(repoRoot, 'openapi', 'v1.customer.json'),
    keep: (path) =>
      path.startsWith('/api/v1/customer-portal') ||
      path.startsWith('/api/v1/auth') ||
      path.startsWith('/api/v1/notifications'),
  },
  {
    name: 'b2b',
    output: resolve(repoRoot, 'openapi', 'v1.dealer.json'),
    keep: (path) =>
      path.startsWith('/api/v1/dealer-portal') ||
      path.startsWith('/api/v1/dealer-accounts') ||
      path.startsWith('/api/v1/dealer-customer-links') ||
      path.startsWith('/api/v1/dealer-users') ||
      path.startsWith('/api/v1/auth') ||
      path.startsWith('/api/v1/notifications'),
  },
];

function loadSpec() {
  try {
    const raw = readFileSync(sourceSpec, 'utf8');
    return JSON.parse(raw);
  } catch (err) {
    process.stderr.write(`[nswag-prefilter] Cannot read ${sourceSpec}: ${err.message}\n`);
    process.stderr.write(
      '[nswag-prefilter] Run "npm run nswag:spec" first to produce openapi/v1.json.\n',
    );
    process.exit(1);
  }
}

function deepClone(value) {
  return JSON.parse(JSON.stringify(value));
}

function pruneUnusedRefs(spec) {
  const usedRefs = new Set();
  const visit = (node) => {
    if (node === null || typeof node !== 'object') return;
    if (Array.isArray(node)) {
      for (const item of node) visit(item);
      return;
    }
    for (const [key, value] of Object.entries(node)) {
      if (key === '$ref' && typeof value === 'string') {
        usedRefs.add(value);
      } else {
        visit(value);
      }
    }
  };
  visit(spec.paths ?? {});

  const componentsSchemas = spec.components?.schemas ?? {};
  let changed = true;
  while (changed) {
    changed = false;
    for (const ref of Array.from(usedRefs)) {
      const match = /^#\/components\/schemas\/(.+)$/.exec(ref);
      if (!match) continue;
      const schemaName = match[1];
      const schema = componentsSchemas[schemaName];
      if (!schema) continue;
      const before = usedRefs.size;
      visit(schema);
      if (usedRefs.size !== before) changed = true;
    }
  }

  if (spec.components?.schemas) {
    const kept = {};
    for (const [name, schema] of Object.entries(componentsSchemas)) {
      if (usedRefs.has(`#/components/schemas/${name}`)) {
        kept[name] = schema;
      }
    }
    spec.components.schemas = kept;
  }
}

function filterSpec(spec, keep) {
  const next = deepClone(spec);
  const paths = next.paths ?? {};
  const filtered = {};
  for (const [path, ops] of Object.entries(paths)) {
    if (keep(path)) filtered[path] = ops;
  }
  next.paths = filtered;
  pruneUnusedRefs(next);
  return next;
}

const spec = loadSpec();
for (const target of TARGETS) {
  const filtered = filterSpec(spec, target.keep);
  mkdirSync(dirname(target.output), { recursive: true });
  writeFileSync(target.output, JSON.stringify(filtered, null, 2), 'utf8');
  const opCount = Object.values(filtered.paths ?? {}).reduce(
    (acc, ops) => acc + Object.keys(ops).filter((k) => !k.startsWith('x-')).length,
    0,
  );
  process.stdout.write(
    `[nswag-prefilter] ${target.name}: ${opCount} operations -> ${target.output}\n`,
  );
}
