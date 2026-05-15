import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  AlertTriangle,
  Edit3,
  Eye,
  Package,
  RefreshCw,
  TrendingDown,
  Warehouse,
} from 'lucide-react';
import { useStockByProductQuery, useStockSummaryQuery } from '../hooks/useInventoryQueries';
import type { StockItem } from '../model/inventory.types';
import { AdjustStockModal } from './AdjustStockModal';
import { StockItemDetailModal } from './StockItemDetailModal';

interface Props {
  productId: string;
  productSku: string;
  productName: string;
  currency: string;
}

const fmtNumber = (n: number, locale: string, decimals = 2) =>
  new Intl.NumberFormat(locale, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(n);

const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

const fmtDateTime = (iso: string | null, locale: string) => {
  if (!iso) return '—';
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short' }).format(
      new Date(iso),
    );
  } catch {
    return iso;
  }
};

export const StockByWarehouseTab = ({ productId, productSku, productName, currency }: Props) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const stockQuery = useStockByProductQuery(productId);
  const summaryQuery = useStockSummaryQuery(productId);
  const [adjustTarget, setAdjustTarget] = useState<StockItem | null>(null);
  const [detailTarget, setDetailTarget] = useState<StockItem | null>(null);
  const [newAdjustWarehouseId, setNewAdjustWarehouseId] = useState<string | null>(null);

  const items = stockQuery.data?.data ?? [];
  const summary = summaryQuery.data?.data;
  const isLoading = stockQuery.isPending && summaryQuery.isPending;

  return (
    <div className="space-y-4">
      {summary && (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <SummaryCard
            icon={<Package size={14} />}
            label={t('inventory.summary.totalOnHand')}
            value={fmtNumber(summary.totalOnHand, locale)}
            tone="indigo"
          />
          <SummaryCard
            icon={<RefreshCw size={14} />}
            label={t('inventory.summary.totalReserved')}
            value={fmtNumber(summary.totalReserved, locale)}
            tone="amber"
          />
          <SummaryCard
            icon={<TrendingDown size={14} />}
            label={t('inventory.summary.totalAvailable')}
            value={fmtNumber(summary.totalAvailable, locale)}
            tone={summary.isBelowReorder ? 'red' : 'emerald'}
            footer={
              summary.isBelowReorder ? (
                <span className="inline-flex items-center gap-1 text-[10px] font-medium text-red-600 dark:text-red-400">
                  <AlertTriangle size={11} />
                  {t('inventory.summary.belowReorder')}
                </span>
              ) : undefined
            }
          />
          <SummaryCard
            icon={<Warehouse size={14} />}
            label={t('inventory.summary.warehouses')}
            value={summary.warehouseCount.toString()}
            tone="slate"
            footer={
              <span className="text-[10px] text-slate-500 dark:text-slate-400">
                {t('inventory.summary.avgCost')}:{' '}
                {fmtCurrency(summary.averageCost, summary.currency, locale)}
              </span>
            }
          />
        </div>
      )}

      <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
        <div className="flex items-center justify-between bg-slate-50 px-3 py-2 dark:bg-slate-900/40">
          <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-200">
            {t('inventory.byWarehouse.title')}
          </h3>
          <button
            type="button"
            onClick={() => setNewAdjustWarehouseId('')}
            className="inline-flex items-center gap-1.5 rounded border border-indigo-300 bg-white px-2 py-1 text-xs font-medium text-indigo-700 hover:bg-indigo-50 dark:border-indigo-700 dark:bg-slate-900 dark:text-indigo-300 dark:hover:bg-indigo-500/10"
          >
            <Edit3 size={12} />
            {t('inventory.byWarehouse.adjust')}
          </button>
        </div>

        {isLoading ? (
          <div className="px-3 py-6 text-center text-sm text-slate-500">{t('common.loading')}</div>
        ) : items.length === 0 ? (
          <div className="px-3 py-6 text-center text-sm text-slate-500 dark:text-slate-400">
            {t('inventory.byWarehouse.empty')}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2 text-left">{t('inventory.fields.warehouse')}</th>
                <th className="px-3 py-2 text-right">{t('inventory.fields.onHand')}</th>
                <th className="px-3 py-2 text-right">{t('inventory.fields.reserved')}</th>
                <th className="px-3 py-2 text-right">{t('inventory.fields.available')}</th>
                <th className="px-3 py-2 text-right">{t('inventory.fields.avgCost')}</th>
                <th className="px-3 py-2 text-right">{t('inventory.fields.value')}</th>
                <th className="px-3 py-2 text-left">{t('inventory.fields.lastMovement')}</th>
                <th className="px-3 py-2"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {items.map((it) => {
                const belowReorder =
                  it.reorderPoint !== null && it.availableToPromise <= it.reorderPoint;
                return (
                  <tr key={it.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                    <td className="px-3 py-2">
                      <div className="font-medium text-slate-800 dark:text-slate-100">
                        {it.warehouseName}
                      </div>
                      <div className="text-[10px] text-slate-500">{it.warehouseCode}</div>
                      {it.lotNumber && (
                        <div className="mt-0.5 inline-flex items-center gap-1 rounded bg-violet-100 px-1.5 py-0.5 text-[10px] font-medium text-violet-700 dark:bg-violet-500/20 dark:text-violet-300">
                          {t('inventory.fields.lot')}: {it.lotNumber}
                        </div>
                      )}
                      {it.binLocation && (
                        <div className="mt-0.5 text-[10px] text-slate-500">
                          {t('inventory.fields.bin')}: {it.binLocation}
                        </div>
                      )}
                    </td>
                    <td className="px-3 py-2 text-right font-mono text-slate-800 dark:text-slate-200">
                      {fmtNumber(it.onHand, locale)}
                    </td>
                    <td className="px-3 py-2 text-right font-mono text-amber-700 dark:text-amber-300">
                      {fmtNumber(it.reserved, locale)}
                    </td>
                    <td
                      className={`px-3 py-2 text-right font-mono font-semibold ${
                        belowReorder
                          ? 'text-red-600 dark:text-red-400'
                          : 'text-emerald-700 dark:text-emerald-300'
                      }`}
                    >
                      {fmtNumber(it.availableToPromise, locale)}
                      {belowReorder && (
                        <div className="text-[10px] font-normal text-red-500">
                          {t('inventory.fields.reorderHint', {
                            value: fmtNumber(it.reorderPoint ?? 0, locale),
                          })}
                        </div>
                      )}
                    </td>
                    <td className="px-3 py-2 text-right font-mono text-slate-700 dark:text-slate-300">
                      {fmtCurrency(it.avgCost, currency, locale)}
                    </td>
                    <td className="px-3 py-2 text-right font-mono text-slate-700 dark:text-slate-300">
                      {fmtCurrency(it.onHand * it.avgCost, currency, locale)}
                    </td>
                    <td className="px-3 py-2 text-[11px] text-slate-500 dark:text-slate-400">
                      {fmtDateTime(it.lastMovementAtUtc, locale)}
                    </td>
                    <td className="px-3 py-2 text-right">
                      <div className="inline-flex items-center gap-1">
                        <button
                          type="button"
                          onClick={() => setDetailTarget(it)}
                          className="inline-flex items-center gap-1 rounded border border-slate-200 bg-white px-2 py-1 text-[11px] font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
                          title={t('common.details')}
                        >
                          <Eye size={11} />
                          {t('common.details')}
                        </button>
                        <button
                          type="button"
                          onClick={() => setAdjustTarget(it)}
                          className="inline-flex items-center gap-1 rounded border border-slate-200 bg-white px-2 py-1 text-[11px] font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
                        >
                          <Edit3 size={11} />
                          {t('common.edit')}
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {(adjustTarget || newAdjustWarehouseId !== null) && (
        <AdjustStockModal
          key={adjustTarget?.id ?? 'new'}
          open
          productId={productId}
          productSku={productSku}
          productName={productName}
          currency={currency}
          presetWarehouseId={adjustTarget?.warehouseId ?? null}
          presetLotId={adjustTarget?.lotId ?? null}
          currentOnHand={adjustTarget?.onHand ?? null}
          currentAvgCost={adjustTarget?.avgCost ?? null}
          onClose={() => {
            setAdjustTarget(null);
            setNewAdjustWarehouseId(null);
          }}
        />
      )}

      {detailTarget && (
        <StockItemDetailModal
          open
          stockItem={detailTarget}
          currency={currency}
          onClose={() => setDetailTarget(null)}
          onAdjust={(item) => {
            setDetailTarget(null);
            setAdjustTarget(item);
          }}
        />
      )}
    </div>
  );
};

interface SummaryCardProps {
  icon: React.ReactNode;
  label: string;
  value: string;
  tone: 'indigo' | 'amber' | 'emerald' | 'red' | 'slate';
  footer?: React.ReactNode;
}

const TONE_BORDER: Record<SummaryCardProps['tone'], string> = {
  indigo: 'border-indigo-200 dark:border-indigo-500/30',
  amber: 'border-amber-200 dark:border-amber-500/30',
  emerald: 'border-emerald-200 dark:border-emerald-500/30',
  red: 'border-red-200 dark:border-red-500/30',
  slate: 'border-slate-200 dark:border-slate-700',
};

const TONE_VALUE: Record<SummaryCardProps['tone'], string> = {
  indigo: 'text-indigo-700 dark:text-indigo-300',
  amber: 'text-amber-700 dark:text-amber-300',
  emerald: 'text-emerald-700 dark:text-emerald-300',
  red: 'text-red-700 dark:text-red-300',
  slate: 'text-slate-800 dark:text-slate-200',
};

const SummaryCard = ({ icon, label, value, tone, footer }: SummaryCardProps) => (
  <div className={`rounded-lg border bg-white p-2.5 dark:bg-slate-900 ${TONE_BORDER[tone]}`}>
    <div className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {icon}
      {label}
    </div>
    <div className={`mt-1 text-lg font-bold tabular-nums ${TONE_VALUE[tone]}`}>{value}</div>
    {footer && <div className="mt-1">{footer}</div>}
  </div>
);
