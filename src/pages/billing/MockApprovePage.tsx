import { useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, Ban, CheckCircle2, Loader2, TriangleAlert, Wallet } from 'lucide-react';
import { toast } from 'sonner';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { Skeleton } from '@/shared/ui/Skeleton/Skeleton';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { SubscriptionStatusBadge } from '@/features/billing/ui/SubscriptionStatusBadge';
import { useMockApprove, useSubscriptionOrderQuery } from '@/features/billing/hooks/useBilling';
import { useIsTenantAdmin } from '@/features/billing/hooks/useIsTenantAdmin';
import type { MockApproveAction } from '@/features/billing/model/billing.types';

const TOAST_BY_ACTION: Record<MockApproveAction, string> = {
  approve: 'billing.toast.approved',
  cancel: 'billing.toast.cancelled',
  fail: 'billing.toast.failed',
};

export const MockApprovePage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const locale = useFormatLocale();
  const [params] = useSearchParams();
  const orderId = params.get('order');
  const intentId = params.get('intent');
  const isAdmin = useIsTenantAdmin();

  const orderQuery = useSubscriptionOrderQuery(orderId);
  const mockApprove = useMockApprove();
  const order = orderQuery.data?.data;

  const handle = (action: MockApproveAction) => {
    if (!orderId) return;
    mockApprove.mutate(
      { orderId, action, reference: intentId ?? undefined },
      {
        onSuccess: () => {
          toast.success(t(TOAST_BY_ACTION[action] as never));
          navigate(`/dashboard/billing/orders/${orderId}`);
        },
        onError: (err) => toastApiError(err, t('billing.toast.failed')),
      },
    );
  };

  return (
    <div className="space-y-4 p-4">
      <PageHeader
        icon={<Wallet size={20} />}
        eyebrow={t('billing.mock.eyebrow')}
        title={t('billing.mock.title')}
        subtitle={t('billing.mock.subtitle')}
        tone="amber"
        crumbs={[
          { label: t('billing.crumbs.billing'), to: '/dashboard/billing' },
          { label: t('billing.mock.title') },
        ]}
      />

      <div className="flex items-start gap-2 rounded-xl border-2 border-dashed border-amber-400 bg-amber-50/70 p-3 dark:border-amber-400/60 dark:bg-amber-500/10">
        <TriangleAlert size={18} className="mt-0.5 shrink-0 text-amber-600 dark:text-amber-300" />
        <div className="text-xs text-amber-800 dark:text-amber-200">
          <p className="font-bold uppercase tracking-wider">{t('billing.mock.banner')}</p>
          <p className="mt-0.5 text-[11px]">{t('billing.mock.bannerDescription')}</p>
        </div>
      </div>

      {!orderId && (
        <div className="rounded-xl border border-rose-200 bg-rose-50/60 p-4 text-xs text-rose-700 dark:border-rose-500/30 dark:bg-rose-500/10 dark:text-rose-200">
          <AlertTriangle size={16} className="inline-block mr-1" />
          {t('billing.mock.missingOrder')}
        </div>
      )}

      {orderId && orderQuery.isPending && <Skeleton className="h-32 w-full" />}

      {orderId && orderQuery.isError && (
        <QueryError
          onRetry={() => orderQuery.refetch()}
          isRetrying={orderQuery.isFetching}
          title={t('billing.errors.orderTitle')}
        />
      )}

      {orderId && order && (
        <section className="rounded-xl border border-slate-200/70 bg-white p-4 shadow-sm dark:border-slate-800/70 dark:bg-slate-900">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <p className="text-[10px] uppercase tracking-wider text-slate-400">
                {t('billing.mock.orderLabel')}
              </p>
              <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100">
                {order.orderNumber}
              </h2>
              <div className="mt-1">
                <SubscriptionStatusBadge status={order.status} />
              </div>
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

          {intentId && (
            <p className="mt-3 break-all rounded-md bg-slate-50 px-2 py-1 font-mono text-[11px] text-slate-600 dark:bg-slate-800/60 dark:text-slate-300">
              {t('billing.order.intentId')}: {intentId}
            </p>
          )}

          {!isAdmin && (
            <p className="mt-3 rounded-md bg-amber-50 px-2 py-1.5 text-[11px] text-amber-800 dark:bg-amber-500/10 dark:text-amber-200">
              {t('billing.mock.adminOnly')}
            </p>
          )}

          <div className="mt-4 grid grid-cols-1 gap-2 sm:grid-cols-3">
            <button
              type="button"
              onClick={() => handle('approve')}
              disabled={!isAdmin || mockApprove.isPending}
              className="inline-flex items-center justify-center gap-1.5 rounded-lg bg-emerald-600 px-3 py-2 text-xs font-semibold text-white hover:bg-emerald-700 disabled:cursor-not-allowed disabled:bg-emerald-600/50"
            >
              {mockApprove.isPending ? (
                <Loader2 size={13} className="animate-spin" />
              ) : (
                <CheckCircle2 size={13} />
              )}
              {t('billing.mock.approve')}
            </button>
            <button
              type="button"
              onClick={() => handle('cancel')}
              disabled={!isAdmin || mockApprove.isPending}
              className="inline-flex items-center justify-center gap-1.5 rounded-lg border border-slate-300 bg-white px-3 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              <Ban size={13} />
              {t('billing.mock.cancel')}
            </button>
            <button
              type="button"
              onClick={() => handle('fail')}
              disabled={!isAdmin || mockApprove.isPending}
              className="inline-flex items-center justify-center gap-1.5 rounded-lg bg-rose-600 px-3 py-2 text-xs font-semibold text-white hover:bg-rose-700 disabled:cursor-not-allowed disabled:bg-rose-600/50"
            >
              <AlertTriangle size={13} />
              {t('billing.mock.fail')}
            </button>
          </div>
        </section>
      )}
    </div>
  );
};

export default MockApprovePage;
