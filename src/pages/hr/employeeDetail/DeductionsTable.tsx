import { useTranslation } from 'react-i18next';
import { Plus } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { formatCurrency } from '@/shared/lib/format';
import type { EmployeeDeduction } from '@/features/hr/model/employee.types';
import { RowActions } from './EmployeeParts';

interface Props {
  deductions: EmployeeDeduction[];
  currency: string;
  locale: string;
  typeLabel: (d: EmployeeDeduction) => string;
  onAdd: () => void;
  onEdit: (d: EmployeeDeduction) => void;
  onDelete: (d: EmployeeDeduction) => void;
}

export const DeductionsTable = ({
  deductions,
  currency,
  locale,
  typeLabel,
  onAdd,
  onEdit,
  onDelete,
}: Props) => {
  const { t } = useTranslation();
  return (
    <div className="space-y-3">
      <div className="flex justify-end">
        <Button size="sm" onClick={onAdd}>
          <Plus size={14} />
          {t('Payroll.employeeDetail.addDeduction', { defaultValue: 'Kesinti Ekle' })}
        </Button>
      </div>
      {deductions.length === 0 ? (
        <div className="py-6 text-center text-sm text-slate-500">
          {t('Payroll.employeeDetail.deductionsEmpty', { defaultValue: 'Kesinti yok.' })}
        </div>
      ) : (
        <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('Payroll.deductionForm.type', { defaultValue: 'Tür' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('Payroll.deductionForm.amount', { defaultValue: 'Toplam' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('Payroll.deductionForm.remaining', { defaultValue: 'Kalan' })}
                </th>
                <th className="px-3 py-2" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {deductions.map((d) => (
                <tr key={d.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                  <td className="px-3 py-2 font-medium text-slate-800 dark:text-slate-100">
                    {typeLabel(d)}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-slate-800 dark:text-slate-200">
                    {d.amount !== null
                      ? formatCurrency(d.amount, locale, currency)
                      : d.percent !== null
                        ? `%${d.percent}`
                        : '—'}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-slate-600 dark:text-slate-300">
                    {formatCurrency(d.remainingBalance, locale, currency)}
                  </td>
                  <td className="px-3 py-2 text-right">
                    <RowActions
                      onEdit={() => onEdit(d)}
                      onDelete={() => onDelete(d)}
                      editLabel={t('common.edit', { defaultValue: 'Düzenle' })}
                      deleteLabel={t('common.delete', { defaultValue: 'Sil' })}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
