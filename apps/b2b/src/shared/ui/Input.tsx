import { forwardRef, type InputHTMLAttributes } from 'react';
import { cn } from '@/shared/lib/cn';

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  hint?: string;
  error?: string;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, hint, error, className, id, ...rest }, ref) => {
    const inputId = id ?? rest.name;
    return (
      <div className="flex flex-col gap-1.5">
        {label ? (
          <label
            htmlFor={inputId}
            className="text-sm font-medium text-slate-700 dark:text-slate-200"
          >
            {label}
          </label>
        ) : null}
        <input
          ref={ref}
          id={inputId}
          className={cn(
            'h-11 w-full rounded-xl border border-slate-200 bg-white px-3.5 text-sm text-slate-900 shadow-sm transition focus-visible:border-amber-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-amber-200 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:focus-visible:ring-amber-900/60',
            error && 'border-rose-400 focus-visible:border-rose-500 focus-visible:ring-rose-200',
            className,
          )}
          aria-invalid={!!error}
          {...rest}
        />
        {hint && !error ? (
          <p className="text-xs text-slate-500 dark:text-slate-400">{hint}</p>
        ) : null}
        {error ? <p className="text-xs text-rose-600 dark:text-rose-400">{error}</p> : null}
      </div>
    );
  },
);
Input.displayName = 'Input';
