import type { TFunction } from 'i18next';
import { queueToast } from '@/shared/api/toastQueue';

export const notifyStackUnavailable = (t: TFunction): void => {
  queueToast({
    dedupeKey: 'glass-stack-multi-selection',
    variant: 'warning',
    description: t('GlassEnclosure.Designer.Stack.MultiSelectionBlocked', {
      defaultValue:
        'Çoklu seçimde üst üste yerleştirme yapılamaz. Tek bir nesne seçip Alt ile sürükleyin.',
    }),
  });
};
