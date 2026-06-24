import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Layers, MapPin, Package, RefreshCw, Tag, TrendingDown, Warehouse } from 'lucide-react';
import type { StockItem } from '@/features/inventory/model/inventory.types';
import { fmtCurrency, fmtNumber } from './format';

export const HeaderChips = ({ stockItem }: { stockItem: StockItem }) => {
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
  icon: ReactNode;
  label: string;
  children: ReactNode;
}) => (
  <span className="inline-flex items-center gap-1 rounded-full border border-slate-200 bg-slate-50 px-2 py-0.5 text-[10px] text-slate-700 dark:border-slate-800 dark:bg-slate-800/60 dark:text-slate-200">
    {icon}
    <span className="font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </span>
    <span className="font-medium">{children}</span>
  </span>
);

export const KpiRow = ({
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
  indigo: 'border-primary-200 dark:border-primary-500/30',
  amber: 'border-warning-200 dark:border-warning-500/30',
  emerald: 'border-success-200 dark:border-success-500/30',
  red: 'border-danger-200 dark:border-danger-500/30',
};

const Kpi = ({
  icon,
  label,
  value,
  sub,
  tone,
}: {
  icon: ReactNode;
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
