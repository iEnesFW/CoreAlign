import React from 'react';
import { cn } from '@/shared/lib/cn';

type CardVariant = 'default' | 'elevated' | 'ghost';
type CardPadding = 'none' | 'sm' | 'md' | 'lg';

interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: CardVariant;
  padding?: CardPadding;
}

const variantClasses: Record<CardVariant, string> = {
  default:
    'border border-slate-200/70 bg-white shadow-[0_1px_2px_rgba(15,23,42,0.04)] dark:border-white/10 dark:bg-slate-900',
  elevated:
    'border border-slate-200/70 bg-white shadow-lg shadow-slate-900/[0.06] dark:border-white/10 dark:bg-slate-900 dark:shadow-black/20',
  ghost: 'border border-transparent bg-transparent',
};

const paddingClasses: Record<CardPadding, string> = {
  none: '',
  sm: 'p-3',
  md: 'p-4 sm:p-5',
  lg: 'p-6',
};

export const Card = ({ variant = 'default', padding = 'md', className, ...props }: CardProps) => (
  <div
    className={cn('rounded-xl', variantClasses[variant], paddingClasses[padding], className)}
    {...props}
  />
);

export const CardHeader = ({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) => (
  <div
    className={cn(
      'flex items-start justify-between gap-3 border-b border-slate-200/70 px-4 py-3 sm:px-5 dark:border-white/5',
      className,
    )}
    {...props}
  />
);

export const CardTitle = ({ className, ...props }: React.HTMLAttributes<HTMLHeadingElement>) => (
  <h3
    className={cn('text-sm font-semibold text-slate-900 dark:text-slate-100', className)}
    {...props}
  />
);

export const CardDescription = ({
  className,
  ...props
}: React.HTMLAttributes<HTMLParagraphElement>) => (
  <p className={cn('text-xs text-slate-500 dark:text-slate-400', className)} {...props} />
);

export const CardBody = ({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) => (
  <div className={cn('px-4 py-4 sm:px-5', className)} {...props} />
);

export const CardFooter = ({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) => (
  <div
    className={cn(
      'flex items-center justify-end gap-2 border-t border-slate-200/70 px-4 py-3 sm:px-5 dark:border-white/5',
      className,
    )}
    {...props}
  />
);
