import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import {
  Banknote,
  Calculator,
  CheckCircle2,
  CalendarDays,
  FileText,
  RotateCcw,
  ShieldCheck,
} from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { DetailPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { Button } from '@/shared/ui/Button/Button';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { RunStatusBadge } from '@/features/hr/ui/RunStatusBadge';
import {
  usePayrollRunAction,
  usePayrollRunPayslipsQuery,
  usePayrollRunQuery,
  type PayrollRunActionType,
} from '@/features/hr/hooks/usePayrollRuns';
import { PAYROLL_RUN_STATUSES, type PayrollRunStatus } from '@/features/hr/model/enums';

const STEP_ORDER: PayrollRunStatus[] = PAYROLL_RUN_STATUSES;

const payslipTotalDeductions = (p: {
  sgkEmployee: number;
  unemploymentEmployee: number;
  incomeTaxNet: number;
  stampTax: number;
  otherDeductionsTotal: number;
}) => p.sgkEmployee + p.unemploymentEmployee + p.incomeTaxNet + p.stampTax + p.otherDeductionsTotal;

const periodLabel = (year: number, month: number, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, { month: 'long', year: 'numeric' }).format(
      new Date(year, month - 1, 1),
    );
  } catch {
    return `${month}/${year}`;
  }
};

export const PayrollRunDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const confirm = useConfirm();
  const navigate = useNavigate();

  const [pendingAction, setPendingAction] = useState<string | null>(null);

  const query = usePayrollRunQuery(id ?? null);
  const payslipsQuery = usePayrollRunPayslipsQuery(id ?? null);
  const action = usePayrollRunAction();

  const run = query.data?.data;
  const payslips = payslipsQuery.data?.data ?? [];

  if (query.isPending) {
    return (
      <div className="p-6 text-sm text-slate-500">
        {t('common.loading', { defaultValue: 'Yükleniyor…' })}
      </div>
    );
  }
  if (!run) {
    return (
      <div className="p-6 text-sm text-slate-500">
        {t('Payroll.runDetail.notFound', { defaultValue: 'Bordro dönemi bulunamadı.' })}
      </div>
    );
  }

  const currentStepIndex = STEP_ORDER.indexOf(run.status);

  const runAction = async (
    act: PayrollRunActionType,
    opts?: { confirmTitle?: string; confirmMessage?: string; tone?: 'default' | 'danger' },
  ) => {
    if (opts?.confirmTitle) {
      const ok = await confirm({
        title: opts.confirmTitle,
        message: opts.confirmMessage ?? '',
        confirmLabel: t('common.confirm', { defaultValue: 'Onayla' }),
        tone: opts.tone ?? 'default',
      });
      if (!ok) return;
    }
    setPendingAction(act);
    try {
      await action.mutateAsync({ id: run.id, action: act });
      toast.success(t('Payroll.runDetail.actionDone', { defaultValue: 'İşlem tamamlandı.' }));
    } catch (err) {
      toastApiError(err);
    } finally {
      setPendingAction(null);
    }
  };

  return (
    <DetailPageTemplate
      header={
        <PageHeader
          icon={<CalendarDays size={20} />}
          title={periodLabel(run.periodYear, run.periodMonth, locale)}
          subtitle={run.runNumber}
          crumbs={[
            {
              label: t('Payroll.runDetail.backToList', { defaultValue: 'Bordro Dönemleri' }),
              to: '/dashboard/hr/payroll-runs',
            },
            { label: run.runNumber },
          ]}
          trailing={
            <div className="flex flex-col items-stretch gap-2 sm:items-end">
              <RunStatusBadge status={run.status} />
              <div className="text-right">
                <div className="text-xs text-slate-500">
                  {t('Payroll.runDetail.netTotal', { defaultValue: 'Net Toplam' })}
                </div>
                <div className="text-xl font-bold text-slate-900 dark:text-slate-100">
                  {formatCurrency(run.totalNet, locale, run.currency)}
                </div>
              </div>
            </div>
          }
        />
      }
    >
      <ol className="flex flex-wrap items-center gap-2 rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
        {STEP_ORDER.map((step, i) => {
          const done = i < currentStepIndex;
          const active = i === currentStepIndex;
          return (
            <li key={step} className="flex items-center gap-2">
              <div
                className={`flex h-7 w-7 items-center justify-center rounded-full text-[11px] font-bold ${
                  done
                    ? 'bg-success-500 text-white'
                    : active
                      ? 'bg-primary-600 text-white'
                      : 'bg-slate-100 text-slate-400 dark:bg-slate-800 dark:text-slate-500'
                }`}
              >
                {done ? '✓' : i + 1}
              </div>
              <span
                className={`text-xs font-medium ${
                  active
                    ? 'text-slate-900 dark:text-slate-100'
                    : 'text-slate-500 dark:text-slate-400'
                }`}
              >
                {t(`Payroll.runStatus.${step}`, { defaultValue: step })}
              </span>
              {i < STEP_ORDER.length - 1 && (
                <span className="mx-1 text-slate-300 dark:text-slate-700">→</span>
              )}
            </li>
          );
        })}
      </ol>

      <div className="flex flex-wrap items-center gap-2 rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
        {run.status === 'Draft' && (
          <Button
            size="sm"
            onClick={() => runAction('calculate')}
            isLoading={pendingAction === 'calculate'}
          >
            <Calculator size={14} />
            {t('Payroll.runDetail.calculate', { defaultValue: 'Hesapla' })}
          </Button>
        )}
        {run.status === 'Calculated' && (
          <>
            <Button
              size="sm"
              onClick={() => runAction('calculate')}
              variant="outline"
              isLoading={pendingAction === 'calculate'}
            >
              <Calculator size={14} />
              {t('Payroll.runDetail.recalculate', { defaultValue: 'Yeniden Hesapla' })}
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={() =>
                runAction('reopen', {
                  confirmTitle: t('Payroll.runDetail.reopenTitle', {
                    defaultValue: 'Bordroyu Yeniden Aç',
                  }),
                  confirmMessage: t('Payroll.runDetail.reopenConfirm', {
                    defaultValue: 'Bordro taslak durumuna döndürülecek. Devam edilsin mi?',
                  }),
                  tone: 'danger',
                })
              }
              isLoading={pendingAction === 'reopen'}
            >
              <RotateCcw size={14} />
              {t('Payroll.runDetail.reopen', { defaultValue: 'Yeniden Aç' })}
            </Button>
            <Button
              size="sm"
              onClick={() =>
                runAction('approve', {
                  confirmTitle: t('Payroll.runDetail.approveTitle', {
                    defaultValue: 'Bordroyu Onayla',
                  }),
                  confirmMessage: t('Payroll.runDetail.approveConfirm', {
                    defaultValue:
                      'Bordro onaylandıktan sonra değişiklik için yeniden açılması gerekir. Devam edilsin mi?',
                  }),
                })
              }
              isLoading={pendingAction === 'approve'}
            >
              <ShieldCheck size={14} />
              {t('Payroll.runDetail.approve', { defaultValue: 'Onayla' })}
            </Button>
          </>
        )}
        {run.status === 'Approved' && (
          <Button
            size="sm"
            onClick={() =>
              runAction('post', {
                confirmTitle: t('Payroll.runDetail.postTitle', {
                  defaultValue: 'Muhasebeleştir',
                }),
                confirmMessage: t('Payroll.runDetail.postConfirm', {
                  defaultValue: 'Bordro muhasebe kayıtlarına işlenecek. Devam edilsin mi?',
                }),
              })
            }
            isLoading={pendingAction === 'post'}
          >
            <CheckCircle2 size={14} />
            {t('Payroll.runDetail.post', { defaultValue: 'Muhasebeleştir' })}
          </Button>
        )}
        {run.status === 'Posted' && (
          <Button size="sm" onClick={() => runAction('pay')} isLoading={pendingAction === 'pay'}>
            <Banknote size={14} />
            {t('Payroll.runDetail.pay', { defaultValue: 'Net Maaşları Öde' })}
          </Button>
        )}
        {run.status === 'Paid' && (
          <span className="text-sm text-success-600 dark:text-success-400">
            {t('Payroll.runDetail.fullyPaid', { defaultValue: 'Bu dönem ödendi.' })}
          </span>
        )}
      </div>

      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
        <Summary
          label={t('Payroll.runDetail.gross', { defaultValue: 'Brüt' })}
          value={formatCurrency(run.totalGross, locale, run.currency)}
        />
        <Summary
          label={t('Payroll.runDetail.sgkEmployee', { defaultValue: 'SGK İşçi' })}
          value={formatCurrency(run.totalSgkEmployee, locale, run.currency)}
        />
        <Summary
          label={t('Payroll.runDetail.incomeTax', { defaultValue: 'Gelir Vergisi' })}
          value={formatCurrency(run.totalIncomeTax, locale, run.currency)}
        />
        <Summary
          label={t('Payroll.runDetail.stampTax', { defaultValue: 'Damga Vergisi' })}
          value={formatCurrency(run.totalStampTax, locale, run.currency)}
        />
        <Summary
          label={t('Payroll.runDetail.net', { defaultValue: 'Net' })}
          value={formatCurrency(run.totalNet, locale, run.currency)}
          highlight
        />
        <Summary
          label={t('Payroll.runDetail.employerCost', { defaultValue: 'İşveren Maliyeti' })}
          value={formatCurrency(run.totalEmployerCost, locale, run.currency)}
        />
      </div>

      <div className="space-y-2">
        <h2 className="text-sm font-semibold text-slate-800 dark:text-slate-100">
          {t('Payroll.runDetail.payslips', { defaultValue: 'Bordro Pusulaları' })}
        </h2>
        <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
          {payslipsQuery.isPending ? (
            <div className="px-3 py-8 text-center text-sm text-slate-500">
              {t('common.loading', { defaultValue: 'Yükleniyor…' })}
            </div>
          ) : payslips.length === 0 ? (
            <div className="px-3 py-10 text-center text-sm text-slate-500 dark:text-slate-400">
              {t('Payroll.runDetail.payslipsEmpty', {
                defaultValue: 'Henüz pusula yok. Hesapla adımını çalıştırın.',
              })}
            </div>
          ) : (
            <table className="w-full text-sm">
              <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
                <tr>
                  <th className="px-3 py-2 text-left">
                    {t('Payroll.runDetail.cols.employee', { defaultValue: 'Personel' })}
                  </th>
                  <th className="px-3 py-2 text-right">
                    {t('Payroll.runDetail.cols.gross', { defaultValue: 'Brüt' })}
                  </th>
                  <th className="px-3 py-2 text-right">
                    {t('Payroll.runDetail.cols.deductions', { defaultValue: 'Kesintiler' })}
                  </th>
                  <th className="px-3 py-2 text-right">
                    {t('Payroll.runDetail.cols.net', { defaultValue: 'Net' })}
                  </th>
                  <th className="px-3 py-2 text-right">
                    {t('Payroll.runDetail.cols.employerCost', { defaultValue: 'İşveren Maliyeti' })}
                  </th>
                  <th className="px-3 py-2" />
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
                {payslips.map((p) => (
                  <tr key={p.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                    <td className="px-3 py-2 text-slate-800 dark:text-slate-100">
                      {p.employeeFullName}
                      <div className="font-mono text-[11px] text-slate-500">{p.employeeNumber}</div>
                    </td>
                    <td className="px-3 py-2 text-right font-mono text-slate-700 dark:text-slate-300">
                      {formatCurrency(p.grossEarnings, locale, run.currency)}
                    </td>
                    <td className="px-3 py-2 text-right font-mono text-danger-600 dark:text-danger-400">
                      {formatCurrency(payslipTotalDeductions(p), locale, run.currency)}
                    </td>
                    <td className="px-3 py-2 text-right font-mono font-semibold text-slate-900 dark:text-slate-100">
                      {formatCurrency(p.netPay, locale, run.currency)}
                    </td>
                    <td className="px-3 py-2 text-right font-mono text-slate-600 dark:text-slate-300">
                      {formatCurrency(p.employerCost, locale, run.currency)}
                    </td>
                    <td className="px-3 py-2 text-right">
                      <button
                        type="button"
                        onClick={() => navigate(`/payslips/${p.id}/print`)}
                        className="inline-flex items-center gap-1 rounded p-1 text-primary-500 hover:bg-primary-50 dark:hover:bg-primary-500/10"
                        title={t('Payroll.runDetail.viewPayslip', {
                          defaultValue: 'Bordro Pusulası',
                        })}
                      >
                        <FileText size={14} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {(run.paidAtUtc || run.postedAtUtc || run.calculatedAtUtc) && (
        <div className="flex flex-wrap gap-4 text-xs text-slate-500 dark:text-slate-400">
          {run.calculatedAtUtc && (
            <span>
              {t('Payroll.runDetail.calculatedAt', { defaultValue: 'Hesaplandı' })}:{' '}
              {formatDate(run.calculatedAtUtc, locale)}
            </span>
          )}
          {run.approvedAtUtc && (
            <span>
              {t('Payroll.runDetail.approvedAt', { defaultValue: 'Onaylandı' })}:{' '}
              {formatDate(run.approvedAtUtc, locale)}
            </span>
          )}
          {run.postedAtUtc && (
            <span>
              {t('Payroll.runDetail.postedAt', { defaultValue: 'Muhasebeleşti' })}:{' '}
              {formatDate(run.postedAtUtc, locale)}
            </span>
          )}
          {run.paidAtUtc && (
            <span>
              {t('Payroll.runDetail.paidAt', { defaultValue: 'Ödendi' })}:{' '}
              {formatDate(run.paidAtUtc, locale)}
            </span>
          )}
        </div>
      )}
    </DetailPageTemplate>
  );
};

const Summary = ({
  label,
  value,
  highlight,
}: {
  label: string;
  value: string;
  highlight?: boolean;
}) => (
  <div
    className={`rounded-lg border p-3 ${
      highlight
        ? 'border-primary-200 bg-primary-50/50 dark:border-primary-500/30 dark:bg-primary-500/10'
        : 'border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900'
    }`}
  >
    <div className="text-[10px] font-semibold uppercase tracking-wider text-slate-500">{label}</div>
    <div className="mt-0.5 font-mono text-sm font-bold text-slate-900 dark:text-slate-100">
      {value}
    </div>
  </div>
);

export default PayrollRunDetailPage;
