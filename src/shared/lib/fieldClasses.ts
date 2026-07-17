import { cn } from './cn';

export const fieldBaseClasses = (error?: boolean) =>
  cn(
    'h-10 w-full rounded-xl border bg-white/60 px-4 text-sm text-slate-900 shadow-sm backdrop-blur-md outline-none transition-all placeholder:text-slate-400 focus-visible:bg-white focus-visible:ring-2 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-slate-900/60 dark:text-slate-100 dark:placeholder:text-slate-500 dark:focus-visible:bg-slate-900',
    error
      ? 'border-red-300 focus-visible:border-red-500 focus-visible:ring-red-500/20'
      : 'border-slate-200 hover:border-slate-300 focus-visible:border-indigo-500 focus-visible:ring-indigo-500/20 dark:border-slate-700/50 dark:hover:border-slate-600',
  );
