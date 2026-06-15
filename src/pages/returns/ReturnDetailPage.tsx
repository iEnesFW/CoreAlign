import { useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Ban, CheckCircle2, PackageCheck, RotateCcw, XCircle } from 'lucide-react';
import { toast } from 'sonner';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { Badge, type BadgeVariant } from '@/shared/ui/Badge/Badge';
import { Button } from '@/shared/ui/Button/Button';
import { formatCurrency, formatDate, formatDateTime } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useApproveReturn,
  useCancelReturn,
  useReceiveReturn,
  useRejectReturn,
  useReturnRequestQuery,
} from '@/features/returns/hooks/useReturnQueries';
import { useWarehousesQuery } from '@/features/master-data/hooks/useMasterData';
import type { ReturnRequest, ReturnRequestStatus } from '@/features/returns/model/return.types';

const statusVariant: Record<ReturnRequestStatus, BadgeVariant> = {
  Requested: 'default',
  Approved: 'default',
  Rejected: 'error',
  Received: 'warning',
  CreditNoted: 'success',
  Refunded: 'success',
  Cancelled: 'neutral',
};

export const ReturnDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const query = useReturnRequestQuery(id ?? null);

  const approve = useApproveReturn();
  const reject = useRejectReturn();
  const cancel = useCancelReturn();
  const receive = useReceiveReturn();
  const warehousesQuery = useWarehousesQuery(true);

  const [rejectionReason, setRejectionReason] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [autoIssueCreditNote, setAutoIssueCreditNote] = useState(true);

  const entity = query.data?.data;
  const lines = useMemo(() => entity?.lines ?? [], [entity]);
  const warehouses = warehousesQuery.data?.data ?? [];

  const runMutation = async (label: string, runner: () => Promise<unknown>) => {
    try {
      await runner();
      toast.success(label);
    } catch (error) {
      toastApiError(error);
    }
  };

  if (query.isError) {
    return (
      <div className="p-4">
        <QueryError onRetry={() => query.refetch()} />
      </div>
    );
  }

  if (!entity) {
    return (
      <div className="p-4 text-center text-sm text-slate-500">
        {t('common.loading', { defaultValue: 'Loading...' })}
      </div>
    );
  }

  const canApprove = entity.status === 'Requested';
  const canReject = entity.status === 'Requested';
  const canCancel = !['Rejected', 'Refunded', 'Cancelled', 'CreditNoted'].includes(entity.status);
  const canReceive = entity.status === 'Approved';

  return (
    <div className="flex flex-col gap-4 p-4">
      <PageHeader
        title={entity.returnNumber}
        eyebrow={t('Returns.title')}
        subtitle={entity.customerName}
        icon={<RotateCcw size={18} />}
        tone="rose"
        crumbs={[
          { label: t('Returns.title'), to: '/dashboard/returns' },
          { label: entity.returnNumber },
        ]}
        trailing={
          <Badge variant={statusVariant[entity.status]}>
            {t(`Returns.status.${entity.status}`)}
          </Badge>
        }
        actions={
          <div className="flex flex-wrap items-center gap-2">
            {canApprove && (
              <Button
                variant="primary"
                onClick={() =>
                  runMutation(t('Returns.toast.approved'), () => approve.mutateAsync(entity.id))
                }
              >
                <CheckCircle2 size={14} /> {t('Returns.actions.approve')}
              </Button>
            )}
            {canReceive && (
              <Button
                variant="primary"
                disabled={!warehouseId}
                onClick={() => {
                  if (!warehouseId) {
                    toast.error(t('Returns.toast.warehouseRequired'));
                    return;
                  }
                  void runMutation(t('Returns.toast.received'), () =>
                    receive.mutateAsync({
                      id: entity.id,
                      payload: { warehouseId, autoIssueCreditNote },
                    }),
                  );
                }}
              >
                <PackageCheck size={14} /> {t('Returns.actions.receive')}
              </Button>
            )}
            {canCancel && (
              <Button
                variant="outline"
                onClick={() =>
                  runMutation(t('Returns.toast.cancelled'), () => cancel.mutateAsync(entity.id))
                }
              >
                <Ban size={14} /> {t('Returns.actions.cancel')}
              </Button>
            )}
          </div>
        }
      />

      <section className="grid grid-cols-1 gap-3 md:grid-cols-3">
        <Card title={t('Returns.fields.order')} value={entity.orderNumber} />
        <Card title={t('Returns.fields.reason')} value={t(`Returns.reason.${entity.reason}`)} />
        <Card
          title={t('Returns.fields.requestedAt')}
          value={formatDate(entity.requestedAtUtc, locale)}
        />
        <Card
          title={t('Returns.fields.total')}
          value={formatCurrency(entity.total, locale, entity.currency)}
        />
        {entity.creditNoteNumber && (
          <Card title={t('Returns.fields.creditNote')} value={entity.creditNoteNumber} />
        )}
        {entity.sourceInvoiceNumber && (
          <Card title={t('Returns.fields.sourceInvoice')} value={entity.sourceInvoiceNumber} />
        )}
      </section>

      {canReceive && (
        <section className="rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
          <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            {t('Returns.receive.title')}
          </h2>
          <div className="mt-3 grid gap-3 md:grid-cols-2">
            <label className="text-xs">
              <span className="mb-1 block font-medium text-slate-700 dark:text-slate-300">
                {t('Returns.receive.warehouseId')}
              </span>
              <select
                value={warehouseId}
                onChange={(e) => setWarehouseId(e.target.value)}
                disabled={warehousesQuery.isLoading || warehouses.length === 0}
                className="w-full rounded border border-slate-300 px-2 py-1.5 text-xs focus:border-indigo-500 focus:outline-none disabled:opacity-60 dark:border-slate-700 dark:bg-slate-800"
              >
                <option value="">{t('Returns.receive.warehousePlaceholder')}</option>
                {warehouses.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.code} — {w.name}
                  </option>
                ))}
              </select>
            </label>
            <label className="flex items-center gap-2 text-xs">
              <input
                type="checkbox"
                checked={autoIssueCreditNote}
                onChange={(e) => setAutoIssueCreditNote(e.target.checked)}
                className="h-4 w-4 accent-indigo-500"
              />
              {t('Returns.receive.autoIssueCreditNote')}
            </label>
          </div>
        </section>
      )}

      {canReject && (
        <section className="rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
          <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            {t('Returns.reject.title')}
          </h2>
          <textarea
            rows={2}
            value={rejectionReason}
            onChange={(e) => setRejectionReason(e.target.value)}
            placeholder={t('Returns.reject.placeholder')}
            className="mt-2 block w-full rounded border border-slate-300 px-2 py-1.5 text-xs focus:border-indigo-500 focus:outline-none dark:border-slate-700 dark:bg-slate-800"
          />
          <div className="mt-2 flex justify-end">
            <Button
              variant="outline"
              onClick={() =>
                runMutation(t('Returns.toast.rejected'), () =>
                  reject.mutateAsync({ id: entity.id, reason: rejectionReason || null }),
                )
              }
            >
              <XCircle size={14} /> {t('Returns.actions.reject')}
            </Button>
          </div>
        </section>
      )}

      <section className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        <table className="w-full text-left text-xs">
          <thead className="bg-slate-50 dark:bg-slate-800/50">
            <tr>
              <th className="px-3 py-2 font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('Returns.fields.product')}
              </th>
              <th className="px-3 py-2 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('Returns.fields.quantity')}
              </th>
              <th className="px-3 py-2 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('Returns.fields.unitPrice')}
              </th>
              <th className="px-3 py-2 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('Returns.fields.subtotal')}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
            {lines.map((line) => (
              <tr key={line.id}>
                <td className="px-3 py-2">
                  <div className="font-medium text-slate-900 dark:text-slate-100">
                    {line.productName}
                  </div>
                  <div className="font-mono text-[10px] text-slate-500">{line.productSku}</div>
                </td>
                <td className="px-3 py-2 text-right tabular-nums">{line.quantityReturned}</td>
                <td className="px-3 py-2 text-right tabular-nums">
                  {formatCurrency(line.unitPrice, locale, entity.currency)}
                </td>
                <td className="px-3 py-2 text-right font-medium tabular-nums">
                  {formatCurrency(line.lineTotal, locale, entity.currency)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <ActivityList entity={entity} locale={locale} />

      <Button variant="ghost" onClick={() => navigate('/dashboard/returns')}>
        {t('Returns.backToList')}
      </Button>
    </div>
  );
};

interface CardProps {
  title: string;
  value: string;
}

const Card = ({ title, value }: CardProps) => (
  <div className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
    <div className="text-[10px] uppercase tracking-wider text-slate-500">{title}</div>
    <div className="mt-1 text-sm font-semibold text-slate-900 dark:text-slate-100">{value}</div>
  </div>
);

interface ActivityListProps {
  entity: ReturnRequest;
  locale: string;
}

const ActivityList = ({ entity, locale }: ActivityListProps) => {
  const { t } = useTranslation();
  const events: { key: string; label: string; at: string; tone: BadgeVariant }[] = [];
  events.push({
    key: 'requested',
    label: t('Returns.activity.requested'),
    at: formatDateTime(entity.requestedAtUtc, locale),
    tone: 'default',
  });
  if (entity.approvedAtUtc) {
    events.push({
      key: 'approved',
      label: t('Returns.activity.approved'),
      at: formatDateTime(entity.approvedAtUtc, locale),
      tone: 'default',
    });
  }
  if (entity.rejectedAtUtc) {
    events.push({
      key: 'rejected',
      label: t('Returns.activity.rejected'),
      at: formatDateTime(entity.rejectedAtUtc, locale),
      tone: 'error',
    });
  }
  if (entity.receivedAtUtc) {
    events.push({
      key: 'received',
      label: t('Returns.activity.received'),
      at: formatDateTime(entity.receivedAtUtc, locale),
      tone: 'warning',
    });
  }
  if (entity.creditNoteIssuedAtUtc) {
    events.push({
      key: 'creditNoted',
      label: t('Returns.activity.creditNoted'),
      at: formatDateTime(entity.creditNoteIssuedAtUtc, locale),
      tone: 'success',
    });
  }
  if (entity.refundedAtUtc) {
    events.push({
      key: 'refunded',
      label: t('Returns.activity.refunded'),
      at: formatDateTime(entity.refundedAtUtc, locale),
      tone: 'success',
    });
  }
  if (entity.cancelledAtUtc) {
    events.push({
      key: 'cancelled',
      label: t('Returns.activity.cancelled'),
      at: formatDateTime(entity.cancelledAtUtc, locale),
      tone: 'neutral',
    });
  }

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
      <h2 className="mb-3 text-sm font-semibold text-slate-900 dark:text-slate-100">
        {t('Returns.activity.title')}
      </h2>
      <ol className="space-y-2">
        {events.map((ev) => (
          <li
            key={ev.key}
            className="flex items-center justify-between gap-2 rounded border border-slate-100 px-3 py-2 text-xs dark:border-slate-800"
          >
            <Badge variant={ev.tone}>{ev.label}</Badge>
            <span className="tabular-nums text-slate-500">{ev.at}</span>
          </li>
        ))}
      </ol>
    </section>
  );
};

export default ReturnDetailPage;
