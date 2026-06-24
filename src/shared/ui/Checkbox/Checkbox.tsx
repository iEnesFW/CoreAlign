import React, { forwardRef } from 'react';
import { cn } from '@/shared/lib/cn';

interface CheckboxProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label?: React.ReactNode;
}

export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(
  ({ className, label, id, name, ...props }, ref) => {
    const checkboxId = id ?? name;
    const input = (
      <input
        ref={ref}
        id={checkboxId}
        name={name}
        type="checkbox"
        className={cn(
          'h-4 w-4 shrink-0 rounded border-slate-300 accent-primary-600 focus-visible:ring-2 focus-visible:ring-primary-500/40 disabled:cursor-not-allowed disabled:opacity-60 dark:border-slate-600 dark:bg-slate-900',
          className,
        )}
        {...props}
      />
    );

    if (!label) return input;

    return (
      <label
        htmlFor={checkboxId}
        className="inline-flex cursor-pointer select-none items-center gap-2 text-sm text-slate-700 dark:text-slate-300"
      >
        {input}
        {label}
      </label>
    );
  },
);

Checkbox.displayName = 'Checkbox';
