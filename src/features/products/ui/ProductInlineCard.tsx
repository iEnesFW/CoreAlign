import { useTranslation } from 'react-i18next';
import { AlertTriangle, Warehouse } from 'lucide-react';
import { InlineDetailCard } from '@/shared/ui/InlineDetailCard/InlineDetailCard';
import { formatCurrency, formatDate, formatNumber } from '@/shared/lib/format';
import {
  useStockByProductQuery,
  useStockSummaryQuery,
} from '@/features/inventory/hooks/useInventoryQueries';
import type { Product } from '@/features/products/model/product.types';

interface ProductInlineCardProps {
  product: Product;
  onClose: () => void;
  onOpenPanel: () => void;
}

export const ProductInlineCard = ({ product, onClose, onOpenPanel }: ProductInlineCardProps) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const summaryQuery = useStockSummaryQuery(product.id);
  const byWarehouseQuery = useStockByProductQuery(product.id);

  const summary = summaryQuery.data?.data;
  const items = byWarehouseQuery.data?.data ?? [];
  const currency = summary?.currency ?? product.currency;

  return (
    <InlineDetailCard
      title={product.name}
      subtitle={[product.sku, product.unit].filter(Boolean).join(' · ') || undefined}
      onOpenPanel={onOpenPanel}
      onClose={onClose}
    >
      {summaryQuery.isPending ? (
        <div className="py-6 text-center text-sm text-slate-500">
          {t('ProductCard.Loading', { defaultValue: 'Yükleniyor…' })}
        </div>
      ) : (
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <Metric
              label={t('ProductCard.TotalStock', { defaultValue: 'Toplam Stok' })}
              value={formatNumber(summary?.totalOnHand ?? 0, locale)}
            />
            <Metric
              label={t('ProductCard.Reserved', { defaultValue: 'Rezerve' })}
              value={formatNumber(summary?.totalReserved ?? 0, locale)}
            />
            <Metric
              label={t('ProductCard.Available', { defaultValue: 'Kullanılabilir' })}
              value={formatNumber(summary?.totalAvailable ?? 0, locale)}
              tone={summary && summary.totalAvailable <= 0 ? 'rose' : 'emerald'}
            />
            <Metric
              label={t('ProductCard.AverageCost', { defaultValue: 'Ort. Maliyet' })}
              value={formatCurrency(summary?.averageCost ?? 0, locale, currency)}
            />
          </div>

          {summary?.isBelowReorder && (
            <div className="flex items-center gap-1.5 rounded-lg bg-warning-50 px-3 py-1.5 text-xs text-warning-800 dark:bg-warning-500/10 dark:text-warning-300">
              <AlertTriangle size={13} />
              {t('ProductCard.BelowReorderLevel', {
                defaultValue: 'Bu ürün yeniden sipariş seviyesinin altında.',
              })}
            </div>
          )}

          <div>
            <h4 className="mb-1.5 flex items-center gap-1.5 text-[11px] font-semibold uppercase text-slate-500">
              <Warehouse size={12} />
              {t('ProductCard.StockByWarehouse', {
                defaultValue: 'Depo Bazında Stok ({{count}})',
                count: summary?.warehouseCount ?? items.length,
              })}
            </h4>
            {items.length > 0 ? (
              <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
                <table className="w-full text-xs">
                  <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
                    <tr>
                      <th className="px-2 py-1.5 text-left">
                        {t('ProductCard.ColWarehouse', { defaultValue: 'Depo' })}
                      </th>
                      <th className="px-2 py-1.5 text-left">
                        {t('ProductCard.ColLot', { defaultValue: 'Lot' })}
                      </th>
                      <th className="px-2 py-1.5 text-right">
                        {t('ProductCard.ColOnHand', { defaultValue: 'Eldeki' })}
                      </th>
                      <th className="px-2 py-1.5 text-right">
                        {t('ProductCard.Reserved', { defaultValue: 'Rezerve' })}
                      </th>
                      <th className="px-2 py-1.5 text-right">
                        {t('ProductCard.Available', { defaultValue: 'Kullanılabilir' })}
                      </th>
                      <th className="px-2 py-1.5 text-left">
                        {t('ProductCard.ColLastMovement', { defaultValue: 'Son Hareket' })}
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {items.map((s) => (
                      <tr key={s.id} className="border-t border-slate-100 dark:border-slate-800">
                        <td className="px-2 py-1.5">
                          {s.warehouseName}
                          {s.binLocation && (
                            <span className="ml-1 text-[10px] text-slate-400">{s.binLocation}</span>
                          )}
                        </td>
                        <td className="px-2 py-1.5 text-slate-500">{s.lotNumber ?? '—'}</td>
                        <td className="px-2 py-1.5 text-right font-mono">
                          {formatNumber(s.onHand, locale)}
                        </td>
                        <td className="px-2 py-1.5 text-right font-mono text-slate-500">
                          {formatNumber(s.reserved, locale)}
                        </td>
                        <td className="px-2 py-1.5 text-right font-mono font-semibold">
                          {formatNumber(s.availableToPromise, locale)}
                        </td>
                        <td className="px-2 py-1.5 text-slate-500">
                          {s.lastMovementAtUtc ? formatDate(s.lastMovementAtUtc, locale) : '—'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="py-3 text-center text-xs text-slate-400">
                {t('ProductCard.NoStockRecords', {
                  defaultValue: 'Bu ürün için stok kaydı bulunamadı.',
                })}
              </p>
            )}
          </div>
        </div>
      )}
    </InlineDetailCard>
  );
};

const toneMap = {
  rose: 'text-danger-600 dark:text-danger-400',
  emerald: 'text-success-600 dark:text-success-400',
  slate: 'text-slate-900 dark:text-slate-100',
} as const;

const Metric = ({
  label,
  value,
  tone = 'slate',
}: {
  label: string;
  value: string;
  tone?: keyof typeof toneMap;
}) => (
  <div className="rounded-lg border border-slate-200 bg-slate-50/50 px-3 py-2 dark:border-slate-800 dark:bg-slate-800/30">
    <div className="text-[10px] font-medium uppercase text-slate-500">{label}</div>
    <div className={`mt-0.5 font-mono text-sm font-semibold ${toneMap[tone]}`}>{value}</div>
  </div>
);
