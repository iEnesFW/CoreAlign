import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { RefreshCw, PlayCircle, LayoutGrid, Factory } from 'lucide-react';
import {
  useGenerateMrpSuggestions,
  useMrpDashboardQuery,
  useStockProjectionQuery,
} from '@/features/mrp/hooks/useMrpDashboard';
import { MrpDashboardCard } from '@/features/mrp/ui/MrpDashboardCard';
import { RequisitionSuggestionsTable } from '@/features/mrp/ui/RequisitionSuggestionsTable';
import { StockProjectionChart } from '@/features/mrp/ui/StockProjectionChart';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { DetailPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';

export const MrpDashboardPage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const dashboard = useMrpDashboardQuery(20);
  const generate = useGenerateMrpSuggestions();
  const [selectedProductId, setSelectedProductId] = useState<string | null>(null);
  const projection = useStockProjectionQuery(selectedProductId, 30);

  const candidates = dashboard.data?.data?.topCandidates ?? [];

  return (
    <DetailPageTemplate
      header={
        <PageHeader
          icon={<Factory size={20} />}
          title={t('Mrp.Dashboard.Title')}
          subtitle={t('Mrp.Dashboard.Subtitle')}
          actions={
            <div className="flex flex-wrap items-center gap-2">
              <Button variant="outline" size="sm" onClick={() => dashboard.refetch()}>
                <RefreshCw size={14} />
                {t('Common.Refresh')}
              </Button>
              <Button
                size="sm"
                disabled={generate.isPending}
                onClick={() => generate.mutate(null)}
                data-tour="mrp-generate"
              >
                <PlayCircle size={14} />
                {t('Mrp.Action.Generate')}
              </Button>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => navigate('/dashboard/mrp/workbench')}
                data-tour="mrp-workbench"
              >
                <LayoutGrid size={14} />
                {t('Mrp.Dashboard.OpenWorkbench')}
              </Button>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => navigate('/dashboard/mrp/requisitions')}
                data-tour="mrp-requisitions"
              >
                {t('Mrp.Dashboard.ViewRequisitions')}
              </Button>
            </div>
          }
        />
      }
    >
      <div className="space-y-6">
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
    </DetailPageTemplate>
  );
};

export default MrpDashboardPage;
