import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { RefreshCw, PlayCircle, LayoutGrid } from 'lucide-react';
import {
  useGenerateMrpSuggestions,
  useMrpDashboardQuery,
  useStockProjectionQuery,
} from '@/features/mrp/hooks/useMrpDashboard';
import { MrpDashboardCard } from '@/features/mrp/ui/MrpDashboardCard';
import { RequisitionSuggestionsTable } from '@/features/mrp/ui/RequisitionSuggestionsTable';
import { StockProjectionChart } from '@/features/mrp/ui/StockProjectionChart';

export const MrpDashboardPage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const dashboard = useMrpDashboardQuery(20);
  const generate = useGenerateMrpSuggestions();
  const [selectedProductId, setSelectedProductId] = useState<string | null>(null);
  const projection = useStockProjectionQuery(selectedProductId, 30);

  const candidates = dashboard.data?.data?.topCandidates ?? [];

  return (
    <div className="space-y-6 p-4 sm:p-6">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-slate-800 dark:text-slate-100">
            {t('Mrp.Dashboard.Title')}
          </h1>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('Mrp.Dashboard.Subtitle')}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => dashboard.refetch()}
            className="flex items-center gap-1 rounded-md border border-slate-300 px-3 py-2 text-sm text-slate-700 hover:bg-slate-100 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-700"
          >
            <RefreshCw className="h-4 w-4" />
            {t('Common.Refresh')}
          </button>
          <button
            type="button"
            disabled={generate.isPending}
            onClick={() => generate.mutate(null)}
            data-tour="mrp-generate"
            className="flex items-center gap-1 rounded-md bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:bg-indigo-400"
          >
            <PlayCircle className="h-4 w-4" />
            {t('Mrp.Action.Generate')}
          </button>
          <button
            type="button"
            onClick={() => navigate('/dashboard/mrp/workbench')}
            data-tour="mrp-workbench"
            className="flex items-center gap-1 rounded-md border border-indigo-300 bg-indigo-50 px-3 py-2 text-sm font-medium text-indigo-700 hover:bg-indigo-100 dark:border-indigo-700 dark:bg-indigo-500/10 dark:text-indigo-300"
          >
            <LayoutGrid className="h-4 w-4" />
            {t('Mrp.Dashboard.OpenWorkbench')}
          </button>
          <button
            type="button"
            onClick={() => navigate('/dashboard/mrp/requisitions')}
            data-tour="mrp-requisitions"
            className="rounded-md border border-indigo-300 bg-indigo-50 px-3 py-2 text-sm font-medium text-indigo-700 hover:bg-indigo-100 dark:border-indigo-700 dark:bg-indigo-500/10 dark:text-indigo-300"
          >
            {t('Mrp.Dashboard.ViewRequisitions')}
          </button>
        </div>
      </header>

      {dashboard.isLoading && (
        <p className="text-sm text-slate-500 dark:text-slate-400">{t('Common.Loading')}</p>
      )}

      {dashboard.data?.data && (
        <div data-tour="mrp-summary">
          <MrpDashboardCard dashboard={dashboard.data.data} />
        </div>
      )}

      <section className="space-y-3" data-tour="mrp-candidates">
        <h2 className="text-lg font-semibold text-slate-700 dark:text-slate-200">
          {t('Mrp.Suggestions.Title')}
        </h2>
        <RequisitionSuggestionsTable candidates={candidates} onSelect={setSelectedProductId} />
      </section>

      {selectedProductId && projection.data?.data && (
        <section className="space-y-3">
          <h2 className="text-lg font-semibold text-slate-700 dark:text-slate-200">
            {projection.data.data.productName}
          </h2>
          <StockProjectionChart projection={projection.data.data} />
        </section>
      )}
    </div>
  );
};

export default MrpDashboardPage;
