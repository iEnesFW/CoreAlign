import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowRight, MoveRight, PackageX, Send } from 'lucide-react';
import { formatNumber } from '@/shared/lib/format';
import { transferSuggestionKey } from '../model/mrp-planning.types';
import type {
  MrpExternalReplenishment,
  MrpTransferSuggestion,
  MrpTransferSuggestionsResult,
  MrpWarehouseNetPosition,
} from '../model/mrp-planning.types';

export type TransferExecuteHandler = (suggestion: MrpTransferSuggestion) => void;

interface Props {
  result: MrpTransferSuggestionsResult | null;
  locale: string;
  isLoading?: boolean;
  onExecute?: TransferExecuteHandler;
  executingKey?: string | null;
  isExecuting?: boolean;
}

interface ProductGroup {
  productId: string;
  productSku: string;
  productName: string;
  transfers: MrpTransferSuggestion[];
}

const emptyState = (text: string) => (
  <div
    data-testid="transfer-suggestions-empty"
    className="rounded-lg border border-dashed border-slate-300 bg-white p-6 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-900"
  >
    {text}
  </div>
);

const groupByProduct = (transfers: MrpTransferSuggestion[]): ProductGroup[] => {
  const groups = new Map<string, ProductGroup>();
  for (const transfer of transfers) {
    const group = groups.get(transfer.productId);
    if (group) {
      group.transfers.push(transfer);
    } else {
      groups.set(transfer.productId, {
        productId: transfer.productId,
        productSku: transfer.productSku,
        productName: transfer.productName,
        transfers: [transfer],
      });
    }
  }
  return Array.from(groups.values());
};

export const TransferSuggestionsView = ({
  result,
  locale,
  isLoading = false,
  onExecute,
  executingKey = null,
  isExecuting = false,
}: Props) => {
  const { t } = useTranslation();

  const groups = useMemo(() => groupByProduct(result?.transfers ?? []), [result]);

  if (isLoading) {
    return <p className="text-sm text-slate-500 dark:text-slate-400">{t('Common.Loading')}</p>;
  }

  if (!result) {
    return emptyState(t('Mrp.Workbench.Distribution.NoSuggestions'));
  }

  return (
    <div className="space-y-4" data-testid="transfer-suggestions-view">
      <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-600 dark:border-slate-700 dark:bg-slate-800/60 dark:text-slate-300">
        {t('Mrp.Workbench.Distribution.Summary', {
          products: result.productsEvaluated,
          transfers: result.transferCount,
        })}
      </div>

      {groups.length === 0 ? (
        emptyState(t('Mrp.Workbench.Distribution.NoSuggestions'))
      ) : (
        <div className="space-y-3">
          {groups.map((group) => (
            <TransferProductCard
              key={group.productId}
              group={group}
              locale={locale}
              onExecute={onExecute}
              executingKey={executingKey}
              isExecuting={isExecuting}
            />
          ))}
        </div>
      )}

      {result.externalReplenishment.length > 0 && (
        <ExternalReplenishmentList items={result.externalReplenishment} locale={locale} />
      )}

      {result.netPositions.length > 0 && (
        <NetPositionsTable positions={result.netPositions} locale={locale} />
      )}
    </div>
  );
};

const TransferProductCard = ({
  group,
  locale,
  onExecute,
  executingKey,
  isExecuting,
}: {
  group: ProductGroup;
  locale: string;
  onExecute?: TransferExecuteHandler;
  executingKey?: string | null;
  isExecuting?: boolean;
}) => {
  const { t } = useTranslation();
  return (
    <div
      data-testid="transfer-product-group"
      data-product-id={group.productId}
      className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900"
    >
      <div className="mb-2 flex items-baseline justify-between gap-2">
        <span className="font-mono text-sm font-semibold text-slate-800 dark:text-slate-100">
          {group.productSku}
        </span>
        <span className="truncate text-xs text-slate-500 dark:text-slate-400">
          {group.productName}
        </span>
      </div>
      <ul className="space-y-1.5">
        {group.transfers.map((transfer, idx) => (
          <li
            key={`${transfer.fromWarehouseId}-${transfer.toWarehouseId}-${idx}`}
            data-testid="transfer-suggestion-row"
            className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-200"
          >
            <MoveRight className="h-4 w-4 shrink-0 text-primary-500 dark:text-primary-300" />
            <span className="inline-flex items-center gap-1.5">
              <span className="rounded bg-success-100 px-2 py-0.5 text-xs font-semibold text-success-700 dark:bg-success-500/20 dark:text-success-300">
                {transfer.fromWarehouseCode}
              </span>
              <ArrowRight className="h-3.5 w-3.5 text-slate-400" />
              <span className="rounded bg-info-100 px-2 py-0.5 text-xs font-semibold text-info-700 dark:bg-info-500/20 dark:text-info-300">
                {transfer.toWarehouseCode}
              </span>
            </span>
            <span className="font-semibold tabular-nums text-slate-800 dark:text-slate-100">
              {formatNumber(transfer.quantity, locale)}
            </span>
            <span className="font-mono text-xs text-slate-500 dark:text-slate-400">
              {group.productSku}
            </span>
            <span className="sr-only">
              {t('Mrp.Workbench.Distribution.TransferAria', {
                from: transfer.fromWarehouseName,
                to: transfer.toWarehouseName,
                quantity: transfer.quantity,
                sku: group.productSku,
              })}
            </span>
            {onExecute && (
              <button
                type="button"
                data-testid="transfer-execute-button"
                disabled={isExecuting}
                onClick={() => onExecute(transfer)}
                className="ml-auto inline-flex shrink-0 items-center gap-1 rounded-md border border-primary-200 bg-primary-50 px-2.5 py-1 text-xs font-semibold text-primary-700 hover:bg-primary-100 disabled:cursor-not-allowed disabled:opacity-50 dark:border-primary-500/30 dark:bg-primary-500/10 dark:text-primary-300 dark:hover:bg-primary-500/20"
              >
                <Send className="h-3.5 w-3.5" />
                {executingKey === transferSuggestionKey(transfer)
                  ? t('Mrp.Workbench.Distribution.Executing')
                  : t('Mrp.Workbench.Distribution.Execute')}
              </button>
            )}
          </li>
        ))}
      </ul>
    </div>
  );
};

const ExternalReplenishmentList = ({
  items,
  locale,
}: {
  items: MrpExternalReplenishment[];
  locale: string;
}) => {
  const { t } = useTranslation();
  return (
    <div
      data-testid="external-replenishment"
      className="rounded-lg border border-warning-200 bg-warning-50 p-4 dark:border-warning-700 dark:bg-warning-500/10"
    >
      <div className="mb-2 flex items-center gap-2 text-sm font-semibold text-warning-800 dark:text-warning-200">
        <PackageX className="h-4 w-4" />
        {t('Mrp.Workbench.Distribution.ExternalReplenishment')}
      </div>
      <ul className="space-y-1 text-xs text-warning-800 dark:text-warning-200">
        {items.map((item, idx) => (
          <li key={`${item.productId}-${item.warehouseId}-${idx}`}>
            {t('Mrp.Workbench.Distribution.ExternalRow', {
              sku: item.productSku,
              warehouse: item.warehouseCode,
              quantity: formatNumber(item.quantity, locale),
            })}
          </li>
        ))}
      </ul>
    </div>
  );
};

const NetPositionsTable = ({
  positions,
  locale,
}: {
  positions: MrpWarehouseNetPosition[];
  locale: string;
}) => {
  const { t } = useTranslation();
  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-900">
      <table className="min-w-full text-xs">
        <thead className="bg-slate-50 text-left text-slate-500 dark:bg-slate-800/60 dark:text-slate-400">
          <tr>
            <th scope="col" className="px-3 py-2">
              {t('Mrp.Workbench.Distribution.Product')}
            </th>
            <th scope="col" className="px-3 py-2">
              {t('Mrp.Workbench.Distribution.Warehouse')}
            </th>
            <th scope="col" className="px-3 py-2 text-right">
              {t('Mrp.Workbench.Distribution.Available')}
            </th>
            <th scope="col" className="px-3 py-2 text-right">
              {t('Mrp.Workbench.Distribution.Demand')}
            </th>
            <th scope="col" className="px-3 py-2 text-right">
              {t('Mrp.Workbench.Distribution.Net')}
            </th>
          </tr>
        </thead>
        <tbody>
          {positions.map((pos, idx) => (
            <tr
              key={`${pos.productId}-${pos.warehouseId}-${idx}`}
              data-testid="net-position-row"
              className="border-t border-slate-100 dark:border-slate-800"
            >
              <td className="px-3 py-2">
                <span className="font-mono font-medium text-slate-800 dark:text-slate-100">
                  {pos.productSku}
                </span>
              </td>
              <td className="px-3 py-2 text-slate-600 dark:text-slate-300">{pos.warehouseCode}</td>
              <td className="px-3 py-2 text-right tabular-nums text-slate-700 dark:text-slate-200">
                {formatNumber(pos.available, locale)}
              </td>
              <td className="px-3 py-2 text-right tabular-nums text-slate-700 dark:text-slate-200">
                {formatNumber(pos.demand, locale)}
              </td>
              <td
                className={`px-3 py-2 text-right font-semibold tabular-nums ${
                  pos.net < 0
                    ? 'text-danger-600 dark:text-danger-300'
                    : 'text-success-600 dark:text-success-300'
                }`}
              >
                {formatNumber(pos.net, locale)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};
