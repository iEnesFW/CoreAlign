import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Calculator, FileDown } from 'lucide-react';
import { useTrialBalanceQuery } from '@/features/accounting/hooks/useJournalEntryQueries';
import type { AccountType } from '@/features/accounting/model/glAccount.types';

const TYPE_STYLES: Record<AccountType, string> = {
  Asset: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Liability: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
  Equity: 'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300',
  Revenue: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
  Expense: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  CostOfGoodsSold: 'bg-orange-100 text-orange-700 dark:bg-orange-500/20 dark:text-orange-300',
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
  const { i18n } = useTranslation();
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
    const headers = ['Kod', 'Hesap Adı', 'Tip', 'Borç', 'Alacak', 'Bakiye'];
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
    lines.push(['', 'TOPLAM', '', totalDebit.toFixed(2), totalCredit.toFixed(2), ''].join(','));
    const blob = new Blob(['﻿' + lines.join('\n')], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `mizan_${fromDate}_${toDate}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-4 p-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100">
            Mizan (Trial Balance)
          </h1>
          <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
            Belirtilen tarih aralığındaki post edilmiş yevmiye fişlerinden hesaplanan hesap
            bakiyeleri.
          </p>
        </div>
        <button
          type="button"
          onClick={exportCsv}
          disabled={!data || rows.length === 0}
          className="inline-flex items-center gap-1.5 rounded border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
        >
          <FileDown size={12} />
          CSV İndir
        </button>
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <div>
          <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
            Yıl
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
            Başlangıç
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
            Bitiş
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
              ? 'border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-300'
              : 'border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-500/30 dark:bg-rose-500/10 dark:text-rose-300'
          }`}
        >
          <Calculator className="mr-1 inline" size={11} />
          {isBalanced ? 'Mizan denk' : `Fark: ${(totalDebit - totalCredit).toFixed(2)}`}
        </div>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
            <tr>
              <th className="px-3 py-2 text-left">Kod</th>
              <th className="px-3 py-2 text-left">Hesap Adı</th>
              <th className="px-3 py-2 text-left">Tip</th>
              <th className="px-3 py-2 text-center">Yön</th>
              <th className="px-3 py-2 text-right">Borç</th>
              <th className="px-3 py-2 text-right">Alacak</th>
              <th className="px-3 py-2 text-right">Bakiye</th>
            </tr>
          </thead>
          <tbody>
            {report.isPending ? (
              <tr>
                <td colSpan={7} className="px-3 py-8 text-center text-sm text-slate-500">
                  Hesaplanıyor…
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-3 py-8 text-center text-sm text-slate-500">
                  Belirtilen aralıkta post edilmiş yevmiye fişi bulunamadı.
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
                    {r.normalSide === 'Debit' ? 'Dr' : 'Cr'}
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
                  Toplam
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
    </div>
  );
};

export default TrialBalancePage;
