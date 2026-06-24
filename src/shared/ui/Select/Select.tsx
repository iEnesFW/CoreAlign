import React, { forwardRef, useId } from 'react';
import { ChevronDown } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { fieldBaseClasses } from '@/shared/lib/fieldClasses';
import { Label } from '@/shared/ui/Label/Label';

interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  error?: string;
  required?: boolean;
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ className, label, error, required, id, name, children, ...props }, ref) => {
    const reactId = useId();
    const selectId = id ?? name ?? reactId;
    return (
      <div className={cn('flex w-full flex-col gap-1.5', className)}>
        {label && (
          <Label htmlFor={selectId} required={required}>
            {label}
          </Label>
        )}
        <div className="relative">
          <select
            ref={ref}
            id={selectId}
            name={name}
            aria-invalid={error ? true : undefined}
            className={cn(fieldBaseClasses(Boolean(error)), 'appearance-none pr-9')}
            {...props}
          >
            {children}
          </select>
          <ChevronDown
            size={16}
            aria-hidden="true"
            className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 dark:text-slate-500"
          />
        </div>
        {error && <span className="text-xs text-danger-600 dark:text-danger-400">{error}</span>}
      </div>
    );
  },
);

Select.displayName = 'Select';
