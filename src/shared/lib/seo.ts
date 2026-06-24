import { useEffect } from 'react';

export interface SeoAlternate {
  lang: string;
  href: string;
}

interface SeoOptions {
  title: string;
  description?: string;
  canonical?: string;
  ogLocale?: string;
  alternates?: SeoAlternate[];
}

const upsertMeta = (key: 'name' | 'property', value: string, content: string) => {
  if (typeof document === 'undefined') return;
  let el = document.head.querySelector<HTMLMetaElement>(`meta[${key}="${value}"]`);
  if (!el) {
    el = document.createElement('meta');
    el.setAttribute(key, value);
    document.head.appendChild(el);
  }
  el.setAttribute('content', content);
};

const upsertCanonical = (href: string) => {
  if (typeof document === 'undefined') return;
  let el = document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
  if (!el) {
    el = document.createElement('link');
    el.setAttribute('rel', 'canonical');
    document.head.appendChild(el);
  }
  el.setAttribute('href', href);
};

const setAlternates = (alternates: SeoAlternate[]) => {
  if (typeof document === 'undefined') return;
  document.head
    .querySelectorAll('link[rel="alternate"][data-seo-alt]')
    .forEach((el) => el.remove());
  for (const alt of alternates) {
    const el = document.createElement('link');
    el.setAttribute('rel', 'alternate');
    el.setAttribute('hreflang', alt.lang);
    el.setAttribute('href', alt.href);
    el.setAttribute('data-seo-alt', 'true');
    document.head.appendChild(el);
  }
};

export const useSeo = ({ title, description, canonical, ogLocale, alternates }: SeoOptions) => {
  useEffect(() => {
    if (title) {
      document.title = title;
      upsertMeta('property', 'og:title', title);
      upsertMeta('name', 'twitter:title', title);
    }
    if (description) {
      upsertMeta('name', 'description', description);
      upsertMeta('property', 'og:description', description);
      upsertMeta('name', 'twitter:description', description);
    }
    if (canonical) {
      upsertCanonical(canonical);
      upsertMeta('property', 'og:url', canonical);
    }
    if (ogLocale) {
      upsertMeta('property', 'og:locale', ogLocale);
    }
    if (alternates) {
      setAlternates(alternates);
    }
  }, [title, description, canonical, ogLocale, alternates]);
};
