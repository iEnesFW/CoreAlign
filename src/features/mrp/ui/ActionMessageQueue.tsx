import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Eye, BellOff, Send } from 'lucide-react';
import { formatNumber } from '@/shared/lib/format';
import type {
  MrpActionMessage,
  MrpActionSeverity,
  MrpActionType,
} from '../model/mrp-planning.types';

interface Props {
  messages: MrpActionMessage[];
  locale: string;
  isReleasing?: boolean;
  canRelease?: boolean;
  onReleaseSelected: (plannedOrderIds: string[]) => void;
  onDismiss: (id: string) => void;
  onOpenInGrid: (productId: string) => void;
}

const severityTone = (severity: MrpActionSeverity): string => {
  switch (severity) {
    case 'Critical':
      return 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300';
    case 'Warning':
      return 'bg-amber-100 text-amber-700 dark:bg-amber-500/20 dark:text-amber-300';
    default:
      return 'bg-slate-100 text-slate-600 dark:bg-slate-700 dark:text-slate-300';
  }
};

const SEVERITY_ORDER: Record<MrpActionSeverity, number> = {
  Critical: 0,
  Warning: 1,
  Info: 2,
};

const isReleasable = (type: MrpActionType): boolean => type === 'Release' || type === 'Expedite';

export const ActionMessageQueue = ({
  messages,
  locale,
  isReleasing = false,
  canRelease = true,
  onReleaseSelected,
  onDismiss,
  onOpenInGrid,
}: Props) => {
  const { t } = useTranslation();
  const [selected, setSelected] = useState<Set<string>>(new Set());

  const sorted = useMemo(
    () =>
      [...messages].sort((a, b) => {
        const bySeverity = SEVERITY_ORDER[a.severity] - SEVERITY_ORDER[b.severity];
        if (bySeverity !== 0) return bySeverity;
        return a.daysUntilStockOut - b.daysUntilStockOut;
      }),
    [messages],
  );

  const releasable = useMemo(
    () => sorted.filter((m) => isReleasable(m.actionType) && m.relatedPlannedOrderId),
    [sorted],
  );

  const releasableIds = useMemo(
    () => Array.from(new Set(releasable.map((m) => m.relatedPlannedOrderId as string))),
    [releasable],
  );

  const allSelected = releasableIds.length > 0 && selected.size === releasableIds.length;

  const toggle = (plannedOrderId: string) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(plannedOrderId)) next.delete(plannedOrderId);
      else next.add(plannedOrderId);
      return next;
    });
  };

  const toggleAll = () => {
    setSelected((prev) =>
      prev.size === releasableIds.length ? new Set() : new Set(releasableIds),
    );
  };

  const handleReleaseSelected = () => {
    if (selected.size === 0) return;
    onReleaseSelected(Array.from(selected));
    setSelected(new Set());
  };

  if (messages.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-slate-300 bg-white p-6 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-900">
        {t('Mrp.Workbench.Queue.Empty')}
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {selected.size > 0 && (
        <div className="flex items-center justify-between rounded-md border border-indigo-200 bg-indigo-50 px-3 py-2 text-sm dark:border-indigo-700 dark:bg-indigo-500/10">
          <span className="text-indigo-700 dark:text-indigo-300">
            {t('Mrp.Workbench.Queue.SelectedCount', { count: selected.size })}
          </span>
          <button
            type="button"
            disabled={isReleasing}
            onClick={handleReleaseSelected}
            className="flex items-center gap-1 rounded-md bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:bg-indigo-400"
          >
            <Send className="h-3.5 w-3.5" />
            {t('Mrp.Workbench.Queue.ReleaseSelected')}
          </button>
        </div>
      )}

      <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-900">
        <table className="min-w-full text-xs">
          <thead className="bg-slate-50 text-left text-slate-500 dark:bg-slate-800/60 dark:text-slate-400">
            <tr>
              <th scope="col" className="px-3 py-2">
                <input
                  type="checkbox"
                  aria-label={t('Mrp.Workbench.Queue.SelectAll') ?? 'Select all'}
                  checked={allSelected}
                  disabled={!canRelease || releasableIds.length === 0}
                  onChange={toggleAll}
                />
              </th>
              <th scope="col" className="px-3 py-2">
                {t('Mrp.Workbench.Queue.Severity')}
              </th>
              <th scope="col" className="px-3 py-2">
                {t('Mrp.Workbench.Queue.Type')}
              </th>
              <th scope="col" className="px-3 py-2">
                {t('Mrp.Workbench.Queue.Product')}
              </th>
              <th scope="col" className="px-3 py-2 text-right">
                {t('Mrp.Workbench.Queue.Quantity')}
              </th>
              <th scope="col" className="px-3 py-2 text-right">
                {t('Mrp.Workbench.Queue.DaysToStockout')}
              </th>
              <th scope="col" className="px-3 py-2">
                {t('Mrp.Workbench.Queue.Message')}
              </th>
              <th scope="col" className="px-3 py-2 text-right">
                {t('Mrp.Workbench.Queue.Actions')}
              </th>
            </tr>
          </thead>
          <tbody>
            {sorted.map((m) => {
              const rowReleasable =
                canRelease && isReleasable(m.actionType) && !!m.relatedPlannedOrderId;
              return (
                <tr
                  key={m.id}
                  data-testid="action-message-row"
                  className="border-t border-slate-100 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/40"
                >
                  <td className="px-3 py-2">
                    {rowReleasable && (
                      <input
                        type="checkbox"
                        aria-label={t('Mrp.Workbench.Queue.SelectRow', { sku: m.productSku }) ?? ''}
                        checked={selected.has(m.relatedPlannedOrderId as string)}
                        onChange={() => toggle(m.relatedPlannedOrderId as string)}
                      />
                    )}
                  </td>
                  <td className="px-3 py-2">
                    <span
                      className={`rounded-full px-2 py-0.5 text-[11px] font-semibold ${severityTone(m.severity)}`}
                    >
                      {t(`Mrp.Workbench.Severity.${m.severity}`)}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-slate-600 dark:text-slate-300">
                    {t(`Mrp.Workbench.ActionType.${m.actionType}`)}
                  </td>
                  <td className="px-3 py-2">
                    <div className="font-medium text-slate-800 dark:text-slate-100">
                      {m.productSku}
                    </div>
                    <div className="text-[11px] text-slate-500 dark:text-slate-400">
                      {m.productName}
                    </div>
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums text-slate-700 dark:text-slate-200">
                    {formatNumber(m.quantity, locale)}
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums text-slate-700 dark:text-slate-200">
                    {m.daysUntilStockOut}
                  </td>
                  <td className="px-3 py-2 text-slate-500 dark:text-slate-400">{m.message}</td>
                  <td className="px-3 py-2">
                    <div className="flex items-center justify-end gap-1">
                      <button
                        type="button"
                        onClick={() => onOpenInGrid(m.productId)}
                        aria-label={t('Mrp.Workbench.Queue.OpenInGrid') ?? 'Open in grid'}
                        className="rounded border border-slate-300 p-1 text-slate-600 hover:bg-slate-100 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700"
                      >
                        <Eye className="h-3.5 w-3.5" />
                      </button>
                      {rowReleasable && (
                        <button
                          type="button"
                          disabled={isReleasing}
                          onClick={() => onReleaseSelected([m.relatedPlannedOrderId as string])}
                          aria-label={t('Mrp.Workbench.Queue.Release') ?? 'Release'}
                          className="rounded border border-indigo-300 bg-indigo-50 p-1 text-indigo-700 hover:bg-indigo-100 disabled:opacity-50 dark:border-indigo-700 dark:bg-indigo-500/10 dark:text-indigo-300"
                        >
                          <Send className="h-3.5 w-3.5" />
                        </button>
                      )}
                      <button
                        type="button"
                        onClick={() => onDismiss(m.id)}
                        aria-label={t('Mrp.Workbench.Queue.Dismiss') ?? 'Dismiss'}
                        className="rounded border border-slate-300 p-1 text-slate-500 hover:bg-slate-100 dark:border-slate-600 dark:text-slate-400 dark:hover:bg-slate-700"
                      >
                        <BellOff className="h-3.5 w-3.5" />
                      </button>
                    </div>
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
