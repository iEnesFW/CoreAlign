import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import { CheckCircle2, ChevronLeft, PlayCircle, Save, XCircle } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { formatCurrency } from '@/shared/lib/format';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import {
  useCancelStockCount,
  usePostStockCount,
  useReconcileStockCount,
  useRecordStockCount,
  useStartStockCount,
  useStockCountQuery,
} from '@/features/inventory/hooks/useStockCountQueries';
import type { StockCount, StockCountStatus } from '@/features/inventory/model/stockCount.types';

const STATUS_TONE: Record<StockCountStatus, string> = {
  Plan: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300',
  Counting: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  Reconciliation: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
  Posted: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Cancelled: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
};

const StockCountDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const { t } = useTranslation();
  const { data, isPending } = useStockCountQuery(id);
  const entity = data?.data;

  if (isPending || !entity) {
    return (
      <div className="p-6 text-sm text-slate-500">
        {t('common.loading', { defaultValue: 'Yükleniyor…' })}
      </div>
    );
  }
  return <StockCountDetailView key={`${entity.id}-${entity.status}`} entity={entity} />;
};

interface ViewProps {
  entity: StockCount;
}

const StockCountDetailView = ({ entity }: ViewProps) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const confirm = useConfirm();
  const start = useStartStockCount();
  const record = useRecordStockCount();
  const reconcile = useReconcileStockCount();
  const post = usePostStockCount();
  const cancel = useCancelStockCount();

  const initialDrafts = useMemo<Record<string, string>>(() => {
    const out: Record<string, string> = {};
    for (const l of entity.lines) {
      out[l.id] = l.countedQuantity?.toString() ?? '';
    }
    return out;
  }, [entity]);
  const [drafts, setDrafts] = useState<Record<string, string>>(initialDrafts);
  const [reconcileNotes, setReconcileNotes] = useState('');

  const allCounted = useMemo(
    () =>
      entity.lines.every((l) => {
        const v = drafts[l.id];
        return v !== undefined && v !== '';
      }),
    [entity, drafts],
  );

  const readonly = entity.status === 'Posted' || entity.status === 'Cancelled';

  const onSaveCounts = async () => {
    const payload = entity.lines
      .map((l) => ({ lineId: l.id, countedQuantity: parseFloat(drafts[l.id] || '0') }))
      .filter((x) => !Number.isNaN(x.countedQuantity));
    try {
      await record.mutateAsync({ id: entity.id, lines: payload });
      toast.success(
        t('Inventory.StockCounts.recorded', { defaultValue: 'Sayım girişleri kaydedildi.' }),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  const onStart = async () => {
    try {
      await start.mutateAsync(entity.id);
      toast.success(t('Inventory.StockCounts.started', { defaultValue: 'Sayım başlatıldı.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  const onReconcile = async () => {
    try {
      await reconcile.mutateAsync({ id: entity.id, notes: reconcileNotes || null });
      toast.success(
        t('Inventory.StockCounts.reconciled', { defaultValue: 'Sayım mutabık kılındı.' }),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  const onPost = async () => {
    const ok = await confirm({
      title: t('Inventory.StockCounts.post.title', { defaultValue: 'Sayımı İşle' }),
      message: t('Inventory.StockCounts.post.confirm', {
        defaultValue:
          'Bu işlem sapma satırlarını stok hareketi olarak yazacak ve geri alınamaz. Devam edilsin mi?',
      }),
      confirmLabel: t('common.confirm', { defaultValue: 'Onayla' }),
    });
    if (!ok) return;
    try {
      await post.mutateAsync(entity.id);
      toast.success(t('Inventory.StockCounts.posted', { defaultValue: 'Sayım işlendi.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  const onCancel = async () => {
    const ok = await confirm({
      title: t('Inventory.StockCounts.cancel.title', { defaultValue: 'Sayımı İptal Et' }),
      message: t('Inventory.StockCounts.cancel.confirm', {
        defaultValue: 'Sayım iptal edilsin mi?',
      }),
      confirmLabel: t('common.confirm', { defaultValue: 'Onayla' }),
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await cancel.mutateAsync(entity.id);
      toast.success(t('Inventory.StockCounts.cancelled', { defaultValue: 'Sayım iptal edildi.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="space-y-4 p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <Link
            to="/dashboard/inventory/stock-counts"
            className="inline-flex items-center gap-1 text-[11px] text-slate-500 hover:text-indigo-600"
          >
            <ChevronLeft size={12} />
            {t('Inventory.StockCounts.backToList', { defaultValue: 'Sayımlara dön' })}
          </Link>
          <h1 className="mt-1 flex items-center gap-2 text-xl font-bold text-slate-900 dark:text-slate-100">
            {entity.countNumber}
            <span
              className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${STATUS_TONE[entity.status]}`}
            >
              {t(`Inventory.StockCounts.status.${entity.status}`, { defaultValue: entity.status })}
            </span>
          </h1>
          <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
            {entity.warehouseName}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {entity.status === 'Plan' && (
            <button
              type="button"
              onClick={onStart}
              disabled={start.isPending}
              className="inline-flex items-center gap-1.5 rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
            >
              <PlayCircle size={13} />
              {t('Inventory.StockCounts.start', { defaultValue: 'Sayımı Başlat' })}
            </button>
          )}
          {entity.status === 'Counting' && (
            <button
              type="button"
              onClick={onReconcile}
              disabled={reconcile.isPending || !allCounted}
              className="inline-flex items-center gap-1.5 rounded bg-sky-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-sky-700 disabled:opacity-50"
              title={
                allCounted
                  ? undefined
                  : t('Inventory.StockCounts.allLinesRequired', {
                      defaultValue: 'Tüm satırlar girilmelidir.',
                    })
              }
            >
              <CheckCircle2 size={13} />
              {t('Inventory.StockCounts.reconcile', { defaultValue: 'Mutabakat' })}
            </button>
          )}
          {entity.status === 'Reconciliation' && (
            <button
              type="button"
              onClick={onPost}
              disabled={post.isPending}
              className="inline-flex items-center gap-1.5 rounded bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
            >
              <CheckCircle2 size={13} />
              {t('Inventory.StockCounts.post.button', { defaultValue: 'Sayımı İşle' })}
            </button>
          )}
          {!readonly && (
            <button
              type="button"
              onClick={onCancel}
              disabled={cancel.isPending}
              className="inline-flex items-center gap-1.5 rounded border border-rose-200 bg-white px-3 py-1.5 text-xs font-semibold text-rose-600 hover:bg-rose-50 dark:border-rose-500/40 dark:bg-slate-900 dark:hover:bg-rose-500/10"
            >
              <XCircle size={13} />
              {t('common.cancel', { defaultValue: 'İptal' })}
            </button>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        <div className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
          <p className="text-[10px] uppercase tracking-wider text-slate-500">
            {t('Inventory.StockCounts.summary.lines', { defaultValue: 'Satır Sayısı' })}
          </p>
          <p className="mt-1 text-lg font-semibold text-slate-900 dark:text-slate-100">
            {entity.lines.length}
          </p>
        </div>
        <div className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
          <p className="text-[10px] uppercase tracking-wider text-slate-500">
            {t('Inventory.StockCounts.summary.varianceQty', { defaultValue: 'Sapma Miktar' })}
          </p>
          <p className="mt-1 text-lg font-mono font-semibold text-slate-900 dark:text-slate-100">
            {entity.totalVarianceQuantity.toFixed(2)}
          </p>
        </div>
        <div className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
          <p className="text-[10px] uppercase tracking-wider text-slate-500">
            {t('Inventory.StockCounts.summary.varianceCost', { defaultValue: 'Sapma Tutarı' })}
          </p>
          <p className="mt-1 text-lg font-mono font-semibold text-slate-900 dark:text-slate-100">
            {formatCurrency(entity.totalVarianceCost, locale, 'TRY')}
          </p>
        </div>
      </div>

      <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
        <table className="w-full text-sm">
          <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
            <tr>
              <th className="px-3 py-2 text-left">
                {t('Inventory.StockCounts.lineCols.sku', { defaultValue: 'SKU' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('Inventory.StockCounts.lineCols.product', { defaultValue: 'Ürün' })}
              </th>
              <th className="px-3 py-2 text-right">
                {t('Inventory.StockCounts.lineCols.expected', { defaultValue: 'Beklenen' })}
              </th>
              <th className="px-3 py-2 text-right">
                {t('Inventory.StockCounts.lineCols.counted', { defaultValue: 'Sayılan' })}
              </th>
              <th className="px-3 py-2 text-right">
                {t('Inventory.StockCounts.lineCols.variance', { defaultValue: 'Sapma' })}
              </th>
              <th className="px-3 py-2 text-right">
                {t('Inventory.StockCounts.lineCols.varianceCost', { defaultValue: 'Sapma Tutarı' })}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
            {entity.lines.map((l) => (
              <tr key={l.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                <td className="px-3 py-2 font-mono text-xs text-slate-700 dark:text-slate-300">
                  {l.productSku}
                </td>
                <td className="px-3 py-2 text-slate-800 dark:text-slate-100">{l.productName}</td>
                <td className="px-3 py-2 text-right font-mono text-slate-700 dark:text-slate-200">
                  {l.expectedQuantity.toFixed(2)}
                </td>
                <td className="px-3 py-2 text-right">
                  {entity.status === 'Counting' ? (
                    <input
                      type="number"
                      step="0.01"
                      value={drafts[l.id] ?? ''}
                      onChange={(e) => setDrafts((prev) => ({ ...prev, [l.id]: e.target.value }))}
                      className="w-24 rounded border border-slate-200 bg-white px-1.5 py-0.5 text-right font-mono text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                    />
                  ) : (
                    <span className="font-mono text-xs text-slate-700 dark:text-slate-200">
                      {l.countedQuantity?.toFixed(2) ?? '—'}
                    </span>
                  )}
                </td>
                <td className="px-3 py-2 text-right font-mono text-xs">
                  <span
                    className={
                      l.varianceQuantity === 0
                        ? 'text-slate-500'
                        : l.varianceQuantity > 0
                          ? 'text-emerald-600'
                          : 'text-rose-600'
                    }
                  >
                    {l.varianceQuantity.toFixed(2)}
                  </span>
                </td>
                <td className="px-3 py-2 text-right font-mono text-xs text-slate-700 dark:text-slate-200">
                  {formatCurrency(l.varianceCost, locale, 'TRY')}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {entity.status === 'Counting' && (
        <div className="flex justify-end">
          <button
            type="button"
            onClick={onSaveCounts}
            disabled={record.isPending}
            className="inline-flex items-center gap-1.5 rounded bg-slate-700 px-3 py-1.5 text-xs font-semibold text-white hover:bg-slate-800 disabled:opacity-50"
          >
            <Save size={13} />
            {t('Inventory.StockCounts.saveCounts', { defaultValue: 'Sayımları Kaydet' })}
          </button>
        </div>
      )}

      {entity.status === 'Counting' && (
        <div className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
          <label className="block text-xs">
            <span className="mb-1 block text-slate-600 dark:text-slate-400">
              {t('Inventory.StockCounts.reconcileNotes', {
                defaultValue: 'Mutabakat Notları (opsiyonel)',
              })}
            </span>
            <textarea
              value={reconcileNotes}
              onChange={(e) => setReconcileNotes(e.target.value)}
              rows={2}
              maxLength={1000}
              className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </label>
        </div>
      )}
    </div>
  );
};

export default StockCountDetailPage;
export { StockCountDetailPage };
