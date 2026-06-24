import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Calculator, FileDown } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { DetailPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { useTrialBalanceQuery } from '@/features/accounting/hooks/useJournalEntryQueries';
import type { AccountType } from '@/features/accounting/model/glAccount.types';

const TYPE_STYLES: Record<AccountType, string> = {
  Asset: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
  Liability: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
  Equity: 'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300',
  Revenue: 'bg-info-100 text-info-700 dark:bg-info-500/20 dark:text-info-300',
  Expense: 'bg-warning-100 text-warning-800 dark:bg-warning-500/20 dark:text-warning-300',
  CostOfGoodsSold: 'bg-warning-100 text-warning-700 dark:bg-warning-500/20 dark:text-warning-300',
  Memorandum: 'bg-slate-200 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
};

const fmtMoney = (n: number, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(n);
  } catch {
    return n.toFixed(2);
  }
};

const yearStart = (year: number) => `${year}-01-01`;
const yearEnd = (year: number) => `${year}-12-31`;
const currentYear = () => new Date().getFullYear();

export const TrialBalancePage = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;

  const [year, setYear] = useState(currentYear());
  const [fromDate, setFromDate] = useState(yearStart(currentYear()));
  const [toDate, setToDate] = useState(yearEnd(currentYear()));

  const params = useMemo(() => ({ fromDate, toDate }), [fromDate, toDate]);
  const report = useTrialBalanceQuery(params);
  const data = report.data?.data;

  const rows = data?.rows ?? [];
  const totalDebit = data?.totalDebit ?? 0;
  const totalCredit = data?.totalCredit ?? 0;
  const isBalanced = Math.abs(totalDebit - totalCredit) < 0.005;

  const exportCsv = () => {
    if (!data) return;
    const headers = [
      t('TrialBalance.colCode', { defaultValue: 'Kod' }),
      t('TrialBalance.colAccountName', { defaultValue: 'Hesap Adı' }),
      t('TrialBalance.colType', { defaultValue: 'Tip' }),
      t('TrialBalance.colDebit', { defaultValue: 'Borç' }),
      t('TrialBalance.colCredit', { defaultValue: 'Alacak' }),
      t('TrialBalance.colBalance', { defaultValue: 'Bakiye' }),
    ];
    const lines = [headers.join(',')];
    for (const r of rows) {
      lines.push(
        [
          r.accountCode,
          `"${r.accountName.replace(/"/g, '""')}"`,
          r.type,
          r.debit.toFixed(2),
          r.credit.toFixed(2),
          r.balance.toFixed(2),
        ].join(','),
      );
    }
    lines.push(
      [
        '',
        t('TrialBalance.totalUpper', { defaultValue: 'TOPLAM' }),
        '',
        totalDebit.toFixed(2),
        totalCredit.toFixed(2),
        '',
      ].join(','),
    );
    const blob = new Blob(['﻿' + lines.join('\n')], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `mizan_${fromDate}_${toDate}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <DetailPageTemplate
      header={
        <PageHeader
          icon={<Calculator size={20} />}
          title={t('TrialBalance.title', { defaultValue: 'Mizan (Trial Balance)' })}
          subtitle={t('TrialBalance.subtitle', {
            defaultValue:
              'Belirtilen tarih aralığındaki post edilmiş yevmiye fişlerinden hesaplanan hesap bakiyeleri.',
          })}
          actions={
            <Button
              size="sm"
              variant="outline"
              onClick={exportCsv}
              disabled={!data || rows.length === 0}
            >
              <FileDown size={14} />
              {t('TrialBalance.exportCsv', { defaultValue: 'CSV İndir' })}
            </Button>
          }
        />
      }
    >
      <div className="flex flex-wrap items-end gap-3">
        <div>
          <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
            {t('TrialBalance.year', { defaultValue: 'Yıl' })}
          </label>
          <div className="mt-1 inline-flex items-center gap-1">
            <button
              type="button"
              onClick={() => {
                const y = year - 1;
                setYear(y);
                setFromDate(yearStart(y));
                setToDate(yearEnd(y));
              }}
              className="rounded border border-slate-200 bg-white px-2 py-1 text-xs hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900"
            >
              ←
            </button>
            <span className="px-2 font-semibold text-slate-900 dark:text-slate-100">{year}</span>
            <button
              type="button"
              onClick={() => {
                const y = year + 1;
                setYear(y);
                setFromDate(yearStart(y));
                setToDate(yearEnd(y));
              }}
              className="rounded border border-slate-200 bg-white px-2 py-1 text-xs hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900"
            >
              →
            </button>
          </div>
        </div>
        <div>
          <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
            {t('TrialBalance.startDate', { defaultValue: 'Başlangıç' })}
          </label>
          <input
            type="date"
            value={fromDate}
            onChange={(e) => setFromDate(e.target.value)}
            className="mt-1 rounded border border-slate-300 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-800"
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
            {t('TrialBalance.endDate', { defaultValue: 'Bitiş' })}
          </label>
          <input
            type="date"
            value={toDate}
            onChange={(e) => setToDate(e.target.value)}
            className="mt-1 rounded border border-slate-300 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-800"
          />
        </div>
        <div
          className={`ml-auto rounded-lg border px-3 py-1.5 text-xs font-semibold ${
            isBalanced
              ? 'border-success-200 bg-success-50 text-success-700 dark:border-success-500/30 dark:bg-success-500/10 dark:text-success-300'
              : 'border-danger-200 bg-danger-50 text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-300'
          }`}
        >
          <Calculator className="mr-1 inline" size={11} />
          {isBalanced
            ? t('TrialBalance.balanced', { defaultValue: 'Mizan denk' })
            : t('TrialBalance.difference', {
                defaultValue: 'Fark: {{amount}}',
                amount: (totalDebit - totalCredit).toFixed(2),
              })}
        </div>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
            <tr>
              <th className="px-3 py-2 text-left">
                {t('TrialBalance.colCode', { defaultValue: 'Kod' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('TrialBalance.colAccountName', { defaultValue: 'Hesap Adı' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('TrialBalance.colType', { defaultValue: 'Tip' })}
              </th>
              <th className="px-3 py-2 text-center">
                {t('TrialBalance.colSide', { defaultValue: 'Yön' })}
              </th>
              <th className="px-3 py-2 text-right">
                {t('TrialBalance.colDebit', { defaultValue: 'Borç' })}
              </th>
              <th className="px-3 py-2 text-right">
                {t('TrialBalance.colCredit', { defaultValue: 'Alacak' })}
              </th>
              <th className="px-3 py-2 text-right">
                {t('TrialBalance.colBalance', { defaultValue: 'Bakiye' })}
              </th>
            </tr>
          </thead>
          <tbody>
            {report.isPending ? (
              <tr>
                <td colSpan={7} className="px-3 py-8 text-center text-sm text-slate-500">
                  {t('TrialBalance.calculating', { defaultValue: 'Hesaplanıyor…' })}
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-3 py-8 text-center text-sm text-slate-500">
                  {t('TrialBalance.empty', {
                    defaultValue: 'Belirtilen aralıkta post edilmiş yevmiye fişi bulunamadı.',
                  })}
                </td>
              </tr>
            ) : (
              rows.map((r) => (
                <tr
                  key={r.accountId}
                  className="border-t border-slate-100 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/30"
                >
                  <td className="px-3 py-2 font-mono text-xs">{r.accountCode}</td>
                  <td className="px-3 py-2 text-xs text-slate-900 dark:text-slate-100">
                    {r.accountName}
                  </td>
                  <td className="px-3 py-2">
                    <span
                      className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${TYPE_STYLES[r.type]}`}
                    >
                      {r.type}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-center text-[10px] uppercase text-slate-500">
                    {r.normalSide === 'Debit'
                      ? t('TrialBalance.debitAbbr', { defaultValue: 'Dr' })
                      : t('TrialBalance.creditAbbr', { defaultValue: 'Cr' })}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-xs">
                    {fmtMoney(r.debit, locale)}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-xs">
                    {fmtMoney(r.credit, locale)}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-xs font-semibold">
                    {fmtMoney(r.balance, locale)}
                  </td>
                </tr>
              ))
            )}
          </tbody>
          {rows.length > 0 && (
            <tfoot className="border-t-2 border-slate-300 bg-slate-50 font-semibold dark:border-slate-700 dark:bg-slate-800/50">
              <tr>
                <td colSpan={4} className="px-3 py-2 text-right text-xs uppercase">
                  {t('TrialBalance.total', { defaultValue: 'Toplam' })}
                </td>
                <td className="px-3 py-2 text-right font-mono text-xs">
                  {fmtMoney(totalDebit, locale)}
                </td>
                <td className="px-3 py-2 text-right font-mono text-xs">
                  {fmtMoney(totalCredit, locale)}
                </td>
                <td className="px-3 py-2 text-right font-mono text-xs">
                  {fmtMoney(totalDebit - totalCredit, locale)}
                </td>
              </tr>
            </tfoot>
          )}
        </table>
      </div>
    </DetailPageTemplate>
  );
};

export default TrialBalancePage;
