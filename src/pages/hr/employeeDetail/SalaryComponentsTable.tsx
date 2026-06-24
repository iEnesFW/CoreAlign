import { useTranslation } from 'react-i18next';
import { Plus } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import type { SalaryComponent } from '@/features/hr/model/employee.types';
import { RowActions } from './EmployeeParts';

interface Props {
  components: SalaryComponent[];
  currency: string;
  locale: string;
  typeLabel: (c: SalaryComponent) => string;
  onAdd: () => void;
  onEdit: (c: SalaryComponent) => void;
  onDelete: (c: SalaryComponent) => void;
}

export const SalaryComponentsTable = ({
  components,
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
          {t('Payroll.employeeDetail.addComponent', { defaultValue: 'Bileşen Ekle' })}
        </Button>
      </div>
      {components.length === 0 ? (
        <div className="py-6 text-center text-sm text-slate-500">
          {t('Payroll.employeeDetail.componentsEmpty', { defaultValue: 'Maaş bileşeni yok.' })}
        </div>
      ) : (
        <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('Payroll.componentForm.type', { defaultValue: 'Tür' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('Payroll.componentForm.amount', { defaultValue: 'Tutar' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('Payroll.componentForm.taxable', { defaultValue: 'Vergiye Tabi' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('Payroll.componentForm.effectiveFrom', {
                    defaultValue: 'Geçerlilik Başlangıcı',
                  })}
                </th>
                <th className="px-3 py-2" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {components.map((c) => (
                <tr key={c.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                  <td className="px-3 py-2 font-medium text-slate-800 dark:text-slate-100">
                    {typeLabel(c)}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-slate-800 dark:text-slate-200">
                    {formatCurrency(c.amount, locale, currency)}
                  </td>
                  <td className="px-3 py-2 text-center">{c.taxExempt ? '—' : '✓'}</td>
                  <td className="px-3 py-2 text-center text-xs text-slate-500 dark:text-slate-400">
                    {formatDate(c.effectiveFrom, locale)}
                  </td>
                  <td className="px-3 py-2 text-right">
                    <RowActions
                      onEdit={() => onEdit(c)}
                      onDelete={() => onDelete(c)}
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
