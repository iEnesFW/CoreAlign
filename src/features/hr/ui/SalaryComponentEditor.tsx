import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Coins } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useAddSalaryComponent, useUpdateSalaryComponent } from '../hooks/useEmployees';
import { SALARY_COMPONENT_TYPES, type SalaryComponentType } from '../model/enums';
import type { SalaryComponent } from '../model/employee.types';

interface Props {
  employeeId: string;
  component: SalaryComponent | null;
  onClose: () => void;
}

const todayIso = () => new Date().toISOString().slice(0, 10);

const fieldClass =
  'mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100';
const labelClass = 'block text-xs font-medium text-slate-700 dark:text-slate-300';

export const SalaryComponentEditor = ({ employeeId, component, onClose }: Props) => {
  const { t } = useTranslation();
  const isEdit = component !== null;

  const addMutation = useAddSalaryComponent();
  const updateMutation = useUpdateSalaryComponent();

  const [componentType, setComponentType] = useState<SalaryComponentType>(
    component?.componentType ?? 'Bonus',
  );
  const [amount, setAmount] = useState(component ? String(component.amount) : '');
  const [isRecurring, setIsRecurring] = useState(component?.isRecurring ?? true);
  const [taxExempt, setTaxExempt] = useState(component?.taxExempt ?? false);
  const [sgkExempt, setSgkExempt] = useState(component?.sgkExempt ?? false);
  const [effectiveFrom, setEffectiveFrom] = useState(
    component?.effectiveFrom?.slice(0, 10) ?? todayIso(),
  );
  const [effectiveTo, setEffectiveTo] = useState(component?.effectiveTo?.slice(0, 10) ?? '');

  const pending = addMutation.isPending || updateMutation.isPending;

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!(Number(amount) > 0)) {
      toast.error(
        t('Payroll.componentForm.amountRequired', { defaultValue: 'Geçerli bir tutar girin.' }),
      );
      return;
    }
    const base = {
      id: employeeId,
      componentType,
      amount: Number(amount),
      effectiveFrom,
      isRecurring,
      taxExempt,
      sgkExempt,
      effectiveTo: effectiveTo || null,
    };
    try {
      if (isEdit && component) {
        await updateMutation.mutateAsync({ ...base, componentId: component.id });
        toast.success(t('Payroll.componentForm.updated', { defaultValue: 'Bileşen güncellendi.' }));
      } else {
        await addMutation.mutateAsync(base);
        toast.success(t('Payroll.componentForm.created', { defaultValue: 'Bileşen eklendi.' }));
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
      icon={<Coins size={16} />}
      title={
        isEdit
          ? t('Payroll.componentForm.editTitle', { defaultValue: 'Maaş Bileşenini Düzenle' })
          : t('Payroll.componentForm.newTitle', { defaultValue: 'Maaş Bileşeni Ekle' })
      }
      footer={
        <>
          <Button variant="outline" size="sm" type="button" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button size="sm" type="submit" form="component-form" isLoading={pending}>
            {t('common.save', { defaultValue: 'Kaydet' })}
          </Button>
        </>
      }
    >
      <form id="component-form" onSubmit={submit} className="space-y-3">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div className="sm:col-span-2">
            <label className={labelClass}>
              {t('Payroll.componentForm.type', { defaultValue: 'Tür' })}
            </label>
            <select
              value={componentType}
              onChange={(e) => setComponentType(e.target.value as SalaryComponentType)}
              className={fieldClass}
            >
              {SALARY_COMPONENT_TYPES.map((ty) => (
                <option key={ty} value={ty}>
                  {t(`Payroll.componentType.${ty}`, { defaultValue: ty })}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.componentForm.amount', { defaultValue: 'Tutar' })} *
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
              {t('Payroll.componentForm.effectiveFrom', { defaultValue: 'Geçerlilik Başlangıcı' })}
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
              {t('Payroll.componentForm.effectiveTo', { defaultValue: 'Geçerlilik Bitişi' })}
            </label>
            <input
              type="date"
              value={effectiveTo}
              onChange={(e) => setEffectiveTo(e.target.value)}
              className={fieldClass}
            />
          </div>
        </div>
        <div className="flex flex-wrap gap-4 text-xs text-slate-700 dark:text-slate-300">
          <label className="inline-flex items-center gap-1.5">
            <input
              type="checkbox"
              checked={isRecurring}
              onChange={(e) => setIsRecurring(e.target.checked)}
            />
            {t('Payroll.componentForm.recurring', { defaultValue: 'Tekrarlayan' })}
          </label>
          <label className="inline-flex items-center gap-1.5">
            <input
              type="checkbox"
              checked={taxExempt}
              onChange={(e) => setTaxExempt(e.target.checked)}
            />
            {t('Payroll.componentForm.taxExempt', { defaultValue: 'Gelir Vergisinden Muaf' })}
          </label>
          <label className="inline-flex items-center gap-1.5">
            <input
              type="checkbox"
              checked={sgkExempt}
              onChange={(e) => setSgkExempt(e.target.checked)}
            />
            {t('Payroll.componentForm.sgkExempt', { defaultValue: "SGK'dan Muaf" })}
          </label>
        </div>
      </form>
    </Modal>
  );
};
