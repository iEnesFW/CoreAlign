import React, { forwardRef, useId } from 'react';
import { cn } from '@/shared/lib/cn';
import { fieldBaseClasses } from '@/shared/lib/fieldClasses';
import { Label } from '@/shared/ui/Label/Label';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  leftIcon?: React.ReactNode;
  /**
   * Optional interactive node pinned to the right of the field (e.g. a
   * password show/hide toggle). Additive + backward compatible: when omitted
   * the field renders exactly as before.
   */
  rightSlot?: React.ReactNode;
  required?: boolean;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ className, label, error, leftIcon, rightSlot, required, id, name, ...props }, ref) => {
    const reactId = useId();
    const inputId = id ?? name ?? reactId;
    return (
      <div className={cn('flex w-full flex-col gap-1.5', className)}>
        {label && (
          <Label htmlFor={inputId} required={required}>
            {label}
          </Label>
        )}
        <div className="relative flex items-center">
          {leftIcon && (
            <span className="pointer-events-none absolute left-3 flex items-center text-slate-400 dark:text-slate-500">
              {leftIcon}
            </span>
          )}
          <input
            ref={ref}
            id={inputId}
            name={name}
            aria-invalid={error ? true : undefined}
            className={cn(
              fieldBaseClasses(Boolean(error)),
              leftIcon && 'pl-9',
              rightSlot && 'pr-11',
            )}
            {...props}
          />
          {rightSlot && <span className="absolute right-1.5 flex items-center">{rightSlot}</span>}
        </div>
        {error && <span className="text-xs text-danger-600 dark:text-danger-400">{error}</span>}
      </div>
    );
  },
);

Input.displayName = 'Input';
