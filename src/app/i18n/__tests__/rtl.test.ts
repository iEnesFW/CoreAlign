import { describe, it, expect, beforeEach } from 'vitest';
import { applyDocumentDirection } from '../rtl';

describe('applyDocumentDirection', () => {
  beforeEach(() => {
    document.documentElement.removeAttribute('dir');
    document.documentElement.removeAttribute('lang');
    delete document.documentElement.dataset.dir;
  });

  it('sets rtl direction for Arabic locale', () => {
    applyDocumentDirection('ar');
    expect(document.documentElement.dir).toBe('rtl');
    expect(document.documentElement.lang).toBe('ar');
    expect(document.documentElement.dataset.dir).toBe('rtl');
  });

  it('sets rtl direction for region-tagged Arabic locale', () => {
    applyDocumentDirection('ar-SA');
    expect(document.documentElement.dir).toBe('rtl');
    expect(document.documentElement.lang).toBe('ar');
  });

  it('sets ltr direction for English locale', () => {
    applyDocumentDirection('en');
    expect(document.documentElement.dir).toBe('ltr');
    expect(document.documentElement.lang).toBe('en');
  });

  it('treats unknown locale as ltr by default', () => {
    applyDocumentDirection('xx');
    expect(document.documentElement.dir).toBe('ltr');
  });
});
