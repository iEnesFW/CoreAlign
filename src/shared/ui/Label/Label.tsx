import React from 'react';
import { cn } from '@/shared/lib/cn';

interface LabelProps extends React.LabelHTMLAttributes<HTMLLabelElement> {
  required?: boolean;
}

export const Label: React.FC<LabelProps> = ({ className, required, children, ...props }) => (
  <label
    className={cn('text-sm font-medium text-slate-700 dark:text-slate-300', className)}
    {...props}
  >
    {children}
    {required && (
      <span className="ml-0.5 text-danger-500" aria-hidden="true">
        *
      </span>
    )}
  </label>
);
