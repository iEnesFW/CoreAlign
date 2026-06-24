import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Modal } from '@/shared/ui/Modal/Modal';
import { useSubmitTemplateMutation } from '../hooks/useMarketplace';

interface SubmitTemplateModalProps {
  open: boolean;
  onClose: () => void;
  onSubmitted?: () => void;
}

export const SubmitTemplateModal = ({ open, onClose, onSubmitted }: SubmitTemplateModalProps) => {
  const { t } = useTranslation();
  const mutation = useSubmitTemplateMutation();
  const [tenantTemplateId, setTenantTemplateId] = useState('');

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!tenantTemplateId.trim()) {
      toast.error(t('Marketplace.Submit.TenantTemplateIdRequired', 'Template id is required'));
      return;
    }
    try {
      await mutation.mutateAsync({ tenantTemplateId: tenantTemplateId.trim() });
      toast.success(t('Marketplace.Submit.Submitted', 'Submission sent for review'));
      setTenantTemplateId('');
      onSubmitted?.();
      onClose();
    } catch {
      toast.error(t('Marketplace.Submit.Failed', 'Failed to submit template'));
    }
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={t('Marketplace.Submit.Title', 'Submit a template')}
      subtitle={t(
        'Marketplace.Submit.Subtitle',
        'Share your private template with the community after approval.',
      )}
      size="md"
    >
      <form onSubmit={handleSubmit} className="space-y-3">
        <label className="block text-sm">
          <span className="text-slate-700 dark:text-slate-200">
            {t('Marketplace.Submit.TenantTemplateId', 'Template id')}
          </span>
          <input
            type="text"
            value={tenantTemplateId}
            onChange={(event) => setTenantTemplateId(event.target.value)}
            placeholder="00000000-0000-0000-0000-000000000000"
            className="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm focus:border-success-500 focus:outline-none dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
          />
        </label>
        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded-md border border-slate-300 px-4 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            {t('Marketplace.Submit.Cancel', 'Cancel')}
          </button>
          <button
            type="submit"
            disabled={mutation.isPending}
            className="rounded-md bg-success-600 px-4 py-1.5 text-sm font-semibold text-white hover:bg-success-700 disabled:opacity-50"
          >
            {mutation.isPending
              ? t('Marketplace.Submit.Submitting', 'Submitting...')
              : t('Marketplace.Submit.Submit', 'Submit')}
          </button>
        </div>
      </form>
    </Modal>
  );
};
