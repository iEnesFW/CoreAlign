import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Coins } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useUpdateBaseSalary } from '../hooks/useEmployees';
import type { Employee } from '../model/employee.types';

interface Props {
  employee: Employee;
  onClose: () => void;
}

const todayIso = () => new Date().toISOString().slice(0, 10);

const fieldClass =
  'mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100';
const labelClass = 'block text-xs font-medium text-slate-700 dark:text-slate-300';

export const ChangeBaseSalaryModal = ({ employee, onClose }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const mutation = useUpdateBaseSalary();

  const [baseSalaryGross, setBaseSalaryGross] = useState(String(employee.baseSalaryGross));
  const [effectiveDate, setEffectiveDate] = useState(todayIso());

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const value = Number(baseSalaryGross);
    if (!Number.isFinite(value) || value <= 0) {
      toast.error(
        t('Payroll.changeSalary.invalidAmount', {
          defaultValue: 'Geçerli bir maaş girin.',
        }),
      );
      return;
    }
    try {
      await mutation.mutateAsync({
        id: employee.id,
        baseSalaryGross: value,
        effectiveDate,
      });
      toast.success(t('Payroll.changeSalary.success', { defaultValue: 'Maaş güncellendi.' }));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open
      onClose={onClose}
      size="md"
      icon={<Coins size={16} />}
      title={t('Payroll.changeSalary.title', { defaultValue: 'Maaş Değiştir' })}
      footer={
        <>
          <Button variant="outline" size="sm" type="button" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button size="sm" type="submit" form="change-salary-form" isLoading={mutation.isPending}>
            {t('Payroll.changeSalary.submit', { defaultValue: 'Kaydet' })}
          </Button>
        </>
      }
    >
      <form id="change-salary-form" onSubmit={submit} className="space-y-3">
        <div className="rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-sm dark:border-slate-800 dark:bg-slate-800/50">
          <div className="text-[10px] font-semibold uppercase text-slate-500">
            {t('Payroll.changeSalary.currentSalary', { defaultValue: 'Mevcut Brüt Maaş' })}
          </div>
          <div className="font-semibold text-slate-900 dark:text-slate-100">
            {formatCurrency(employee.baseSalaryGross, locale, employee.salaryCurrency)}
          </div>
        </div>
        <div>
          <label className={labelClass} htmlFor="change-salary-amount">
            {t('Payroll.changeSalary.newSalary', { defaultValue: 'Yeni Brüt Maaş' })}
          </label>
          <input
            id="change-salary-amount"
            type="number"
            min={0}
            step="any"
            value={baseSalaryGross}
            onChange={(e) => setBaseSalaryGross(e.target.value)}
            className={`${fieldClass} text-right`}
          />
        </div>
        <div>
          <label className={labelClass} htmlFor="change-salary-date">
            {t('Payroll.changeSalary.effectiveDate', { defaultValue: 'Geçerlilik Tarihi' })}
          </label>
          <input
            id="change-salary-date"
            type="date"
            value={effectiveDate}
            onChange={(e) => setEffectiveDate(e.target.value)}
            className={fieldClass}
          />
        </div>
      </form>
    </Modal>
  );
};
