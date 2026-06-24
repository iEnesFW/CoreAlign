import { cn } from './cn';

export const fieldBaseClasses = (error?: boolean) =>
  cn(
    'h-10 w-full rounded-lg border bg-white px-3 text-sm text-slate-900 shadow-sm outline-none transition placeholder:text-slate-400 focus-visible:ring-2 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-slate-900 dark:text-slate-100 dark:placeholder:text-slate-500',
    error
      ? 'border-danger-400 focus-visible:border-danger-500 focus-visible:ring-danger-500/30'
      : 'border-slate-300 hover:border-slate-400 focus-visible:border-primary-500 focus-visible:ring-primary-500/30 dark:border-slate-700 dark:hover:border-slate-600',
  );
