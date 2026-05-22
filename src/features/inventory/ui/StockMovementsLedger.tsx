import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowDown, ArrowUp, Filter, Hash, Warehouse as WarehouseIcon } from 'lucide-react';
import { useWarehousesQuery } from '@/features/master-data/hooks/useMasterData';
import { useStockMovementsQuery } from '../hooks/useInventoryQueries';
import type { StockMovementType } from '../model/inventory.types';

const TYPE_TONE: Record<
  StockMovementType,
  { tone: string; sign: 'positive' | 'negative' | 'neutral' }
> = {
  OpeningBalance: {
    tone: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
    sign: 'positive',
  },
  Receipt: {
    tone: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
    sign: 'positive',
  },
  Issue: {
    tone: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
    sign: 'negative',
  },
  TransferIn: {
    tone: 'bg-blue-100 text-blue-700 dark:bg-blue-500/20 dark:text-blue-300',
    sign: 'positive',
  },
  TransferOut: {
    tone: 'bg-blue-100 text-blue-700 dark:bg-blue-500/20 dark:text-blue-300',
    sign: 'negative',
  },
  AdjustmentPositive: {
    tone: 'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300',
    sign: 'positive',
  },
  AdjustmentNegative: {
    tone: 'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300',
    sign: 'negative',
  },
  CountVariancePositive: {
    tone: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300',
    sign: 'positive',
  },
  CountVarianceNegative: {
    tone: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300',
    sign: 'negative',
  },
  Reservation: {
    tone: 'bg-amber-100 text-amber-700 dark:bg-amber-500/20 dark:text-amber-300',
    sign: 'neutral',
  },
  UnReservation: {
    tone: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
    sign: 'neutral',
  },
};

const fmtNumber = (n: number, locale: string) =>
  new Intl.NumberFormat(locale, { minimumFractionDigits: 0, maximumFractionDigits: 4 }).format(n);

const fmtDateTime = (iso: string, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(
      new Date(iso),
    );
  } catch {
    return iso;
  }
};

export const StockMovementsLedger = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const [warehouseId, setWarehouseId] = useState('');
  const [type, setType] = useState<StockMovementType | ''>('');
  const [page, setPage] = useState(1);

  const warehousesQuery = useWarehousesQuery(true);
  const warehouses = warehousesQuery.data?.data ?? [];

  const movementsQuery = useStockMovementsQuery({
    warehouseId: warehouseId === '' ? undefined : warehouseId,
    type: type === '' ? undefined : (type as StockMovementType),
    page,
    pageSize: 25,
  });

  const items = movementsQuery.data?.data?.items ?? [];
  const total = movementsQuery.data?.data?.total ?? 0;
  const totalPages = movementsQuery.data?.data?.totalPages ?? 0;

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-2">
        <div className="inline-flex items-center gap-1 text-xs text-slate-600 dark:text-slate-400">
          <WarehouseIcon size={12} />
          {t('inventory.status.warehouse', { defaultValue: 'Depo' })}
        </div>
        <select
          value={warehouseId}
          onChange={(e) => {
            setWarehouseId(e.target.value);
            setPage(1);
          }}
          className="rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-indigo-500 focus:outline-none dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        >
          <option value="">
            {t('inventory.status.allWarehouses', { defaultValue: 'Tüm depolar' })}
          </option>
          {warehouses.map((w) => (
            <option key={w.id} value={w.id}>
              {w.name} ({w.code})
            </option>
          ))}
        </select>
        <div className="inline-flex items-center gap-1 text-xs text-slate-600 dark:text-slate-400">
          <Filter size={12} />
          {t('inventory.movements.filterByType', { defaultValue: 'Hareket tipi' })}
        </div>
        <select
          value={type}
          onChange={(e) => {
            setType(e.target.value as StockMovementType | '');
            setPage(1);
          }}
          className="rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-indigo-500 focus:outline-none dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        >
          <option value="">
            {t('inventory.movements.allTypes', { defaultValue: 'Tüm tipler' })}
          </option>
          {Object.keys(TYPE_TONE).map((k) => (
            <option key={k} value={k}>
              {t(`inventory.movements.type.${k}`, { defaultValue: k })}
            </option>
          ))}
        </select>
        <span className="ml-auto text-[11px] text-slate-500 dark:text-slate-400">
          {t('inventory.movements.totalCount', { defaultValue: '{{count}} hareket', count: total })}
        </span>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
        {movementsQuery.isPending && items.length === 0 ? (
          <div className="px-3 py-6 text-center text-sm text-slate-500">
            {t('common.loading', { defaultValue: 'Yükleniyor…' })}
          </div>
        ) : items.length === 0 ? (
          <div className="px-3 py-6 text-center text-sm text-slate-500">
            {t('inventory.movements.empty', { defaultValue: 'Hareket bulunamadı.' })}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('inventory.movements.when', { defaultValue: 'Tarih' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('inventory.fields.product', { defaultValue: 'Ürün' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('inventory.movements.type', { defaultValue: 'Tip' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('inventory.movements.warehouse', { defaultValue: 'Depo' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('inventory.movements.quantity', { defaultValue: 'Miktar' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('inventory.movements.onHandAfter', { defaultValue: 'Sonraki Eldeki' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('inventory.movements.source', { defaultValue: 'Kaynak' })}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {items.map((m) => {
                const meta = TYPE_TONE[m.type];
                const ArrowIcon =
                  meta.sign === 'positive' ? ArrowUp : meta.sign === 'negative' ? ArrowDown : Hash;
                return (
                  <tr key={m.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                    <td className="px-3 py-2 text-[11px] text-slate-500 dark:text-slate-400">
                      {fmtDateTime(m.occurredAtUtc, locale)}
                    </td>
                    <td className="px-3 py-2">
                      <div className="font-medium text-slate-800 dark:text-slate-100">
                        {m.productName}
                      </div>
                      <div className="font-mono text-[10px] text-slate-400 dark:text-slate-500">
                        {m.productSku}
                      </div>
                    </td>
                    <td className="px-3 py-2">
                      <span
                        className={`inline-flex items-center gap-1 rounded px-1.5 py-0.5 text-[10px] font-semibold ${meta.tone}`}
                      >
                        <ArrowIcon size={10} />
                        {t(`inventory.movements.type.${m.type}`, { defaultValue: m.type })}
                      </span>
                    </td>
                    <td className="px-3 py-2 text-slate-700 dark:text-slate-300">
                      <div>{m.warehouseName}</div>
                      <div className="text-[10px] text-slate-500">{m.warehouseCode}</div>
                    </td>
                    <td
                      className={`px-3 py-2 text-right font-mono ${
                        meta.sign === 'positive'
                          ? 'text-emerald-700 dark:text-emerald-300'
                          : meta.sign === 'negative'
                            ? 'text-amber-700 dark:text-amber-300'
                            : 'text-slate-700 dark:text-slate-300'
                      }`}
                    >
                      {meta.sign === 'positive' ? '+' : meta.sign === 'negative' ? '−' : ''}
                      {fmtNumber(m.quantity, locale)}
                    </td>
                    <td className="px-3 py-2 text-right font-mono text-slate-800 dark:text-slate-200">
                      {fmtNumber(m.onHandAfter, locale)}
                    </td>
                    <td className="px-3 py-2 text-[11px] text-slate-600 dark:text-slate-400">
                      <div>
                        {t(`inventory.movements.source_${m.sourceDocumentType}`, {
                          defaultValue: m.sourceDocumentType,
                        })}
                      </div>
                      {m.sourceReference && (
                        <div className="font-mono text-[10px] text-slate-500">
                          {m.sourceReference}
                        </div>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-between text-xs">
          <span className="text-slate-500 dark:text-slate-400">
            {t('common.pagination', {
              defaultValue: 'Sayfa {{page}} / {{totalPages}}',
              page,
              totalPages,
            })}
          </span>
          <div className="flex gap-1">
            <button
              type="button"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
              className="rounded border border-slate-200 bg-white px-2 py-1 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900"
            >
              {t('common.prev', { defaultValue: 'Önceki' })}
            </button>
            <button
              type="button"
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="rounded border border-slate-200 bg-white px-2 py-1 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900"
            >
              {t('common.next', { defaultValue: 'Sonraki' })}
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
