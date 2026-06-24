import { readFileSync, writeFileSync, mkdirSync, rmSync, existsSync } from 'node:fs';
import { dirname, resolve, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..', '..');
const sidebarPath = join(root, 'src', 'widgets', 'Sidebar', 'Sidebar.tsx');
const localesDir = join(root, 'src', 'app', 'i18n', 'locales');
const docsRoot = join(root, 'docs', 'ai-kb');
const locales = ['tr', 'en'];

const templates = {
  tr: (label, href) =>
    `${label}, CoreAlign uygulamasındaki bir sayfadır. Sol kenar çubuğundan (menü) "${label}" bağlantısına tıklayarak açılır. Adres yolu: ${href}. Bu sayfaya gitmek için sol menüden ilgili bölümü ve "${label}" öğesini seçin.`,
  en: (label, href) =>
    `${label} is a page in CoreAlign. Open it from the left sidebar (menu) by clicking the "${label}" link. Path: ${href}. To navigate here, choose the matching section and the "${label}" item in the left menu.`,
};

function resolveKey(obj, key) {
  let node = obj;
  for (const part of key.split('.')) {
    if (node && typeof node === 'object' && part in node) {
      node = node[part];
    } else {
      return null;
    }
  }
  return typeof node === 'string' ? node : null;
}

function humanize(href) {
  const last = href.split('/').filter(Boolean).pop() ?? href;
  return last
    .split('-')
    .map((w) => (w.length > 0 ? w[0].toUpperCase() + w.slice(1) : w))
    .join(' ');
}

function extractPairs(source) {
  const pairs = new Map();
  const labelThenHref = /labelKey:\s*'([^']+)'[\s\S]{0,120}?href:\s*'([^']+)'/g;
  const hrefThenLabel = /href:\s*'([^']+)'[\s\S]{0,120}?labelKey:\s*'([^']+)'/g;
  let match;
  while ((match = labelThenHref.exec(source)) !== null) {
    pairs.set(match[2], match[1]);
  }
  while ((match = hrefThenLabel.exec(source)) !== null) {
    if (!pairs.has(match[1])) {
      pairs.set(match[1], match[2]);
    }
  }
  return pairs;
}

const source = readFileSync(sidebarPath, 'utf8');
const pairs = extractPairs(source);

const localeData = {};
for (const locale of locales) {
  localeData[locale] = JSON.parse(readFileSync(join(localesDir, `${locale}.json`), 'utf8'));
}

let written = 0;
for (const locale of locales) {
  const generatedDir = join(docsRoot, locale, 'generated');
  if (existsSync(generatedDir)) {
    rmSync(generatedDir, { recursive: true, force: true });
  }
  mkdirSync(generatedDir, { recursive: true });

  for (const [href, labelKey] of pairs) {
    if (!href.startsWith('/')) {
      continue;
    }
    const label =
      resolveKey(localeData[locale], labelKey) ??
      resolveKey(localeData.en, labelKey) ??
      humanize(href);
    const slug = href.replace(/^\//, '').replace(/[^a-zA-Z0-9]+/g, '-').replace(/-+$/, '');
    const body = templates[locale](label, href);
    const md = `Route: ${href}\n# ${label}\n\n${body}\n`;
    writeFileSync(join(generatedDir, `nav-${slug}.md`), md, 'utf8');
    written++;
  }
}

const flatten = (obj, prefix, out) => {
  for (const [k, v] of Object.entries(obj)) {
    const key = prefix ? `${prefix}.${k}` : k;
    if (v && typeof v === 'object') {
      flatten(v, key, out);
    } else if (typeof v === 'string') {
      out[key] = v;
    }
  }
  return out;
};

const enFlat = flatten(localeData.en, '', {});
const trFlat = flatten(localeData.tr, '', {});
const allKeys = [...new Set([...Object.keys(enFlat), ...Object.keys(trFlat)])];
const namespaces = [...new Set(allKeys.map((k) => k.split('.')[0]))];

const sharedI18nDir = join(docsRoot, 'shared', 'i18n');
if (existsSync(sharedI18nDir)) {
  rmSync(sharedI18nDir, { recursive: true, force: true });
}
mkdirSync(sharedI18nDir, { recursive: true });

let i18nWritten = 0;
for (const ns of namespaces) {
  const keys = allKeys.filter((k) => k.split('.')[0] === ns).sort();
  const lines = keys
    .map((k) => {
      const en = enFlat[k] ?? '';
      const tr = trFlat[k] ?? '';
      if (!en && !tr) return null;
      return tr && tr !== en ? `- ${en} / ${tr}` : `- ${en || tr}`;
    })
    .filter(Boolean);
  if (lines.length === 0) continue;
  const md = `# ${ns} (CoreAlign UI)\n\n"${ns}" alanındaki ekran ve özelliklerde geçen terimler / terms used in the "${ns}" area (English / Türkçe):\n\n${lines.join('\n')}\n`;
  const slug = ns.replace(/[^a-zA-Z0-9]+/g, '-');
  writeFileSync(join(sharedI18nDir, `i18n-${slug}.md`), md, 'utf8');
  i18nWritten++;
}

console.log(`ai-kb extractor: wrote ${written} navigation docs from ${pairs.size} routes + ${i18nWritten} i18n vocabulary docs across ${locales.length} locales.`);
