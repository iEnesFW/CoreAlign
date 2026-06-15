export const CONSENT_STORAGE_KEY = 'corealign.consent.v1';
export const CONSENT_VERSION = 'v2026-06-01';
export const CONSENT_TTL_MS = 365 * 24 * 60 * 60 * 1000;

export type ConsentCategoryKey = 'essential' | 'analytics' | 'marketing';

export interface ConsentDecision {
  essential: true;
  analytics: boolean;
  marketing: boolean;
  version: string;
  decidedAt: number;
}

export const readConsentDecision = (): ConsentDecision | null => {
  if (typeof window === 'undefined') return null;
  const raw = window.localStorage.getItem(CONSENT_STORAGE_KEY);
  if (!raw) return null;
  const parsed = JSON.parse(raw) as ConsentDecision;
  if (!parsed || typeof parsed !== 'object') return null;
  if (parsed.version !== CONSENT_VERSION) return null;
  if (Date.now() - parsed.decidedAt > CONSENT_TTL_MS) return null;
  return parsed;
};

export const writeConsentDecision = (analytics: boolean, marketing: boolean): ConsentDecision => {
  const decision: ConsentDecision = {
    essential: true,
    analytics,
    marketing,
    version: CONSENT_VERSION,
    decidedAt: Date.now(),
  };
  if (typeof window !== 'undefined') {
    window.localStorage.setItem(CONSENT_STORAGE_KEY, JSON.stringify(decision));
  }
  return decision;
};
