import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Modal } from '@/shared/ui/Modal';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { useForwardDocument } from './forwardHooks';
import type { ForwardableDocumentType } from './forwardApi';

interface Props {
  open: boolean;
  onClose: () => void;
  documentType: ForwardableDocumentType;
  documentId: string;
  documentNumber: string;
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export const ForwardDocumentModal = ({
  open,
  onClose,
  documentType,
  documentId,
  documentNumber,
}: Props) => {
  const { t } = useTranslation();
  const [recipient, setRecipient] = useState('');
  const [touched, setTouched] = useState(false);
  const forward = useForwardDocument();

  const isValid = EMAIL_PATTERN.test(recipient.trim());

  const handleClose = () => {
    setRecipient('');
    setTouched(false);
    onClose();
  };

  const onSubmit = async () => {
    setTouched(true);
    if (!isValid) return;
    try {
      await forward.mutateAsync({
        documentType,
        documentId,
        recipientEmail: recipient.trim(),
        idempotencyKey: crypto.randomUUID(),
      });
      toast.success(t('forward.success', { email: recipient.trim() }));
      handleClose();
    } catch {
      toast.error(t('forward.error'));
    }
  };

  return (
    <Modal
      open={open}
      onClose={handleClose}
      title={t('forward.title')}
      description={t('forward.subtitle', { number: documentNumber })}
      footer={
        <>
          <Button variant="ghost" size="sm" onClick={handleClose}>
            {t('common.cancel')}
          </Button>
          <Button size="sm" onClick={onSubmit} disabled={forward.isPending || !isValid}>
            {forward.isPending ? t('forward.sending') : t('forward.send')}
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        <Input
          type="email"
          label={t('forward.recipient')}
          value={recipient}
          onChange={(e) => setRecipient(e.target.value)}
          placeholder="name@example.com"
          error={touched && !isValid ? t('forward.invalidEmail') : undefined}
        />
        <p className="text-xs text-slate-500 dark:text-slate-400">{t('forward.hint')}</p>
      </div>
    </Modal>
  );
};
