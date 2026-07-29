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

export const notifyPlacementBlocked = (t: TFunction): void => {
  queueToast({
    dedupeKey: 'glass-placement-blocked',
    variant: 'warning',
    description: t('GlassEnclosure.Designer.Placement.Blocked', {
      defaultValue: 'Burada başka bir gövde var — yerleştirmek için boş bir nokta seçin.',
    }),
  });
};
