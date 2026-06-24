import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { CalendarDays, Plus } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { Modal } from '@/shared/ui/Modal/Modal';
import { DataToolbar } from '@/shared/ui/DataToolbar/DataToolbar';
import { SegmentedControl } from '@/shared/ui/SegmentedControl/SegmentedControl';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { RunStatusBadge } from '@/features/hr/ui/RunStatusBadge';
import { useCreatePayrollRun, usePayrollRunsQuery } from '@/features/hr/hooks/usePayrollRuns';
import { PAYROLL_RUN_STATUSES, type PayrollRunStatus } from '@/features/hr/model/enums';

type StatusFilter = 'all' | PayrollRunStatus;

const fieldClass =
  'mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100';

const periodLabel = (year: number, month: number, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, { month: 'long', year: 'numeric' }).format(
      new Date(year, month - 1, 1),
    );
  } catch {
    return `${month}/${year}`;
  }
};

export const PayrollRunsPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const navigate = useNavigate();

  const now = new Date();
  const [status, setStatus] = useState<StatusFilter>('all');
  const [page, setPage] = useState(1);
  const [showCreate, setShowCreate] = useState(false);
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);

  const createMutation = useCreatePayrollRun();

  const params = useMemo(
    () => ({ status: status === 'all' ? undefined : status, page, pageSize: 25 }),
    [status, page],
  );
  const query = usePayrollRunsQuery(params);
  const runs = query.data?.data?.items ?? [];
  const total = query.data?.data?.total ?? 0;
  const totalPages = query.data?.data?.totalPages ?? 0;

  const statusLabel = (s: PayrollRunStatus) => t(`Payroll.runStatus.${s}`, { defaultValue: s });

  const createRun = async () => {
    try {
      const res = await createMutation.mutateAsync({ periodYear: year, periodMonth: month });
      toast.success(t('Payroll.runs.created', { defaultValue: 'Bordro dönemi oluşturuldu.' }));
      setShowCreate(false);
      const newId = res.data?.id;
      if (newId) navigate(`/dashboard/hr/payroll-runs/${newId}`);
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<CalendarDays size={20} />}
          title={t('Payroll.runs.title', { defaultValue: 'Bordro Dönemleri' })}
          subtitle={t('Payroll.runs.subtitle', {
            defaultValue: 'Aylık bordro dönemlerini oluştur, hesapla ve öde.',
          })}
          actions={
            <Button size="sm" onClick={() => setShowCreate(true)}>
              <Plus size={14} />
              {t('Payroll.runs.new', { defaultValue: 'Yeni Dönem' })}
            </Button>
          }
        />
      }
      toolbar={
        <DataToolbar
          viewMode={
            <SegmentedControl
              value={status}
              onChange={(v) => {
                setStatus(v);
                setPage(1);
              }}
              ariaLabel={t('Payroll.runs.statusAria', { defaultValue: 'Duruma göre filtrele' })}
              options={[
                { value: 'all', label: t('Payroll.runs.all', { defaultValue: 'Tümü' }) },
                ...PAYROLL_RUN_STATUSES.map((s) => ({ value: s, label: statusLabel(s) })),
              ]}
            />
          }
          resultCount={{
            count: total,
            label: t('Payroll.runs.resultCountLabel', { defaultValue: 'dönem' }),
          }}
          hasActiveFilters={status !== 'all'}
          onClearFilters={() => {
            setStatus('all');
            setPage(1);
          }}
        />
      }
      pagination={
        totalPages > 1 ? (
          <div className="flex items-center justify-end gap-1 text-xs">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
            >
              {t('common.prev', { defaultValue: 'Önceki' })}
            </Button>
            <span className="px-2 text-slate-500">
              {page} / {totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
            >
              {t('common.next', { defaultValue: 'Sonraki' })}
            </Button>
          </div>
        ) : undefined
      }
    >
      <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
        {query.isPending ? (
          <div className="px-3 py-8 text-center text-sm text-slate-500">
            {t('common.loading', { defaultValue: 'Yükleniyor…' })}
          </div>
        ) : runs.length === 0 ? (
          <div className="px-3 py-10 text-center text-sm text-slate-500 dark:text-slate-400">
            {t('Payroll.runs.empty', { defaultValue: 'Bordro dönemi bulunamadı.' })}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('Payroll.runs.cols.number', { defaultValue: 'No' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('Payroll.runs.cols.period', { defaultValue: 'Dönem' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('Payroll.runs.cols.employees', { defaultValue: 'Personel' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('Payroll.runs.cols.status', { defaultValue: 'Durum' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('Payroll.runs.cols.gross', { defaultValue: 'Brüt' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('Payroll.runs.cols.net', { defaultValue: 'Net' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('Payroll.runs.cols.payDate', { defaultValue: 'Ödeme' })}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {runs.map((r) => (
                <tr
                  key={r.id}
                  onClick={() => navigate(`/dashboard/hr/payroll-runs/${r.id}`)}
                  className="cursor-pointer hover:bg-slate-50/40 dark:hover:bg-slate-800/30"
                >
                  <td className="px-3 py-2 font-mono text-xs text-slate-700 dark:text-slate-300">
                    {r.runNumber}
                  </td>
                  <td className="px-3 py-2 text-slate-800 dark:text-slate-100">
                    {periodLabel(r.periodYear, r.periodMonth, locale)}
                  </td>
                  <td className="px-3 py-2 text-center tabular-nums text-slate-600 dark:text-slate-300">
                    {r.payslipCount}
                  </td>
                  <td className="px-3 py-2 text-center">
                    <RunStatusBadge status={r.status} />
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-slate-800 dark:text-slate-200">
                    {formatCurrency(r.totalGross, locale, r.currency)}
                  </td>
                  <td className="px-3 py-2 text-right font-mono font-semibold text-slate-900 dark:text-slate-100">
                    {formatCurrency(r.totalNet, locale, r.currency)}
                  </td>
                  <td className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">
                    {r.paidAtUtc ? formatDate(r.paidAtUtc, locale) : '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {showCreate && (
        <Modal
          open
          onClose={() => setShowCreate(false)}
          size="md"
          icon={<CalendarDays size={16} />}
          title={t('Payroll.runs.newTitle', { defaultValue: 'Yeni Bordro Dönemi' })}
          footer={
            <>
              <Button variant="outline" size="sm" onClick={() => setShowCreate(false)}>
                {t('common.cancel', { defaultValue: 'İptal' })}
              </Button>
              <Button size="sm" onClick={createRun} isLoading={createMutation.isPending}>
                {t('Payroll.runs.create', { defaultValue: 'Oluştur' })}
              </Button>
            </>
          }
        >
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                {t('Payroll.runs.year', { defaultValue: 'Yıl' })}
              </label>
              <input
                type="number"
                value={year}
                min={2000}
                max={2100}
                onChange={(e) => setYear(Number(e.target.value))}
                className={fieldClass}
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                {t('Payroll.runs.month', { defaultValue: 'Ay' })}
              </label>
              <select
                value={month}
                onChange={(e) => setMonth(Number(e.target.value))}
                className={fieldClass}
              >
                {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => (
                  <option key={m} value={m}>
                    {periodLabel(year, m, locale)}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </Modal>
      )}
    </ListPageTemplate>
  );
};

export default PayrollRunsPage;
