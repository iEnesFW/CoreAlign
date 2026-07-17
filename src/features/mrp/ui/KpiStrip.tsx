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
    className="group relative overflow-hidden flex items-center gap-4 rounded-2xl border border-white/40 bg-white/60 p-5 shadow-sm backdrop-blur-xl transition-all duration-300 hover:-translate-y-1 hover:shadow-xl dark:border-slate-700/50 dark:bg-slate-800/60"
  >
    <div
      className={`absolute -right-6 -top-6 h-24 w-24 rounded-full opacity-20 blur-2xl transition-all duration-500 group-hover:scale-150 group-hover:opacity-40 ${tone.split(' ')[0]}`}
    />

    <span
      className={`relative flex h-12 w-12 shrink-0 items-center justify-center rounded-xl shadow-inner ${tone}`}
    >
      <Icon className="h-6 w-6" />
    </span>

    <div className="relative flex-1">
      <p className="text-[11px] font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-1 line-clamp-1">
        {label}
      </p>
      <p
        data-testid="stat-value"
        className="text-3xl font-extrabold text-slate-800 dark:text-slate-100 tabular-nums leading-none"
      >
        {value}
      </p>
    </div>
  </div>
);

export const KpiStrip = ({ plan }: Props) => {
  const { t } = useTranslation();
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
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
        tone="bg-indigo-100 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300"
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
