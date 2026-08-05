import i18n from '@/app/i18n/config';
import { queueToast } from '@/shared/api/toastQueue';
import type { PanelOutlineRejection } from './panelShapeOutline';

/**
 * A refused pane outline has to SAY why. Silently keeping the old shape reads as "the editor is
 * broken" — which is exactly how the earlier silent lock rejections read. The store cannot use a
 * hook, so the visible text comes from the i18n singleton (the ErrorBoundary pattern).
 */
const REASON_KEY: Record<PanelOutlineRejection, { key: string; fallback: string }> = {
  selfIntersecting: {
    key: 'GlassEnclosure.Designer.Panel.OutlineSelfIntersecting',
    fallback: 'Çizim kendini kesiyor — cam bu şekilde kesilemez, önceki şekil korundu.',
  },
  tooFewPoints: {
    key: 'GlassEnclosure.Designer.Panel.OutlineTooFewPoints',
    fallback: 'Şekil için en az üç farklı köşe gerekir — önceki şekil korundu.',
  },
  degenerate: {
    key: 'GlassEnclosure.Designer.Panel.OutlineDegenerate',
    fallback: 'Şekil kesilemeyecek kadar ince — önceki şekil korundu.',
  },
  unparsable: {
    key: 'GlassEnclosure.Designer.Panel.OutlineUnreadable',
    fallback: 'Şekil verisi okunamadı — önceki şekil korundu.',
  },
};

export const notifyPanelOutlineRejected = (reason: PanelOutlineRejection | null): void => {
  const entry = REASON_KEY[reason ?? 'unparsable'];
  queueToast({
    dedupeKey: 'glass-panel-outline-rejected',
    variant: 'warning',
    description: i18n.t(entry.key, { defaultValue: entry.fallback }),
  });
};
