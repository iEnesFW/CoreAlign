import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Factory } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCreateWorkCenter, useUpdateWorkCenter } from '../hooks/useManufacturingQueries';
import type { WorkCenter } from '../model/manufacturing.types';

interface Props {
  onClose: () => void;
  workCenter?: WorkCenter;
}

export const WorkCenterFormModal = ({ onClose, workCenter }: Props) => {
  const { t } = useTranslation();
  const createMutation = useCreateWorkCenter();
  const updateMutation = useUpdateWorkCenter();
  const isEdit = Boolean(workCenter);

  const [code, setCode] = useState(workCenter?.code ?? '');
  const [name, setName] = useState(workCenter?.name ?? '');
  const [dailyCapacityMinutes, setDailyCapacityMinutes] = useState(
    workCenter ? String(workCenter.dailyCapacityMinutes) : '480',
  );
  const [isActive, setIsActive] = useState(workCenter?.isActive ?? true);
  const [submitting, setSubmitting] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!code.trim() || !name.trim()) {
      toast.error(t('Manufacturing.workCenterForm.codeNameRequired'));
      return;
    }

    setSubmitting(true);
    const capacity = Number(dailyCapacityMinutes) || 0;
    const result = await (
      isEdit
        ? updateMutation.mutateAsync({
            id: workCenter!.id,
            code: code.trim(),
            name: name.trim(),
            dailyCapacityMinutes: capacity,
            isActive,
          })
        : createMutation.mutateAsync({
            code: code.trim(),
            name: name.trim(),
            dailyCapacityMinutes: capacity,
          })
    ).catch((err) => {
      toastApiError(err);
      return null;
    });
    setSubmitting(false);

    if (result?.isSuccess) {
      toast.success(t('Manufacturing.workCenterForm.saved'));
      onClose();
    } else if (result && !result.isSuccess) {
      toast.error(result.errors?.[0] ?? t('Manufacturing.workCenterForm.failed'));
    }
  };

  return (
    <Modal
      open={true}
      title={t(
        isEdit ? 'Manufacturing.workCenterForm.editTitle' : 'Manufacturing.workCenterForm.title',
      )}
      icon={<Factory size={18} />}
      onClose={onClose}
      size="md"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('Manufacturing.actions.cancel')}
          </Button>
          <Button type="submit" form="work-center-form" isLoading={submitting}>
            {t('Manufacturing.actions.save')}
          </Button>
        </>
      }
    >
      <form id="work-center-form" onSubmit={submit} className="space-y-3">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Input
            label={t('Manufacturing.workCenter.code')}
            value={code}
            onChange={(e) => setCode(e.target.value)}
            maxLength={40}
            required
          />
          <Input
            label={t('Manufacturing.workCenter.name')}
            value={name}
            onChange={(e) => setName(e.target.value)}
            maxLength={200}
            required
          />
        </div>
        <Input
          label={t('Manufacturing.workCenter.dailyCapacity')}
          type="number"
          min={0}
          step="any"
          value={dailyCapacityMinutes}
          onChange={(e) => setDailyCapacityMinutes(e.target.value)}
        />
        {isEdit && (
          <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              className="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
            />
            {t('Manufacturing.workCenter.active')}
          </label>
        )}
      </form>
    </Modal>
  );
};
