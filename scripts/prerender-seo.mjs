import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'node:fs';
import { join, dirname } from 'node:path';

const DEFAULT_SITE = 'https://corealign.com';
const SITE = (process.env.SITE_URL || DEFAULT_SITE).replace(/\/$/, '');
const DIST = 'dist';
const YEAR = new Date().getFullYear();
const TODAY = new Date().toISOString().slice(0, 10);

const loc = {
  tr: JSON.parse(readFileSync('src/app/i18n/locales/tr.json', 'utf8')),
  en: JSON.parse(readFileSync('src/app/i18n/locales/en.json', 'utf8')),
};

const SEO = {
  tr: {
    '/': {
      title: 'CoreAlign — Cam & İmalat için Bulut ERP',
      description:
        'Cam kabin/cephe tasarımı (3D CAD + CPQ), üretim (MRP), stok ve muhasebe tek bulut ERP’de. Teklif süresini günlerden saatlere indirin.',
    },
    '/solutions': {
      title: 'Çözümler — CoreAlign Bulut ERP',
      description:
        'Cam kabin CPQ, 3D tasarım, MRP üretim planlama ve canlı maliyet simülasyonu — CoreAlign çözümlerini keşfedin.',
    },
    '/about': {
      title: 'Hakkımızda — CoreAlign',
      description: 'CoreAlign’in misyonu, yaklaşımı ve güvenlik/uyumluluk taahhüdü.',
    },
    '/articles': {
      title: 'Kaynaklar & Blog — CoreAlign',
      description: 'Cam imalatı, ERP ve dijital dönüşüm üzerine içerikler ve rehberler.',
    },
    '/contact': {
      title: 'İletişim — CoreAlign',
      description: 'Demo planlayın veya CoreAlign ekibiyle iletişime geçin.',
    },
  },
  en: {
    '/': {
      title: 'CoreAlign — Cloud ERP for Glass & Manufacturing',
      description:
        'Multi-tenant cloud ERP unifying glass enclosure/façade design (3D CAD + CPQ), production planning (MRP), inventory and accounting on one platform.',
    },
    '/solutions': {
      title: 'Solutions — CoreAlign Cloud ERP',
      description:
        'Glass enclosure CPQ, 3D design, MRP production planning and live cost simulation — explore CoreAlign solutions.',
    },
    '/about': {
      title: 'About — CoreAlign',
      description: 'CoreAlign mission, approach, and security & compliance commitment.',
    },
    '/articles': {
      title: 'Resources & Blog — CoreAlign',
      description: 'Articles and guides on glass manufacturing, ERP, and digital transformation.',
    },
    '/contact': {
      title: 'Contact — CoreAlign',
      description: 'Schedule a demo or get in touch with the CoreAlign team.',
    },
  },
};

const OG_IMAGE_ALT = {
  tr: 'CoreAlign — cam ve imalat için bulut ERP',
  en: 'CoreAlign — cloud ERP for glass and manufacturing',
};
const ORG_DESC = {
  tr: 'Cam ve imalat sektörü için çok-kiracılı bulut ERP platformu.',
  en: 'Multi-tenant cloud ERP platform for the glass and manufacturing industry.',
};

const ROUTES = ['/', '/solutions', '/about', '/articles', '/contact'];
const H1_KEY = {
  '/': 'LandingPage.intro.slogan',
  '/solutions': 'LandingPage.solutions.title',
  '/about': 'LandingPage.about.title',
  '/articles': 'LandingPage.articles.title',
  '/contact': 'LandingPage.contact.title',
};

const get = (o, path) => path.split('.').reduce((a, k) => (a == null ? a : a[k]), o);
const esc = (s) =>
  String(s ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');

const setMeta = (html, attr, key, val) =>
  html.replace(new RegExp(`(<meta\\s+${attr}="${key}"\\s+content=")[^"]*(")`), `$1${esc(val)}$2`);

const faqPairs = (lang) => {
  const faq = get(loc[lang], 'LandingPage.faq') || {};
  const seen = new Set();
  const out = [];
  const push = (q, a) => {
    if (q && a && !seen.has(q)) {
      seen.add(q);
      out.push([q, a]);
    }
  };
  for (const k of Object.keys(faq)) {
    const m = /^q(\d+)$/.exec(k);
    if (m) push(faq[k], faq[`a${m[1]}`]);
    if (/Q$/.test(k)) push(faq[k], faq[`${k.slice(0, -1)}A`]);
  }
  return out;
};

const buildJsonLd = (lang) => {
  const graph = {
    '@context': 'https://schema.org',
    '@graph': [
      {
        '@type': 'SoftwareApplication',
        '@id': `${SITE}/#software`,
        name: 'CoreAlign',
        applicationCategory: 'BusinessApplication',
        operatingSystem: 'Web',
        description: SEO[lang]['/'].description,
        url: `${SITE}/`,
        inLanguage: ['tr-TR', 'en-US'],
        publisher: { '@id': `${SITE}/#org` },
      },
      {
        '@type': 'Organization',
        '@id': `${SITE}/#org`,
        name: 'CoreAlign',
        url: `${SITE}/`,
        logo: { '@type': 'ImageObject', url: `${SITE}/og-image.png`, width: 1200, height: 630 },
        description: ORG_DESC[lang],
      },
      {
        '@type': 'WebSite',
        '@id': `${SITE}/#website`,
        name: 'CoreAlign',
        url: `${SITE}/`,
        inLanguage: ['tr-TR', 'en-US'],
        publisher: { '@id': `${SITE}/#org` },
      },
      {
        '@type': 'FAQPage',
        '@id': `${SITE}/#faq`,
        inLanguage: lang === 'en' ? 'en-US' : 'tr-TR',
        mainEntity: faqPairs(lang).map(([q, a]) => ({
          '@type': 'Question',
          name: q,
          acceptedAnswer: { '@type': 'Answer', text: a },
        })),
      },
    ],
  };
  const json = JSON.stringify(graph, null, 2)
    .split('\n')
    .map((l) => '      ' + l)
    .join('\n');
  return `<script type="application/ld+json">\n${json}\n    </script>`;
};

const navHtml = (lang, prefix) => {
  const n = get(loc[lang], 'LandingPage.nav') || {};
  const link = (p, label) => `<a href="${prefix}${p === '/' ? '/' : p}">${esc(label)}</a>`;
  return `<nav aria-label="Primary">${link('/', n.home || 'CoreAlign')} ${link('/solutions', n.solutions || 'Solutions')} ${link('/about', n.about || 'About')} ${link('/articles', n.articles || 'Articles')} ${link('/contact', n.contact || 'Contact')}</nav>`;
};

const snapshotHtml = (lang, route, prefix) => {
  const lp = get(loc[lang], 'LandingPage') || {};
  const h1 = get(loc[lang], H1_KEY[route]) || SEO[lang][route].title;
  const desc = SEO[lang][route].description;
  let body = `<h1>${esc(h1)}</h1><p>${esc(desc)}</p>`;
  if (route === '/') {
    const mods = ['m1Title', 'm2Title', 'm3Title', 'm4Title', 'm5Title', 'm6Title']
      .map((k) => get(lp, `showcase.${k}`))
      .filter(Boolean);
    if (mods.length) {
      body += `<section><h2>${esc(get(lp, 'showcase.title') || 'Modüller')}</h2><ul>${mods
        .map((m) => `<li>${esc(m)}</li>`)
        .join('')}</ul></section>`;
    }
  }
  const demoLabel = lang === 'en' ? 'Schedule a demo' : 'Demo planlayın';
  return `<div id="root" data-prerendered="true"><header><a href="${prefix || '/'}">CoreAlign</a>${navHtml(lang, prefix)}</header><main>${body}<p><a href="${prefix || '/'}#demo">${esc(demoLabel)}</a></p></main><footer>&copy; ${YEAR} CoreAlign</footer></div>`;
};

const alternatesHtml = (route) => {
  const trHref = `${SITE}${route}`;
  const enHref = `${SITE}/en${route === '/' ? '' : route}`;
  return [
    `<link rel="alternate" hreflang="tr" href="${trHref}" />`,
    `<link rel="alternate" hreflang="en" href="${enHref}" />`,
    `<link rel="alternate" hreflang="x-default" href="${trHref}" />`,
  ].join('\n    ');
};

const shellPath = join(DIST, 'index.html');
if (!existsSync(shellPath)) {
  console.error('prerender-seo: dist/index.html not found — run vite build first.');
  process.exit(1);
}
let shell = readFileSync(shellPath, 'utf8');
shell = shell.replace(/ *<link rel="alternate" hreflang="[^"]*"[^>]*>\n?/g, '');
shell = shell.replace(
  /<div id="root" data-prerendered="true">[\s\S]*<\/div>(\s*<\/body>)/,
  '<div id="root"></div>$1',
);

const write = (outPath, html) => {
  mkdirSync(dirname(outPath), { recursive: true });
  writeFileSync(outPath, html);
};

let count = 0;
const sitemapUrls = [];
for (const lang of ['tr', 'en']) {
  const prefix = lang === 'en' ? '/en' : '';
  for (const route of ROUTES) {
    const seo = SEO[lang][route];
    let canonical = `${SITE}${prefix}${route === '/' ? '' : route}`;
    if (canonical === SITE) canonical = `${SITE}/`;
    let html = shell;
    html = html.replace(/<html lang="[^"]*">/, `<html lang="${lang}">`);
    html = html.replace(/<title>[\s\S]*?<\/title>/, `<title>${esc(seo.title)}</title>`);
    html = setMeta(html, 'name', 'description', seo.description);
    html = setMeta(html, 'property', 'og:title', seo.title);
    html = setMeta(html, 'property', 'og:description', seo.description);
    html = setMeta(html, 'property', 'og:url', canonical);
    html = setMeta(html, 'property', 'og:type', route === '/articles' ? 'article' : 'website');
    html = setMeta(html, 'property', 'og:locale', lang === 'en' ? 'en_US' : 'tr_TR');
    html = setMeta(html, 'property', 'og:locale:alternate', lang === 'en' ? 'tr_TR' : 'en_US');
    html = setMeta(html, 'property', 'og:image:alt', OG_IMAGE_ALT[lang]);
    html = setMeta(html, 'name', 'twitter:title', seo.title);
    html = setMeta(html, 'name', 'twitter:description', seo.description);
    html = setMeta(html, 'name', 'twitter:image:alt', OG_IMAGE_ALT[lang]);
    html = html.replace(/(<link rel="canonical" href=")[^"]*(")/, `$1${canonical}$2`);
    html = html.replace(
      /<script type="application\/ld\+json">[\s\S]*?<\/script>/,
      buildJsonLd(lang),
    );
    html = html.replace('</head>', `    ${alternatesHtml(route)}\n  </head>`);
    html = html.replace('<div id="root"></div>', snapshotHtml(lang, route, prefix));
    html = html.replace(
      /\s*<link rel="modulepreload"[^>]*href="[^"]*\/(?:vendor-3d|vendor-charts|ar|de|ru)-[A-Za-z0-9_-]+\.js"[^>]*>/g,
      '',
    );
    if (SITE !== DEFAULT_SITE) {
      html = html.split(DEFAULT_SITE).join(SITE);
    }

    const rel = `${prefix}${route === '/' ? '' : route}`;
    write(join(DIST, rel, 'index.html'), html);
    sitemapUrls.push({ loc: canonical, route });
    count++;
  }
}

const sitemap = `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9" xmlns:xhtml="http://www.w3.org/1999/xhtml">
${sitemapUrls
  .map(
    (u) => `  <url>
    <loc>${u.loc}</loc>
    <lastmod>${TODAY}</lastmod>
    <xhtml:link rel="alternate" hreflang="tr" href="${SITE}${u.route}" />
    <xhtml:link rel="alternate" hreflang="en" href="${SITE}/en${u.route === '/' ? '' : u.route}" />
    <xhtml:link rel="alternate" hreflang="x-default" href="${SITE}${u.route}" />
    <changefreq>weekly</changefreq>
    <priority>${u.route === '/' ? '1.0' : '0.7'}</priority>
  </url>`,
  )
  .join('\n')}
</urlset>
`;
writeFileSync(join(DIST, 'sitemap.xml'), sitemap);

console.log(
  `prerender-seo: ${count} route+locale HTML files + sitemap (${sitemapUrls.length} URLs), site=${SITE}.`,
);
