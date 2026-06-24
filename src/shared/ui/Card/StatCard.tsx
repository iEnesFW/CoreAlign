import React from 'react';
import type { LucideIcon } from 'lucide-react';
import { ArrowUpRight, ArrowDownRight } from 'lucide-react';

export interface StatCardProps {
  name: string;
  value: string;
  change: string;
  trend: 'up' | 'down';
  icon: LucideIcon;
  color: string;
}

export const StatCard: React.FC<StatCardProps> = ({
  name,
  value,
  change,
  trend,
  icon: Icon,
  color,
}) => {
  return (
    <div className="group relative flex items-center justify-between overflow-hidden rounded-xl border border-slate-200/70 bg-white/80 p-3.5 shadow-sm backdrop-blur-md transition-all duration-300 hover:-translate-y-0.5 hover:border-primary-300/60 hover:shadow-lg hover:shadow-primary-500/5 dark:border-white/10 dark:bg-slate-900/60 dark:hover:border-primary-500/40">
      <div
        className={`absolute -inset-1 bg-gradient-to-r ${color} opacity-0 blur-xl transition-opacity duration-500 group-hover:opacity-10`}
      />

      <div className="relative z-10 flex items-center gap-3">
        <div
          className={`rounded-lg bg-gradient-to-br p-2 text-white shadow-sm ring-1 ring-white/20 transition-transform duration-300 group-hover:scale-110 ${color}`}
        >
          <Icon className="h-4 w-4" />
        </div>
        <div>
          <h3 className="text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
            {name}
          </h3>
          <p className="text-base font-bold leading-tight tabular-nums text-slate-900 dark:text-white">
            {value}
          </p>
        </div>
      </div>

      <div className="relative z-10 flex flex-col items-end">
        <div
          className={`flex items-center gap-0.5 rounded-md border px-1.5 py-0.5 text-[10px] font-bold backdrop-blur-sm ${
            trend === 'up'
              ? 'border-success-500/20 bg-success-500/10 text-success-600 dark:text-success-400'
              : 'border-danger-500/20 bg-danger-500/10 text-danger-600 dark:text-danger-400'
          }`}
        >
          {trend === 'up' ? (
            <ArrowUpRight size={10} strokeWidth={3} />
          ) : (
            <ArrowDownRight size={10} strokeWidth={3} />
          )}
          {change}
        </div>
        <div className="mt-1.5 flex h-3 items-end gap-0.5 opacity-50 transition-opacity group-hover:opacity-100">
          {[40, 70, 45, 90, 65, 85, 100].map((h, i) => (
            <div
              key={i}
              className={`w-1 rounded-t-sm bg-gradient-to-t ${color}`}
              style={{ height: `${h}%` }}
            />
          ))}
        </div>
      </div>
    </div>
  );
};
