import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Scale } from 'lucide-react';
import { formatNumber } from '@/shared/lib/format';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { DetailPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { useBalanceSheetQuery } from '@/features/accounting/hooks/useFinancialStatementQueries';
import { useDecimalPlaces } from '@/features/settings/hooks/useSettingsQueries';
import type { StatementSectionDto } from '@/features/accounting/model/financialStatement.types';
import { ReportPeriodControls } from '@/features/accounting/ui/ReportPeriodControls';

const currentYear = () => new Date().getFullYear();

export const BalanceSheetPage = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const decimals = useDecimalPlaces();
  const fmt = (n: number) => formatNumber(n, locale, decimals);

  const [year, setYear] = useState(currentYear());
  const [fromDate, setFromDate] = useState(`${currentYear()}-01-01`);
  const [toDate, setToDate] = useState(`${currentYear()}-12-31`);

  const report = useBalanceSheetQuery(toDate);
  const sheet = report.data?.data ?? null;
  const earnings = useMemo(
    () => (sheet ? sheet.currentYearEarnings + sheet.retainedPriorEarnings : 0),
    [sheet],
  );

  return (
    <DetailPageTemplate
      header={
        <PageHeader
          icon={<Scale size={20} />}
          title={t('BalanceSheet.title', { defaultValue: 'Bilanço (Balance Sheet)' })}
          subtitle={t('BalanceSheet.subtitle', {
            defaultValue:
              'Belirtilen tarih itibarıyla varlıklar, yükümlülükler ve özkaynaklar. Dönem net kârı özkaynağa eklenir.',
          })}
          tone="indigo"
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
          sheet && (
            <div
              className={`rounded-lg border px-3 py-1.5 text-xs font-semibold ${
                sheet.isBalanced
                  ? 'border-success-200 bg-success-50 text-success-700 dark:border-success-500/30 dark:bg-success-500/10 dark:text-success-300'
                  : 'border-danger-200 bg-danger-50 text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-300'
              }`}
            >
              <Scale className="mr-1 inline" size={11} />
              {sheet.isBalanced
                ? t('BalanceSheet.balanced', { defaultValue: 'Bilanço denk' })
                : t('BalanceSheet.variance', {
                    defaultValue: 'Fark: {{value}}',
                    value: fmt(sheet.variance),
                  })}
            </div>
          )
        }
      />

      {report.isPending ? (
        <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900">
          {t('BalanceSheet.computing', { defaultValue: 'Hesaplanıyor…' })}
        </div>
      ) : !sheet ? (
        <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900">
          {t('BalanceSheet.empty', {
            defaultValue: 'Belirtilen tarih itibarıyla post edilmiş yevmiye fişi bulunamadı.',
          })}
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
            <table className="w-full text-sm">
              <tbody>
                <Section
                  title={t('BalanceSheet.assets', { defaultValue: 'Varlıklar' })}
                  section={sheet.assets}
                  fmt={fmt}
                />
              </tbody>
              <tfoot>
                <TotalRow
                  label={t('BalanceSheet.totalAssets', { defaultValue: 'Toplam Varlıklar' })}
                  value={sheet.assets.total}
                  fmt={fmt}
                />
              </tfoot>
            </table>
          </div>

          <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
            <table className="w-full text-sm">
              <tbody>
                <Section
                  title={t('BalanceSheet.liabilities', { defaultValue: 'Yükümlülükler' })}
                  section={sheet.liabilities}
                  fmt={fmt}
                />
                <Section
                  title={t('BalanceSheet.equity', { defaultValue: 'Özkaynaklar' })}
                  section={sheet.equity}
                  fmt={fmt}
                />
                <tr className="border-t border-slate-100 dark:border-slate-800/60">
                  <td className="px-3 py-1.5 text-xs text-slate-700 dark:text-slate-300">
                    {t('BalanceSheet.currentYearEarnings', {
                      defaultValue: 'Dönem Net Kârı/Zararı (kapanış öncesi)',
                    })}
                  </td>
                  <td className="px-3 py-1.5 text-right font-mono text-xs text-slate-800 dark:text-slate-200">
                    {fmt(earnings)}
                  </td>
                </tr>
              </tbody>
              <tfoot>
                <TotalRow
                  label={t('BalanceSheet.totalLiabilitiesAndEquity', {
                    defaultValue: 'Toplam Kaynaklar',
                  })}
                  value={sheet.totalLiabilitiesAndEquity}
                  fmt={fmt}
                />
              </tfoot>
            </table>
          </div>
        </div>
      )}
    </DetailPageTemplate>
  );
};

const Section = ({
  title,
  section,
  fmt,
}: {
  title: string;
  section: StatementSectionDto;
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
