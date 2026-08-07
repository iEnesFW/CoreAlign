import { useMemo } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, BadgeCheck, Ban, CreditCard, Loader2, Wallet } from 'lucide-react';
import { toast } from 'sonner';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { Skeleton } from '@/shared/ui/Skeleton/Skeleton';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatCurrency, formatDateTime } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { SubscriptionStatusBadge } from '@/features/billing/ui/SubscriptionStatusBadge';
import {
  useCancelSubscriptionOrder,
  useSubscriptionOrderQuery,
} from '@/features/billing/hooks/useBilling';
import { useIsTenantAdmin } from '@/shared/lib/auth/useIsTenantAdmin';

const MOCK_GATEWAY = 'mock';

export const OrderDetailPage = () => {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const confirm = useConfirm();
  const locale = useFormatLocale();
  const isAdmin = useIsTenantAdmin();

  const orderQuery = useSubscriptionOrderQuery(id ?? null);
  const cancel = useCancelSubscriptionOrder();

  const order = orderQuery.data?.data;
  const isMockPending = useMemo(
    () =>
      order?.status === 'PendingPayment' &&
      (order?.gatewayName ?? '').toLowerCase() === MOCK_GATEWAY,
    [order],
  );
  const isPaidLike = order?.status === 'Paid';
  const canCancel = isAdmin && (order?.status === 'Draft' || order?.status === 'PendingPayment');

  const handleCancel = async () => {
    if (!order) return;
    const ok = await confirm({
      title: t('billing.cancel.confirmTitle'),
      message: t('billing.cancel.confirmMessage', { number: order.orderNumber }),
      confirmLabel: t('billing.cancel.confirm'),
      cancelLabel: t('common.cancel', { defaultValue: 'Cancel' }),
      tone: 'danger',
    });
    if (!ok) return;
    cancel.mutate(
      { id: order.id },
      {
        onSuccess: () => toast.success(t('billing.toast.cancelled')),
        onError: (err) => toastApiError(err, t('billing.toast.failed')),
      },
    );
  };

  const handleGoToMock = () => {
    if (!order) return;
    const params = new URLSearchParams({ order: order.id });
    if (order.gatewayIntentId) params.set('intent', order.gatewayIntentId);
    navigate(`/dashboard/billing/mock-approve?${params.toString()}`);
  };

  if (orderQuery.isPending) {
    return (
      <div className="space-y-4 p-4">
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-48 w-full" />
      </div>
    );
  }

  if (orderQuery.isError || !order) {
    return (
      <div className="space-y-4 p-4">
        <QueryError
          onRetry={() => orderQuery.refetch()}
          isRetrying={orderQuery.isFetching}
          title={t('billing.errors.orderTitle')}
          description={t('billing.errors.orderDescription')}
        />
      </div>
    );
  }

  return (
    <div className="space-y-4 p-4">
      <PageHeader
        icon={<CreditCard size={20} />}
        eyebrow={t('billing.eyebrow')}
        title={t('billing.order.title', { number: order.orderNumber })}
        subtitle={t('billing.order.subtitle')}
        tone="indigo"
        crumbs={[
          { label: t('billing.crumbs.billing'), to: '/dashboard/billing' },
          { label: t('billing.crumbs.orders'), to: '/dashboard/billing/orders' },
          { label: order.orderNumber },
        ]}
        actions={
          <div className="flex flex-wrap items-center gap-2">
            <Link
              to="/dashboard/billing/orders"
              className="inline-flex items-center gap-1 rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300 dark:hover:bg-slate-800"
            >
              <ArrowLeft size={12} />
              {t('billing.order.backToList')}
            </Link>
            {canCancel && (
              <button
                type="button"
                onClick={handleCancel}
                disabled={cancel.isPending}
                className="inline-flex items-center gap-1 rounded-lg border border-danger-200 bg-danger-50 px-2.5 py-1.5 text-xs font-semibold text-danger-700 hover:bg-danger-100 disabled:opacity-50 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-300"
              >
                {cancel.isPending ? (
                  <Loader2 size={12} className="animate-spin" />
                ) : (
                  <Ban size={12} />
                )}
                {t('billing.cancel.action')}
              </button>
            )}
          </div>
        }
      />

      <section className="rounded-xl border border-slate-200/70 bg-white p-4 shadow-sm dark:border-slate-800/70 dark:bg-slate-900">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100">
                {order.orderNumber}
              </h2>
              <SubscriptionStatusBadge status={order.status} />
            </div>
            <p className="mt-1 text-[11px] text-slate-500 dark:text-slate-400">
              {t('billing.order.createdAt', { value: formatDateTime(order.createdAtUtc, locale) })}
            </p>
          </div>
          <div className="text-right">
            <p className="text-[10px] uppercase tracking-wider text-slate-400">
              {t('billing.order.totalLabel')}
            </p>
            <p className="text-xl font-bold tabular-nums text-slate-900 dark:text-slate-100">
              {formatCurrency(order.totalAmount, locale, order.currency)}
            </p>
          </div>
        </div>

        {isPaidLike && (
          <div className="mt-3 flex items-start gap-2 rounded-lg border border-success-200 bg-success-50/60 p-3 text-xs text-success-800 dark:border-success-500/30 dark:bg-success-500/10 dark:text-success-200">
            <BadgeCheck size={16} className="mt-0.5 shrink-0" />
            <div>
              <p className="font-semibold">{t('billing.order.paidTitle')}</p>
              <p className="mt-0.5 text-[11px] text-success-700 dark:text-success-200/80">
                {t('billing.order.paidDescription', {
                  value: order.paidAtUtc ? formatDateTime(order.paidAtUtc, locale) : '—',
                })}
              </p>
            </div>
          </div>
        )}

        {order.status === 'Cancelled' && (
          <div className="mt-3 rounded-lg border border-slate-200 bg-slate-50/60 p-3 text-xs text-slate-600 dark:border-slate-700 dark:bg-slate-800/40 dark:text-slate-300">
            {t('billing.order.cancelled')}
          </div>
        )}

        {isMockPending && (
          <div className="mt-3 flex flex-wrap items-center justify-between gap-2 rounded-lg border border-warning-200 bg-warning-50/70 p-3 dark:border-warning-500/30 dark:bg-warning-500/10">
            <div className="min-w-0">
              <p className="text-xs font-semibold text-warning-800 dark:text-warning-200">
                {t('billing.order.payNowTitle')}
              </p>
              <p className="mt-0.5 text-[11px] text-warning-700 dark:text-warning-200/80">
                {t('billing.order.payNowDescription')}
              </p>
            </div>
            <button
              type="button"
              onClick={handleGoToMock}
              className="inline-flex items-center gap-1.5 rounded-lg bg-warning-500 px-3 py-1.5 text-xs font-semibold text-white shadow-sm hover:bg-warning-600"
            >
              <Wallet size={13} />
              {t('billing.order.payNow')}
            </button>
          </div>
        )}
      </section>

      <section className="rounded-xl border border-slate-200/70 bg-white p-4 shadow-sm dark:border-slate-800/70 dark:bg-slate-900">
        <h3 className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {t('billing.order.itemsTitle')}
        </h3>
        <div className="mt-2 overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="border-b border-slate-200/70 text-[10px] uppercase tracking-wider text-slate-500 dark:border-slate-800/70 dark:text-slate-400">
              <tr>
                <th className="px-2 py-2 font-semibold">{t('billing.order.col.module')}</th>
                <th className="px-2 py-2 font-semibold">{t('billing.order.col.plan')}</th>
                <th className="px-2 py-2 text-right font-semibold">
                  {t('billing.order.col.duration')}
                </th>
                <th className="px-2 py-2 text-right font-semibold">
                  {t('billing.order.col.unitPrice')}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {order.items.map((item) => (
                <tr key={item.id}>
                  <td className="px-2 py-2 text-slate-800 dark:text-slate-200">
                    <div className="font-medium">{item.moduleName}</div>
                    <div className="text-[10px] text-slate-400">{item.moduleCode}</div>
                  </td>
                  <td className="px-2 py-2 text-slate-700 dark:text-slate-300">{item.planLabel}</td>
                  <td className="px-2 py-2 text-right tabular-nums text-slate-700 dark:text-slate-300">
                    {t('billing.modules.durationDays', { count: item.durationDays })}
                  </td>
                  <td className="px-2 py-2 text-right tabular-nums font-medium text-slate-900 dark:text-slate-100">
                    {formatCurrency(item.unitPrice, locale, item.currency)}
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t border-slate-200/70 dark:border-slate-800/70">
                <td
                  className="px-2 py-2 text-[11px] uppercase tracking-wider text-slate-500"
                  colSpan={3}
                >
                  {t('billing.order.totalLabel')}
                </td>
                <td className="px-2 py-2 text-right text-sm font-bold tabular-nums text-slate-900 dark:text-slate-100">
                  {formatCurrency(order.totalAmount, locale, order.currency)}
                </td>
              </tr>
            </tfoot>
          </table>
        </div>
      </section>

      <section className="rounded-xl border border-slate-200/70 bg-white p-4 shadow-sm dark:border-slate-800/70 dark:bg-slate-900">
        <h3 className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {t('billing.order.paymentTitle')}
        </h3>
        <dl className="mt-2 grid grid-cols-1 gap-3 text-xs sm:grid-cols-2 lg:grid-cols-3">
          <PaymentField label={t('billing.order.gateway')} value={order.gatewayName ?? '—'} />
          <PaymentField
            label={t('billing.order.intentId')}
            value={order.gatewayIntentId ?? '—'}
            mono
          />
          <PaymentField
            label={t('billing.order.reference')}
            value={order.paymentReference ?? '—'}
            mono
          />
          <PaymentField
            label={t('billing.order.paidAt')}
            value={order.paidAtUtc ? formatDateTime(order.paidAtUtc, locale) : '—'}
          />
          <PaymentField
            label={t('billing.order.completedAt')}
            value={order.completedAtUtc ? formatDateTime(order.completedAtUtc, locale) : '—'}
          />
        </dl>

        {order.attempts.length > 0 && (
          <div className="mt-3">
            <h4 className="text-[11px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {t('billing.order.attempts')}
            </h4>
            <ul className="mt-1 space-y-1">
              {order.attempts.map((attempt) => (
                <li
                  key={attempt.id}
                  className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-slate-100 px-2 py-1.5 text-[11px] dark:border-slate-800"
                >
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="font-medium text-slate-700 dark:text-slate-200">
                      {attempt.gatewayName}
                    </span>
                    <span className="rounded-full bg-slate-100 px-1.5 py-0.5 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                      {attempt.status}
                    </span>
                    <span className="text-slate-500 dark:text-slate-400">
                      {formatDateTime(attempt.attemptedAtUtc, locale)}
                    </span>
                  </div>
                  <span className="tabular-nums text-slate-700 dark:text-slate-300">
                    {formatCurrency(attempt.amount, locale, attempt.currency)}
                  </span>
                </li>
              ))}
            </ul>
          </div>
        )}
      </section>
    </div>
  );
};

interface PaymentFieldProps {
  label: string;
  value: string;
  mono?: boolean;
}

const PaymentField = ({ label, value, mono }: PaymentFieldProps) => (
  <div>
    <dt className="text-[10px] font-semibold uppercase tracking-wider text-slate-400">{label}</dt>
    <dd
      className={`mt-0.5 text-xs text-slate-800 dark:text-slate-200 ${mono ? 'font-mono break-all' : ''}`}
    >
      {value}
    </dd>
  </div>
);

export default OrderDetailPage;
