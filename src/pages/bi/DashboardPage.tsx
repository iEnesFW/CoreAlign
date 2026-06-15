import { useTranslation } from 'react-i18next';
import { useDashboardQuery, useRemoveDashboardWidget } from '@/features/bi/hooks/useDashboard';
import { useDashboardWidgetResult } from '@/features/bi/hooks/useDashboardWidgetResult';
import { BarChartWidget } from '@/features/bi/ui/BarChartWidget';
import { LineChartWidget } from '@/features/bi/ui/LineChartWidget';
import { StatCardWidget } from '@/features/bi/ui/StatCardWidget';
import { TableWidget } from '@/features/bi/ui/TableWidget';
import { WidgetGrid } from '@/features/bi/ui/WidgetGrid';
import type { BIResult, DashboardWidget } from '@/features/bi/model/bi.types';

const EMPTY_RESULT: BIResult = { columns: [], rows: [], totalRowCount: 0 };

const WidgetSkeleton = ({ title }: { title: string }) => (
  <div className="flex h-full flex-col rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900">
    <h3 className="mb-2 text-sm font-semibold text-slate-700 dark:text-slate-200">{title}</h3>
    <div className="flex-1 animate-pulse rounded bg-slate-100 dark:bg-slate-800" />
  </div>
);

const WidgetErrorBanner = ({ title }: { title: string }) => {
  const { t } = useTranslation();
  return (
    <div className="flex h-full flex-col rounded-lg border border-red-200 bg-red-50 p-4 shadow-sm dark:border-red-900/50 dark:bg-red-950/30">
      <h3 className="mb-2 text-sm font-semibold text-slate-700 dark:text-slate-200">{title}</h3>
      <div className="text-sm text-red-700 dark:text-red-300">
        {t('BI.Dashboard.WidgetError', { defaultValue: 'Failed to load widget data.' })}
      </div>
    </div>
  );
};

const renderWidgetBody = (widget: DashboardWidget, result: BIResult) => {
  switch (widget.type) {
    case 'LineChart':
    case 'AreaChart':
      return <LineChartWidget title={widget.title} result={result} />;
    case 'BarChart':
    case 'PieChart':
      return <BarChartWidget title={widget.title} result={result} />;
    case 'StatCard':
      return <StatCardWidget title={widget.title} result={result} />;
    case 'Table':
    case 'Calendar':
    default:
      return <TableWidget title={widget.title} result={result} />;
  }
};

const DashboardWidgetView = ({ widget }: { widget: DashboardWidget }) => {
  const { data, isLoading, isError } = useDashboardWidgetResult(widget);
  if (isLoading) {
    return <WidgetSkeleton title={widget.title} />;
  }
  if (isError) {
    return <WidgetErrorBanner title={widget.title} />;
  }
  return renderWidgetBody(widget, data ?? EMPTY_RESULT);
};

export const DashboardPage = () => {
  const { t } = useTranslation();
  const dashboard = useDashboardQuery();
  const removeMutation = useRemoveDashboardWidget();

  return (
    <div className="space-y-4 p-4">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-50">
          {t('BI.Dashboard.Title', { defaultValue: 'KPI Dashboard' })}
        </h1>
      </div>
      {dashboard.isLoading ? (
        <div className="text-sm text-slate-500">
          {t('BI.Common.Loading', { defaultValue: 'Loading...' })}
        </div>
      ) : (
        <WidgetGrid
          widgets={dashboard.data ?? []}
          renderWidget={(w) => <DashboardWidgetView widget={w} />}
          onRemove={(w) => removeMutation.mutate(w.id)}
        />
      )}
    </div>
  );
};

export default DashboardPage;
