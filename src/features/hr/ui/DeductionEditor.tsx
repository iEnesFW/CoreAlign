import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { MinusCircle } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useAddDeduction, useUpdateDeduction } from '../hooks/useEmployees';
import { DEDUCTION_TYPES, type DeductionType } from '../model/enums';
import type { EmployeeDeduction } from '../model/employee.types';

interface Props {
  employeeId: string;
  deduction: EmployeeDeduction | null;
  onClose: () => void;
}

const todayIso = () => new Date().toISOString().slice(0, 10);

const fieldClass =
  'mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100';
const labelClass = 'block text-xs font-medium text-slate-700 dark:text-slate-300';

export const DeductionEditor = ({ employeeId, deduction, onClose }: Props) => {
  const { t } = useTranslation();
  const isEdit = deduction !== null;

  const addMutation = useAddDeduction();
  const updateMutation = useUpdateDeduction();

  const [deductionType, setDeductionType] = useState<DeductionType>(
    deduction?.deductionType ?? 'Advance',
  );
  const [amount, setAmount] = useState(
    deduction && deduction.amount !== null ? String(deduction.amount) : '',
  );
  const [percent, setPercent] = useState(
    deduction && deduction.percent !== null ? String(deduction.percent) : '',
  );
  const [remainingBalance, setRemainingBalance] = useState(
    deduction ? String(deduction.remainingBalance) : '',
  );
  const [priority, setPriority] = useState(deduction ? String(deduction.priority) : '0');
  const [effectiveFrom, setEffectiveFrom] = useState(
    deduction?.effectiveFrom?.slice(0, 10) ?? todayIso(),
  );
  const [effectiveTo, setEffectiveTo] = useState(deduction?.effectiveTo?.slice(0, 10) ?? '');

  const pending = addMutation.isPending || updateMutation.isPending;

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const amountValue = amount === '' ? null : Number(amount);
    const percentValue = percent === '' ? null : Number(percent);
    if (amountValue === null && percentValue === null) {
      toast.error(
        t('Payroll.deductionForm.amountOrPercentRequired', {
          defaultValue: 'Tutar veya yüzde girin.',
        }),
      );
      return;
    }
    const base = {
      id: employeeId,
      deductionType,
      effectiveFrom,
      amount: amountValue,
      percent: percentValue,
      remainingBalance: Number(remainingBalance) || 0,
      priority: Number(priority) || 0,
      effectiveTo: effectiveTo || null,
    };
    try {
      if (isEdit && deduction) {
        await updateMutation.mutateAsync({ ...base, deductionId: deduction.id });
        toast.success(t('Payroll.deductionForm.updated', { defaultValue: 'Kesinti güncellendi.' }));
      } else {
        await addMutation.mutateAsync(base);
        toast.success(t('Payroll.deductionForm.created', { defaultValue: 'Kesinti eklendi.' }));
      }
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open
      onClose={onClose}
      size="lg"
      icon={<MinusCircle size={16} />}
      title={
        isEdit
          ? t('Payroll.deductionForm.editTitle', { defaultValue: 'Kesintiyi Düzenle' })
          : t('Payroll.deductionForm.newTitle', { defaultValue: 'Kesinti Ekle' })
      }
      footer={
        <>
          <Button variant="outline" size="sm" type="button" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button size="sm" type="submit" form="deduction-form" isLoading={pending}>
            {t('common.save', { defaultValue: 'Kaydet' })}
          </Button>
        </>
      }
    >
      <form id="deduction-form" onSubmit={submit} className="space-y-3">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div className="sm:col-span-2">
            <label className={labelClass}>
              {t('Payroll.deductionForm.type', { defaultValue: 'Tür' })}
            </label>
            <select
              value={deductionType}
              onChange={(e) => setDeductionType(e.target.value as DeductionType)}
              className={fieldClass}
            >
              {DEDUCTION_TYPES.map((ty) => (
                <option key={ty} value={ty}>
                  {t(`Payroll.deductionType.${ty}`, { defaultValue: ty })}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.deductionForm.amount', { defaultValue: 'Tutar' })}
            </label>
            <input
              type="number"
              min={0}
              step="any"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              className={`${fieldClass} text-right`}
            />
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.deductionForm.percent', { defaultValue: 'Yüzde %' })}
            </label>
            <input
              type="number"
              min={0}
              max={100}
              step="any"
              value={percent}
              onChange={(e) => setPercent(e.target.value)}
              className={`${fieldClass} text-right`}
            />
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.deductionForm.remaining', { defaultValue: 'Kalan Bakiye' })}
            </label>
            <input
              type="number"
              min={0}
              step="any"
              value={remainingBalance}
              onChange={(e) => setRemainingBalance(e.target.value)}
              className={`${fieldClass} text-right`}
            />
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.deductionForm.priority', { defaultValue: 'Öncelik' })}
            </label>
            <input
              type="number"
              min={0}
              step="1"
              value={priority}
              onChange={(e) => setPriority(e.target.value)}
              className={`${fieldClass} text-right`}
            />
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.deductionForm.effectiveFrom', { defaultValue: 'Başlangıç' })}
            </label>
            <input
              type="date"
              value={effectiveFrom}
              onChange={(e) => setEffectiveFrom(e.target.value)}
              className={fieldClass}
            />
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.deductionForm.effectiveTo', { defaultValue: 'Bitiş' })}
            </label>
            <input
              type="date"
              value={effectiveTo}
              onChange={(e) => setEffectiveTo(e.target.value)}
              className={fieldClass}
            />
          </div>
        </div>
      </form>
    </Modal>
  );
};
