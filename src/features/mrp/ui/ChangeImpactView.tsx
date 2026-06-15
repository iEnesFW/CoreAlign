import { useTranslation } from 'react-i18next';
import { formatDate, formatNumber } from '@/shared/lib/format';
import { sinkKindToProcurementType, type ChangeImpactResult } from '../model/mrp-planning.types';
import { ProcurementBadge } from './ProcurementBadge';

export interface ChangeImpactProductInfo {
  sku: string;
  name: string;
}

interface Props {
  result: ChangeImpactResult | null;
  locale: string;
  sourceLabel?: string;
  productInfo?: Record<string, ChangeImpactProductInfo>;
  isLoading?: boolean;
}

const emptyState = (text: string) => (
  <div className="rounded-lg border border-dashed border-slate-300 bg-white p-6 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-900">
    {text}
  </div>
);

export const ChangeImpactView = ({
  result,
  locale,
  sourceLabel,
  productInfo = {},
  isLoading = false,
}: Props) => {
  const { t } = useTranslation();

  if (isLoading) {
    return <p className="text-sm text-slate-500 dark:text-slate-400">{t('Common.Loading')}</p>;
  }

  if (!result) {
    return emptyState(t('Mrp.Workbench.ChangeImpact.Prompt'));
  }

  const supply = result.downstreamSupply;

  if (supply.length === 0) {
    return emptyState(t('Mrp.Workbench.ChangeImpact.Empty'));
  }

  return (
    <div className="space-y-3" data-testid="change-impact-view">
      <div className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800 dark:border-amber-700 dark:bg-amber-500/10 dark:text-amber-200">
        {t('Mrp.Workbench.ChangeImpact.Summary', {
          source: sourceLabel ?? result.sourceOrderLineId,
          count: supply.length,
        })}
      </div>
      <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-900">
        <table className="min-w-full text-xs">
          <thead className="bg-slate-50 text-left text-slate-500 dark:bg-slate-800/60 dark:text-slate-400">
            <tr>
              <th scope="col" className="px-3 py-2">
                {t('Mrp.Workbench.ChangeImpact.Type')}
              </th>
              <th scope="col" className="px-3 py-2">
                {t('Mrp.Workbench.ChangeImpact.Product')}
              </th>
              <th scope="col" className="px-3 py-2">
                {t('Mrp.Workbench.ChangeImpact.Level')}
              </th>
              <th scope="col" className="px-3 py-2 text-right">
                {t('Mrp.Workbench.ChangeImpact.Quantity')}
              </th>
              <th scope="col" className="px-3 py-2">
                {t('Mrp.Workbench.ChangeImpact.Due')}
              </th>
            </tr>
          </thead>
          <tbody>
            {supply.map((node, idx) => {
              const procurementType = sinkKindToProcurementType(node.sinkKind);
              const info = productInfo[node.productId];
              return (
                <tr
                  key={`${node.productId}-${node.sinkKind}-${idx}`}
                  data-testid="change-impact-row"
                  data-procurement-type={procurementType}
                  className="border-t border-slate-100 dark:border-slate-800"
                >
                  <td className="px-3 py-2">
                    <ProcurementBadge type={procurementType} />
                  </td>
                  <td className="px-3 py-2">
                    <div
                      className="font-medium text-slate-800 dark:text-slate-100"
                      style={{ paddingLeft: `${node.lowLevelCode * 12}px` }}
                    >
                      {info?.sku ?? node.productId}
                    </div>
                    {info?.name && (
                      <div className="text-[11px] text-slate-500 dark:text-slate-400">
                        {info.name}
                      </div>
                    )}
                  </td>
                  <td className="px-3 py-2 tabular-nums text-slate-600 dark:text-slate-300">
                    {node.lowLevelCode}
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums text-slate-700 dark:text-slate-200">
                    {formatNumber(node.quantity, locale)}
                  </td>
                  <td className="px-3 py-2 text-slate-600 dark:text-slate-300">
                    {formatDate(node.dueDateUtc, locale)}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
};
