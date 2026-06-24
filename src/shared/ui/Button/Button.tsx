import React from 'react';
import { cn } from '@/shared/lib/cn';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
}

const baseClasses =
  'relative inline-flex select-none items-center justify-center gap-2 whitespace-nowrap rounded-lg font-medium transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-1 active:translate-y-0 active:scale-[0.99] disabled:pointer-events-none disabled:opacity-60 dark:focus-visible:ring-offset-slate-900';

const variantClasses: Record<NonNullable<ButtonProps['variant']>, string> = {
  primary:
    'bg-primary-600 text-white shadow-sm shadow-primary-600/30 ring-1 ring-inset ring-white/10 hover:-translate-y-px hover:bg-primary-500 hover:shadow-md hover:shadow-primary-600/40',
  secondary:
    'border border-slate-200 bg-slate-100 text-slate-900 shadow-sm hover:border-slate-300 hover:bg-slate-200 dark:border-white/10 dark:bg-white/5 dark:text-slate-100 dark:hover:border-white/20 dark:hover:bg-white/10',
  outline:
    'border border-slate-300 bg-white/60 text-slate-700 hover:border-primary-400 hover:bg-primary-50/50 hover:text-primary-600 dark:border-white/10 dark:bg-white/5 dark:text-slate-200 dark:hover:border-primary-400/60 dark:hover:bg-primary-500/10 dark:hover:text-primary-300',
  ghost:
    'text-slate-600 hover:bg-slate-100 hover:text-slate-900 dark:text-slate-300 dark:hover:bg-white/5 dark:hover:text-white',
  danger:
    'bg-danger-600 text-white shadow-sm shadow-danger-600/30 ring-1 ring-inset ring-white/10 hover:-translate-y-px hover:bg-danger-500 hover:shadow-md hover:shadow-danger-600/40',
};

const sizeClasses: Record<NonNullable<ButtonProps['size']>, string> = {
  sm: 'h-8 px-3 text-sm',
  md: 'h-10 px-4 text-sm',
  lg: 'h-12 px-6 text-base',
};

export const Button: React.FC<ButtonProps> = ({
  className,
  variant = 'primary',
  size = 'md',
  isLoading,
  children,
  ...props
}) => {
  return (
    <button
      className={cn(baseClasses, variantClasses[variant], sizeClasses[size], className)}
      disabled={isLoading || props.disabled}
      {...props}
    >
      {isLoading && (
        <span
          aria-hidden="true"
          className="absolute inline-block h-4 w-4 animate-spin rounded-full border-2 border-current border-r-transparent"
        />
      )}
      <span className={cn('inline-flex items-center gap-2', isLoading && 'invisible')}>
        {children}
      </span>
    </button>
  );
};
