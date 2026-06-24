import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { toast } from 'sonner';
import { CheckCircle2, ClipboardList, PlayCircle, Save, XCircle } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { formatCurrency } from '@/shared/lib/format';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { DetailPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Textarea } from '@/shared/ui/Textarea/Textarea';
import { Badge } from '@/shared/ui/Badge/Badge';
import type { BadgeVariant } from '@/shared/ui/Badge/Badge';
import {
  useCancelStockCount,
  usePostStockCount,
  useReconcileStockCount,
  useRecordStockCount,
  useStartStockCount,
  useStockCountQuery,
} from '@/features/inventory/hooks/useStockCountQueries';
import type { StockCount, StockCountStatus } from '@/features/inventory/model/stockCount.types';

const STATUS_VARIANT: Record<StockCountStatus, BadgeVariant> = {
  Plan: 'neutral',
  Counting: 'warning',
  Reconciliation: 'info',
  Posted: 'success',
  Cancelled: 'danger',
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
    <DetailPageTemplate
      header={
        <PageHeader
          icon={<ClipboardList size={20} />}
          title={entity.countNumber}
          subtitle={entity.warehouseName}
          crumbs={[
            {
              label: t('Inventory.StockCounts.backToList', { defaultValue: 'Sayımlara dön' }),
              to: '/dashboard/inventory/stock-counts',
            },
            { label: entity.countNumber },
          ]}
          trailing={
            <Badge variant={STATUS_VARIANT[entity.status]}>
              {t(`Inventory.StockCounts.status.${entity.status}`, { defaultValue: entity.status })}
            </Badge>
          }
          actions={
            <>
              {entity.status === 'Plan' && (
                <Button
                  type="button"
                  size="sm"
                  onClick={onStart}
                  isLoading={start.isPending}
                  disabled={start.isPending}
                >
                  <PlayCircle size={13} />
                  {t('Inventory.StockCounts.start', { defaultValue: 'Sayımı Başlat' })}
                </Button>
              )}
              {entity.status === 'Counting' && (
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  onClick={onReconcile}
                  isLoading={reconcile.isPending}
                  disabled={reconcile.isPending || !allCounted}
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
                </Button>
              )}
              {entity.status === 'Reconciliation' && (
                <Button
                  type="button"
                  size="sm"
                  onClick={onPost}
                  isLoading={post.isPending}
                  disabled={post.isPending}
                >
                  <CheckCircle2 size={13} />
                  {t('Inventory.StockCounts.post.button', { defaultValue: 'Sayımı İşle' })}
                </Button>
              )}
              {!readonly && (
                <Button
                  type="button"
                  variant="danger"
                  size="sm"
                  onClick={onCancel}
                  isLoading={cancel.isPending}
                  disabled={cancel.isPending}
                >
                  <XCircle size={13} />
                  {t('common.cancel', { defaultValue: 'İptal' })}
                </Button>
              )}
            </>
          }
        />
      }
    >
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
                    <Input
                      type="number"
                      step="0.01"
                      value={drafts[l.id] ?? ''}
                      onChange={(e) => setDrafts((prev) => ({ ...prev, [l.id]: e.target.value }))}
                      className="w-24"
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
                          ? 'text-success-600'
                          : 'text-danger-600'
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
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={onSaveCounts}
            isLoading={record.isPending}
            disabled={record.isPending}
          >
            <Save size={13} />
            {t('Inventory.StockCounts.saveCounts', { defaultValue: 'Sayımları Kaydet' })}
          </Button>
        </div>
      )}

      {entity.status === 'Counting' && (
        <div className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
          <Textarea
            label={t('Inventory.StockCounts.reconcileNotes', {
              defaultValue: 'Mutabakat Notları (opsiyonel)',
            })}
            value={reconcileNotes}
            onChange={(e) => setReconcileNotes(e.target.value)}
            rows={2}
            maxLength={1000}
          />
        </div>
      )}
    </DetailPageTemplate>
  );
};

export default StockCountDetailPage;
export { StockCountDetailPage };
