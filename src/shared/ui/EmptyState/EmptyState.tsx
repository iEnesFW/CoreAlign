import { cn } from '@/shared/lib/cn';

interface Props {
  icon?: React.ReactNode;
  title: string;
  description?: string;
  action?: React.ReactNode;
  secondary?: React.ReactNode;
  className?: string;
  variant?: 'card' | 'plain';
}

export const EmptyState = ({
  icon,
  title,
  description,
  action,
  secondary,
  className,
  variant = 'card',
}: Props) => {
  return (
    <div
      className={cn(
        variant === 'card'
          ? 'rounded-xl border border-dashed border-slate-300 bg-slate-50/50 px-6 py-10 dark:border-slate-700 dark:bg-slate-900/40'
          : 'px-6 py-10',
        'flex flex-col items-center justify-center text-center animate-fade-up',
        className,
      )}
    >
      {icon && (
        <div className="mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-gradient-to-br from-primary-500/15 to-purple-500/15 text-primary-500 ring-1 ring-primary-200/60 dark:text-primary-300 dark:ring-primary-500/30">
          {icon}
        </div>
      )}
      <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">{title}</h3>
      {description && (
        <p className="mt-1 max-w-sm text-xs text-slate-500 dark:text-slate-400">{description}</p>
      )}
      {(action || secondary) && (
        <div className="mt-4 flex items-center gap-2">
          {action}
          {secondary}
        </div>
      )}
    </div>
  );
};
