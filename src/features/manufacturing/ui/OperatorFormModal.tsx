import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { UserCog } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useEmployeesQuery } from '@/features/hr/hooks/useEmployees';
import {
  useCreateOperator,
  useUpdateOperator,
  useWorkCentersQuery,
} from '../hooks/useManufacturingQueries';
import type { OperatorQualificationLevel, WorkCenterOperator } from '../model/manufacturing.types';

interface Props {
  onClose: () => void;
  operator?: WorkCenterOperator;
}

const LEVELS: OperatorQualificationLevel[] = ['Trainee', 'Qualified', 'Expert'];

export const OperatorFormModal = ({ onClose, operator }: Props) => {
  const { t } = useTranslation();
  const workCentersQuery = useWorkCentersQuery(false);
  const employeesQuery = useEmployeesQuery({ page: 1, pageSize: 200 });
  const createMutation = useCreateOperator();
  const updateMutation = useUpdateOperator();
  const isEdit = Boolean(operator);

  const workCenters = workCentersQuery.data ?? [];
  const employees = employeesQuery.data?.data?.items ?? [];

  const [workCenterId, setWorkCenterId] = useState(operator?.workCenterId ?? '');
  const [employeeId, setEmployeeId] = useState(operator?.employeeId ?? '');
  const [level, setLevel] = useState<OperatorQualificationLevel>(
    operator?.qualificationLevel ?? 'Qualified',
  );
  const [isPrimary, setIsPrimary] = useState(operator?.isPrimary ?? false);
  const [isActive, setIsActive] = useState(operator?.isActive ?? true);
  const [certifiedOn, setCertifiedOn] = useState(operator?.certifiedOn ?? '');
  const [notes, setNotes] = useState(operator?.notes ?? '');
  const [submitting, setSubmitting] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!isEdit && (!workCenterId || !employeeId)) {
      toast.error(t('Manufacturing.operatorForm.workCenterEmployeeRequired'));
      return;
    }

    setSubmitting(true);
    const certified = certifiedOn.trim() || null;
    const result = await (
      isEdit
        ? updateMutation.mutateAsync({
            id: operator!.id,
            qualificationLevel: level,
            isPrimary,
            isActive,
            certifiedOn: certified,
            notes: notes.trim() || null,
          })
        : createMutation.mutateAsync({
            workCenterId,
            employeeId,
            qualificationLevel: level,
            isPrimary,
            certifiedOn: certified,
            notes: notes.trim() || null,
          })
    ).catch((err) => {
      toastApiError(err);
      return null;
    });
    setSubmitting(false);

    if (result?.isSuccess) {
      toast.success(t('Manufacturing.operatorForm.saved'));
      onClose();
    } else if (result && !result.isSuccess) {
      toast.error(result.errors?.[0] ?? t('Manufacturing.operatorForm.failed'));
    }
  };

  return (
    <Modal
      open={true}
      title={t(
        isEdit ? 'Manufacturing.operatorForm.editTitle' : 'Manufacturing.operatorForm.title',
      )}
      icon={<UserCog size={18} />}
      onClose={onClose}
      size="md"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('Manufacturing.actions.cancel')}
          </Button>
          <Button type="submit" form="operator-form" isLoading={submitting}>
            {t('Manufacturing.actions.save')}
          </Button>
        </>
      }
    >
      <form id="operator-form" onSubmit={submit} className="space-y-3">
        <Select
          label={t('Manufacturing.operator.workCenter')}
          value={workCenterId}
          onChange={(e) => setWorkCenterId(e.target.value)}
          disabled={isEdit}
          required
        >
          <option value="">{t('Manufacturing.operatorForm.selectWorkCenter')}</option>
          {workCenters.map((w) => (
            <option key={w.id} value={w.id}>
              {w.code} — {w.name}
            </option>
          ))}
        </Select>
        <Select
          label={t('Manufacturing.operator.employee')}
          value={employeeId}
          onChange={(e) => setEmployeeId(e.target.value)}
          disabled={isEdit}
          required
        >
          <option value="">{t('Manufacturing.operatorForm.selectEmployee')}</option>
          {employees.map((emp) => (
            <option key={emp.id} value={emp.id}>
              {emp.fullName} ({emp.employeeNumber})
            </option>
          ))}
        </Select>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Select
            label={t('Manufacturing.operator.level')}
            value={level}
            onChange={(e) => setLevel(e.target.value as OperatorQualificationLevel)}
          >
            {LEVELS.map((l) => (
              <option key={l} value={l}>
                {t(`Manufacturing.qualificationLevel.${l}`)}
              </option>
            ))}
          </Select>
          <Input
            label={t('Manufacturing.operator.certifiedOn')}
            type="date"
            value={certifiedOn}
            onChange={(e) => setCertifiedOn(e.target.value)}
          />
        </div>
        <div className="flex flex-wrap gap-4">
          <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={isPrimary}
              onChange={(e) => setIsPrimary(e.target.checked)}
              className="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
            />
            {t('Manufacturing.operator.primary')}
          </label>
          {isEdit && (
            <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
              <input
                type="checkbox"
                checked={isActive}
                onChange={(e) => setIsActive(e.target.checked)}
                className="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
              />
              {t('Manufacturing.operator.active')}
            </label>
          )}
        </div>
        <Input
          label={t('Manufacturing.operator.notes')}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          maxLength={500}
        />
      </form>
    </Modal>
  );
};
