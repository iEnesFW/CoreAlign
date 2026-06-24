import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, Calendar, CalendarClock, Hash, Tag } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge/Badge';
import type { StockItem } from '@/features/inventory/model/inventory.types';
import type { useLotsByProductQuery } from '@/features/inventory/hooks/useInventoryQueries';
import { fmtDate, fmtNumber } from './format';

export const ReorderCard = ({
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
      className={`rounded-lg border bg-white p-2.5 dark:bg-slate-900 ${belowReorder ? 'border-danger-200 dark:border-danger-500/30' : 'border-slate-200 dark:border-slate-800'}`}
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
            className={`font-semibold ${reorderQty > 0 ? 'text-warning-700 dark:text-warning-400' : 'text-slate-600 dark:text-slate-300'}`}
          >
            {fmtNumber(reorderQty, locale)}
          </span>
        </Cell>
      </dl>
    </section>
  );
};

const Cell = ({ label, children }: { label: string; children: ReactNode }) => (
  <div className="flex flex-col">
    <dt className="text-[9px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </dt>
    <dd className="font-mono tabular-nums text-slate-900 dark:text-slate-100">{children}</dd>
  </div>
);

export const LotInfoCard = ({
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
