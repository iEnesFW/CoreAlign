import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, Warehouse as WarehouseIcon } from 'lucide-react';
import { useWarehousesQuery } from '@/shared/master-data/hooks/useMasterData';
import { useStockItemsQuery } from '../hooks/useInventoryQueries';

const fmtNumber = (n: number, locale: string) =>
  new Intl.NumberFormat(locale, { minimumFractionDigits: 0, maximumFractionDigits: 4 }).format(n);

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

export const StockStatusLedger = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const [warehouseId, setWarehouseId] = useState('');
  const [onlyBelowReorder, setOnlyBelowReorder] = useState(false);
  const [page, setPage] = useState(1);

  const warehousesQuery = useWarehousesQuery(true);
  const warehouses = warehousesQuery.data?.data ?? [];

  const stockQuery = useStockItemsQuery({
    warehouseId: warehouseId === '' ? undefined : warehouseId,
    onlyBelowReorder: onlyBelowReorder || undefined,
    page,
    pageSize: 25,
  });

  const items = stockQuery.data?.data?.items ?? [];
  const total = stockQuery.data?.data?.total ?? 0;
  const totalPages = stockQuery.data?.data?.totalPages ?? 0;

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
          className="rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
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
        <label className="inline-flex items-center gap-1.5 text-xs text-slate-600 dark:text-slate-400">
          <input
            type="checkbox"
            checked={onlyBelowReorder}
            onChange={(e) => {
              setOnlyBelowReorder(e.target.checked);
              setPage(1);
            }}
            className="rounded border-slate-300 text-primary-600 focus:ring-primary-500 dark:border-slate-600 dark:bg-slate-800"
          />
          {t('inventory.status.belowReorderOnly', { defaultValue: 'Sadece kritik seviye' })}
        </label>
        <span className="ml-auto text-[11px] text-slate-500 dark:text-slate-400">
          {t('inventory.status.totalCount', { defaultValue: '{{count}} kayıt', count: total })}
        </span>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
        {stockQuery.isPending && items.length === 0 ? (
          <div className="px-3 py-6 text-center text-sm text-slate-500">
            {t('common.loading', { defaultValue: 'Yükleniyor…' })}
          </div>
        ) : items.length === 0 ? (
          <div className="px-3 py-6 text-center text-sm text-slate-500 dark:text-slate-400">
            {t('inventory.status.empty', { defaultValue: 'Stok kaydı bulunamadı.' })}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('inventory.fields.product', { defaultValue: 'Ürün' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('inventory.fields.warehouse', { defaultValue: 'Depo' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('inventory.fields.onHand', { defaultValue: 'Eldeki' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('inventory.fields.reserved', { defaultValue: 'Rezerve' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('inventory.fields.available', { defaultValue: 'Kullanılabilir' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('inventory.fields.avgCost', { defaultValue: 'Ort. Maliyet' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('inventory.fields.value', { defaultValue: 'Değer' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('inventory.fields.lastMovement', { defaultValue: 'Son Hareket' })}
                </th>
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
                        {it.productName}
                      </div>
                      <div className="font-mono text-[10px] text-slate-400 dark:text-slate-500">
                        {it.productSku}
                      </div>
                    </td>
                    <td className="px-3 py-2 text-slate-700 dark:text-slate-300">
                      <div>{it.warehouseName}</div>
                      <div className="text-[10px] text-slate-500">{it.warehouseCode}</div>
                      {it.lotNumber && (
                        <div className="mt-0.5 text-[10px] text-violet-600 dark:text-violet-400">
                          {t('inventory.fields.lot', { defaultValue: 'Lot' })}: {it.lotNumber}
                        </div>
                      )}
                    </td>
                    <td className="px-3 py-2 text-right font-mono text-slate-800 dark:text-slate-200">
                      {fmtNumber(it.onHand, locale)}
                    </td>
                    <td className="px-3 py-2 text-right font-mono text-warning-700 dark:text-warning-300">
                      {fmtNumber(it.reserved, locale)}
                    </td>
                    <td
                      className={`px-3 py-2 text-right font-mono font-semibold ${
                        belowReorder
                          ? 'text-danger-600 dark:text-danger-400'
                          : 'text-success-700 dark:text-success-300'
                      }`}
                    >
                      <span className="inline-flex items-center justify-end gap-1">
                        {belowReorder && <AlertTriangle size={11} />}
                        {fmtNumber(it.availableToPromise, locale)}
                      </span>
                    </td>
                    <td className="px-3 py-2 text-right font-mono text-slate-700 dark:text-slate-300">
                      {fmtCurrency(it.avgCost, it.currency, locale)}
                    </td>
                    <td className="px-3 py-2 text-right font-mono text-slate-700 dark:text-slate-300">
                      {fmtCurrency(it.onHand * it.avgCost, it.currency, locale)}
                    </td>
                    <td className="px-3 py-2 text-[11px] text-slate-500 dark:text-slate-400">
                      {fmtDateTime(it.lastMovementAtUtc, locale)}
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
