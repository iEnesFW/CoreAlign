import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { MessageSquare } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Textarea } from '@/shared/ui/Textarea/Textarea';

interface Props {
  title: string;
  confirmLabel: string;
  confirmTone?: 'rose' | 'slate';
  isSubmitting?: boolean;
  onConfirm: (reason: string | null) => void;
  onCancel: () => void;
}

export const ReasonPromptDialog = ({
  title,
  confirmLabel,
  confirmTone = 'rose',
  isSubmitting = false,
  onConfirm,
  onCancel,
}: Props) => {
  const { t } = useTranslation();
  const [reason, setReason] = useState<string>('');

  return (
    <Modal
      open
      title={title}
      icon={<MessageSquare size={18} />}
      onClose={onCancel}
      size="md"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onCancel}>
            {t('Common.Cancel')}
          </Button>
          <Button
            type="button"
            variant={confirmTone === 'rose' ? 'danger' : 'secondary'}
            isLoading={isSubmitting}
            onClick={() => onConfirm(reason.trim() ? reason.trim() : null)}
          >
            {confirmLabel}
          </Button>
        </>
      }
    >
      <Textarea
        label={t('Mrp.Requisition.ReasonPrompt')}
        value={reason}
        onChange={(e) => setReason(e.target.value)}
        rows={3}
      />
    </Modal>
  );
};
