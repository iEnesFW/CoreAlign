import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { TrendingUp } from 'lucide-react';
import { formatNumber } from '@/shared/lib/format';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { DetailPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { useIncomeStatementQuery } from '@/features/accounting/hooks/useFinancialStatementQueries';
import { useDecimalPlaces } from '@/features/settings/hooks/useSettingsQueries';
import type { StatementSectionDto } from '@/features/accounting/model/financialStatement.types';
import { ReportPeriodControls } from '@/features/accounting/ui/ReportPeriodControls';

const currentYear = () => new Date().getFullYear();

export const IncomeStatementPage = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const decimals = useDecimalPlaces();
  const fmt = (n: number) => formatNumber(n, locale, decimals);

  const [year, setYear] = useState(currentYear());
  const [fromDate, setFromDate] = useState(`${currentYear()}-01-01`);
  const [toDate, setToDate] = useState(`${currentYear()}-12-31`);

  const params = useMemo(() => ({ fromDate, toDate }), [fromDate, toDate]);
  const report = useIncomeStatementQuery(params);
  const statement = report.data?.data ?? null;

  return (
    <DetailPageTemplate
      header={
        <PageHeader
          icon={<TrendingUp size={20} />}
          title={t('IncomeStatement.title', { defaultValue: 'Gelir Tablosu (Income Statement)' })}
          subtitle={t('IncomeStatement.subtitle', {
            defaultValue:
              'Belirtilen dönemdeki post edilmiş yevmiye fişlerinden türetilen gelir, satış maliyeti ve giderler.',
          })}
        />
      }
    >
      <ReportPeriodControls
        year={year}
        fromDate={fromDate}
        toDate={toDate}
        onYearChange={setYear}
        onFromChange={setFromDate}
        onToChange={setToDate}
        right={
          statement && (
            <div
              className={`rounded-lg border px-3 py-1.5 text-xs font-semibold ${
                statement.netIncome >= 0
                  ? 'border-success-200 bg-success-50 text-success-700 dark:border-success-500/30 dark:bg-success-500/10 dark:text-success-300'
                  : 'border-danger-200 bg-danger-50 text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-300'
              }`}
            >
              <TrendingUp className="mr-1 inline" size={11} />
              {statement.netIncome >= 0
                ? t('IncomeStatement.netProfit', {
                    defaultValue: 'Net Kâr: {{value}}',
                    value: fmt(statement.netIncome),
                  })
                : t('IncomeStatement.netLoss', {
                    defaultValue: 'Net Zarar: {{value}}',
                    value: fmt(statement.netIncome),
                  })}
            </div>
          )
        }
      />

      {report.isPending ? (
        <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900">
          {t('IncomeStatement.computing', { defaultValue: 'Hesaplanıyor…' })}
        </div>
      ) : !statement ? (
        <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900">
          {t('IncomeStatement.empty', {
            defaultValue: 'Belirtilen aralıkta post edilmiş yevmiye fişi bulunamadı.',
          })}
        </div>
      ) : (
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
          <table className="w-full text-sm">
            <tbody>
              <Section
                title={t('IncomeStatement.revenue', { defaultValue: 'Gelirler' })}
                section={statement.revenue}
                fmt={fmt}
              />
              <SubtotalRow
                label={t('IncomeStatement.totalRevenue', { defaultValue: 'Toplam Gelir' })}
                value={statement.revenue.total}
                fmt={fmt}
              />

              <Section
                title={t('IncomeStatement.cogs', {
                  defaultValue: 'Satılan Malın Maliyeti (SMM)',
                })}
                section={statement.cogs}
                fmt={fmt}
                negate
              />
              <SubtotalRow
                label={t('IncomeStatement.grossProfit', { defaultValue: 'Brüt Kâr' })}
                value={statement.grossProfit}
                fmt={fmt}
                strong
              />

              <Section
                title={t('IncomeStatement.opex', { defaultValue: 'Faaliyet Giderleri' })}
                section={statement.opex}
                fmt={fmt}
                negate
              />
            </tbody>
            <tfoot>
              <tr className="border-t-2 border-slate-300 bg-slate-50 dark:border-slate-700 dark:bg-slate-800/50">
                <td className="px-3 py-2.5 text-sm font-bold text-slate-900 dark:text-slate-100">
                  {statement.netIncome >= 0
                    ? t('IncomeStatement.netIncomeRow', { defaultValue: 'Net Dönem Kârı' })
                    : t('IncomeStatement.netLossRow', { defaultValue: 'Net Dönem Zararı' })}
                </td>
                <td
                  className={`px-3 py-2.5 text-right font-mono text-sm font-bold ${
                    statement.netIncome >= 0
                      ? 'text-success-600 dark:text-success-400'
                      : 'text-danger-600 dark:text-danger-400'
                  }`}
                >
                  {fmt(statement.netIncome)}
                </td>
              </tr>
            </tfoot>
          </table>
        </div>
      )}
    </DetailPageTemplate>
  );
};

const Section = ({
  title,
  section,
  fmt,
  negate,
}: {
  title: string;
  section: StatementSectionDto;
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
