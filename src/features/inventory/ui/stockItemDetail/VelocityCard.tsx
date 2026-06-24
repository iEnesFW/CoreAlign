import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { TrendingUp } from 'lucide-react';
import type { StockItem, StockMovement } from '@/features/inventory/model/inventory.types';
import { fmtInt, fmtNumber } from './format';

export const VelocityCard = ({
  stockItem,
  movements,
  locale,
}: {
  stockItem: StockItem;
  movements: StockMovement[];
  locale: string;
}) => {
  const { t } = useTranslation();
  const stats = useMemo(() => {
    const horizonDays = 90;
    const horizonMs = horizonDays * 24 * 60 * 60 * 1000;
    const latestTs = movements
      .map((m) => new Date(m.occurredAtUtc).getTime())
      .filter((t) => !Number.isNaN(t))
      .reduce((acc, t) => Math.max(acc, t), 0);
    const cutoff = latestTs > 0 ? latestTs - horizonMs : 0;
    let totalOut = 0;
    let totalIn = 0;
    const days = new Set<string>();
    for (const m of movements) {
      const ts = new Date(m.occurredAtUtc).getTime();
      if (Number.isNaN(ts)) continue;
      if (ts < cutoff) continue;
      days.add(m.occurredAtUtc.slice(0, 10));
      if (
        m.type === 'Issue' ||
        m.type === 'TransferOut' ||
        m.type === 'AdjustmentNegative' ||
        m.type === 'CountVarianceNegative'
      ) {
        totalOut += Math.abs(m.quantity);
      } else if (
        m.type === 'Receipt' ||
        m.type === 'TransferIn' ||
        m.type === 'AdjustmentPositive' ||
        m.type === 'CountVariancePositive'
      ) {
        totalIn += Math.abs(m.quantity);
      }
    }
    const activeDays = days.size;
    const avgDailyOut = totalOut / horizonDays;
    const avgDailyIn = totalIn / horizonDays;
    const daysOfStock = avgDailyOut > 0 ? stockItem.availableToPromise / avgDailyOut : Infinity;
    const netDelta = totalIn - totalOut;
    return { totalOut, totalIn, avgDailyOut, avgDailyIn, daysOfStock, netDelta, activeDays };
  }, [movements, stockItem.availableToPromise]);

  const abc = useMemo<{ class: 'A' | 'B' | 'C' | 'D'; reason: string }>(() => {
    const value = stockItem.onHand * stockItem.avgCost;
    if (stats.avgDailyOut > 5 && value > 10000)
      return { class: 'A', reason: t('inventory.velocity.abcA') };
    if (stats.avgDailyOut > 1 || value > 2000)
      return { class: 'B', reason: t('inventory.velocity.abcB') };
    if (stats.avgDailyOut > 0.1) return { class: 'C', reason: t('inventory.velocity.abcC') };
    return { class: 'D', reason: t('inventory.velocity.abcD') };
  }, [stats.avgDailyOut, stockItem.onHand, stockItem.avgCost, t]);

  const abcTone: Record<'A' | 'B' | 'C' | 'D', { bg: string; text: string }> = {
    A: {
      bg: 'bg-success-100 dark:bg-success-500/20',
      text: 'text-success-700 dark:text-success-300',
    },
    B: {
      bg: 'bg-primary-100 dark:bg-primary-500/20',
      text: 'text-primary-700 dark:text-primary-300',
    },
    C: {
      bg: 'bg-warning-100 dark:bg-warning-500/20',
      text: 'text-warning-700 dark:text-warning-300',
    },
    D: { bg: 'bg-slate-100 dark:bg-slate-700/40', text: 'text-slate-700 dark:text-slate-300' },
  };

  const dosLabel =
    stats.daysOfStock === Infinity ? '∞' : `${fmtInt(Math.round(stats.daysOfStock), locale)}`;
  const dosTone =
    stats.daysOfStock === Infinity
      ? 'text-slate-500 dark:text-slate-400'
      : stats.daysOfStock < 7
        ? 'text-danger-600 dark:text-danger-400'
        : stats.daysOfStock < 14
          ? 'text-warning-600 dark:text-warning-400'
          : 'text-success-600 dark:text-success-400';

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <TrendingUp size={12} />
          {t('inventory.velocity.title')}
        </span>
        <span
          className={`inline-flex items-center gap-1 rounded-full px-1.5 py-0.5 text-[10px] font-bold ${abcTone[abc.class].bg} ${abcTone[abc.class].text}`}
          title={abc.reason}
        >
          {t('inventory.velocity.abcClass')} {abc.class}
        </span>
      </header>
      <div className="mt-2 grid grid-cols-2 gap-2 sm:grid-cols-4 text-[11px]">
        <div className="rounded border border-slate-200 px-2 py-1.5 dark:border-slate-800">
          <div className="text-[9px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
            {t('inventory.velocity.avgDailyOut')}
          </div>
          <div className="mt-0.5 font-bold tabular-nums text-slate-900 dark:text-slate-100">
            {fmtNumber(stats.avgDailyOut, locale)}
          </div>
          <div className="text-[9px] text-slate-500 dark:text-slate-400">
            {t('inventory.velocity.windowDays', { count: 90 })}
          </div>
        </div>
        <div className="rounded border border-slate-200 px-2 py-1.5 dark:border-slate-800">
          <div className="text-[9px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
            {t('inventory.velocity.avgDailyIn')}
          </div>
          <div className="mt-0.5 font-bold tabular-nums text-slate-900 dark:text-slate-100">
            {fmtNumber(stats.avgDailyIn, locale)}
          </div>
          <div className="text-[9px] text-slate-500 dark:text-slate-400">
            {t('inventory.velocity.windowDays', { count: 90 })}
          </div>
        </div>
        <div className="rounded border border-slate-200 px-2 py-1.5 dark:border-slate-800">
          <div className="text-[9px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
            {t('inventory.velocity.daysOfStock')}
          </div>
          <div className={`mt-0.5 font-bold tabular-nums ${dosTone}`}>{dosLabel}</div>
          <div className="text-[9px] text-slate-500 dark:text-slate-400">
            {t('inventory.velocity.daysOfStockSub')}
          </div>
        </div>
        <div className="rounded border border-slate-200 px-2 py-1.5 dark:border-slate-800">
          <div className="text-[9px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
            {t('inventory.velocity.netDelta')}
          </div>
          <div
            className={`mt-0.5 font-bold tabular-nums ${stats.netDelta > 0 ? 'text-success-600 dark:text-success-400' : stats.netDelta < 0 ? 'text-danger-600 dark:text-danger-400' : 'text-slate-700 dark:text-slate-200'}`}
          >
            {stats.netDelta > 0 ? '+' : ''}
            {fmtNumber(stats.netDelta, locale)}
          </div>
          <div className="text-[9px] text-slate-500 dark:text-slate-400">
            {stats.activeDays} {t('inventory.velocity.activeDays')}
          </div>
        </div>
      </div>
      <p className="mt-2 text-[10px] text-slate-500 dark:text-slate-400">{abc.reason}</p>
    </section>
  );
};
