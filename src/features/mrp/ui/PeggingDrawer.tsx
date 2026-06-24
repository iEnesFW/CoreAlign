import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { X, Pin, Send, Factory, CheckCircle } from 'lucide-react';
import { formatDate, formatNumber } from '@/shared/lib/format';
import type {
  MrpItemPlan,
  MrpPegging,
  MrpPlannedOrder,
  MrpProductionOrderDraft,
  ProcurementType,
} from '../model/mrp-planning.types';
import { MrpTimePhasedChart } from './MrpTimePhasedChart';
import { ProcurementBadge } from './ProcurementBadge';
import { AbcBadge } from './AbcBadge';

interface DrawerOrder {
  procurementType: ProcurementType;
  plannedOrderId?: string | null;
  productionOrderId?: string | null;
  productId: string;
  quantity: number;
  dueDateUtc: string;
  releaseDateUtc: string;
  isFirmed: boolean;
  isReleased: boolean;
  isCompleted: boolean;
  isQuantityOverridden?: boolean;
  isDueDateOverridden?: boolean;
  originalQuantity?: number | null;
}

export interface FirmOrderRequest {
  procurementType: ProcurementType;
  plannedOrderId?: string | null;
  productionOrderId?: string | null;
  overrideQuantity?: number | null;
}

export interface ReleaseOrderRequest {
  procurementType: ProcurementType;
  plannedOrderId?: string | null;
  productionOrderId?: string | null;
}

export interface CompleteOrderRequest {
  productionOrderId: string;
}

interface Props {
  item: MrpItemPlan | null;
  pegging: MrpPegging[];
  planRunId: string | null;
  locale: string;
  isFirming?: boolean;
  isReleasing?: boolean;
  isCompleting?: boolean;
  onClose: () => void;
  onFirm: (input: FirmOrderRequest) => void;
  onRelease: (input: ReleaseOrderRequest) => void;
  onComplete: (input: CompleteOrderRequest) => void;
}

const buyToDrawerOrder = (order: MrpPlannedOrder): DrawerOrder => ({
  procurementType: 'Buy',
  plannedOrderId: order.id ?? null,
  productionOrderId: null,
  productId: order.productId,
  quantity: order.quantity,
  dueDateUtc: order.dueDateUtc,
  releaseDateUtc: order.releaseDateUtc,
  isFirmed: !!order.isFirmed,
  isReleased: !!order.isReleased || !!order.convertedRequisitionId,
  isCompleted: false,
  isQuantityOverridden: order.isQuantityOverridden,
  isDueDateOverridden: order.isDueDateOverridden,
  originalQuantity: order.originalQuantity,
});

const makeToDrawerOrder = (order: MrpProductionOrderDraft): DrawerOrder => {
  const status = order.status ?? null;
  return {
    procurementType: 'Make',
    plannedOrderId: null,
    productionOrderId: order.id ?? null,
    productId: order.productId,
    quantity: order.quantity,
    dueDateUtc: order.dueDateUtc,
    releaseDateUtc: order.releaseDateUtc,
    isFirmed: status === 'Firm' || status === 'Released' || status === 'Closed',
    isReleased: status === 'Released' || status === 'Closed',
    isCompleted: status === 'Closed',
  };
};

const PlannedOrderRow = ({
  order,
  locale,
  isFirming,
  isReleasing,
  isCompleting,
  isCommitted,
  onFirm,
  onRelease,
  onComplete,
}: {
  order: DrawerOrder;
  locale: string;
  isFirming?: boolean;
  isReleasing?: boolean;
  isCompleting?: boolean;
  isCommitted: boolean;
  onFirm: Props['onFirm'];
  onRelease: Props['onRelease'];
  onComplete: Props['onComplete'];
}) => {
  const { t } = useTranslation();
  const [qty, setQty] = useState<number>(order.quantity);
  const orderId = order.procurementType === 'Make' ? order.productionOrderId : order.plannedOrderId;
  const { isReleased, isCompleted } = order;
  const locked = !isCommitted || !orderId || isReleased;
  const isMake = order.procurementType === 'Make';
  const canComplete = isMake && isReleased && !isCompleted;
  const releaseLabel = isMake
    ? t('Mrp.Workbench.Drawer.CreateProductionOrder')
    : t('Mrp.Action.Convert');

  return (
    <div
      data-testid="planned-order-row"
      data-procurement-type={order.procurementType}
      className={`rounded-md border p-2 ${
        isMake
          ? 'border-violet-200 bg-violet-50 dark:border-violet-700/50 dark:bg-violet-500/5'
          : 'border-info-200 bg-info-50 dark:border-info-700/50 dark:bg-info-500/5'
      }`}
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2 text-xs text-slate-600 dark:text-slate-300">
          <ProcurementBadge type={order.procurementType} />
          <span>
            {t('Mrp.Workbench.Drawer.Release')}: {formatDate(order.releaseDateUtc, locale)} ·{' '}
            {t('Mrp.Workbench.Drawer.Due')}: {formatDate(order.dueDateUtc, locale)}
          </span>
        </div>
        {order.isFirmed && (
          <span className="rounded-full bg-success-100 px-2 py-0.5 text-[11px] font-semibold text-success-700 dark:bg-success-500/20 dark:text-success-300">
            {t('Mrp.Workbench.Drawer.Firmed')}
          </span>
        )}
        {(order.isQuantityOverridden || order.isDueDateOverridden) && (
          <span
            data-testid="override-badge"
            title={
              order.isQuantityOverridden && typeof order.originalQuantity === 'number'
                ? `${order.originalQuantity.toLocaleString(locale)} → ${order.quantity.toLocaleString(locale)}`
                : t('Mrp.Workbench.Drawer.Overridden')
            }
            className="rounded-full bg-warning-100 px-2 py-0.5 text-[11px] font-semibold text-warning-700 dark:bg-warning-500/20 dark:text-warning-300"
          >
            {t('Mrp.Workbench.Drawer.Overridden')}
            {order.isQuantityOverridden && typeof order.originalQuantity === 'number'
              ? ` (${order.originalQuantity.toLocaleString(locale)} → ${order.quantity.toLocaleString(locale)})`
              : ''}
          </span>
        )}
        {isReleased && (
          <span className="rounded-full bg-success-100 px-2 py-0.5 text-[11px] font-semibold text-success-700 dark:bg-success-500/20 dark:text-success-300">
            {isMake
              ? t('Mrp.Workbench.Drawer.ProductionOrderCreated')
              : t('Mrp.Workbench.Drawer.Released')}
          </span>
        )}
        {isMake && isCompleted && (
          <span className="rounded-full bg-teal-100 px-2 py-0.5 text-[11px] font-semibold text-teal-700 dark:bg-teal-500/20 dark:text-teal-300">
            {t('Mrp.Workbench.Drawer.Completed')}
          </span>
        )}
      </div>
      <div className="mt-2 flex flex-wrap items-end gap-2">
        <label className="flex flex-col gap-1">
          <span className="text-[11px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
            {t('Mrp.Workbench.Drawer.Quantity')}
          </span>
          <input
            type="number"
            min={0}
            step="0.0001"
            value={qty}
            disabled={locked || isMake}
            onChange={(e) => setQty(Number(e.target.value))}
            className="w-28 rounded border border-slate-300 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
        </label>
        <button
          type="button"
          disabled={isFirming || locked}
          onClick={() =>
            orderId &&
            onFirm(
              isMake
                ? { procurementType: 'Make', productionOrderId: orderId }
                : { procurementType: 'Buy', plannedOrderId: orderId, overrideQuantity: qty },
            )
          }
          className="flex items-center gap-1 rounded-md border border-slate-300 bg-white px-2 py-1 text-xs font-medium text-slate-700 hover:bg-slate-100 disabled:opacity-50 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-200"
        >
          <Pin className="h-3.5 w-3.5" />
          {t('Mrp.Workbench.Drawer.Firm')}
        </button>
        <button
          type="button"
          disabled={isReleasing || locked}
          onClick={() =>
            orderId &&
            onRelease(
              isMake
                ? { procurementType: 'Make', productionOrderId: orderId }
                : { procurementType: 'Buy', plannedOrderId: orderId },
            )
          }
          className={`flex items-center gap-1 rounded-md px-2 py-1 text-xs font-semibold text-white disabled:cursor-not-allowed ${
            isMake
              ? 'bg-violet-600 hover:bg-violet-500 disabled:bg-violet-400'
              : 'bg-primary-600 hover:bg-primary-500 disabled:bg-primary-400'
          }`}
        >
          {isMake ? <Factory className="h-3.5 w-3.5" /> : <Send className="h-3.5 w-3.5" />}
          {releaseLabel}
        </button>
        {canComplete && (
          <button
            type="button"
            disabled={isCompleting || !orderId}
            onClick={() => orderId && onComplete({ productionOrderId: orderId })}
            className="flex items-center gap-1 rounded-md bg-teal-600 px-2 py-1 text-xs font-semibold text-white hover:bg-teal-500 disabled:cursor-not-allowed disabled:bg-teal-400"
          >
            <CheckCircle className="h-3.5 w-3.5" />
            {t('Mrp.Workbench.Drawer.Complete')}
          </button>
        )}
      </div>
    </div>
  );
};

export const PeggingDrawer = ({
  item,
  pegging,
  planRunId,
  locale,
  isFirming,
  isReleasing,
  isCompleting,
  onClose,
  onFirm,
  onRelease,
  onComplete,
}: Props) => {
  const { t } = useTranslation();
  const makeOrders = useMemo(() => (item?.productionOrders ?? []).map(makeToDrawerOrder), [item]);
  const buyOrders = useMemo(
    () =>
      (item?.plannedOrders ?? []).filter((o) => o.procurementType === 'Buy').map(buyToDrawerOrder),
    [item],
  );
  if (!item) return null;

  const isCommitted = !!planRunId;
  const hasOrders = makeOrders.length > 0 || buyOrders.length > 0;

  return (
    <aside
      role="dialog"
      aria-label={t('Mrp.Workbench.Drawer.Title', { sku: item.sku }) ?? item.sku}
      className="fixed inset-y-0 right-0 z-50 flex w-full max-w-md flex-col border-l border-slate-200 bg-white shadow-xl dark:border-slate-700 dark:bg-slate-900"
    >
      <header className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-700">
        <div>
          <h2 className="flex items-center gap-2 text-sm font-semibold text-slate-800 dark:text-slate-100">
            {item.sku}
            <ProcurementBadge type={item.procurementType} />
            <AbcBadge abcClass={item.abcClass} />
          </h2>
          <p className="text-xs text-slate-500 dark:text-slate-400">{item.name}</p>
        </div>
        <button
          type="button"
          onClick={onClose}
          aria-label={t('Common.Close') ?? 'Close'}
          className="rounded p-1 text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-700"
        >
          <X className="h-4 w-4" />
        </button>
      </header>

      <div className="flex-1 space-y-4 overflow-y-auto p-4">
        <dl className="grid grid-cols-2 gap-2 text-xs">
          <Stat
            label={t('Mrp.Workbench.Drawer.OnHand')}
            value={formatNumber(item.onHand, locale)}
          />
          <Stat
            label={t('Mrp.Workbench.Drawer.SafetyStock')}
            value={formatNumber(item.safetyStock, locale)}
          />
          <Stat
            label={t('Mrp.Workbench.Drawer.ReorderPoint')}
            value={formatNumber(item.reorderPoint, locale)}
          />
          <Stat
            label={t('Mrp.Workbench.Drawer.Policy')}
            value={t(`Mrp.Workbench.Policy.${item.policy}`)}
          />
        </dl>

        <MrpTimePhasedChart item={item} />

        <section>
          <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
            {t('Mrp.Workbench.Drawer.PlannedOrders')}
          </h3>
          {!hasOrders ? (
            <p className="text-xs text-slate-400">{t('Mrp.Workbench.Drawer.NoPlannedOrders')}</p>
          ) : (
            <div className="space-y-3">
              {!isCommitted && (
                <p className="rounded-md border border-warning-200 bg-warning-50 px-2 py-1.5 text-[11px] text-warning-700 dark:border-warning-700 dark:bg-warning-500/10 dark:text-warning-300">
                  {t('Mrp.Workbench.Drawer.CommitRequired')}
                </p>
              )}
              <PlannedOrderGroup
                type="Make"
                orders={makeOrders}
                locale={locale}
                isFirming={isFirming}
                isReleasing={isReleasing}
                isCompleting={isCompleting}
                isCommitted={isCommitted}
                onFirm={onFirm}
                onRelease={onRelease}
                onComplete={onComplete}
              />
              <PlannedOrderGroup
                type="Buy"
                orders={buyOrders}
                locale={locale}
                isFirming={isFirming}
                isReleasing={isReleasing}
                isCompleting={isCompleting}
                isCommitted={isCommitted}
                onFirm={onFirm}
                onRelease={onRelease}
                onComplete={onComplete}
              />
            </div>
          )}
        </section>

        <section>
          <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
            {t('Mrp.Workbench.Drawer.Pegging')}
          </h3>
          {pegging.length === 0 ? (
            <p className="text-xs text-slate-400">{t('Mrp.Workbench.Drawer.NoPegging')}</p>
          ) : (
            <ul className="space-y-1" data-testid="pegging-list">
              {pegging.map((p, idx) => (
                <li
                  key={idx}
                  className="flex items-center justify-between rounded border border-slate-100 bg-slate-50 px-2 py-1.5 text-xs dark:border-slate-800 dark:bg-slate-800/40"
                >
                  <span className="text-slate-600 dark:text-slate-300">
                    {t(`Mrp.Workbench.PegSource.${p.sourceKind}`)}
                    {p.sourceOrderNumber ? ` · ${p.sourceOrderNumber}` : ''}
                    {p.sourceParentProductName ? ` · ${p.sourceParentProductName}` : ''}
                  </span>
                  <span className="tabular-nums font-medium text-slate-700 dark:text-slate-200">
                    {formatNumber(p.requirementQuantity, locale)} ·{' '}
                    {formatDate(p.dueDateUtc, locale)}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </aside>
  );
};

const PlannedOrderGroup = ({
  type,
  orders,
  locale,
  isFirming,
  isReleasing,
  isCompleting,
  isCommitted,
  onFirm,
  onRelease,
  onComplete,
}: {
  type: ProcurementType;
  orders: DrawerOrder[];
  locale: string;
  isFirming?: boolean;
  isReleasing?: boolean;
  isCompleting?: boolean;
  isCommitted: boolean;
  onFirm: Props['onFirm'];
  onRelease: Props['onRelease'];
  onComplete: Props['onComplete'];
}) => {
  const { t } = useTranslation();
  if (orders.length === 0) return null;
  return (
    <div data-testid={`planned-order-group-${type}`}>
      <div className="mb-1 flex items-center gap-2">
        <ProcurementBadge type={type} />
        <span className="text-[11px] text-slate-400 dark:text-slate-500">
          {t('Mrp.Workbench.Drawer.OrderCount', { count: orders.length })}
        </span>
      </div>
      <div className="space-y-2">
        {orders.map((o, idx) => (
          <PlannedOrderRow
            key={
              o.plannedOrderId ?? o.productionOrderId ?? `${o.productId}-${o.releaseDateUtc}-${idx}`
            }
            order={o}
            locale={locale}
            isFirming={isFirming}
            isReleasing={isReleasing}
            isCompleting={isCompleting}
            isCommitted={isCommitted}
            onFirm={onFirm}
            onRelease={onRelease}
            onComplete={onComplete}
          />
        ))}
      </div>
    </div>
  );
};

const Stat = ({ label, value }: { label: string; value: string }) => (
  <div className="rounded border border-slate-100 bg-slate-50 p-2 dark:border-slate-800 dark:bg-slate-800/40">
    <dt className="text-slate-500 dark:text-slate-400">{label}</dt>
    <dd className="font-semibold text-slate-700 dark:text-slate-100">{value}</dd>
  </div>
);
