import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Layers, MapPin, Package, Tag } from 'lucide-react';
import { useAllocationsByOrderQuery } from '@/features/inventory/hooks/useInventoryQueries';
import type { AllocationStatus, StockAllocation } from '@/features/inventory/model/inventory.types';

interface Props {
  orderId: string;
  locale: string;
}

const fmtNumber = (value: number, locale: string, decimals = 2) =>
  new Intl.NumberFormat(locale, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(value);

const fmtDate = (iso: string, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short' }).format(
      new Date(iso),
    );
  } catch {
    return iso;
  }
};

const statusTone: Record<AllocationStatus, string> = {
  Active: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  PartiallyConsumed: 'bg-amber-100 text-amber-700 dark:bg-amber-500/20 dark:text-amber-300',
  Consumed: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300',
  Released: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
};

export const OrderAllocationsTab = ({ orderId, locale }: Props) => {
  const { t } = useTranslation();
  const query = useAllocationsByOrderQuery(orderId);
  const allocations = useMemo(() => query.data?.data ?? [], [query.data?.data]);

  const totals = useMemo(() => {
    const total = allocations.reduce((s, a) => s + a.quantity, 0);
    const consumed = allocations.reduce((s, a) => s + a.quantityConsumed, 0);
    const remaining = allocations.reduce((s, a) => s + a.remaining, 0);
    const released = allocations.filter((a) => a.status === 'Released').length;
    return { total, consumed, remaining, released };
  }, [allocations]);

  const byWarehouse = useMemo(() => {
    const map = new Map<string, { warehouseName: string; quantity: number; remaining: number }>();
    for (const a of allocations) {
      if (a.status === 'Released') continue;
      const prev = map.get(a.warehouseId) ?? {
        warehouseName: a.warehouseName,
        quantity: 0,
        remaining: 0,
      };
      prev.quantity += a.quantity;
      prev.remaining += a.remaining;
      map.set(a.warehouseId, prev);
    }
    return Array.from(map.entries()).map(([id, v]) => ({ id, ...v }));
  }, [allocations]);

  if (query.isPending && allocations.length === 0) {
    return <div className="text-sm italic text-slate-500">{t('common.loading')}</div>;
  }
  if (allocations.length === 0) {
    return (
      <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800">
        {t('orders.allocations.empty')}
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
        <Kpi
          icon={<Package size={11} />}
          label={t('orders.allocations.totalAllocated')}
          value={fmtNumber(totals.total, locale)}
        />
        <Kpi
          icon={<Layers size={11} />}
          label={t('orders.allocations.consumed')}
          value={fmtNumber(totals.consumed, locale)}
          tone="amber"
        />
        <Kpi
          icon={<Layers size={11} />}
          label={t('orders.allocations.remaining')}
          value={fmtNumber(totals.remaining, locale)}
          tone={totals.remaining > 0 ? 'emerald' : 'slate'}
        />
        <Kpi
          icon={<Tag size={11} />}
          label={t('orders.allocations.released')}
          value={String(totals.released)}
          tone="slate"
        />
      </div>

      {byWarehouse.length > 0 && (
        <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
          <header className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
            <MapPin size={12} />
            {t('orders.allocations.byWarehouse')}
          </header>
          <ul className="mt-2 space-y-1">
            {byWarehouse.map((w) => {
              const usedPct = w.quantity > 0 ? ((w.quantity - w.remaining) / w.quantity) * 100 : 0;
              return (
                <li
                  key={w.id}
                  className="rounded border border-slate-200 px-2 py-1.5 text-[11px] dark:border-slate-800"
                >
                  <div className="flex items-center justify-between">
                    <span className="font-medium text-slate-900 dark:text-slate-100">
                      {w.warehouseName}
                    </span>
                    <span className="tabular-nums text-slate-500 dark:text-slate-400">
                      {fmtNumber(w.quantity - w.remaining, locale)} /{' '}
                      {fmtNumber(w.quantity, locale)}
                    </span>
                  </div>
                  <div className="mt-1 h-1 w-full overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
                    <div
                      className="h-full rounded-full bg-amber-500"
                      style={{ width: `${usedPct}%` }}
                    />
                  </div>
                </li>
              );
            })}
          </ul>
        </section>
      )}

      <section className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        <header className="flex items-center justify-between gap-2 border-b border-slate-100 px-3 py-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:border-slate-800 dark:text-slate-400">
          <span>{t('orders.allocations.list')}</span>
          <span className="text-slate-400">{allocations.length}</span>
        </header>
        <ul className="divide-y divide-slate-100 dark:divide-slate-800">
          {allocations.map((alloc) => (
            <AllocationRow key={alloc.id} allocation={alloc} locale={locale} />
          ))}
        </ul>
      </section>
    </div>
  );
};

const kpiTones: Record<'slate' | 'indigo' | 'amber' | 'emerald', string> = {
  slate: 'border-slate-200 dark:border-slate-800',
  indigo: 'border-indigo-200 dark:border-indigo-500/30',
  amber: 'border-amber-200 dark:border-amber-500/30',
  emerald: 'border-emerald-200 dark:border-emerald-500/30',
};

const Kpi = ({
  icon,
  label,
  value,
  tone = 'indigo',
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  tone?: keyof typeof kpiTones;
}) => (
  <div className={`rounded-lg border bg-white p-2 dark:bg-slate-900 ${kpiTones[tone]}`}>
    <div className="flex items-center gap-1 text-[9px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {icon}
      <span>{label}</span>
    </div>
    <div className="mt-0.5 text-sm font-bold tabular-nums text-slate-900 dark:text-slate-100">
      {value}
    </div>
  </div>
);

const AllocationRow = ({ allocation, locale }: { allocation: StockAllocation; locale: string }) => {
  const { t } = useTranslation();
  const tone = statusTone[allocation.status] ?? statusTone.Released;
  return (
    <li className="flex items-center justify-between gap-2 px-3 py-2 text-[11px]">
      <div className="min-w-0">
        <div className="flex items-center gap-1.5">
          <span className="font-medium text-slate-900 dark:text-slate-100">
            {allocation.productName}
          </span>
          <span className={`rounded px-1.5 py-0.5 text-[9px] font-semibold ${tone}`}>
            {t(`orders.allocations.status.${allocation.status}` as never, {
              defaultValue: allocation.status,
            })}
          </span>
        </div>
        <div className="mt-0.5 flex flex-wrap items-center gap-x-2 text-[10px] text-slate-500 dark:text-slate-400">
          <span className="font-mono">{allocation.productSku}</span>
          <span className="inline-flex items-center gap-0.5">
            <MapPin size={9} />
            {allocation.warehouseName}
          </span>
          {allocation.lotNumber && (
            <span className="inline-flex items-center gap-0.5">
              <Tag size={9} />
              {allocation.lotNumber}
            </span>
          )}
          <span>{fmtDate(allocation.allocatedAtUtc, locale)}</span>
        </div>
      </div>
      <div className="shrink-0 text-right">
        <div className="text-[11px] font-semibold tabular-nums text-slate-900 dark:text-slate-100">
          {fmtNumber(allocation.remaining, locale)} / {fmtNumber(allocation.quantity, locale)}
        </div>
        <div className="text-[9px] text-slate-500 dark:text-slate-400">
          {t('orders.allocations.remainingLabel')}
        </div>
      </div>
    </li>
  );
};
