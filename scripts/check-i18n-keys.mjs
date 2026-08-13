import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';

const SURFACES = [
  { name: 'admin', src: 'src', locales: 'src/app/i18n/locales' },
  {
    name: 'customer-portal',
    src: 'apps/customer-portal/src',
    locales: 'apps/customer-portal/src/app/locales',
  },
  { name: 'b2b', src: 'apps/b2b/src', locales: 'apps/b2b/src/app/locales' },
];

const LANGS = ['tr', 'en'];

// WHY the plural suffixes count as present: i18next resolves `key_one`/`key_other` before `key`
// when a `count` option is passed, so a block that only defines the plural forms is complete.
const PLURAL_SUFFIXES = ['', '_one', '_other', '_zero', '_few', '_many'];

const readJson = (file) => JSON.parse(fs.readFileSync(file, 'utf8'));

const resolves = (tree, dotted) => {
  const parts = dotted.split('.');
  const leaf = parts[parts.length - 1];
  return PLURAL_SUFFIXES.some((suffix) => {
    let node = tree;
    for (let i = 0; i < parts.length; i += 1) {
      if (node === null || typeof node !== 'object') return false;
      node = node[i === parts.length - 1 ? leaf + suffix : parts[i]];
    }
    return node !== undefined;
  });
};

const collectFiles = (dir) => {
  const out = [];
  const walk = (d) => {
    for (const entry of fs.readdirSync(d, { withFileTypes: true })) {
      const p = path.join(d, entry.name);
      if (entry.isDirectory()) {
        if (!/node_modules|__tests__/.test(p)) walk(p);
      } else if (/\.(ts|tsx)$/.test(entry.name) && !/\.test\./.test(entry.name)) {
        out.push(p);
      }
    }
  };
  walk(dir);
  return out;
};

const KEY_CALL = /\bt\(\s*'([A-Za-z][A-Za-z0-9_.]*)'\s*(?:,\s*\{([^}]*)\})?\s*\)/g;

const problems = [];
for (const surface of SURFACES) {
  if (!fs.existsSync(surface.src)) continue;
  const trees = {};
  for (const lang of LANGS) {
    const file = path.join(surface.locales, `${lang}.json`);
    if (fs.existsSync(file)) trees[lang] = readJson(file);
  }
  if (Object.keys(trees).length === 0) continue;

  for (const file of collectFiles(surface.src)) {
    const lines = fs.readFileSync(file, 'utf8').split(/\r?\n/);
    lines.forEach((line, index) => {
      const re = new RegExp(KEY_CALL.source, 'g');
      let match;
      while ((match = re.exec(line)) !== null) {
        const key = match[1];
        const options = match[2] ?? '';
        if (options.includes('defaultValue')) continue;
        if (!key.includes('.')) continue;
        for (const [lang, tree] of Object.entries(trees)) {
          if (resolves(tree, key)) continue;
          problems.push(
            `${path.relative(process.cwd(), file).replace(/\\/g, '/')}:${index + 1} [${lang}] ${key}`,
          );
        }
      }
    });
  }
}

if (problems.length > 0) {
  console.error(
    `i18n: ${problems.length} translation call(s) resolve to no value and carry no defaultValue.`,
  );
  console.error('These render the raw key to the user:\n');
  console.error(problems.join('\n'));
  process.exit(1);
}

console.log('i18n: every literal t() key without a defaultValue resolves in tr and en.');
