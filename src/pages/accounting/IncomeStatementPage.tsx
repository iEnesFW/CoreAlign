import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { TrendingUp } from 'lucide-react';
import { formatNumber } from '@/shared/lib/format';
import { useTrialBalanceQuery } from '@/features/accounting/hooks/useJournalEntryQueries';
import { useDecimalPlaces } from '@/features/settings/hooks/useSettingsQueries';
import {
  buildIncomeStatement,
  type StatementSection,
} from '@/features/accounting/lib/financialStatements';
import { ReportPeriodControls } from '@/features/accounting/ui/ReportPeriodControls';

const currentYear = () => new Date().getFullYear();

export const IncomeStatementPage = () => {
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

  const statement = useMemo(() => buildIncomeStatement(rows), [rows]);
  const hasData = rows.length > 0;

  return (
    <div className="space-y-4 p-4">
      <div>
        <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100">
          Gelir Tablosu (Income Statement)
        </h1>
        <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
          Belirtilen dönemdeki post edilmiş yevmiye fişlerinden türetilen gelir, satış maliyeti ve
          giderler.
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
              statement.netIncome >= 0
                ? 'border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-300'
                : 'border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-500/30 dark:bg-rose-500/10 dark:text-rose-300'
            }`}
          >
            <TrendingUp className="mr-1 inline" size={11} />
            Net {statement.netIncome >= 0 ? 'Kâr' : 'Zarar'}: {fmt(statement.netIncome)}
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
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
          <table className="w-full text-sm">
            <tbody>
              <Section title="Gelirler" section={statement.revenue} fmt={fmt} />
              <SubtotalRow label="Toplam Gelir" value={statement.revenue.total} fmt={fmt} />

              <Section
                title="Satılan Malın Maliyeti (SMM)"
                section={statement.cogs}
                fmt={fmt}
                negate
              />
              <SubtotalRow label="Brüt Kâr" value={statement.grossProfit} fmt={fmt} strong />

              <Section title="Faaliyet Giderleri" section={statement.opex} fmt={fmt} negate />
            </tbody>
            <tfoot>
              <tr className="border-t-2 border-slate-300 bg-slate-50 dark:border-slate-700 dark:bg-slate-800/50">
                <td className="px-3 py-2.5 text-sm font-bold text-slate-900 dark:text-slate-100">
                  Net Dönem {statement.netIncome >= 0 ? 'Kârı' : 'Zararı'}
                </td>
                <td
                  className={`px-3 py-2.5 text-right font-mono text-sm font-bold ${
                    statement.netIncome >= 0
                      ? 'text-emerald-600 dark:text-emerald-400'
                      : 'text-rose-600 dark:text-rose-400'
                  }`}
                >
                  {fmt(statement.netIncome)}
                </td>
              </tr>
            </tfoot>
          </table>
        </div>
      )}
    </div>
  );
};

const Section = ({
  title,
  section,
  fmt,
  negate,
}: {
  title: string;
  section: StatementSection;
  fmt: (n: number) => string;
  negate?: boolean;
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
            {negate ? `(${fmt(l.amount)})` : fmt(l.amount)}
          </td>
        </tr>
      ))
    )}
  </>
);

const SubtotalRow = ({
  label,
  value,
  fmt,
  strong,
}: {
  label: string;
  value: number;
  fmt: (n: number) => string;
  strong?: boolean;
}) => (
  <tr className="border-t border-slate-200 dark:border-slate-700">
    <td
      className={`px-3 py-1.5 text-xs ${strong ? 'font-bold text-slate-900 dark:text-slate-100' : 'font-semibold text-slate-700 dark:text-slate-300'}`}
    >
      {label}
    </td>
    <td
      className={`px-3 py-1.5 text-right font-mono text-xs ${strong ? 'font-bold text-slate-900 dark:text-slate-100' : 'font-semibold text-slate-700 dark:text-slate-300'}`}
    >
      {fmt(value)}
    </td>
  </tr>
);

export default IncomeStatementPage;
