import { useCallback, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';

const escapeStack: symbol[] = [];

export const useModalClose = (isDirty: boolean, onClose: () => void, enabled = true) => {
  const confirm = useConfirm();
  const { t } = useTranslation();

  const requestClose = useCallback(async () => {
    if (isDirty) {
      const ok = await confirm({
        title: t('common.unsaved.title', { defaultValue: 'Kaydedilmemiş değişiklikler' }),
        message: t('common.unsaved.message', {
          defaultValue: 'Kaydedilmemiş değişiklikleriniz var. Çıkmak istediğinize emin misiniz?',
        }),
        confirmLabel: t('common.unsaved.discard', { defaultValue: 'Değişiklikleri at' }),
        cancelLabel: t('common.unsaved.keep', { defaultValue: 'Düzenlemeye devam et' }),
        tone: 'danger',
      });
      if (!ok) return;
    }
    onClose();
  }, [isDirty, onClose, confirm, t]);

  const requestCloseRef = useRef(requestClose);
  useEffect(() => {
    requestCloseRef.current = requestClose;
  }, [requestClose]);

  const tokenRef = useRef(Symbol('modal'));

  useEffect(() => {
    if (!enabled) return;
    const token = tokenRef.current;
    escapeStack.push(token);
    const handler = (e: KeyboardEvent) => {
      if (e.key !== 'Escape') return;
      if (escapeStack[escapeStack.length - 1] !== token) return;
      void requestCloseRef.current();
    };
    document.addEventListener('keydown', handler);
    return () => {
      document.removeEventListener('keydown', handler);
      const i = escapeStack.lastIndexOf(token);
      if (i !== -1) escapeStack.splice(i, 1);
    };
  }, [enabled]);

  return requestClose;
};
