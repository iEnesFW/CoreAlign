import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import {
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  PackageCheck,
  Undo2,
  XCircle,
} from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { formatCurrency, formatDateTime, formatNumber } from '@/shared/lib/format';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Select } from '@/shared/ui/Select/Select';
import { Textarea } from '@/shared/ui/Textarea/Textarea';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Badge } from '@/shared/ui/Badge/Badge';
import type { BadgeVariant } from '@/shared/ui/Badge/Badge';
import { useIsTenantAdmin } from '@/shared/lib/auth/useIsTenantAdmin';
import { useVendorsQuery } from '@/features/vendors/hooks/useVendorQueries';
import {
  useApproveGoodsReceiptQc,
  useGoodsReceiptsQuery,
  useRejectGoodsReceiptQc,
  useReverseGoodsReceipt,
} from '@/features/purchasing/hooks/useGoodsReceipts';
import type {
  GoodsReceipt,
  GoodsReceiptQcStatus,
  GoodsReceiptStatus,
} from '@/features/purchasing/model/goodsReceipt.types';

const STATUS_VARIANT: Record<GoodsReceiptStatus, BadgeVariant> = {
  Posted: 'success',
  Reversed: 'danger',
};

const QC_VARIANT: Record<GoodsReceiptQcStatus, BadgeVariant> = {
  NotRequired: 'neutral',
  PendingInspection: 'warning',
  Approved: 'success',
  Rejected: 'danger',
};

const STATUSES: GoodsReceiptStatus[] = ['Posted', 'Reversed'];

interface Props {
  purchaseOrderId?: string;
}

export const GoodsReceiptsPage = ({ purchaseOrderId }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const isTenantAdmin = useIsTenantAdmin();

  const [vendorId, setVendorId] = useState('');
  const [status, setStatus] = useState<GoodsReceiptStatus | ''>('');
  const [page, setPage] = useState(1);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [reverseTarget, setReverseTarget] = useState<GoodsReceipt | null>(null);
  const [qcRejectTarget, setQcRejectTarget] = useState<GoodsReceipt | null>(null);
  const approveQc = useApproveGoodsReceiptQc();

  const handleApproveQc = async (g: GoodsReceipt) => {
    try {
      await approveQc.mutateAsync(g.id);
      toast.success(
        t('grn.qc.approve.done', { defaultValue: 'Mal kabul onaylandı ve stoğa eklendi.' }),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  const vendorsQuery = useVendorsQuery({ page: 1, pageSize: 200 });
  const vendors = vendorsQuery.data?.data?.items ?? [];

  const query = useGoodsReceiptsQuery({
    purchaseOrderId,
    vendorId: vendorId || undefined,
    status: status || undefined,
    page,
    pageSize: 25,
  });
  const items = query.data?.data?.items ?? [];
  const total = query.data?.data?.total ?? 0;
  const totalPages = query.data?.data?.totalPages ?? 0;

  const statusLabel = (s: GoodsReceiptStatus) => t(`grn.status.${s}`, { defaultValue: s });

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<PackageCheck size={20} />}
          title={t('grn.page.title', { defaultValue: 'Mal Kabul Fişleri' })}
          subtitle={t('grn.page.subtitle', {
            defaultValue: 'Teslim alınan malların fişlerini görüntüle ve gerektiğinde iade et.',
          })}
          tone="emerald"
        />
      }
      toolbar={
        <div className="flex flex-wrap items-center gap-2">
          {!purchaseOrderId && (
            <Select
              value={vendorId}
              onChange={(e) => {
                setVendorId(e.target.value);
                setPage(1);
              }}
              aria-label={t('grn.filter.vendor', { defaultValue: 'Tedarikçi' })}
              className="w-full sm:w-72"
            >
              <option value="">
                {t('grn.filter.allVendors', { defaultValue: 'Tüm tedarikçiler' })}
              </option>
              {vendors.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.code ? `${v.code} · ${v.name}` : v.name}
                </option>
              ))}
            </Select>
          )}
          <Select
            value={status}
            onChange={(e) => {
              setStatus(e.target.value as GoodsReceiptStatus | '');
              setPage(1);
            }}
            aria-label={t('grn.filter.status', { defaultValue: 'Durum' })}
            className="w-full sm:w-48"
          >
            <option value="">
              {t('grn.filter.allStatuses', { defaultValue: 'Tüm durumlar' })}
            </option>
            {STATUSES.map((s) => (
              <option key={s} value={s}>
                {statusLabel(s)}
              </option>
            ))}
          </Select>
          <span className="ml-auto text-[11px] text-slate-500 dark:text-slate-400">
            {t('grn.count', { defaultValue: '{{count}} fiş', count: total })}
          </span>
        </div>
      }
      pagination={
        totalPages > 1 ? (
          <div className="flex items-center justify-end gap-1 text-xs">
            <Button
              type="button"
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
              type="button"
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
            <span>{t('grn.error', { defaultValue: 'Mal kabul fişleri yüklenemedi.' })}</span>
            <Button variant="outline" size="sm" onClick={() => query.refetch()}>
              {t('common.retry', { defaultValue: 'Yeniden dene' })}
            </Button>
          </div>
        ) : items.length === 0 ? (
          <div className="px-3 py-10 text-center text-sm text-slate-500 dark:text-slate-400">
            {t('grn.empty', { defaultValue: 'Mal kabul fişi bulunamadı.' })}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
              <tr>
                <th className="w-8 px-2 py-2" />
                <th className="px-3 py-2 text-left">
                  {t('grn.cols.number', { defaultValue: 'Fiş No' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('grn.cols.vendor', { defaultValue: 'Tedarikçi' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('grn.cols.po', { defaultValue: 'Sipariş' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('grn.cols.date', { defaultValue: 'Tarih' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('grn.cols.lines', { defaultValue: 'Satır' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('grn.cols.total', { defaultValue: 'Tutar' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('grn.cols.status', { defaultValue: 'Durum' })}
                </th>
                <th className="px-3 py-2" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {items.map((g) => {
                const expanded = expandedId === g.id;
                return (
                  <GoodsReceiptRow
                    key={g.id}
                    receipt={g}
                    locale={locale}
                    expanded={expanded}
                    statusLabel={statusLabel}
                    isAdmin={isTenantAdmin}
                    qcBusy={approveQc.isPending}
                    onToggle={() => setExpandedId(expanded ? null : g.id)}
                    onReverse={() => setReverseTarget(g)}
                    onApproveQc={() => handleApproveQc(g)}
                    onRejectQc={() => setQcRejectTarget(g)}
                  />
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {reverseTarget && (
        <ReverseGoodsReceiptModal receipt={reverseTarget} onClose={() => setReverseTarget(null)} />
      )}
      {qcRejectTarget && (
        <QcRejectGoodsReceiptModal
          receipt={qcRejectTarget}
          onClose={() => setQcRejectTarget(null)}
        />
      )}
    </ListPageTemplate>
  );
};

interface RowProps {
  receipt: GoodsReceipt;
  locale: string;
  expanded: boolean;
  isAdmin: boolean;
  qcBusy: boolean;
  statusLabel: (s: GoodsReceiptStatus) => string;
  onToggle: () => void;
  onReverse: () => void;
  onApproveQc: () => void;
  onRejectQc: () => void;
}

const GoodsReceiptRow = ({
  receipt,
  locale,
  expanded,
  isAdmin,
  qcBusy,
  statusLabel,
  onToggle,
  onReverse,
  onApproveQc,
  onRejectQc,
}: RowProps) => {
  const { t } = useTranslation();
  const awaitingQc = receipt.qcStatus === 'PendingInspection';
  const canReverse =
    isAdmin &&
    receipt.status === 'Posted' &&
    receipt.qcStatus !== 'PendingInspection' &&
    receipt.qcStatus !== 'Rejected';
  return (
    <>
      <tr className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
        <td className="px-2 py-2 text-center">
          <button
            type="button"
            onClick={onToggle}
            className="rounded p-0.5 text-slate-400 hover:text-slate-700 dark:hover:text-slate-200"
            aria-label={t('grn.toggleLines', { defaultValue: 'Satırları aç/kapat' })}
          >
            {expanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
          </button>
        </td>
        <td className="px-3 py-2 font-mono text-xs text-slate-700 dark:text-slate-300">
          {receipt.grnNumber}
        </td>
        <td className="px-3 py-2 text-slate-800 dark:text-slate-100">{receipt.vendorName}</td>
        <td className="px-3 py-2 font-mono text-xs text-slate-500 dark:text-slate-400">
          {receipt.poNumber}
        </td>
        <td className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">
          {formatDateTime(receipt.receiptDateUtc, locale)}
        </td>
        <td className="px-3 py-2 text-right font-mono text-slate-700 dark:text-slate-200">
          {receipt.lines.length}
        </td>
        <td className="px-3 py-2 text-right font-mono text-slate-800 dark:text-slate-200">
          {formatCurrency(receipt.totalCost, locale, receipt.currency)}
        </td>
        <td className="px-3 py-2 text-center">
          <div className="flex flex-col items-center gap-1">
            <Badge variant={STATUS_VARIANT[receipt.status]}>{statusLabel(receipt.status)}</Badge>
            {receipt.qcStatus !== 'NotRequired' && (
              <Badge variant={QC_VARIANT[receipt.qcStatus]}>
                {t(`grn.qc.status.${receipt.qcStatus}`, { defaultValue: receipt.qcStatus })}
              </Badge>
            )}
          </div>
        </td>
        <td className="px-3 py-2 text-right">
          <div className="flex items-center justify-end gap-1">
            {awaitingQc && isAdmin && (
              <>
                <button
                  type="button"
                  onClick={onApproveQc}
                  disabled={qcBusy}
                  className="rounded p-1 text-slate-400 hover:bg-success-50 hover:text-success-700 disabled:opacity-50 dark:hover:bg-success-500/10"
                  title={t('grn.qc.approve.action', { defaultValue: 'Kaliteyi Onayla' })}
                >
                  <CheckCircle2 size={13} />
                </button>
                <button
                  type="button"
                  onClick={onRejectQc}
                  className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 dark:hover:bg-danger-500/10"
                  title={t('grn.qc.reject.action', { defaultValue: 'Kaliteyi Reddet' })}
                >
                  <XCircle size={13} />
                </button>
              </>
            )}
            {canReverse && (
              <button
                type="button"
                onClick={onReverse}
                className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 dark:hover:bg-danger-500/10"
                title={t('grn.reverse.action', { defaultValue: 'İade Et' })}
              >
                <Undo2 size={13} />
              </button>
            )}
          </div>
        </td>
      </tr>
      {expanded && (
        <tr className="bg-slate-50/50 dark:bg-slate-900/30">
          <td colSpan={9} className="px-3 py-2">
            {receipt.status === 'Reversed' && receipt.reversalReason && (
              <p className="mb-2 text-[11px] text-danger-600 dark:text-danger-400">
                {t('grn.reverse.reasonLabel', { defaultValue: 'İade nedeni' })}:{' '}
                {receipt.reversalReason}
              </p>
            )}
            {receipt.qcStatus === 'Rejected' && receipt.qcRejectionReason && (
              <p className="mb-2 text-[11px] text-danger-600 dark:text-danger-400">
                {t('grn.qc.rejectedReason', { defaultValue: 'Kalite ret nedeni' })}:{' '}
                {receipt.qcRejectionReason}
              </p>
            )}
            <table className="w-full text-xs">
              <thead className="text-[10px] uppercase text-slate-400 dark:text-slate-500">
                <tr>
                  <th className="px-2 py-1 text-left">
                    {t('grn.line.product', { defaultValue: 'Ürün' })}
                  </th>
                  <th className="px-2 py-1 text-right">
                    {t('grn.line.qty', { defaultValue: 'Miktar' })}
                  </th>
                  <th className="px-2 py-1 text-right">
                    {t('grn.line.unitCost', { defaultValue: 'Birim Maliyet' })}
                  </th>
                  <th className="px-2 py-1 text-right">
                    {t('grn.line.lineCost', { defaultValue: 'Satır Tutarı' })}
                  </th>
                </tr>
              </thead>
              <tbody>
                {receipt.lines.map((l) => (
                  <tr key={l.id} className="border-t border-slate-100 dark:border-slate-800">
                    <td className="px-2 py-1">
                      <div className="font-medium text-slate-800 dark:text-slate-100">
                        {l.productName}
                      </div>
                      <div className="font-mono text-[10px] text-slate-400 dark:text-slate-500">
                        {l.productSku}
                      </div>
                    </td>
                    <td className="px-2 py-1 text-right font-mono text-slate-700 dark:text-slate-200">
                      {formatNumber(l.quantityReceived, locale)}
                    </td>
                    <td className="px-2 py-1 text-right font-mono text-slate-700 dark:text-slate-200">
                      {formatCurrency(l.unitCost, locale, receipt.currency)}
                    </td>
                    <td className="px-2 py-1 text-right font-mono text-slate-800 dark:text-slate-100">
                      {formatCurrency(l.lineCost, locale, receipt.currency)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </td>
        </tr>
      )}
    </>
  );
};

interface ReverseModalProps {
  receipt: GoodsReceipt;
  onClose: () => void;
}

const ReverseGoodsReceiptModal = ({ receipt, onClose }: ReverseModalProps) => {
  const { t } = useTranslation();
  const reverse = useReverseGoodsReceipt();
  const [reason, setReason] = useState('');

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await reverse.mutateAsync({ id: receipt.id, reason: reason.trim() || null });
      toast.success(t('grn.reverse.done', { defaultValue: 'Mal kabul iade edildi.' }));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open
      onClose={onClose}
      icon={<Undo2 size={16} />}
      title={`${t('grn.reverse.title', { defaultValue: 'Mal Kabulü İade Et' })} — ${receipt.grnNumber}`}
      size="md"
    >
      <form onSubmit={onSubmit} className="space-y-3">
        <p className="text-[11px] text-slate-500 dark:text-slate-400">
          {t('grn.reverse.hint', {
            defaultValue:
              'İade işlemi stok hareketlerini ve sipariş teslim miktarını geri alır, muhasebe kaydını ters çevirir.',
          })}
        </p>
        <Textarea
          label={t('grn.reverse.reasonLabel', { defaultValue: 'İade nedeni' })}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          rows={3}
          maxLength={500}
        />
        <div className="flex justify-end gap-2 border-t border-slate-200 pt-3 dark:border-slate-800">
          <Button type="button" variant="secondary" size="sm" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'Vazgeç' })}
          </Button>
          <Button type="submit" variant="danger" size="sm" isLoading={reverse.isPending}>
            {reverse.isPending
              ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
              : t('grn.reverse.submit', { defaultValue: 'İade Et' })}
          </Button>
        </div>
      </form>
    </Modal>
  );
};

const QcRejectGoodsReceiptModal = ({ receipt, onClose }: ReverseModalProps) => {
  const { t } = useTranslation();
  const reject = useRejectGoodsReceiptQc();
  const [reason, setReason] = useState('');

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await reject.mutateAsync({ id: receipt.id, reason: reason.trim() });
      toast.success(
        t('grn.qc.reject.done', { defaultValue: 'Mal kabul reddedildi. Stoğa ekleme yapılmadı.' }),
      );
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open
      onClose={onClose}
      icon={<XCircle size={16} />}
      title={`${t('grn.qc.reject.title', { defaultValue: 'Kalite Muayenesini Reddet' })} — ${receipt.grnNumber}`}
      size="md"
    >
      <form onSubmit={onSubmit} className="space-y-3">
        <p className="text-[11px] text-slate-500 dark:text-slate-400">
          {t('grn.qc.reject.hint', {
            defaultValue: 'Reddedilen mal stoğa girmez; sipariş teslim miktarı geri alınır.',
          })}
        </p>
        <Textarea
          label={t('grn.qc.reject.reasonLabel', { defaultValue: 'Ret nedeni' })}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          rows={3}
          maxLength={500}
        />
        <div className="flex justify-end gap-2 border-t border-slate-200 pt-3 dark:border-slate-800">
          <Button type="button" variant="secondary" size="sm" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'Vazgeç' })}
          </Button>
          <Button type="submit" variant="danger" size="sm" isLoading={reject.isPending}>
            {t('grn.qc.reject.submit', { defaultValue: 'Reddet' })}
          </Button>
        </div>
      </form>
    </Modal>
  );
};

export default GoodsReceiptsPage;
