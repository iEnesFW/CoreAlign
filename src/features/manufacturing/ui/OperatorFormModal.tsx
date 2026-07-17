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
  operator?: WorkCenterOperator & { pinCode?: string | null };
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
  const [pinCode, setPinCode] = useState(operator?.pinCode ?? '');
  const [submitting, setSubmitting] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!isEdit && (!workCenterId || !employeeId)) {
      toast.error(t('Manufacturing.operatorForm.workCenterEmployeeRequired'));
      return;
    }

    setSubmitting(true);
    const certified = certifiedOn.trim() || null;
    const pin = pinCode.trim() || null;
    const result = await (
      isEdit
        ? updateMutation.mutateAsync({
            id: operator!.id,
            qualificationLevel: level,
            isPrimary,
            isActive,
            certifiedOn: certified,
            notes: notes.trim() || null,
            pinCode: pin,
          })
        : createMutation.mutateAsync({
            workCenterId,
            employeeId,
            qualificationLevel: level,
            isPrimary,
            certifiedOn: certified,
            notes: notes.trim() || null,
            pinCode: pin,
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
      <form id="operator-form" onSubmit={submit} className="space-y-5 px-1 pb-2">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
          <Select
            label={t('Manufacturing.operator.workCenter')}
            value={workCenterId}
            onChange={(e) => setWorkCenterId(e.target.value)}
            disabled={isEdit}
            required
            className="rounded-xl shadow-sm border-slate-200 focus:border-indigo-500 focus:ring-indigo-500/20"
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
            className="rounded-xl shadow-sm border-slate-200 focus:border-indigo-500 focus:ring-indigo-500/20"
          >
            <option value="">{t('Manufacturing.operatorForm.selectEmployee')}</option>
            {employees.map((emp) => (
              <option key={emp.id} value={emp.id}>
                {emp.fullName} ({emp.employeeNumber})
              </option>
            ))}
          </Select>
        </div>

        <div className="grid grid-cols-1 gap-5 sm:grid-cols-3">
          <Select
            label={t('Manufacturing.operator.level')}
            value={level}
            onChange={(e) => setLevel(e.target.value as OperatorQualificationLevel)}
            className="rounded-xl shadow-sm border-slate-200 focus:border-indigo-500 focus:ring-indigo-500/20"
          >
            {LEVELS.map((l) => (
              <option key={l} value={l}>
                {t(`Manufacturing.qualificationLevel.${l}`)}
              </option>
            ))}
          </Select>

          <div className="relative">
            <Input
              label={t('Manufacturing.operator.certifiedOn')}
              type="date"
              value={certifiedOn}
              onChange={(e) => setCertifiedOn(e.target.value)}
              className="rounded-xl shadow-sm border-slate-200 focus:border-indigo-500 focus:ring-indigo-500/20"
            />
          </div>

          <div className="relative">
            <Input
              label={t('Manufacturing.operator.pinCode')}
              type="text"
              placeholder="e.g. 1234"
              maxLength={10}
              value={pinCode}
              onChange={(e) => setPinCode(e.target.value)}
              className="rounded-xl shadow-sm border-slate-200 focus:border-indigo-500 focus:ring-indigo-500/20 font-mono tracking-wider"
            />
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-6 p-4 rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-100 dark:border-slate-700/50">
          <label className="flex items-center gap-3 text-sm font-medium text-slate-700 dark:text-slate-300 cursor-pointer">
            <div className="relative flex items-center">
              <input
                type="checkbox"
                checked={isPrimary}
                onChange={(e) => setIsPrimary(e.target.checked)}
                className="peer h-5 w-5 rounded-md border-slate-300 text-indigo-600 focus:ring-indigo-500 transition-all"
              />
            </div>
            {t('Manufacturing.operator.primary')}
          </label>

          {isEdit && (
            <label className="flex items-center gap-3 text-sm font-medium text-slate-700 dark:text-slate-300 cursor-pointer">
              <div className="relative flex items-center">
                <input
                  type="checkbox"
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="peer h-5 w-5 rounded-md border-slate-300 text-emerald-600 focus:ring-emerald-500 transition-all"
                />
              </div>
              {t('Manufacturing.operator.active')}
            </label>
          )}
        </div>

        <div className="relative">
          <Input
            label={t('Manufacturing.operator.notes')}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            maxLength={500}
            className="rounded-xl shadow-sm border-slate-200 focus:border-indigo-500 focus:ring-indigo-500/20"
          />
        </div>
      </form>
    </Modal>
  );
};
