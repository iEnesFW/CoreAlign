import { ArrowDownRight, ArrowUpRight } from 'lucide-react';
import { AnimatedNumber } from '@/shared/ui/AnimatedNumber/AnimatedNumber';
import { Sparkline } from '@/shared/ui/Sparkline/Sparkline';
import { cn } from '@/shared/lib/cn';

export type StatTone = 'indigo' | 'violet' | 'emerald' | 'amber' | 'rose' | 'sky' | 'slate';

export interface StatStripItem {
  id: string;
  label: string;
  value: number;
  format: (value: number) => string;
  icon?: React.ReactNode;
  sub?: string;
  delta?: { value: number; positiveIsGood?: boolean; suffix?: string };
  sparkline?: number[];
  tone?: StatTone;
  onClick?: () => void;
}

interface Props {
  items: StatStripItem[];
  className?: string;
  columnsClassName?: string;
}

const toneIconBg: Record<StatTone, string> = {
  indigo: 'from-indigo-500 to-blue-600',
  violet: 'from-violet-500 to-fuchsia-600',
  emerald: 'from-emerald-500 to-teal-600',
  amber: 'from-amber-500 to-orange-500',
  rose: 'from-rose-500 to-pink-600',
  sky: 'from-sky-500 to-cyan-600',
  slate: 'from-slate-500 to-slate-700',
};

const toneSpark: Record<StatTone, { stroke: string; fillFrom: string; fillTo: string }> = {
  indigo: { stroke: '#6366f1', fillFrom: 'rgba(99,102,241,0.35)', fillTo: 'rgba(99,102,241,0)' },
  violet: { stroke: '#8b5cf6', fillFrom: 'rgba(139,92,246,0.35)', fillTo: 'rgba(139,92,246,0)' },
  emerald: {
    stroke: '#10b981',
    fillFrom: 'rgba(16,185,129,0.35)',
    fillTo: 'rgba(16,185,129,0)',
  },
  amber: { stroke: '#f59e0b', fillFrom: 'rgba(245,158,11,0.35)', fillTo: 'rgba(245,158,11,0)' },
  rose: { stroke: '#f43f5e', fillFrom: 'rgba(244,63,94,0.35)', fillTo: 'rgba(244,63,94,0)' },
  sky: { stroke: '#0ea5e9', fillFrom: 'rgba(14,165,233,0.35)', fillTo: 'rgba(14,165,233,0)' },
  slate: { stroke: '#64748b', fillFrom: 'rgba(100,116,139,0.35)', fillTo: 'rgba(100,116,139,0)' },
};

export const StatStrip = ({ items, className, columnsClassName }: Props) => {
  return (
    <div
      className={cn(
        'ca-stagger grid gap-2 sm:gap-3',
        columnsClassName ?? 'grid-cols-2 lg:grid-cols-4',
        className,
      )}
    >
      {items.map((it) => (
        <StatCardItem key={it.id} item={it} />
      ))}
    </div>
  );
};

const StatCardItem = ({ item }: { item: StatStripItem }) => {
  const tone = item.tone ?? 'indigo';
  const delta = item.delta;
  const positiveIsGood = delta?.positiveIsGood ?? true;
  const showDeltaPositive = (delta?.value ?? 0) >= 0;
  const goodDirection = positiveIsGood ? showDeltaPositive : !showDeltaPositive;

  const sparkColors = toneSpark[tone];

  const containerClass = cn(
    'group relative isolate overflow-hidden rounded-xl border border-slate-200/70 bg-white/80 p-3 shadow-sm transition-all duration-300 dark:border-slate-800/70 dark:bg-slate-900/60',
    'hover:-translate-y-0.5 hover:shadow-lg hover:shadow-indigo-500/5 hover:border-indigo-300/60 dark:hover:border-indigo-500/40',
    item.onClick && 'cursor-pointer',
  );

  const Content = (
    <>
      <div
        className={cn(
          'absolute inset-x-0 -bottom-px h-[2px] bg-gradient-to-r opacity-60 transition-opacity duration-300 group-hover:opacity-100',
          toneIconBg[tone],
        )}
      />
      <div className="relative flex items-start justify-between gap-2">
        <div className="flex min-w-0 items-center gap-2">
          {item.icon && (
            <div
              className={cn(
                'flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br text-white shadow ring-1 ring-white/20 transition-transform group-hover:scale-105',
                toneIconBg[tone],
              )}
            >
              {item.icon}
            </div>
          )}
          <div className="min-w-0">
            <div className="text-[10px] font-semibold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">
              {item.label}
            </div>
            <div className="mt-0.5 truncate text-lg font-bold tabular-nums text-slate-900 dark:text-slate-100">
              <AnimatedNumber value={item.value} format={item.format} />
            </div>
            {item.sub && (
              <div className="text-[10px] text-slate-500 dark:text-slate-400">{item.sub}</div>
            )}
          </div>
        </div>

        <div className="flex shrink-0 flex-col items-end gap-1.5">
          {delta && (
            <span
              className={cn(
                'inline-flex items-center gap-0.5 rounded-md border px-1.5 py-0.5 text-[10px] font-semibold tabular-nums',
                goodDirection
                  ? 'border-emerald-500/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300'
                  : 'border-rose-500/30 bg-rose-500/10 text-rose-700 dark:text-rose-300',
              )}
            >
              {showDeltaPositive ? <ArrowUpRight size={10} /> : <ArrowDownRight size={10} />}
              {Math.abs(delta.value).toFixed(1)}
              {delta.suffix ?? '%'}
            </span>
          )}
          {item.sparkline && item.sparkline.length > 1 && (
            <Sparkline
              data={item.sparkline}
              width={72}
              height={22}
              variant="area"
              strokeColor={sparkColors.stroke}
              fillFrom={sparkColors.fillFrom}
              fillTo={sparkColors.fillTo}
              className="opacity-80 transition-opacity group-hover:opacity-100"
            />
          )}
        </div>
      </div>
    </>
  );

  if (item.onClick) {
    return (
      <button type="button" onClick={item.onClick} className={cn(containerClass, 'text-left')}>
        {Content}
      </button>
    );
  }
  return <div className={containerClass}>{Content}</div>;
};
