import React, { forwardRef, useId } from 'react';
import { cn } from '@/shared/lib/cn';
import { fieldBaseClasses } from '@/shared/lib/fieldClasses';
import { Label } from '@/shared/ui/Label/Label';

interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  error?: string;
  required?: boolean;
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, label, error, required, id, name, rows = 3, ...props }, ref) => {
    const reactId = useId();
    const textareaId = id ?? name ?? reactId;
    return (
      <div className={cn('flex w-full flex-col gap-1.5', className)}>
        {label && (
          <Label htmlFor={textareaId} required={required}>
            {label}
          </Label>
        )}
        <textarea
          ref={ref}
          id={textareaId}
          name={name}
          rows={rows}
          aria-invalid={error ? true : undefined}
          className={cn(
            fieldBaseClasses(Boolean(error)),
            'h-auto min-h-[80px] resize-y py-2 leading-relaxed',
          )}
          {...props}
        />
        {error && <span className="text-xs text-danger-600 dark:text-danger-400">{error}</span>}
      </div>
    );
  },
);

Textarea.displayName = 'Textarea';
