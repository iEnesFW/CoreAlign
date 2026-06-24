import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowDownToLine, ArrowUpFromLine, Layers, TrendingUp } from 'lucide-react';
import type { StockMovement } from '@/features/inventory/model/inventory.types';
import { fmtCurrency, fmtDate, fmtDateTime, fmtInt, fmtNumber, isInbound } from './format';

export const MovementChart = ({
  movements,
  locale,
}: {
  movements: StockMovement[];
  locale: string;
}) => {
  const { t } = useTranslation();
  const series = useMemo(() => {
    const buckets = new Map<string, { in: number; out: number; date: string }>();
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    for (let i = 29; i >= 0; i--) {
      const d = new Date(today);
      d.setDate(d.getDate() - i);
      const key = d.toISOString().slice(0, 10);
      buckets.set(key, { in: 0, out: 0, date: key });
    }
    for (const m of movements) {
      const key = m.occurredAtUtc.slice(0, 10);
      if (!buckets.has(key)) continue;
      const bucket = buckets.get(key)!;
      if (isInbound(m.type)) bucket.in += m.quantity;
      else bucket.out += m.quantity;
    }
    return Array.from(buckets.values());
  }, [movements]);

  const maxVal = Math.max(1, ...series.map((s) => Math.max(s.in, s.out)));
  const totalIn = series.reduce((acc, s) => acc + s.in, 0);
  const totalOut = series.reduce((acc, s) => acc + s.out, 0);

  if (movements.length === 0) return null;

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <TrendingUp size={12} />
          {t('inventory.stockItem.movementChart')}
        </span>
        <span className="space-x-2 text-[10px] normal-case">
          <span className="inline-flex items-center gap-1 text-success-600 dark:text-success-400">
            <ArrowDownToLine size={10} /> {fmtInt(totalIn, locale)}
          </span>
          <span className="inline-flex items-center gap-1 text-danger-600 dark:text-danger-400">
            <ArrowUpFromLine size={10} /> {fmtInt(totalOut, locale)}
          </span>
        </span>
      </header>
      <div className="mt-2 flex h-16 items-end gap-px">
        {series.map((b) => {
          const inH = (b.in / maxVal) * 100;
          const outH = (b.out / maxVal) * 100;
          return (
            <div key={b.date} className="flex flex-1 flex-col items-stretch justify-end">
              {b.in > 0 && (
                <div
                  className="rounded-t-sm bg-success-500/80"
                  style={{ height: `${inH}%`, minHeight: 1 }}
                />
              )}
              {b.out > 0 && (
                <div
                  className="rounded-b-sm bg-danger-500/80"
                  style={{ height: `${outH}%`, minHeight: 1 }}
                />
              )}
            </div>
          );
        })}
      </div>
      <div className="mt-1 flex items-center justify-between text-[9px] text-slate-500 dark:text-slate-400">
        <span>{fmtDate(series[0]?.date ?? null, locale)}</span>
        <span>{fmtDate(series[series.length - 1]?.date ?? null, locale)}</span>
      </div>
    </section>
  );
};

export const MovementsList = ({
  movements,
  loading,
  locale,
  currency,
}: {
  movements: StockMovement[];
  loading: boolean;
  locale: string;
  currency: string;
}) => {
  const { t } = useTranslation();
  return (
    <section className="rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 border-b border-slate-100 px-3 py-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:border-slate-800 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <Layers size={12} />
          {t('inventory.stockItem.recentMovements')}
        </span>
        <span className="text-slate-400">{movements.length}</span>
      </header>
      {loading ? (
        <div className="p-4 text-center text-[11px] italic text-slate-400">
          {t('common.loading')}
        </div>
      ) : movements.length === 0 ? (
        <div className="p-4 text-center text-[11px] italic text-slate-400">
          {t('inventory.movements.empty')}
        </div>
      ) : (
        <ul className="max-h-72 divide-y divide-slate-100 overflow-y-auto dark:divide-slate-800">
          {movements.slice(0, 30).map((m) => {
            const inbound = isInbound(m.type);
            return (
              <li
                key={m.id}
                className="flex items-center justify-between gap-2 px-3 py-1.5 text-[11px]"
              >
                <div className="flex min-w-0 items-center gap-2">
                  <span
                    className={`inline-flex h-5 w-5 shrink-0 items-center justify-center rounded ${inbound ? 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300' : 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300'}`}
                  >
                    {inbound ? <ArrowDownToLine size={11} /> : <ArrowUpFromLine size={11} />}
                  </span>
                  <div className="min-w-0">
                    <div className="font-medium text-slate-900 dark:text-slate-100">
                      {t(`inventory.movements.type.${m.type}`, { defaultValue: m.type })}
                    </div>
                    <div className="text-[10px] text-slate-500 dark:text-slate-400">
                      {fmtDateTime(m.occurredAtUtc, locale)}
                      {m.sourceReference ? ` · ${m.sourceReference}` : ''}
                      {m.reasonCodeName ? ` · ${m.reasonCodeName}` : ''}
                    </div>
                  </div>
                </div>
                <div className="shrink-0 text-right">
                  <div
                    className={`font-semibold tabular-nums ${inbound ? 'text-success-600 dark:text-success-400' : 'text-danger-600 dark:text-danger-400'}`}
                  >
                    {inbound ? '+' : '−'}
                    {fmtNumber(Math.abs(m.quantity), locale)}
                  </div>
                  <div className="text-[9px] text-slate-500 dark:text-slate-400">
                    {fmtCurrency(m.unitCost, currency, locale)}
                  </div>
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
};
