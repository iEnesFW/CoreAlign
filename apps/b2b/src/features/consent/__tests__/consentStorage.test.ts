import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  CONSENT_STORAGE_KEY,
  CONSENT_VERSION,
  readConsentDecision,
  writeConsentDecision,
} from '@/features/consent/consentStorage';

describe('writeConsentDecision', () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it('persists analytics + marketing flags', () => {
    const decision = writeConsentDecision(true, false);
    expect(decision.analytics).toBe(true);
    expect(decision.marketing).toBe(false);
    expect(decision.essential).toBe(true);
    expect(decision.version).toBe(CONSENT_VERSION);
    const raw = window.localStorage.getItem(CONSENT_STORAGE_KEY);
    expect(raw).not.toBeNull();
  });

  it('stamps decidedAt timestamp', () => {
    const before = Date.now();
    const decision = writeConsentDecision(false, false);
    expect(decision.decidedAt).toBeGreaterThanOrEqual(before);
  });
});

describe('readConsentDecision', () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it('returns null when nothing stored', () => {
    expect(readConsentDecision()).toBeNull();
  });

  it('round-trips a written decision', () => {
    writeConsentDecision(true, true);
    const read = readConsentDecision();
    expect(read?.analytics).toBe(true);
    expect(read?.marketing).toBe(true);
  });

  it('returns null when version mismatches', () => {
    window.localStorage.setItem(
      CONSENT_STORAGE_KEY,
      JSON.stringify({
        essential: true,
        analytics: true,
        marketing: false,
        version: 'old-version',
        decidedAt: Date.now(),
      }),
    );
    expect(readConsentDecision()).toBeNull();
  });

  it('returns null when ttl has elapsed', () => {
    const oldDecidedAt = Date.now() - 366 * 24 * 60 * 60 * 1000;
    window.localStorage.setItem(
      CONSENT_STORAGE_KEY,
      JSON.stringify({
        essential: true,
        analytics: true,
        marketing: false,
        version: CONSENT_VERSION,
        decidedAt: oldDecidedAt,
      }),
    );
    expect(readConsentDecision()).toBeNull();
  });

  it('handles JSON parse errors gracefully', () => {
    window.localStorage.setItem(CONSENT_STORAGE_KEY, 'not-json');
    expect(() => readConsentDecision()).toThrow();
  });

  it('returns null on null window guard', () => {
    const originalWindow = global.window;
    Object.defineProperty(global, 'window', { value: undefined, configurable: true });
    try {
      expect(readConsentDecision()).toBeNull();
    } finally {
      Object.defineProperty(global, 'window', { value: originalWindow, configurable: true });
    }
    vi.clearAllMocks();
  });
});
