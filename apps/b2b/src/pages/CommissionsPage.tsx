import { Download } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { Button } from '@/shared/ui/Button';
import { Card, CardBody, CardHeader } from '@/shared/ui/Card';
import { PageHeader } from '@/shared/ui/PageHeader';
import { Spinner } from '@/shared/ui/Spinner';
import { CommissionStatusBadge } from '@/shared/ui/StatusBadge';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { usePdfDownload } from '@/shared/lib/usePdfDownload';
import { useDealerCommissions, useDealerCommissionSummary } from '@/features/portal/hooks';
import type { CommissionStatus } from '@/features/portal/types';

const STATUSES: CommissionStatus[] = ['Accrued', 'Paid', 'Cancelled'];

export const CommissionsPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const [statusFilter, setStatusFilter] = useState<CommissionStatus | ''>('');

  const { data: summary, isLoading: summaryLoading } = useDealerCommissionSummary();
  const { data: list, isLoading: listLoading } = useDealerCommissions({
    status: statusFilter || undefined,
    page: 1,
    pageSize: 100,
  });

  const yearStart = useMemo(() => {
    const now = new Date();
    return new Date(Date.UTC(now.getUTCFullYear(), 0, 1)).toISOString();
  }, []);
  const yearEnd = useMemo(() => new Date().toISOString(), []);

  const pdf = usePdfDownload(
    `/dealer-portal/commissions/statement/pdf?fromUtc=${encodeURIComponent(yearStart)}&toUtc=${encodeURIComponent(yearEnd)}`,
    `CommissionStatement-${yearStart.slice(0, 10)}-${yearEnd.slice(0, 10)}.pdf`,
  );

  const monthlyTrend = useMemo(() => {
    if (!list?.items?.length) return [];
    const map = new Map<string, { month: string; accrued: number; paid: number }>();
    for (const entry of list.items) {
      const d = new Date(entry.accruedAtUtc);
      const key = `${d.getUTCFullYear()}-${String(d.getUTCMonth() + 1).padStart(2, '0')}`;
      const existing = map.get(key) ?? { month: key, accrued: 0, paid: 0 };
      existing.accrued += entry.commissionAmount;
      if (entry.status === 'Paid') existing.paid += entry.commissionAmount;
      map.set(key, existing);
    }
    return Array.from(map.values()).sort((a, b) => a.month.localeCompare(b.month));
  }, [list]);

  const currency = summary?.currency ?? 'TRY';

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('b2b.commissions.title')}
        subtitle={t('b2b.commissions.subtitle')}
        action={
          <Button onClick={pdf.download} disabled={pdf.isLoading}>
            <Download size={14} /> {t('b2b.commissions.downloadStatement')}
          </Button>
        }
      />

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <KpiCard
          label={t('b2b.commissions.kpiYtdEarned')}
          value={summary ? formatCurrency(summary.ytdAccrued, locale, currency) : '—'}
          loading={summaryLoading}
        />
        <KpiCard
          label={t('b2b.commissions.kpiThisMonth')}
          value={summary ? formatCurrency(summary.thisMonthAccrued, locale, currency) : '—'}
          loading={summaryLoading}
        />
        <KpiCard
          label={t('b2b.commissions.kpiYtdPaid')}
          value={summary ? formatCurrency(summary.ytdPaid, locale, currency) : '—'}
          loading={summaryLoading}
        />
        <KpiCard
          label={t('b2b.commissions.kpiOutstanding')}
          value={
            summary
              ? formatCurrency(Math.max(0, summary.ytdAccrued - summary.ytdPaid), locale, currency)
              : '—'
          }
          loading={summaryLoading}
        />
      </div>

      <Card>
        <CardHeader
          title={t('b2b.commissions.trendTitle')}
          subtitle={t('b2b.commissions.trendSubtitle')}
        />
        <CardBody>
          {monthlyTrend.length === 0 ? (
            <p className="text-sm text-slate-500">{t('b2b.common.noData')}</p>
          ) : (
            <div className="h-64 w-full">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={monthlyTrend}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                  <XAxis dataKey="month" stroke="#64748b" fontSize={12} />
                  <YAxis stroke="#64748b" fontSize={12} />
                  <Tooltip
                    formatter={(value) =>
                      formatCurrency(
                        typeof value === 'number' ? value : Number(value),
                        locale,
                        currency,
                      )
                    }
                  />
                  <Legend />
                  <Bar dataKey="accrued" name={t('b2b.commissions.legendAccrued')} fill="#f59e0b" />
                  <Bar dataKey="paid" name={t('b2b.commissions.legendPaid')} fill="#10b981" />
                </BarChart>
              </ResponsiveContainer>
            </div>
          )}
        </CardBody>
      </Card>

      <Card className="overflow-hidden">
        <div className="flex flex-wrap items-center gap-3 border-b border-slate-100 px-5 py-4 dark:border-slate-800">
          <label className="ml-auto flex items-center gap-2 text-sm text-slate-500">
            {t('b2b.commissions.filterByStatus')}
            <select
              value={statusFilter}
              onChange={(event) => setStatusFilter((event.target.value as CommissionStatus) || '')}
              className="h-10 rounded-xl border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-900"
            >
              <option value="">{t('b2b.commissions.allStatuses')}</option>
              {STATUSES.map((s) => (
                <option key={s} value={s}>
                  {t(`b2b.commissionStatus.${s}`, s)}
                </option>
              ))}
            </select>
          </label>
        </div>

        {listLoading ? (
          <div className="flex items-center gap-2 px-6 py-10 text-sm text-slate-500">
            <Spinner /> {t('b2b.common.loading')}
          </div>
        ) : !list || list.items.length === 0 ? (
          <p className="px-6 py-10 text-sm text-slate-400">{t('b2b.common.noData')}</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-100 text-sm dark:divide-slate-800">
              <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500 dark:bg-slate-900 dark:text-slate-400">
                <tr>
                  <th scope="col" className="px-6 py-3 font-medium">
                    {t('b2b.commissions.date')}
                  </th>
                  <th scope="col" className="px-6 py-3 text-right font-medium">
                    {t('b2b.commissions.orderTotal')}
                  </th>
                  <th scope="col" className="px-6 py-3 text-right font-medium">
                    {t('b2b.commissions.percent')}
                  </th>
                  <th scope="col" className="px-6 py-3 text-right font-medium">
                    {t('b2b.commissions.amount')}
                  </th>
                  <th scope="col" className="px-6 py-3 font-medium">
                    {t('b2b.commissions.status')}
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white dark:divide-slate-800 dark:bg-slate-950">
                {list.items.map((entry) => (
                  <tr key={entry.id}>
                    <td className="px-6 py-3 text-slate-700 dark:text-slate-200">
                      {formatDate(entry.accruedAtUtc, locale)}
                    </td>
                    <td className="px-6 py-3 text-right text-slate-700 dark:text-slate-200">
                      {formatCurrency(entry.orderTotal, locale, entry.currency)}
                    </td>
                    <td className="px-6 py-3 text-right text-slate-600 dark:text-slate-300">
                      {entry.commissionPercent.toFixed(2)}%
                    </td>
                    <td className="px-6 py-3 text-right font-semibold text-slate-900 dark:text-slate-100">
                      {formatCurrency(entry.commissionAmount, locale, entry.currency)}
                    </td>
                    <td className="px-6 py-3">
                      <CommissionStatusBadge status={entry.status} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
};

const KpiCard = ({
  label,
  value,
  loading,
}: {
  label: string;
  value: string;
  loading?: boolean;
}) => (
  <Card>
    <CardBody>
      <p className="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">{label}</p>
      <p className="mt-2 text-2xl font-bold text-slate-900 dark:text-slate-100">
        {loading ? <Spinner /> : value}
      </p>
    </CardBody>
  </Card>
);
