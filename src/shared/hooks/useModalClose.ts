import { useCallback, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';

// Shared stack of open modals so a single Escape only closes the topmost one
// (e.g. a quick-add modal opened over a form modal must not close both).
const escapeStack: symbol[] = [];

/**
 * Guarded modal close: when the form is dirty, asks for confirmation before
 * discarding. Also wires Escape-to-close (guarded) for the topmost modal only.
 *
 * Pass the raw `onClose` for programmatic closes (e.g. after a successful save);
 * use the returned `requestClose` for user-initiated closes (backdrop, X, Cancel).
 */
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

  // Keep the latest closer in a ref so the (stable) Escape listener always calls
  // the current logic without re-subscribing and reshuffling the stack order.
  const requestCloseRef = useRef(requestClose);
  useEffect(() => {
    requestCloseRef.current = requestClose;
  }, [requestClose]);

  // Stable per-mount token; Symbol() runs each render but useRef keeps the first.
  const tokenRef = useRef(Symbol('modal'));

  useEffect(() => {
    if (!enabled) return;
    const token = tokenRef.current;
    escapeStack.push(token);
    const handler = (e: KeyboardEvent) => {
      if (e.key !== 'Escape') return;
      if (escapeStack[escapeStack.length - 1] !== token) return; // not topmost
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
