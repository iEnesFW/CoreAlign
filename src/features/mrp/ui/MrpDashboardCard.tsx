import { useTranslation } from 'react-i18next';
import { AlertTriangle, PackageSearch, ClipboardCheck, ShoppingCart } from 'lucide-react';
import type { MrpDashboard } from '../model/mrp.types';

interface Props {
  dashboard: MrpDashboard;
}

interface MetricProps {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  value: number;
  tone: string;
}

const Metric = ({ icon: Icon, label, value, tone }: MetricProps) => (
  <div className="flex items-center gap-3 rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900">
    <span className={`flex h-10 w-10 items-center justify-center rounded-full ${tone}`}>
      <Icon className="h-5 w-5" />
    </span>
    <div className="flex-1">
      <p className="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">{label}</p>
      <p className="text-2xl font-semibold text-slate-800 dark:text-slate-100">{value}</p>
    </div>
  </div>
);

export const MrpDashboardCard = ({ dashboard }: Props) => {
  const { t } = useTranslation();
  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
      <Metric
        icon={PackageSearch}
        label={t('Mrp.Dashboard.TotalTracked')}
        value={dashboard.totalProductsTracked}
        tone="bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-200"
      />
      <Metric
        icon={AlertTriangle}
        label={t('Mrp.Dashboard.ReorderCandidates')}
        value={dashboard.reorderCandidateCount}
        tone="bg-amber-100 text-amber-700 dark:bg-amber-500/20 dark:text-amber-300"
      />
      <Metric
        icon={ClipboardCheck}
        label={t('Mrp.Dashboard.PendingRequisitions')}
        value={dashboard.pendingRequisitionCount}
        tone="bg-indigo-100 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300"
      />
      <Metric
        icon={ShoppingCart}
        label={t('Mrp.Dashboard.OpenPurchaseOrders')}
        value={dashboard.openPurchaseOrderCount}
        tone="bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300"
      />
    </div>
  );
};
