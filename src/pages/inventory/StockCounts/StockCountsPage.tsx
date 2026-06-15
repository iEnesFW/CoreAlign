import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { toast } from 'sonner';
import { ClipboardList, Plus } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import {
  usePlanStockCount,
  useStockCountsQuery,
} from '@/features/inventory/hooks/useStockCountQueries';
import { useWarehousesQuery } from '@/features/master-data/hooks/useMasterData';
import type { StockCountStatus } from '@/features/inventory/model/stockCount.types';

const STATUS_TONE: Record<StockCountStatus, string> = {
  Plan: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300',
  Counting: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  Reconciliation: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
  Posted: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Cancelled: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
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
    <div className="space-y-4 p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="flex items-center gap-2 text-xl font-bold text-slate-900 dark:text-slate-100">
            <ClipboardList size={20} />
            {t('Inventory.StockCounts.title', { defaultValue: 'Stok Sayımları' })}
          </h1>
          <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
            {t('Inventory.StockCounts.subtitle', {
              defaultValue: 'Depo bazlı sayım dönemleri planla, gir, mutabık kıl ve işle.',
            })}
          </p>
        </div>
        <button
          type="button"
          onClick={() => setCreateOpen(true)}
          className="inline-flex items-center gap-1.5 rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700"
        >
          <Plus size={13} />
          {t('Inventory.StockCounts.new', { defaultValue: 'Yeni Sayım' })}
        </button>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <select
          value={warehouseId}
          onChange={(e) => {
            setWarehouseId(e.target.value);
            setPage(1);
          }}
          className="rounded border border-slate-200 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        >
          <option value="">
            {t('Inventory.StockCounts.filter.allWarehouses', { defaultValue: 'Tüm depolar' })}
          </option>
          {warehouses.map((w) => (
            <option key={w.id} value={w.id}>
              {w.name}
            </option>
          ))}
        </select>
        <select
          value={status}
          onChange={(e) => {
            setStatus(e.target.value as StockCountStatus | '');
            setPage(1);
          }}
          className="rounded border border-slate-200 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
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
        </select>
        <span className="ml-auto text-[11px] text-slate-500 dark:text-slate-400">
          {t('Inventory.StockCounts.count', { defaultValue: '{{count}} sayım', count: total })}
        </span>
      </div>

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
                  <td className="px-3 py-2 font-mono text-xs text-indigo-600 dark:text-indigo-400">
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
                    <span
                      className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${STATUS_TONE[c.status]}`}
                    >
                      {t(`Inventory.StockCounts.status.${c.status}`, { defaultValue: c.status })}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {totalPages > 1 && (
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
      )}

      {createOpen && (
        <PlanStockCountModal warehouses={warehouses} onClose={() => setCreateOpen(false)} />
      )}
    </div>
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
      await plan.mutateAsync({ warehouseId, notes: notes || null });
      toast.success(t('Inventory.StockCounts.planned', { defaultValue: 'Sayım oluşturuldu.' }));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4"
      onClick={onClose}
    >
      <form
        onClick={(e) => e.stopPropagation()}
        onSubmit={onSubmit}
        className="w-full max-w-md space-y-3 rounded-lg bg-white p-4 shadow-xl dark:bg-slate-900"
      >
        <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100">
          {t('Inventory.StockCounts.plan.title', { defaultValue: 'Yeni Sayım Planla' })}
        </h2>
        <label className="block text-xs">
          <span className="mb-1 block text-slate-600 dark:text-slate-400">
            {t('Inventory.StockCounts.plan.warehouse', { defaultValue: 'Depo' })}
          </span>
          <select
            value={warehouseId}
            onChange={(e) => setWarehouseId(e.target.value)}
            required
            className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          >
            {warehouses.map((w) => (
              <option key={w.id} value={w.id}>
                {w.name}
              </option>
            ))}
          </select>
        </label>
        <label className="block text-xs">
          <span className="mb-1 block text-slate-600 dark:text-slate-400">
            {t('Inventory.StockCounts.plan.notes', { defaultValue: 'Notlar' })}
          </span>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={3}
            maxLength={1000}
            className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
        </label>
        <p className="text-[11px] text-slate-500 dark:text-slate-400">
          {t('Inventory.StockCounts.plan.snapshotHint', {
            defaultValue:
              'Plan oluşturulduğunda seçilen deponun tüm stok kalemleri sayım satırlarına anlık görüntü olarak yazılır.',
          })}
        </p>
        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-slate-200 bg-white px-3 py-1.5 text-xs dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200"
          >
            {t('common.cancel', { defaultValue: 'Vazgeç' })}
          </button>
          <button
            type="submit"
            disabled={plan.isPending}
            className="rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
          >
            {t('Inventory.StockCounts.plan.submit', { defaultValue: 'Sayımı Oluştur' })}
          </button>
        </div>
      </form>
    </div>
  );
};

export default StockCountsPage;
export { StockCountsPage };
