import { useTranslation } from 'react-i18next';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import type { PayslipDeductionLine, PayslipEarningLine } from '../model/payroll.types';

interface Props {
  earningLines: PayslipEarningLine[];
  deductionLines: PayslipDeductionLine[];
  currency: string;
}

export const PayslipLinesTable = ({ earningLines, deductionLines, currency }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();

  if (earningLines.length === 0 && deductionLines.length === 0) {
    return (
      <div className="px-3 py-6 text-center text-sm text-slate-500">
        {t('Payroll.payslip.noLines', { defaultValue: 'Bordro kalemi yok.' })}
      </div>
    );
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
      <table className="w-full text-sm">
        <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
          <tr>
            <th className="px-3 py-2 text-left">
              {t('Payroll.payslip.description', { defaultValue: 'Açıklama' })}
            </th>
            <th className="px-3 py-2 text-left">
              {t('Payroll.payslip.category', { defaultValue: 'Kategori' })}
            </th>
            <th className="px-3 py-2 text-right">
              {t('Payroll.payslip.amount', { defaultValue: 'Tutar' })}
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
          {earningLines.map((line) => (
            <tr key={line.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
              <td className="px-3 py-2 text-slate-800 dark:text-slate-100">
                {t(`Payroll.componentType.${line.componentType}`, {
                  defaultValue: line.componentType,
                })}
              </td>
              <td className="px-3 py-2 text-xs font-medium text-success-700 dark:text-success-300">
                {t('Payroll.payslip.categoryLabel.Earning', { defaultValue: 'Kazanç' })}
              </td>
              <td className="px-3 py-2 text-right font-mono text-slate-800 dark:text-slate-200">
                {formatCurrency(line.amount, locale, currency)}
              </td>
            </tr>
          ))}
          {deductionLines.map((line) => (
            <tr key={line.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
              <td className="px-3 py-2 text-slate-800 dark:text-slate-100">
                {t(`Payroll.deductionType.${line.deductionType}`, {
                  defaultValue: line.deductionType,
                })}
              </td>
              <td className="px-3 py-2 text-xs font-medium text-danger-700 dark:text-danger-300">
                {t('Payroll.payslip.categoryLabel.Deduction', { defaultValue: 'Kesinti' })}
              </td>
              <td className="px-3 py-2 text-right font-mono text-slate-800 dark:text-slate-200">
                {formatCurrency(line.amount, locale, currency)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};
