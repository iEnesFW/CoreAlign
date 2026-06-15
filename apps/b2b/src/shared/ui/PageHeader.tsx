import { type ReactNode } from 'react';

export const PageHeader = ({
  title,
  subtitle,
  action,
}: {
  title: ReactNode;
  subtitle?: ReactNode;
  action?: ReactNode;
}) => (
  <div className="flex flex-col gap-3 border-b border-slate-100 pb-5 sm:flex-row sm:items-center sm:justify-between dark:border-slate-800">
    <div>
      <h1 className="text-2xl font-semibold tracking-tight text-slate-900 dark:text-slate-100">
        {title}
      </h1>
      {subtitle ? (
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{subtitle}</p>
      ) : null}
    </div>
    {action ? <div>{action}</div> : null}
  </div>
);
