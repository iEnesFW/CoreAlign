import { Check, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
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
    'border-primary-300/80 bg-primary-50 text-primary-700 ring-1 ring-inset ring-primary-200/60 dark:border-primary-500/40 dark:bg-primary-500/15 dark:text-primary-200 dark:ring-primary-500/30',
  emerald:
    'border-success-300/80 bg-success-50 text-success-700 ring-1 ring-inset ring-success-200/60 dark:border-success-500/40 dark:bg-success-500/15 dark:text-success-200',
  amber:
    'border-warning-300/80 bg-warning-50 text-warning-800 ring-1 ring-inset ring-warning-200/60 dark:border-warning-500/40 dark:bg-warning-500/15 dark:text-warning-200',
  rose: 'border-danger-300/80 bg-danger-50 text-danger-700 ring-1 ring-inset ring-danger-200/60 dark:border-danger-500/40 dark:bg-danger-500/15 dark:text-danger-200',
  sky: 'border-info-300/80 bg-info-50 text-info-700 ring-1 ring-inset ring-info-200/60 dark:border-info-500/40 dark:bg-info-500/15 dark:text-info-200',
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
  const { t } = useTranslation();
  const base = cn(
    'group inline-flex items-center gap-1.5 rounded-full border font-medium transition-all duration-200',
    size === 'sm' ? 'px-2 py-0.5 text-[10px]' : 'px-2.5 py-1 text-[11px]',
    active
      ? activeToneClasses[tone]
      : 'border-slate-200 bg-white text-slate-600 hover:border-primary-300 hover:bg-primary-50/50 hover:text-primary-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300 dark:hover:border-primary-500/40 dark:hover:bg-primary-500/10 dark:hover:text-primary-200',
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
          aria-label={t('Common.RemoveFilter', { defaultValue: 'Remove filter' })}
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
