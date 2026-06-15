import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { PlayCircle, Save, Tags } from 'lucide-react';
import {
  useClassifyAbc,
  useCommitMrpPlan,
  useCompleteProductionOrder,
  useFirmPlannedOrder,
  useFirmProductionOrder,
  useMrpCapacityLoadQuery,
  useMrpItemPlanQuery,
  useMrpPlanRunsQuery,
  useMrpPreviewQuery,
  useReleasePlannedOrders,
  useReleaseProductionOrder,
} from '@/features/mrp/hooks/useMrpWorkbench';
import {
  useDismissActionMessage,
  useMrpActionMessagesQuery,
} from '@/features/mrp/hooks/useMrpActionMessages';
import {
  useExecuteTransferSuggestion,
  useMrpChangeImpactQuery,
  useMrpPeggingQuery,
  useMrpTransferSuggestionsQuery,
} from '@/features/mrp/hooks/useMrpPlanRun';
import { KpiStrip } from '@/features/mrp/ui/KpiStrip';
import { MrpPlanningGrid } from '@/features/mrp/ui/MrpPlanningGrid';
import { ActionMessageQueue } from '@/features/mrp/ui/ActionMessageQueue';
import { PeggingDrawer } from '@/features/mrp/ui/PeggingDrawer';
import type {
  CompleteOrderRequest,
  FirmOrderRequest,
  ReleaseOrderRequest,
} from '@/features/mrp/ui/PeggingDrawer';
import { ChangeImpactView } from '@/features/mrp/ui/ChangeImpactView';
import { CapacityLoadView } from '@/features/mrp/ui/CapacityLoadView';
import { TransferSuggestionsView } from '@/features/mrp/ui/TransferSuggestionsView';
import {
  transferSuggestionKey,
  type MrpTransferSuggestion,
} from '@/features/mrp/model/mrp-planning.types';
import { ProcurementBadge } from '@/features/mrp/ui/ProcurementBadge';
import { useVendorsQuery } from '@/features/vendors/hooks/useVendorQueries';
import type {
  MrpActionSeverity,
  MrpActionType,
  MrpBucketKind,
  ProcurementFilter,
} from '@/features/mrp/model/mrp-planning.types';
import { safeRequest, safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { formatDateTime } from '@/shared/lib/format';

type WorkbenchTab = 'grid' | 'queue' | 'impact' | 'distribution' | 'capacity' | 'runs';

const BUCKET_KINDS: MrpBucketKind[] = ['Day', 'Week'];
const HORIZON_OPTIONS = [30, 60, 90, 120];
const PROCUREMENT_FILTERS: ProcurementFilter[] = ['All', 'Make', 'Buy'];
const ACTION_TYPES: MrpActionType[] = [
  'Release',
  'RescheduleIn',
  'RescheduleOut',
  'Expedite',
  'CancelSupply',
  'BelowSafetyStock',
  'ProjectedStockout',
];
const SEVERITIES: MrpActionSeverity[] = ['Critical', 'Warning', 'Info'];

export const MrpWorkbenchPage = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;

  const [bucketKind, setBucketKind] = useState<MrpBucketKind>('Day');
  const [horizonDays, setHorizonDays] = useState<number>(60);
  const [tab, setTab] = useState<WorkbenchTab>('grid');
  const [selectedProductId, setSelectedProductId] = useState<string | null>(null);
  const [typeFilter, setTypeFilter] = useState<MrpActionType | 'All'>('All');
  const [severityFilter, setSeverityFilter] = useState<MrpActionSeverity | 'All'>('All');
  const [supplierFilter, setSupplierFilter] = useState<string>('');
  const [procurementFilter, setProcurementFilter] = useState<ProcurementFilter>('All');
  const [impactSourceId, setImpactSourceId] = useState<string>('');
  const [impactQuery, setImpactQuery] = useState<{ id: string; label: string } | null>(null);
  const [executingTransferKey, setExecutingTransferKey] = useState<string | null>(null);

  const previewParams = useMemo(() => ({ bucketKind, horizonDays }), [bucketKind, horizonDays]);
  const preview = useMrpPreviewQuery(previewParams);
  const commit = useCommitMrpPlan();
  const classifyAbc = useClassifyAbc();
  const release = useReleasePlannedOrders();
  const firm = useFirmPlannedOrder();
  const firmProduction = useFirmProductionOrder();
  const releaseProduction = useReleaseProductionOrder();
  const completeProduction = useCompleteProductionOrder();
  const dismiss = useDismissActionMessage();
  const executeTransfer = useExecuteTransferSuggestion();
  const planRuns = useMrpPlanRunsQuery(1, 25);
  const vendors = useVendorsQuery({ page: 1, pageSize: 100 });

  const plan = preview.data?.data ?? null;
  const planRunId = plan?.planRunId ?? null;

  const filteredItems = useMemo(
    () =>
      (plan?.items ?? []).filter(
        (i) => procurementFilter === 'All' || i.procurementType === procurementFilter,
      ),
    [plan, procurementFilter],
  );

  const impactSources = useMemo(() => {
    const seen = new Map<string, string>();
    for (const item of plan?.items ?? []) {
      for (const peg of item.pegs ?? []) {
        if (peg.sourceKind !== 'SalesOrder' || !peg.sourceOrderLineId) continue;
        if (!seen.has(peg.sourceOrderLineId)) {
          seen.set(peg.sourceOrderLineId, peg.sourceOrderNumber ?? peg.sourceOrderLineId);
        }
      }
    }
    return Array.from(seen.entries()).map(([id, label]) => ({ id, label }));
  }, [plan]);

  const changeImpact = useMrpChangeImpactQuery(planRunId, impactQuery?.id ?? null);
  const transferSuggestions = useMrpTransferSuggestionsQuery(tab === 'distribution');
  const capacityLoad = useMrpCapacityLoadQuery(previewParams, tab === 'capacity');

  const impactProductInfo = useMemo(() => {
    const lookup: Record<string, { sku: string; name: string }> = {};
    for (const item of plan?.items ?? []) {
      lookup[item.productId] = { sku: item.sku, name: item.name };
    }
    return lookup;
  }, [plan]);

  const actionMessages = useMrpActionMessagesQuery({
    planRunId,
    type: typeFilter === 'All' ? null : typeFilter,
    severity: severityFilter === 'All' ? null : severityFilter,
    supplierId: supplierFilter || null,
    page: 1,
    pageSize: 100,
  });

  const selectedItem = (plan?.items ?? []).find((i) => i.productId === selectedProductId) ?? null;
  const itemPlan = useMrpItemPlanQuery(
    selectedProductId && !selectedItem
      ? { productId: selectedProductId, bucketKind, horizonDays }
      : null,
  );
  const drawerItem = selectedItem ?? itemPlan.data?.data ?? null;
  const pegging = useMrpPeggingQuery(planRunId, selectedProductId);

  const handleRelease = async (plannedOrderIds: string[]) => {
    if (!planRunId || plannedOrderIds.length === 0) return;
    await safeRequest(release.mutateAsync({ planRunId, plannedOrderIds }));
  };

  const handleDrawerFirm = async (input: FirmOrderRequest) => {
    if (input.procurementType === 'Make') {
      if (!input.productionOrderId) return;
      await safeRequest(firmProduction.mutateAsync({ productionOrderId: input.productionOrderId }));
      return;
    }
    if (!input.plannedOrderId) return;
    await safeRequest(
      firm.mutateAsync({
        plannedOrderId: input.plannedOrderId,
        overrideQuantity: input.overrideQuantity ?? null,
      }),
    );
  };

  const handleDrawerRelease = async (input: ReleaseOrderRequest) => {
    if (input.procurementType === 'Make') {
      if (!input.productionOrderId) return;
      await safeRequest(
        releaseProduction.mutateAsync({ productionOrderId: input.productionOrderId }),
      );
      return;
    }
    if (!planRunId || !input.plannedOrderId) return;
    await safeRequest(release.mutateAsync({ planRunId, plannedOrderIds: [input.plannedOrderId] }));
  };

  const handleDrawerComplete = async (input: CompleteOrderRequest) => {
    if (!input.productionOrderId) return;
    await safeRequestWithNotify(
      completeProduction.mutateAsync({ productionOrderId: input.productionOrderId }),
    );
  };

  const handleDismiss = async (id: string) => {
    await safeRequest(dismiss.mutateAsync(id));
  };

  const handleClassifyAbc = async () => {
    await safeRequestWithNotify(classifyAbc.mutateAsync());
  };

  const handleExecuteTransfer = async (suggestion: MrpTransferSuggestion) => {
    setExecutingTransferKey(transferSuggestionKey(suggestion));
    await safeRequestWithNotify(
      executeTransfer.mutateAsync({
        productId: suggestion.productId,
        fromWarehouseId: suggestion.fromWarehouseId,
        toWarehouseId: suggestion.toWarehouseId,
        quantity: suggestion.quantity,
      }),
    );
    setExecutingTransferKey(null);
  };

  const messages = actionMessages.data?.data?.items ?? [];
  const runs = planRuns.data?.data?.items ?? [];

  return (
    <div className="space-y-6 p-4 sm:p-6">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-slate-800 dark:text-slate-100">
            {t('Mrp.Workbench.Title')}
          </h1>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('Mrp.Workbench.Subtitle')}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <select
            aria-label={t('Mrp.Workbench.BucketKind') ?? 'Bucket'}
            value={bucketKind}
            onChange={(e) => setBucketKind(e.target.value as MrpBucketKind)}
            className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
          >
            {BUCKET_KINDS.map((b) => (
              <option key={b} value={b}>
                {t(`Mrp.Workbench.Bucket.${b}`)}
              </option>
            ))}
          </select>
          <select
            aria-label={t('Mrp.Workbench.Horizon') ?? 'Horizon'}
            value={horizonDays}
            onChange={(e) => setHorizonDays(Number(e.target.value))}
            className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
          >
            {HORIZON_OPTIONS.map((h) => (
              <option key={h} value={h}>
                {t('Mrp.Workbench.HorizonDays', { days: h })}
              </option>
            ))}
          </select>
          <button
            type="button"
            onClick={() => preview.refetch()}
            className="flex items-center gap-1 rounded-md border border-slate-300 px-3 py-2 text-sm text-slate-700 hover:bg-slate-100 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-700"
          >
            <PlayCircle className="h-4 w-4" />
            {t('Mrp.Workbench.RunPreview')}
          </button>
          <button
            type="button"
            disabled={classifyAbc.isPending}
            onClick={handleClassifyAbc}
            className="flex items-center gap-1 rounded-md border border-slate-300 px-3 py-2 text-sm text-slate-700 hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-700"
          >
            <Tags className="h-4 w-4" />
            {t('Mrp.Workbench.ClassifyAbc')}
          </button>
          <button
            type="button"
            disabled={commit.isPending}
            onClick={() => commit.mutate(previewParams)}
            className="flex items-center gap-1 rounded-md bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:bg-indigo-400"
          >
            <Save className="h-4 w-4" />
            {t('Mrp.Workbench.Commit')}
          </button>
        </div>
      </header>

      {preview.isLoading && (
        <p className="text-sm text-slate-500 dark:text-slate-400">{t('Common.Loading')}</p>
      )}

      {plan && <KpiStrip plan={plan} />}

      {plan && (
        <p className="text-xs text-slate-400 dark:text-slate-500">
          {t('Mrp.Workbench.AsOf')}: {formatDateTime(plan.asOfUtc, locale)} ·{' '}
          {t('Mrp.Workbench.ProductsEvaluated', { count: plan.productsEvaluated })}
        </p>
      )}

      <nav className="flex flex-wrap gap-2 border-b border-slate-200 dark:border-slate-700">
        {(['grid', 'queue', 'impact', 'distribution', 'capacity', 'runs'] as WorkbenchTab[]).map(
          (tk) => (
            <button
              key={tk}
              type="button"
              onClick={() => setTab(tk)}
              className={
                tab === tk
                  ? 'border-b-2 border-indigo-600 px-3 py-2 text-sm font-semibold text-indigo-700 dark:text-indigo-300'
                  : 'px-3 py-2 text-sm text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200'
              }
            >
              {t(`Mrp.Workbench.Tab.${tk}`)}
            </button>
          ),
        )}
      </nav>

      {tab === 'grid' && plan && (
        <div className="space-y-3">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-[11px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
              {t('Mrp.Workbench.Filter.Procurement')}
            </span>
            <div className="inline-flex rounded-md border border-slate-300 p-0.5 dark:border-slate-700">
              {PROCUREMENT_FILTERS.map((pf) => (
                <button
                  key={pf}
                  type="button"
                  onClick={() => setProcurementFilter(pf)}
                  aria-pressed={procurementFilter === pf}
                  className={
                    procurementFilter === pf
                      ? 'rounded px-2.5 py-1 text-xs font-semibold text-white bg-indigo-600'
                      : 'rounded px-2.5 py-1 text-xs text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-700'
                  }
                >
                  {pf === 'All' ? t('Common.All') : t(`Mrp.Workbench.Procurement.${pf}`)}
                </button>
              ))}
            </div>
            <span className="flex items-center gap-2 text-xs text-slate-500 dark:text-slate-400">
              <ProcurementBadge type="Make" /> {plan.makeOrderCount}
              <ProcurementBadge type="Buy" /> {plan.buyOrderCount}
            </span>
          </div>
          <MrpPlanningGrid
            items={filteredItems}
            locale={locale}
            selectedProductId={selectedProductId}
            onSelectItem={setSelectedProductId}
          />
        </div>
      )}

      {tab === 'queue' && (
        <div className="space-y-3">
          <div className="flex flex-wrap items-center gap-2">
            <FilterSelect
              label={t('Mrp.Workbench.Filter.Type')}
              value={typeFilter}
              onChange={(v) => setTypeFilter(v as MrpActionType | 'All')}
              options={['All', ...ACTION_TYPES]}
              render={(o) => (o === 'All' ? t('Common.All') : t(`Mrp.Workbench.ActionType.${o}`))}
            />
            <FilterSelect
              label={t('Mrp.Workbench.Filter.Severity')}
              value={severityFilter}
              onChange={(v) => setSeverityFilter(v as MrpActionSeverity | 'All')}
              options={['All', ...SEVERITIES]}
              render={(o) => (o === 'All' ? t('Common.All') : t(`Mrp.Workbench.Severity.${o}`))}
            />
            <label className="flex flex-col gap-1">
              <span className="text-[11px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('Mrp.Workbench.Filter.Supplier')}
              </span>
              <select
                value={supplierFilter}
                onChange={(e) => setSupplierFilter(e.target.value)}
                className="rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
              >
                <option value="">{t('Common.All')}</option>
                {(vendors.data?.data?.items ?? []).map((v) => (
                  <option key={v.id} value={v.id}>
                    {v.name}
                  </option>
                ))}
              </select>
            </label>
          </div>
          {!planRunId && (
            <p className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-700 dark:border-amber-700 dark:bg-amber-500/10 dark:text-amber-300">
              {t('Mrp.Workbench.Queue.CommitToRelease')}
            </p>
          )}
          {actionMessages.isLoading ? (
            <p className="text-sm text-slate-500 dark:text-slate-400">{t('Common.Loading')}</p>
          ) : (
            <ActionMessageQueue
              messages={messages}
              locale={locale}
              isReleasing={release.isPending}
              canRelease={!!planRunId}
              onReleaseSelected={handleRelease}
              onDismiss={handleDismiss}
              onOpenInGrid={(productId) => {
                setSelectedProductId(productId);
                setTab('grid');
              }}
            />
          )}
        </div>
      )}

      {tab === 'impact' && (
        <div className="space-y-3">
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('Mrp.Workbench.ChangeImpact.Help')}
          </p>
          {!planRunId && (
            <p className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-700 dark:border-amber-700 dark:bg-amber-500/10 dark:text-amber-300">
              {t('Mrp.Workbench.ChangeImpact.CommitRequired')}
            </p>
          )}
          <div className="flex flex-wrap items-end gap-2">
            <label className="flex flex-col gap-1">
              <span className="text-[11px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('Mrp.Workbench.ChangeImpact.Source')}
              </span>
              <select
                value={impactSourceId}
                onChange={(e) => setImpactSourceId(e.target.value)}
                className="rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
              >
                <option value="">{t('Mrp.Workbench.ChangeImpact.SelectSource')}</option>
                {impactSources.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.label}
                  </option>
                ))}
              </select>
            </label>
            <button
              type="button"
              disabled={!planRunId || !impactSourceId}
              onClick={() => {
                const selected = impactSources.find((s) => s.id === impactSourceId);
                setImpactQuery({ id: impactSourceId, label: selected?.label ?? impactSourceId });
              }}
              className="rounded-md bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:bg-indigo-400"
            >
              {t('Mrp.Workbench.ChangeImpact.Analyze')}
            </button>
          </div>
          <ChangeImpactView
            result={changeImpact.data?.data ?? null}
            locale={locale}
            sourceLabel={impactQuery?.label}
            productInfo={impactProductInfo}
            isLoading={changeImpact.isLoading && !!impactQuery}
          />
        </div>
      )}

      {tab === 'distribution' && (
        <div className="space-y-3">
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('Mrp.Workbench.Distribution.Help')}
          </p>
          <TransferSuggestionsView
            result={transferSuggestions.data?.data ?? null}
            locale={locale}
            isLoading={transferSuggestions.isLoading}
            onExecute={handleExecuteTransfer}
            executingKey={executingTransferKey}
            isExecuting={executeTransfer.isPending}
          />
        </div>
      )}

      {tab === 'capacity' && (
        <div className="space-y-3">
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('Mrp.Workbench.Capacity.Help')}
          </p>
          <CapacityLoadView
            result={capacityLoad.data?.data ?? null}
            locale={locale}
            isLoading={capacityLoad.isLoading}
          />
        </div>
      )}

      {tab === 'runs' && (
        <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-900">
          <table className="min-w-full text-xs">
            <thead className="bg-slate-50 text-left text-slate-500 dark:bg-slate-800/60 dark:text-slate-400">
              <tr>
                <th scope="col" className="px-3 py-2">
                  {t('Mrp.Workbench.Runs.Number')}
                </th>
                <th scope="col" className="px-3 py-2">
                  {t('Mrp.Workbench.Runs.AsOf')}
                </th>
                <th scope="col" className="px-3 py-2 text-right">
                  {t('Mrp.Workbench.Runs.Products')}
                </th>
                <th scope="col" className="px-3 py-2 text-right">
                  {t('Mrp.Workbench.Runs.PlannedOrders')}
                </th>
                <th scope="col" className="px-3 py-2 text-right">
                  {t('Mrp.Workbench.Runs.Exceptions')}
                </th>
              </tr>
            </thead>
            <tbody>
              {runs.map((r) => (
                <tr key={r.id} className="border-t border-slate-100 dark:border-slate-800">
                  <td className="px-3 py-2 font-mono font-semibold text-slate-800 dark:text-slate-100">
                    {r.number}
                  </td>
                  <td className="px-3 py-2 text-slate-600 dark:text-slate-300">
                    {formatDateTime(r.asOfDateUtc, locale)}
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums">{r.productsEvaluated}</td>
                  <td className="px-3 py-2 text-right tabular-nums">{r.plannedOrderCount}</td>
                  <td className="px-3 py-2 text-right tabular-nums">{r.actionMessageCount}</td>
                </tr>
              ))}
              {!planRuns.isLoading && runs.length === 0 && (
                <tr>
                  <td
                    colSpan={5}
                    className="px-3 py-6 text-center text-slate-500 dark:text-slate-400"
                  >
                    {t('Mrp.Workbench.Runs.Empty')}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {selectedProductId && (
        <PeggingDrawer
          item={drawerItem}
          pegging={pegging.data?.data ?? []}
          planRunId={planRunId}
          locale={locale}
          isFirming={firm.isPending || firmProduction.isPending}
          isReleasing={release.isPending || releaseProduction.isPending}
          isCompleting={completeProduction.isPending}
          onClose={() => setSelectedProductId(null)}
          onFirm={handleDrawerFirm}
          onRelease={handleDrawerRelease}
          onComplete={handleDrawerComplete}
        />
      )}
    </div>
  );
};

interface FilterSelectProps<T extends string> {
  label: string;
  value: T;
  onChange: (value: T) => void;
  options: T[];
  render: (option: T) => string;
}

const FilterSelect = <T extends string>({
  label,
  value,
  onChange,
  options,
  render,
}: FilterSelectProps<T>) => (
  <label className="flex flex-col gap-1">
    <span className="text-[11px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
      {label}
    </span>
    <select
      value={value}
      onChange={(e) => onChange(e.target.value as T)}
      className="rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
    >
      {options.map((o) => (
        <option key={o} value={o}>
          {render(o)}
        </option>
      ))}
    </select>
  </label>
);

export default MrpWorkbenchPage;
