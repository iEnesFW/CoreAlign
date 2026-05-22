import { Check, X } from 'lucide-react';
import { cn } from '@/shared/lib/cn';

export type ChipTone = 'indigo' | 'emerald' | 'amber' | 'rose' | 'sky' | 'violet' | 'slate';

interface Props {
  label: React.ReactNode;
  active?: boolean;
  count?: number;
  icon?: React.ReactNode;
  onClick?: () => void;
  onRemove?: () => void;
  tone?: ChipTone;
  size?: 'sm' | 'md';
  className?: string;
}

const activeToneClasses: Record<ChipTone, string> = {
  indigo:
    'border-indigo-300/80 bg-indigo-50 text-indigo-700 ring-1 ring-inset ring-indigo-200/60 dark:border-indigo-500/40 dark:bg-indigo-500/15 dark:text-indigo-200 dark:ring-indigo-500/30',
  emerald:
    'border-emerald-300/80 bg-emerald-50 text-emerald-700 ring-1 ring-inset ring-emerald-200/60 dark:border-emerald-500/40 dark:bg-emerald-500/15 dark:text-emerald-200',
  amber:
    'border-amber-300/80 bg-amber-50 text-amber-800 ring-1 ring-inset ring-amber-200/60 dark:border-amber-500/40 dark:bg-amber-500/15 dark:text-amber-200',
  rose: 'border-rose-300/80 bg-rose-50 text-rose-700 ring-1 ring-inset ring-rose-200/60 dark:border-rose-500/40 dark:bg-rose-500/15 dark:text-rose-200',
  sky: 'border-sky-300/80 bg-sky-50 text-sky-700 ring-1 ring-inset ring-sky-200/60 dark:border-sky-500/40 dark:bg-sky-500/15 dark:text-sky-200',
  violet:
    'border-violet-300/80 bg-violet-50 text-violet-700 ring-1 ring-inset ring-violet-200/60 dark:border-violet-500/40 dark:bg-violet-500/15 dark:text-violet-200',
  slate:
    'border-slate-300 bg-slate-100 text-slate-800 ring-1 ring-inset ring-slate-200 dark:border-slate-500/40 dark:bg-slate-700/40 dark:text-slate-200',
};

export const FilterChip = ({
  label,
  active = false,
  count,
  icon,
  onClick,
  onRemove,
  tone = 'indigo',
  size = 'md',
  className,
}: Props) => {
  const base = cn(
    'group inline-flex items-center gap-1.5 rounded-full border font-medium transition-all duration-200',
    size === 'sm' ? 'px-2 py-0.5 text-[10px]' : 'px-2.5 py-1 text-[11px]',
    active
      ? activeToneClasses[tone]
      : 'border-slate-200 bg-white text-slate-600 hover:border-indigo-300 hover:bg-indigo-50/50 hover:text-indigo-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300 dark:hover:border-indigo-500/40 dark:hover:bg-indigo-500/10 dark:hover:text-indigo-200',
    className,
  );

  return (
    <button type="button" onClick={onClick} className={base}>
      {active && <Check size={11} className="shrink-0" />}
      {icon}
      <span>{label}</span>
      {count !== undefined && (
        <span
          className={cn(
            'rounded-full px-1.5 py-px text-[9px] font-semibold tabular-nums',
            active
              ? 'bg-white/60 text-current dark:bg-slate-900/40'
              : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400',
          )}
        >
          {count}
        </span>
      )}
      {onRemove && active && (
        <span
          role="button"
          aria-label="Remove filter"
          onClick={(e) => {
            e.stopPropagation();
            onRemove();
          }}
          className="-mr-0.5 ml-0.5 rounded-full p-0.5 transition-colors hover:bg-white/40 dark:hover:bg-slate-800/60"
        >
          <X size={10} />
        </span>
      )}
    </button>
  );
};
