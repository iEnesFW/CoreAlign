import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Lock, Pencil, Plus, SlidersHorizontal } from 'lucide-react';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Badge } from '@/shared/ui/Badge/Badge';
import { ParametersForm } from '@/features/hr/ui/ParametersForm';
import {
  usePayrollParametersDetailQuery,
  usePayrollParametersQuery,
} from '@/features/hr/hooks/usePayrollParameters';
import type { PayrollParameters } from '@/features/hr/model/parameters.types';

export const PayrollParametersPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();

  const [editId, setEditId] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);

  const listQuery = usePayrollParametersQuery();
  const detailQuery = usePayrollParametersDetailQuery(editId);

  const items = listQuery.data?.data ?? [];
  const editTarget: PayrollParameters | null = editId ? (detailQuery.data?.data ?? null) : null;

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<SlidersHorizontal size={20} />}
          title={t('Payroll.parameters.title', { defaultValue: 'Bordro Parametreleri' })}
          subtitle={t('Payroll.parameters.subtitle', {
            defaultValue:
              'SGK, işsizlik, damga ve gelir vergisi oranları ile asgari ücret değerlerini dönemsel olarak tanımlayın.',
          })}
          actions={
            <Button size="sm" onClick={() => setShowCreate(true)}>
              <Plus size={14} />
              {t('Payroll.parameters.new', { defaultValue: 'Yeni Set' })}
            </Button>
          }
        />
      }
    >
      <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
        {listQuery.isPending ? (
          <div className="px-3 py-8 text-center text-sm text-slate-500">
            {t('common.loading', { defaultValue: 'Yükleniyor…' })}
          </div>
        ) : items.length === 0 ? (
          <div className="px-3 py-10 text-center text-sm text-slate-500 dark:text-slate-400">
            {t('Payroll.parameters.empty', { defaultValue: 'Parametre seti bulunamadı.' })}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('Payroll.parameters.cols.name', { defaultValue: 'Set Adı' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('Payroll.parameters.cols.effectiveFrom', { defaultValue: 'Geçerlilik' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('Payroll.parameters.cols.minWage', { defaultValue: 'Asgari Ücret' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('Payroll.parameters.cols.scope', { defaultValue: 'Kapsam' })}
                </th>
                <th className="px-3 py-2" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {items.map((p) => (
                <tr key={p.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                  <td className="px-3 py-2 font-medium text-slate-800 dark:text-slate-100">
                    {p.description ?? String(p.effectiveYear)}
                  </td>
                  <td className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">
                    {formatDate(p.effectiveFrom, locale)}
                    {p.effectiveTo ? ` — ${formatDate(p.effectiveTo, locale)}` : ''}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-slate-800 dark:text-slate-200">
                    {formatCurrency(p.grossMinimumWage, locale)}
                  </td>
                  <td className="px-3 py-2 text-center">
                    {p.isGlobal ? (
                      <Badge variant="info">
                        {t('Payroll.parameters.system', { defaultValue: 'Sistem' })}
                      </Badge>
                    ) : (
                      <Badge variant="neutral">
                        {t('Payroll.parameters.tenant', { defaultValue: 'Kuruluş' })}
                      </Badge>
                    )}
                  </td>
                  <td className="px-3 py-2 text-right">
                    {p.isGlobal ? (
                      <span
                        className="inline-flex items-center gap-1 text-[11px] text-slate-400"
                        title={t('Payroll.parameters.readOnly', { defaultValue: 'Salt okunur' })}
                      >
                        <Lock size={12} />
                        {t('Payroll.parameters.readOnly', { defaultValue: 'Salt okunur' })}
                      </span>
                    ) : (
                      <button
                        type="button"
                        onClick={() => setEditId(p.id)}
                        className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800 dark:hover:text-slate-200"
                        title={t('common.edit', { defaultValue: 'Düzenle' })}
                      >
                        <Pencil size={13} />
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {showCreate && <ParametersForm parameters={null} onClose={() => setShowCreate(false)} />}
      {editId && editTarget && (
        <ParametersForm parameters={editTarget} onClose={() => setEditId(null)} />
      )}
    </ListPageTemplate>
  );
};

export default PayrollParametersPage;
