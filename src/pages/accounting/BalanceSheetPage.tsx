import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Scale } from 'lucide-react';
import { formatNumber } from '@/shared/lib/format';
import { useTrialBalanceQuery } from '@/features/accounting/hooks/useJournalEntryQueries';
import { useDecimalPlaces } from '@/features/settings/hooks/useSettingsQueries';
import {
  buildBalanceSheet,
  type StatementSection,
} from '@/features/accounting/lib/financialStatements';
import { ReportPeriodControls } from '@/features/accounting/ui/ReportPeriodControls';

const currentYear = () => new Date().getFullYear();

export const BalanceSheetPage = () => {
  const { i18n } = useTranslation();
  const locale = i18n.language;
  const decimals = useDecimalPlaces();
  const fmt = (n: number) => formatNumber(n, locale, decimals);

  const [year, setYear] = useState(currentYear());
  const [fromDate, setFromDate] = useState(`${currentYear()}-01-01`);
  const [toDate, setToDate] = useState(`${currentYear()}-12-31`);

  const params = useMemo(() => ({ fromDate, toDate }), [fromDate, toDate]);
  const report = useTrialBalanceQuery(params);
  const rows = useMemo(() => report.data?.data?.rows ?? [], [report.data]);

  const sheet = useMemo(() => buildBalanceSheet(rows), [rows]);
  const hasData = rows.length > 0;

  return (
    <div className="space-y-4 p-4">
      <div>
        <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100">
          Bilanço (Balance Sheet)
        </h1>
        <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
          Dönem sonu itibarıyla varlıklar, yükümlülükler ve özkaynaklar. Dönem net kârı özkaynağa
          eklenir.
        </p>
      </div>

      <ReportPeriodControls
        year={year}
        fromDate={fromDate}
        toDate={toDate}
        onYearChange={setYear}
        onFromChange={setFromDate}
        onToChange={setToDate}
        right={
          <div
            className={`rounded-lg border px-3 py-1.5 text-xs font-semibold ${
              sheet.isBalanced
                ? 'border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-300'
                : 'border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-500/30 dark:bg-rose-500/10 dark:text-rose-300'
            }`}
          >
            <Scale className="mr-1 inline" size={11} />
            {sheet.isBalanced
              ? 'Bilanço denk'
              : `Fark: ${fmt(sheet.assets.total - sheet.totalLiabilitiesAndEquity)}`}
          </div>
        }
      />

      {report.isPending ? (
        <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900">
          Hesaplanıyor…
        </div>
      ) : !hasData ? (
        <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900">
          Belirtilen aralıkta post edilmiş yevmiye fişi bulunamadı.
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          {/* Assets */}
          <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
            <table className="w-full text-sm">
              <tbody>
                <Section title="Varlıklar" section={sheet.assets} fmt={fmt} />
              </tbody>
              <tfoot>
                <TotalRow label="Toplam Varlıklar" value={sheet.assets.total} fmt={fmt} />
              </tfoot>
            </table>
          </div>

          {/* Liabilities + Equity */}
          <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
            <table className="w-full text-sm">
              <tbody>
                <Section title="Yükümlülükler" section={sheet.liabilities} fmt={fmt} />
                <Section title="Özkaynaklar" section={sheet.equity} fmt={fmt} />
                <tr className="border-t border-slate-100 dark:border-slate-800/60">
                  <td className="px-3 py-1.5 text-xs text-slate-700 dark:text-slate-300">
                    Dönem Net {sheet.netIncome >= 0 ? 'Kârı' : 'Zararı'}
                  </td>
                  <td className="px-3 py-1.5 text-right font-mono text-xs text-slate-800 dark:text-slate-200">
                    {fmt(sheet.netIncome)}
                  </td>
                </tr>
              </tbody>
              <tfoot>
                <TotalRow
                  label="Toplam Kaynaklar"
                  value={sheet.totalLiabilitiesAndEquity}
                  fmt={fmt}
                />
              </tfoot>
            </table>
          </div>
        </div>
      )}
    </div>
  );
};

const Section = ({
  title,
  section,
  fmt,
}: {
  title: string;
  section: StatementSection;
  fmt: (n: number) => string;
}) => (
  <>
    <tr className="bg-slate-50 dark:bg-slate-800/40">
      <td
        colSpan={2}
        className="px-3 py-1.5 text-xs font-semibold uppercase tracking-wider text-slate-500"
      >
        {title}
      </td>
    </tr>
    {section.lines.length === 0 ? (
      <tr>
        <td colSpan={2} className="px-3 py-1.5 text-xs italic text-slate-400">
          —
        </td>
      </tr>
    ) : (
      section.lines.map((l) => (
        <tr key={l.accountId} className="border-t border-slate-100 dark:border-slate-800/60">
          <td className="px-3 py-1.5 text-xs text-slate-700 dark:text-slate-300">
            <span className="font-mono text-slate-400">{l.accountCode}</span> {l.accountName}
          </td>
          <td className="px-3 py-1.5 text-right font-mono text-xs text-slate-800 dark:text-slate-200">
            {fmt(l.amount)}
          </td>
        </tr>
      ))
    )}
  </>
);

const TotalRow = ({
  label,
  value,
  fmt,
}: {
  label: string;
  value: number;
  fmt: (n: number) => string;
}) => (
  <tr className="border-t-2 border-slate-300 bg-slate-50 dark:border-slate-700 dark:bg-slate-800/50">
    <td className="px-3 py-2.5 text-sm font-bold text-slate-900 dark:text-slate-100">{label}</td>
    <td className="px-3 py-2.5 text-right font-mono text-sm font-bold text-slate-900 dark:text-slate-100">
      {fmt(value)}
    </td>
  </tr>
);

export default BalanceSheetPage;
