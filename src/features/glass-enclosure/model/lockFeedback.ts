import i18n from '@/app/i18n/config';
import { queueToast } from '@/shared/api/toastQueue';

/**
 * Every lock rejection used to be a silent `return` in the store, so an inspector field, the
 * transform toolbar and the mirror/array tools all accepted the edit visually and dropped it —
 * indistinguishable from a bug. The store cannot use a hook, so the visible text comes from the
 * i18n singleton (the ErrorBoundary pattern).
 */
export const notifyLockedBlocked = (): void => {
  queueToast({
    dedupeKey: 'glass-body-locked',
    variant: 'warning',
    description: i18n.t('GlassEnclosure.Designer.LockedBlocked', {
      defaultValue: 'Bu nesne kilitli — düzenlemek için önce kilidi açın.',
    }),
  });
};
