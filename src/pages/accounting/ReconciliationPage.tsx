import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CheckCircle2, GitCompareArrows, XCircle } from 'lucide-react';
import { formatNumber } from '@/shared/lib/format';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { DetailPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { useReconciliationQuery } from '@/features/accounting/hooks/useFinancialStatementQueries';
import { useDecimalPlaces } from '@/features/settings/hooks/useSettingsQueries';
import type { ReconciliationLineDto } from '@/features/accounting/model/financialStatement.types';

const today = () => new Date().toISOString().slice(0, 10);

export const ReconciliationPage = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const decimals = useDecimalPlaces();
  const fmt = (n: number) => formatNumber(n, locale, decimals);

  const [asOf, setAsOf] = useState(today());

  const report = useReconciliationQuery(asOf);
  const data = report.data?.data ?? null;
  const lines = data?.lines ?? [];

  return (
    <DetailPageTemplate
      header={
        <PageHeader
          icon={<GitCompareArrows size={20} />}
          title={t('Accounting.Reconciliation.title', { defaultValue: 'Reconciliation' })}
          subtitle={t('Accounting.Reconciliation.subtitle', {
            defaultValue:
              'Control account balances in the general ledger compared against their subledger totals as of the selected date.',
          })}
          tone="indigo"
        />
      }
    >
      <div className="flex flex-wrap items-end gap-3">
        <div>
          <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
            {t('Accounting.Reconciliation.asOf', { defaultValue: 'As of' })}
          </label>
          <input
            type="date"
            value={asOf}
            onChange={(e) => setAsOf(e.target.value)}
            className="mt-1 rounded border border-slate-300 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-800"
          />
        </div>
        {data && (
          <div
            className={`ml-auto rounded-lg border px-3 py-1.5 text-xs font-semibold ${
              data.allReconciled
                ? 'border-success-200 bg-success-50 text-success-700 dark:border-success-500/30 dark:bg-success-500/10 dark:text-success-300'
                : 'border-danger-200 bg-danger-50 text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-300'
            }`}
          >
            {data.allReconciled ? (
              <CheckCircle2 className="mr-1 inline" size={11} />
            ) : (
              <XCircle className="mr-1 inline" size={11} />
            )}
            {data.allReconciled
              ? t('Accounting.Reconciliation.allReconciled', { defaultValue: 'All reconciled' })
              : t('Accounting.Reconciliation.notReconciled', {
                  defaultValue: 'Discrepancies found',
                })}
          </div>
        )}
      </div>

      {report.isPending ? (
        <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900">
          {t('Accounting.Reconciliation.computing', { defaultValue: 'Calculating…' })}
        </div>
      ) : !data || lines.length === 0 ? (
        <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900">
          {t('Accounting.Reconciliation.empty', {
            defaultValue: 'No control accounts to reconcile as of the selected date.',
          })}
        </div>
      ) : (
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('Accounting.Reconciliation.controlAccount', {
                    defaultValue: 'Control Account',
                  })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('Accounting.Reconciliation.subledger', { defaultValue: 'Subledger' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('Accounting.Reconciliation.glBalance', { defaultValue: 'GL Balance' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('Accounting.Reconciliation.subledgerBalance', {
                    defaultValue: 'Subledger Balance',
                  })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('Accounting.Reconciliation.variance', { defaultValue: 'Variance' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('Accounting.Reconciliation.reconciled', { defaultValue: 'Reconciled?' })}
                </th>
              </tr>
            </thead>
            <tbody>
              {lines.map((line) => (
                <Row key={`${line.controlCode}-${line.subledger}`} line={line} fmt={fmt} t={t} />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </DetailPageTemplate>
  );
};

const Row = ({
  line,
  fmt,
  t,
}: {
  line: ReconciliationLineDto;
  fmt: (n: number) => string;
  t: (key: string, options?: Record<string, unknown>) => string;
}) => (
  <tr className="border-t border-slate-100 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/30">
    <td className="px-3 py-2 text-xs text-slate-700 dark:text-slate-300">
      <span className="font-mono text-slate-400">{line.controlCode}</span> {line.controlName}
    </td>
    <td className="px-3 py-2 text-xs text-slate-700 dark:text-slate-300">{line.subledger}</td>
    <td className="px-3 py-2 text-right font-mono text-xs text-slate-800 dark:text-slate-200">
      {fmt(line.glBalance)}
    </td>
    <td className="px-3 py-2 text-right font-mono text-xs text-slate-800 dark:text-slate-200">
      {fmt(line.subledgerBalance)}
    </td>
    <td
      className={`px-3 py-2 text-right font-mono text-xs font-semibold ${
        line.isReconciled
          ? 'text-slate-800 dark:text-slate-200'
          : 'text-danger-600 dark:text-danger-400'
      }`}
    >
      {fmt(line.variance)}
    </td>
    <td className="px-3 py-2 text-center">
      <span
        className={`inline-flex items-center gap-1 rounded px-1.5 py-0.5 text-[10px] font-semibold ${
          line.isReconciled
            ? 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300'
            : 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300'
        }`}
      >
        {line.isReconciled ? <CheckCircle2 size={11} /> : <XCircle size={11} />}
        {line.isReconciled
          ? t('Accounting.Reconciliation.yes', { defaultValue: 'Yes' })
          : t('Accounting.Reconciliation.no', { defaultValue: 'No' })}
      </span>
    </td>
  </tr>
);

export default ReconciliationPage;
