import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { toast } from 'sonner';
import { ClipboardList, Plus } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Select } from '@/shared/ui/Select/Select';
import { Textarea } from '@/shared/ui/Textarea/Textarea';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Badge } from '@/shared/ui/Badge/Badge';
import type { BadgeVariant } from '@/shared/ui/Badge/Badge';
import { NextNumberBadge } from '@/shared/ui/NextNumberBadge/NextNumberBadge';
import {
  usePlanStockCount,
  useStockCountsQuery,
} from '@/features/inventory/hooks/useStockCountQueries';
import { useWarehousesQuery } from '@/shared/master-data/hooks/useMasterData';
import type { StockCountStatus } from '@/features/inventory/model/stockCount.types';

const STATUS_VARIANT: Record<StockCountStatus, BadgeVariant> = {
  Plan: 'neutral',
  Counting: 'warning',
  Reconciliation: 'info',
  Posted: 'success',
  Cancelled: 'danger',
};

const StockCountsPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const [warehouseId, setWarehouseId] = useState('');
  const [status, setStatus] = useState<StockCountStatus | ''>('');
  const [page, setPage] = useState(1);
  const [createOpen, setCreateOpen] = useState(false);

  const warehousesQuery = useWarehousesQuery(true);
  const warehouses = warehousesQuery.data?.data ?? [];
  const countsQuery = useStockCountsQuery({
    warehouseId: warehouseId || undefined,
    status: status || undefined,
    page,
    pageSize: 25,
  });
  const items = countsQuery.data?.data?.items ?? [];
  const total = countsQuery.data?.data?.total ?? 0;
  const totalPages = countsQuery.data?.data?.totalPages ?? 0;

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<ClipboardList size={20} />}
          title={t('Inventory.StockCounts.title', { defaultValue: 'Stok Sayımları' })}
          subtitle={t('Inventory.StockCounts.subtitle', {
            defaultValue: 'Depo bazlı sayım dönemleri planla, gir, mutabık kıl ve işle.',
          })}
          actions={
            <Button size="sm" onClick={() => setCreateOpen(true)}>
              <Plus size={14} />
              {t('Inventory.StockCounts.new', { defaultValue: 'Yeni Sayım' })}
            </Button>
          }
        />
      }
      toolbar={
        <div className="flex flex-wrap items-center gap-2">
          <Select
            value={warehouseId}
            onChange={(e) => {
              setWarehouseId(e.target.value);
              setPage(1);
            }}
            className="w-full sm:w-48"
          >
            <option value="">
              {t('Inventory.StockCounts.filter.allWarehouses', { defaultValue: 'Tüm depolar' })}
            </option>
            {warehouses.map((w) => (
              <option key={w.id} value={w.id}>
                {w.name}
              </option>
            ))}
          </Select>
          <Select
            value={status}
            onChange={(e) => {
              setStatus(e.target.value as StockCountStatus | '');
              setPage(1);
            }}
            className="w-full sm:w-48"
          >
            <option value="">
              {t('Inventory.StockCounts.filter.allStatuses', { defaultValue: 'Tüm durumlar' })}
            </option>
            {(
              ['Plan', 'Counting', 'Reconciliation', 'Posted', 'Cancelled'] as StockCountStatus[]
            ).map((s) => (
              <option key={s} value={s}>
                {t(`Inventory.StockCounts.status.${s}`, { defaultValue: s })}
              </option>
            ))}
          </Select>
          <span className="ml-auto text-[11px] text-slate-500 dark:text-slate-400">
            {t('Inventory.StockCounts.count', { defaultValue: '{{count}} sayım', count: total })}
          </span>
        </div>
      }
      pagination={
        totalPages > 1 ? (
          <div className="flex items-center justify-end gap-1 text-xs">
            <button
              type="button"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
              className="rounded border border-slate-200 bg-white px-2 py-1 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900"
            >
              {t('common.prev', { defaultValue: 'Önceki' })}
            </button>
            <span className="px-2 text-slate-500">
              {page} / {totalPages}
            </span>
            <button
              type="button"
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="rounded border border-slate-200 bg-white px-2 py-1 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900"
            >
              {t('common.next', { defaultValue: 'Sonraki' })}
            </button>
          </div>
        ) : undefined
      }
    >
      <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
        {countsQuery.isPending ? (
          <div className="px-3 py-8 text-center text-sm text-slate-500">
            {t('common.loading', { defaultValue: 'Yükleniyor…' })}
          </div>
        ) : items.length === 0 ? (
          <div className="px-3 py-10 text-center text-sm text-slate-500 dark:text-slate-400">
            {t('Inventory.StockCounts.empty', { defaultValue: 'Sayım bulunamadı.' })}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('Inventory.StockCounts.cols.number', { defaultValue: 'Sayım No' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('Inventory.StockCounts.cols.warehouse', { defaultValue: 'Depo' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('Inventory.StockCounts.cols.planned', { defaultValue: 'Planlandı' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('Inventory.StockCounts.cols.lines', { defaultValue: 'Satır' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('Inventory.StockCounts.cols.varianceCost', { defaultValue: 'Sapma Tutarı' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('Inventory.StockCounts.cols.status', { defaultValue: 'Durum' })}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {items.map((c) => (
                <tr key={c.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                  <td className="px-3 py-2 font-mono text-xs text-primary-600 dark:text-primary-400">
                    <Link to={`/dashboard/inventory/stock-counts/${c.id}`}>{c.countNumber}</Link>
                  </td>
                  <td className="px-3 py-2 text-slate-800 dark:text-slate-100">
                    {c.warehouseName}
                  </td>
                  <td className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">
                    {formatDate(c.plannedAtUtc, locale)}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-slate-700 dark:text-slate-200">
                    {c.lines.length}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-slate-800 dark:text-slate-200">
                    {formatCurrency(c.totalVarianceCost, locale, 'TRY')}
                  </td>
                  <td className="px-3 py-2 text-center">
                    <Badge variant={STATUS_VARIANT[c.status]}>
                      {t(`Inventory.StockCounts.status.${c.status}`, { defaultValue: c.status })}
                    </Badge>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {createOpen && (
        <PlanStockCountModal warehouses={warehouses} onClose={() => setCreateOpen(false)} />
      )}
    </ListPageTemplate>
  );
};

interface PlanModalProps {
  warehouses: { id: string; name: string }[];
  onClose: () => void;
}

const PlanStockCountModal = ({ warehouses, onClose }: PlanModalProps) => {
  const { t } = useTranslation();
  const plan = usePlanStockCount();
  const [warehouseId, setWarehouseId] = useState(warehouses[0]?.id ?? '');
  const [notes, setNotes] = useState('');

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!warehouseId) return;
    try {
      const result = await plan.mutateAsync({ warehouseId, notes: notes || null });
      const number = result?.data?.countNumber;
      toast.success(
        number
          ? t('Inventory.StockCounts.plannedNumber', {
              defaultValue: 'Sayım {{number}} oluşturuldu.',
              number,
            })
          : t('Inventory.StockCounts.planned', { defaultValue: 'Sayım oluşturuldu.' }),
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
      title={t('Inventory.StockCounts.plan.title', { defaultValue: 'Yeni Sayım Planla' })}
      size="md"
    >
      <form onSubmit={onSubmit} className="space-y-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300">
            {t('Inventory.StockCounts.cols.number', { defaultValue: 'Sayım No' })}
          </label>
          <NextNumberBadge type="StockCountNumber" />
        </div>
        <Select
          label={t('Inventory.StockCounts.plan.warehouse', { defaultValue: 'Depo' })}
          value={warehouseId}
          onChange={(e) => setWarehouseId(e.target.value)}
          required
        >
          {warehouses.map((w) => (
            <option key={w.id} value={w.id}>
              {w.name}
            </option>
          ))}
        </Select>
        <Textarea
          label={t('Inventory.StockCounts.plan.notes', { defaultValue: 'Notlar' })}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={3}
          maxLength={1000}
        />
        <p className="text-[11px] text-slate-500 dark:text-slate-400">
          {t('Inventory.StockCounts.plan.snapshotHint', {
            defaultValue:
              'Plan oluşturulduğunda seçilen deponun tüm stok kalemleri sayım satırlarına anlık görüntü olarak yazılır.',
          })}
        </p>
        <div className="flex justify-end gap-2 border-t border-slate-200 pt-3 dark:border-slate-800">
          <Button type="button" variant="secondary" size="sm" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'Vazgeç' })}
          </Button>
          <Button type="submit" size="sm" isLoading={plan.isPending}>
            {t('Inventory.StockCounts.plan.submit', { defaultValue: 'Sayımı Oluştur' })}
          </Button>
        </div>
      </form>
    </Modal>
  );
};

export default StockCountsPage;
export { StockCountsPage };
