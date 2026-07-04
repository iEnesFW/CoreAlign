import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import {
  CheckCircle2,
  Lock,
  PackageCheck,
  Pencil,
  Plus,
  Send,
  ShoppingCart,
  Trash2,
  XCircle,
} from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { DataToolbar } from '@/shared/ui/DataToolbar/DataToolbar';
import { SegmentedControl } from '@/shared/ui/SegmentedControl/SegmentedControl';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Select } from '@/shared/ui/Select/Select';
import { useVendorsQuery } from '@/features/vendors/hooks/useVendorQueries';
import {
  useDeletePurchaseOrder,
  usePurchaseOrderAction,
  usePurchaseOrdersQuery,
} from '@/features/purchasing/hooks/usePurchaseOrders';
import { PurchaseOrderFormModal } from '@/features/purchasing/ui/PurchaseOrderFormModal';
import { ReceivePurchaseOrderModal } from '@/features/purchasing/ui/ReceivePurchaseOrderModal';
import type {
  PurchaseOrder,
  PurchaseOrderStatus,
} from '@/features/purchasing/model/purchaseOrder.types';

type StatusFilter = 'all' | PurchaseOrderStatus;

const STATUS_TONE: Record<PurchaseOrderStatus, string> = {
  Draft: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300',
  Submitted: 'bg-info-100 text-info-700 dark:bg-info-500/20 dark:text-info-300',
  Approved: 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300',
  PartiallyReceived: 'bg-warning-100 text-warning-800 dark:bg-warning-500/20 dark:text-warning-300',
  Received: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
  Closed: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300',
  Cancelled: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
};

const STATUS_LABEL_FALLBACK: Record<PurchaseOrderStatus, string> = {
  Draft: 'Taslak',
  Submitted: 'Gönderildi',
  Approved: 'Onaylandı',
  PartiallyReceived: 'Kısmi Teslim',
  Received: 'Teslim Alındı',
  Closed: 'Kapandı',
  Cancelled: 'İptal',
};

const STATUSES: PurchaseOrderStatus[] = [
  'Draft',
  'Submitted',
  'Approved',
  'PartiallyReceived',
  'Received',
  'Closed',
  'Cancelled',
];

const fmtDate = (iso: string, locale: string) => formatDate(iso, locale);

export const PurchaseOrdersPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const confirm = useConfirm();

  const [status, setStatus] = useState<StatusFilter>('all');
  const [vendorId, setVendorId] = useState('');
  const [page, setPage] = useState(1);
  const [modal, setModal] = useState<
    { mode: 'create' } | { mode: 'edit'; order: PurchaseOrder } | null
  >(null);
  const [receiveOrder, setReceiveOrder] = useState<PurchaseOrder | null>(null);

  const statusLabel = (s: PurchaseOrderStatus) =>
    t(`po.status.${s}`, { defaultValue: STATUS_LABEL_FALLBACK[s] });

  const query = usePurchaseOrdersQuery({
    status: status === 'all' ? undefined : status,
    vendorId: vendorId || undefined,
    page,
    pageSize: 25,
  });
  const action = usePurchaseOrderAction();
  const deleteMutation = useDeletePurchaseOrder();

  const vendorsQuery = useVendorsQuery({ page: 1, pageSize: 200 });
  const vendorOptions = useMemo(
    () => vendorsQuery.data?.data?.items ?? [],
    [vendorsQuery.data?.data?.items],
  );

  const orders = query.data?.data?.items ?? [];
  const total = query.data?.data?.total ?? 0;
  const totalPages = query.data?.data?.totalPages ?? 0;

  const hasActiveFilters = status !== 'all' || vendorId !== '';

  const clearFilters = () => {
    setStatus('all');
    setVendorId('');
    setPage(1);
  };

  const runAction = async (
    order: PurchaseOrder,
    act: 'submit' | 'approve' | 'cancel' | 'close',
  ) => {
    if (act === 'cancel') {
      const ok = await confirm({
        title: t('po.cancelTitle', { defaultValue: 'Siparişi İptal Et' }),
        message: t('po.cancelConfirm', {
          defaultValue: '{{po}} iptal edilsin mi?',
          po: order.poNumber,
        }),
        confirmLabel: t('common.confirm', { defaultValue: 'Onayla' }),
        tone: 'danger',
      });
      if (!ok) return;
    }
    try {
      await action.mutateAsync({ id: order.id, action: act });
      toast.success(t('po.actionDone', { defaultValue: 'İşlem tamamlandı.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  const remove = async (order: PurchaseOrder) => {
    const ok = await confirm({
      title: t('po.deleteTitle', { defaultValue: 'Siparişi Sil' }),
      message: t('po.deleteConfirm', { defaultValue: '{{po}} silinsin mi?', po: order.poNumber }),
      confirmLabel: t('common.delete', { defaultValue: 'Sil' }),
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await deleteMutation.mutateAsync(order.id);
      toast.success(t('po.deleted', { defaultValue: 'Sipariş silindi.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<ShoppingCart size={20} />}
          title={t('po.page.title', { defaultValue: 'Satınalma Siparişleri' })}
          subtitle={t('po.page.subtitle', {
            defaultValue: 'Tedarikçilere sipariş oluştur, onayla ve takip et.',
          })}
          actions={
            <Button size="sm" onClick={() => setModal({ mode: 'create' })}>
              <Plus size={14} />
              {t('po.page.new', { defaultValue: 'Yeni Sipariş' })}
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
              ariaLabel={t('po.filter.statusAria', { defaultValue: 'Duruma göre filtrele' })}
              options={[
                { value: 'all', label: t('po.filter.all', { defaultValue: 'Tümü' }) },
                ...STATUSES.map((s) => ({ value: s, label: statusLabel(s) })),
              ]}
            />
          }
          leading={
            <Select
              value={vendorId}
              onChange={(e) => {
                setVendorId(e.target.value);
                setPage(1);
              }}
              aria-label={t('po.filter.vendor', { defaultValue: 'Tedarikçi' })}
              className="w-full sm:w-48"
            >
              <option value="">
                {t('po.filter.allVendors', { defaultValue: 'Tüm tedarikçiler' })}
              </option>
              {vendorOptions.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.code ? `${v.code} · ${v.name}` : v.name}
                </option>
              ))}
            </Select>
          }
          resultCount={{
            count: total,
            label: t('po.resultCountLabel', { defaultValue: 'sipariş' }),
          }}
          hasActiveFilters={hasActiveFilters}
          onClearFilters={clearFilters}
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
        ) : query.isError ? (
          <div className="flex flex-col items-center gap-2 px-3 py-10 text-center text-sm text-danger-600 dark:text-danger-400">
            <span>{t('po.error', { defaultValue: 'Satınalma siparişleri yüklenemedi.' })}</span>
            <Button variant="outline" size="sm" onClick={() => query.refetch()}>
              {t('common.retry', { defaultValue: 'Yeniden dene' })}
            </Button>
          </div>
        ) : orders.length === 0 ? (
          <div className="px-3 py-10 text-center text-sm text-slate-500 dark:text-slate-400">
            {t('po.empty', { defaultValue: 'Satınalma siparişi bulunamadı.' })}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('po.cols.number', { defaultValue: 'No' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('po.cols.vendor', { defaultValue: 'Tedarikçi' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('po.cols.date', { defaultValue: 'Tarih' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('po.cols.status', { defaultValue: 'Durum' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('po.cols.total', { defaultValue: 'Tutar' })}
                </th>
                <th className="px-3 py-2" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {orders.map((o) => (
                <tr key={o.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                  <td className="px-3 py-2 font-mono text-xs text-slate-700 dark:text-slate-300">
                    {o.poNumber}
                  </td>
                  <td className="px-3 py-2 text-slate-800 dark:text-slate-100">{o.vendorName}</td>
                  <td className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">
                    {fmtDate(o.orderDate, locale)}
                  </td>
                  <td className="px-3 py-2 text-center">
                    <span
                      className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${STATUS_TONE[o.status]}`}
                    >
                      {statusLabel(o.status)}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-slate-800 dark:text-slate-200">
                    {formatCurrency(o.total, locale, o.currency)}
                  </td>
                  <td className="px-3 py-2 text-right">
                    <div className="inline-flex items-center gap-1">
                      {o.status === 'Draft' && (
                        <>
                          <button
                            type="button"
                            onClick={() => setModal({ mode: 'edit', order: o })}
                            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800 dark:hover:text-slate-200"
                            title={t('common.edit', { defaultValue: 'Düzenle' })}
                          >
                            <Pencil size={13} />
                          </button>
                          <button
                            type="button"
                            onClick={() => runAction(o, 'submit')}
                            className="rounded p-1 text-primary-500 hover:bg-primary-50 dark:hover:bg-primary-500/10"
                            title={t('po.actions.submit', { defaultValue: 'Gönder' })}
                          >
                            <Send size={13} />
                          </button>
                          <button
                            type="button"
                            onClick={() => remove(o)}
                            className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 dark:hover:bg-danger-500/10"
                            title={t('common.delete', { defaultValue: 'Sil' })}
                          >
                            <Trash2 size={13} />
                          </button>
                        </>
                      )}
                      {o.status === 'Submitted' && (
                        <button
                          type="button"
                          onClick={() => runAction(o, 'approve')}
                          className="rounded p-1 text-success-500 hover:bg-success-50 dark:hover:bg-success-500/10"
                          title={t('po.actions.approve', { defaultValue: 'Onayla' })}
                        >
                          <CheckCircle2 size={13} />
                        </button>
                      )}
                      {(o.status === 'Approved' || o.status === 'PartiallyReceived') && (
                        <button
                          type="button"
                          onClick={() => setReceiveOrder(o)}
                          className="rounded p-1 text-success-500 hover:bg-success-50 dark:hover:bg-success-500/10"
                          title={t('po.actions.receive', { defaultValue: 'Mal Kabul' })}
                        >
                          <PackageCheck size={13} />
                        </button>
                      )}
                      {o.status === 'Received' && (
                        <button
                          type="button"
                          onClick={() => runAction(o, 'close')}
                          className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800 dark:hover:text-slate-200"
                          title={t('po.actions.close', { defaultValue: 'Kapat' })}
                        >
                          <Lock size={13} />
                        </button>
                      )}
                      {['Draft', 'Submitted', 'Approved', 'PartiallyReceived'].includes(
                        o.status,
                      ) && (
                        <button
                          type="button"
                          onClick={() => runAction(o, 'cancel')}
                          className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 dark:hover:bg-danger-500/10"
                          title={t('po.actions.cancel', { defaultValue: 'İptal' })}
                        >
                          <XCircle size={13} />
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {modal && (
        <PurchaseOrderFormModal
          key={modal.mode === 'edit' ? modal.order.id : 'new'}
          order={modal.mode === 'edit' ? modal.order : null}
          onClose={() => setModal(null)}
        />
      )}

      {receiveOrder && (
        <ReceivePurchaseOrderModal order={receiveOrder} onClose={() => setReceiveOrder(null)} />
      )}
    </ListPageTemplate>
  );
};

export default PurchaseOrdersPage;
