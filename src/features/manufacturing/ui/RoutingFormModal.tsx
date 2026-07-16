import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Workflow } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCreateRouting, useUpdateRouting } from '../hooks/useManufacturingQueries';
import type { ProductionRoutingSummary } from '../model/manufacturing.types';

interface Props {
  onClose: () => void;
  routing?: ProductionRoutingSummary;
}

export const RoutingFormModal = ({ onClose, routing }: Props) => {
  const { t } = useTranslation();
  const createMutation = useCreateRouting();
  const updateMutation = useUpdateRouting();
  const isEdit = Boolean(routing);

  const [code, setCode] = useState(routing?.code ?? '');
  const [name, setName] = useState(routing?.name ?? '');
  const [description, setDescription] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!code.trim() || !name.trim()) {
      toast.error(t('Manufacturing.routingForm.codeNameRequired'));
      return;
    }

    setSubmitting(true);
    const payload = {
      code: code.trim(),
      name: name.trim(),
      description: description.trim() || null,
    };
    const result = await (
      isEdit
        ? updateMutation.mutateAsync({ id: routing!.id, ...payload })
        : createMutation.mutateAsync(payload)
    ).catch((err) => {
      toastApiError(err);
      return null;
    });
    setSubmitting(false);

    if (result?.isSuccess) {
      toast.success(t('Manufacturing.routingForm.saved'));
      onClose();
    } else if (result && !result.isSuccess) {
      toast.error(result.errors?.[0] ?? t('Manufacturing.routingForm.failed'));
    }
  };

  return (
    <Modal
      open={true}
      title={t(isEdit ? 'Manufacturing.routingForm.editTitle' : 'Manufacturing.routingForm.title')}
      icon={<Workflow size={18} />}
      onClose={onClose}
      size="md"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('Manufacturing.actions.cancel')}
          </Button>
          <Button type="submit" form="routing-form" isLoading={submitting}>
            {t('Manufacturing.actions.save')}
          </Button>
        </>
      }
    >
      <form id="routing-form" onSubmit={submit} className="space-y-3">
        <Input
          label={t('Manufacturing.routing.code')}
          value={code}
          onChange={(e) => setCode(e.target.value)}
          maxLength={40}
          required
        />
        <Input
          label={t('Manufacturing.routing.name')}
          value={name}
          onChange={(e) => setName(e.target.value)}
          maxLength={200}
          required
        />
        <Input
          label={t('Manufacturing.routing.description')}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          maxLength={1000}
        />
        {isEdit && (
          <p className="text-xs text-slate-500">{t('Manufacturing.routingForm.editHint')}</p>
        )}
      </form>
    </Modal>
  );
};
