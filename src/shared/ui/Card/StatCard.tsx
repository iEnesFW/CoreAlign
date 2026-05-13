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
    <div className="relative overflow-hidden p-3 bg-white/50 dark:bg-[#0B0F19]/50 backdrop-blur-md rounded-[5px] border border-slate-200/60 dark:border-slate-800/60 transition-all duration-300 hover:border-indigo-500/50 group flex items-center justify-between">
      {/* Animated Background Glow */}
      <div
        className={`absolute -inset-1 bg-gradient-to-r ${color} opacity-0 group-hover:opacity-10 blur-xl transition-opacity duration-500`}
      />

      <div className="flex items-center gap-3 relative z-10">
        <div
          className={`p-2 rounded-[5px] bg-gradient-to-br ${color} text-white shadow-lg shadow-${color.split('-')[1]}-500/30 group-hover:scale-110 transition-transform duration-300`}
        >
          <Icon className="w-4 h-4" />
        </div>
        <div>
          <h3 className="text-[10px] font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wider">
            {name}
          </h3>
          <p className="text-base font-bold text-slate-900 dark:text-white leading-tight font-mono">
            {value}
          </p>
        </div>
      </div>

      <div className="relative z-10 flex flex-col items-end">
        <div
          className={`flex items-center gap-0.5 text-[10px] font-bold px-1.5 py-0.5 rounded-[3px] backdrop-blur-sm ${
            trend === 'up'
              ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20'
              : 'bg-red-500/10 text-red-600 dark:text-red-400 border border-red-500/20'
          }`}
        >
          {trend === 'up' ? (
            <ArrowUpRight size={10} strokeWidth={3} />
          ) : (
            <ArrowDownRight size={10} strokeWidth={3} />
          )}
          {change}
        </div>
        {/* Mini Sparkline Placeholder (Visual only) */}
        <div className="mt-1.5 flex items-end gap-0.5 h-3 opacity-50 group-hover:opacity-100 transition-opacity">
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
