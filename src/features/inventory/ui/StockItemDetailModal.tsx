import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  AlertTriangle,
  ArrowDownToLine,
  ArrowUpFromLine,
  Boxes,
  Calendar,
  CalendarClock,
  Edit3,
  Hash,
  Layers,
  MapPin,
  Package,
  RefreshCw,
  Tag,
  TrendingDown,
  TrendingUp,
  Warehouse,
  X,
} from 'lucide-react';
import { Badge } from '@/shared/ui/Badge/Badge';
import { useStockMovementsQuery, useLotsByProductQuery } from '../hooks/useInventoryQueries';
import type { StockItem, StockMovement, StockMovementType } from '../model/inventory.types';

interface Props {
  open: boolean;
  stockItem: StockItem;
  currency: string;
  onClose: () => void;
  onAdjust?: (stockItem: StockItem) => void;
}

const fmtNumber = (n: number, locale: string, decimals = 2) =>
  new Intl.NumberFormat(locale, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(n);

const fmtInt = (n: number, locale: string) => new Intl.NumberFormat(locale).format(n);

const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

const fmtDate = (iso: string | null, locale: string) => {
  if (!iso) return '—';
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(new Date(iso));
  } catch {
    return iso.slice(0, 10);
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

const inboundTypes: StockMovementType[] = [
  'OpeningBalance',
  'Receipt',
  'TransferIn',
  'AdjustmentPositive',
  'CountVariancePositive',
  'UnReservation',
];

const isInbound = (type: StockMovementType) => inboundTypes.includes(type);

export const StockItemDetailModal = ({ open, stockItem, currency, onClose, onAdjust }: Props) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;

  const movementsQuery = useStockMovementsQuery({
    productId: stockItem.productId,
    warehouseId: stockItem.warehouseId,
    page: 1,
    pageSize: 50,
  });
  const lotsQuery = useLotsByProductQuery(stockItem.lotId ? stockItem.productId : null);

  const movements = useMemo(() => {
    const all = movementsQuery.data?.data?.items ?? [];
    return stockItem.lotId ? all.filter((m) => m.lotId === stockItem.lotId) : all;
  }, [movementsQuery.data, stockItem.lotId]);

  const lot = useMemo(() => {
    if (!stockItem.lotId) return null;
    return (lotsQuery.data?.data ?? []).find((l) => l.id === stockItem.lotId) ?? null;
  }, [lotsQuery.data, stockItem.lotId]);

  const belowReorder =
    stockItem.reorderPoint !== null && stockItem.availableToPromise <= stockItem.reorderPoint;
  const reorderQty =
    stockItem.reorderPoint !== null
      ? Math.max(
          0,
          stockItem.reorderPoint - stockItem.availableToPromise + (stockItem.minStock ?? 0),
        )
      : 0;

  const value = stockItem.onHand * stockItem.avgCost;

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-40 flex items-end justify-center bg-slate-900/50 p-2 backdrop-blur sm:items-center sm:p-4">
      <div className="relative flex max-h-[92vh] w-full max-w-2xl flex-col overflow-hidden rounded-lg border border-slate-200 bg-white shadow-2xl dark:border-slate-800 dark:bg-slate-950">
        <header className="flex items-start justify-between gap-3 border-b border-slate-200 bg-slate-50/60 px-4 py-3 dark:border-slate-800 dark:bg-slate-900/40">
          <div className="min-w-0">
            <div className="flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-slate-100">
              <Boxes size={14} className="text-indigo-500" />
              {t('inventory.stockItem.title')}
            </div>
            <div className="mt-0.5 truncate text-[11px] text-slate-500 dark:text-slate-400">
              {stockItem.productName} · {stockItem.productSku}
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800"
          >
            <X size={14} />
          </button>
        </header>

        <div className="flex-1 space-y-3 overflow-y-auto p-3">
          <HeaderChips stockItem={stockItem} />

          <KpiRow stockItem={stockItem} value={value} currency={currency} locale={locale} />

          <ReorderCard
            stockItem={stockItem}
            belowReorder={belowReorder}
            reorderQty={reorderQty}
            locale={locale}
          />

          {lot && <LotInfoCard lot={lot} locale={locale} />}

          <MovementChart movements={movements} locale={locale} />

          <MovementsList
            movements={movements}
            loading={movementsQuery.isPending}
            locale={locale}
            currency={currency}
          />
        </div>

        <footer className="flex items-center justify-end gap-2 border-t border-slate-200 bg-slate-50/40 px-4 py-2.5 dark:border-slate-800 dark:bg-slate-900/40">
          <button
            type="button"
            onClick={onClose}
            className="rounded-md border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            {t('common.cancel')}
          </button>
          {onAdjust && (
            <button
              type="button"
              onClick={() => onAdjust(stockItem)}
              className="inline-flex items-center gap-1.5 rounded-md border border-indigo-300 bg-indigo-50 px-3 py-1.5 text-xs font-medium text-indigo-700 hover:bg-indigo-100 dark:border-indigo-500/40 dark:bg-indigo-500/10 dark:text-indigo-300 dark:hover:bg-indigo-500/20"
            >
              <Edit3 size={12} />
              {t('inventory.byWarehouse.adjust')}
            </button>
          )}
        </footer>
      </div>
    </div>
  );
};

const HeaderChips = ({ stockItem }: { stockItem: StockItem }) => {
  const { t } = useTranslation();
  return (
    <div className="flex flex-wrap items-center gap-1.5">
      <Chip icon={<Warehouse size={11} />} label={t('inventory.fields.warehouse')}>
        {stockItem.warehouseName}
        <span className="ml-1 text-slate-400">({stockItem.warehouseCode})</span>
      </Chip>
      {stockItem.lotNumber && (
        <Chip icon={<Tag size={11} />} label={t('inventory.fields.lot')}>
          <span className="font-mono">{stockItem.lotNumber}</span>
        </Chip>
      )}
      {stockItem.binLocation && (
        <Chip icon={<MapPin size={11} />} label={t('inventory.fields.bin')}>
          {stockItem.binLocation}
        </Chip>
      )}
    </div>
  );
};

const Chip = ({
  icon,
  label,
  children,
}: {
  icon: React.ReactNode;
  label: string;
  children: React.ReactNode;
}) => (
  <span className="inline-flex items-center gap-1 rounded-full border border-slate-200 bg-slate-50 px-2 py-0.5 text-[10px] text-slate-700 dark:border-slate-800 dark:bg-slate-800/60 dark:text-slate-200">
    {icon}
    <span className="font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </span>
    <span className="font-medium">{children}</span>
  </span>
);

const KpiRow = ({
  stockItem,
  value,
  currency,
  locale,
}: {
  stockItem: StockItem;
  value: number;
  currency: string;
  locale: string;
}) => {
  const { t } = useTranslation();
  return (
    <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
      <Kpi
        icon={<Package size={11} />}
        label={t('inventory.fields.onHand')}
        value={fmtNumber(stockItem.onHand, locale)}
        tone="indigo"
      />
      <Kpi
        icon={<RefreshCw size={11} />}
        label={t('inventory.fields.reserved')}
        value={fmtNumber(stockItem.reserved, locale)}
        tone="amber"
      />
      <Kpi
        icon={<TrendingDown size={11} />}
        label={t('inventory.fields.available')}
        value={fmtNumber(stockItem.availableToPromise, locale)}
        tone={
          stockItem.reorderPoint !== null && stockItem.availableToPromise <= stockItem.reorderPoint
            ? 'red'
            : 'emerald'
        }
      />
      <Kpi
        icon={<Layers size={11} />}
        label={t('inventory.fields.value')}
        value={fmtCurrency(value, currency, locale)}
        sub={`@${fmtCurrency(stockItem.avgCost, currency, locale)}`}
        tone="slate"
      />
    </div>
  );
};

const kpiTones: Record<'slate' | 'indigo' | 'amber' | 'emerald' | 'red', string> = {
  slate: 'border-slate-200 dark:border-slate-800',
  indigo: 'border-indigo-200 dark:border-indigo-500/30',
  amber: 'border-amber-200 dark:border-amber-500/30',
  emerald: 'border-emerald-200 dark:border-emerald-500/30',
  red: 'border-red-200 dark:border-red-500/30',
};

const Kpi = ({
  icon,
  label,
  value,
  sub,
  tone,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  sub?: string;
  tone: keyof typeof kpiTones;
}) => (
  <div className={`rounded-lg border bg-white p-2 dark:bg-slate-900 ${kpiTones[tone]}`}>
    <div className="flex items-center gap-1 text-[9px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {icon}
      <span>{label}</span>
    </div>
    <div className="mt-0.5 text-sm font-bold tabular-nums text-slate-900 dark:text-slate-100">
      {value}
    </div>
    {sub && <div className="text-[9px] text-slate-500 dark:text-slate-400">{sub}</div>}
  </div>
);

const ReorderCard = ({
  stockItem,
  belowReorder,
  reorderQty,
  locale,
}: {
  stockItem: StockItem;
  belowReorder: boolean;
  reorderQty: number;
  locale: string;
}) => {
  const { t } = useTranslation();
  if (stockItem.reorderPoint === null) {
    return (
      <section className="rounded-lg border border-dashed border-slate-200 bg-white p-2.5 text-[11px] text-slate-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <Hash size={11} />
          {t('inventory.stockItem.noReorderPoint')}
        </span>
      </section>
    );
  }
  return (
    <section
      className={`rounded-lg border bg-white p-2.5 dark:bg-slate-900 ${belowReorder ? 'border-red-200 dark:border-red-500/30' : 'border-slate-200 dark:border-slate-800'}`}
    >
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <CalendarClock size={12} />
          {t('inventory.stockItem.reorderTitle')}
        </span>
        {belowReorder && (
          <Badge variant="error" pill>
            <AlertTriangle size={9} className="mr-1" />
            {t('inventory.summary.belowReorder')}
          </Badge>
        )}
      </header>
      <dl className="mt-2 grid grid-cols-3 gap-2 text-[11px]">
        <Cell label={t('inventory.stockItem.reorderPoint')}>
          {fmtNumber(stockItem.reorderPoint, locale)}
        </Cell>
        <Cell label={t('inventory.stockItem.minStock')}>
          {stockItem.minStock !== null ? fmtNumber(stockItem.minStock, locale) : '—'}
        </Cell>
        <Cell label={t('inventory.stockItem.suggestedQty')}>
          <span
            className={`font-semibold ${reorderQty > 0 ? 'text-amber-700 dark:text-amber-400' : 'text-slate-600 dark:text-slate-300'}`}
          >
            {fmtNumber(reorderQty, locale)}
          </span>
        </Cell>
      </dl>
    </section>
  );
};

const Cell = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <div className="flex flex-col">
    <dt className="text-[9px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </dt>
    <dd className="font-mono tabular-nums text-slate-900 dark:text-slate-100">{children}</dd>
  </div>
);

const LotInfoCard = ({
  lot,
  locale,
}: {
  lot: NonNullable<NonNullable<ReturnType<typeof useLotsByProductQuery>['data']>['data']>[number];
  locale: string;
}) => {
  const { t } = useTranslation();
  const expiringSoon = lot.daysUntilExpiry !== null && lot.daysUntilExpiry <= 30;
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-2.5 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <Tag size={12} />
          {t('inventory.stockItem.lotInfo')}
        </span>
        {lot.isBlocked && (
          <Badge variant="error" pill>
            {t('inventory.stockItem.lotBlocked')}
          </Badge>
        )}
        {!lot.isBlocked && lot.isExpired && (
          <Badge variant="error" pill>
            {t('inventory.stockItem.lotExpired')}
          </Badge>
        )}
        {!lot.isBlocked && !lot.isExpired && expiringSoon && (
          <Badge variant="warning" pill>
            <Calendar size={9} className="mr-1" />
            {t('inventory.stockItem.expiringInDays', { count: lot.daysUntilExpiry ?? 0 })}
          </Badge>
        )}
      </header>
      <dl className="mt-2 grid grid-cols-2 gap-2 text-[11px] sm:grid-cols-4">
        <Cell label={t('inventory.stockItem.lotNumber')}>{lot.lotNumber}</Cell>
        <Cell label={t('inventory.stockItem.manufactureDate')}>
          {fmtDate(lot.manufactureDate, locale)}
        </Cell>
        <Cell label={t('inventory.stockItem.expiryDate')}>{fmtDate(lot.expiryDate, locale)}</Cell>
        <Cell label={t('inventory.stockItem.daysUntilExpiry')}>
          {lot.daysUntilExpiry !== null ? lot.daysUntilExpiry : '—'}
        </Cell>
      </dl>
    </section>
  );
};

const MovementChart = ({ movements, locale }: { movements: StockMovement[]; locale: string }) => {
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
          <span className="inline-flex items-center gap-1 text-emerald-600 dark:text-emerald-400">
            <ArrowDownToLine size={10} /> {fmtInt(totalIn, locale)}
          </span>
          <span className="inline-flex items-center gap-1 text-red-600 dark:text-red-400">
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
                  className="rounded-t-sm bg-emerald-500/80"
                  style={{ height: `${inH}%`, minHeight: 1 }}
                />
              )}
              {b.out > 0 && (
                <div
                  className="rounded-b-sm bg-red-500/80"
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

const MovementsList = ({
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
                    className={`inline-flex h-5 w-5 shrink-0 items-center justify-center rounded ${inbound ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300' : 'bg-red-100 text-red-700 dark:bg-red-500/20 dark:text-red-300'}`}
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
                    className={`font-semibold tabular-nums ${inbound ? 'text-emerald-600 dark:text-emerald-400' : 'text-red-600 dark:text-red-400'}`}
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
