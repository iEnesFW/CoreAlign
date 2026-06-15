import { type ReactNode } from 'react';
import { cn } from '@/shared/lib/cn';

export const Card = ({ children, className }: { children: ReactNode; className?: string }) => (
  <div
    className={cn(
      'rounded-2xl border border-slate-100 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900',
      className,
    )}
  >
    {children}
  </div>
);

export const CardHeader = ({
  title,
  subtitle,
  action,
}: {
  title: ReactNode;
  subtitle?: ReactNode;
  action?: ReactNode;
}) => (
  <div className="flex items-start justify-between gap-3 border-b border-slate-100 px-6 py-4 dark:border-slate-800">
    <div>
      <h3 className="text-base font-semibold text-slate-900 dark:text-slate-100">{title}</h3>
      {subtitle ? (
        <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">{subtitle}</p>
      ) : null}
    </div>
    {action ? <div className="flex-shrink-0">{action}</div> : null}
  </div>
);

export const CardBody = ({ children, className }: { children: ReactNode; className?: string }) => (
  <div className={cn('px-6 py-5', className)}>{children}</div>
);
