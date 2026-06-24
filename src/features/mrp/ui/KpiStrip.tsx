import { useTranslation } from 'react-i18next';
import { AlertTriangle, ShieldAlert, PackageX, Truck } from 'lucide-react';
import type { MrpPlanResult } from '../model/mrp-planning.types';

interface Props {
  plan: MrpPlanResult;
}

interface KpiProps {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  value: number;
  tone: string;
}

const Kpi = ({ icon: Icon, label, value, tone }: KpiProps) => (
  <div
    role="group"
    aria-label={label}
    className="flex items-center gap-3 rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900"
  >
    <span className={`flex h-10 w-10 items-center justify-center rounded-full ${tone}`}>
      <Icon className="h-5 w-5" />
    </span>
    <div className="flex-1">
      <p className="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">{label}</p>
      <p
        data-testid="stat-value"
        className="text-2xl font-semibold text-slate-800 dark:text-slate-100"
      >
        {value}
      </p>
    </div>
  </div>
);

export const KpiStrip = ({ plan }: Props) => {
  const { t } = useTranslation();
  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
      <Kpi
        icon={AlertTriangle}
        label={t('Mrp.Workbench.Kpi.StockoutRisk')}
        value={plan.stockoutRiskCount}
        tone="bg-warning-100 text-warning-700 dark:bg-warning-500/20 dark:text-warning-300"
      />
      <Kpi
        icon={PackageX}
        label={t('Mrp.Workbench.Kpi.ProjectedStockouts')}
        value={plan.projectedStockoutCount}
        tone="bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300"
      />
      <Kpi
        icon={ShieldAlert}
        label={t('Mrp.Workbench.Kpi.OpenExceptions')}
        value={plan.actionMessageCount}
        tone="bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300"
      />
      <Kpi
        icon={Truck}
        label={t('Mrp.Workbench.Kpi.OnOrder')}
        value={plan.onOrderCount}
        tone="bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300"
      />
    </div>
  );
};
