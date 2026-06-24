import i18n from 'i18next';
import type { UxComplexityMode } from '@/shared/model/persona';

const GLOSSARY_PREFIX = 'Designer.Term.';

export const tSector = (term: string, mode: UxComplexityMode): string => {
  const modeKey = `${GLOSSARY_PREFIX}${term}.${mode}`;
  const modeTranslated = i18n.t(modeKey, { defaultValue: '' }) as string;
  if (modeTranslated && modeTranslated !== modeKey) return modeTranslated;

  const fallbackKey = `${GLOSSARY_PREFIX}${term}`;
  const fallback = i18n.t(fallbackKey, { defaultValue: term }) as string;
  return fallback || term;
};
