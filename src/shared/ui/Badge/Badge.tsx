import React from 'react';
import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export type BadgeVariant = 'default' | 'success' | 'warning' | 'error' | 'neutral';

export interface BadgeProps {
  variant?: BadgeVariant;
  children: React.ReactNode;
  className?: string;
  pill?: boolean;
}

const variantClassMap: Record<BadgeVariant, string> = {
  default: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-400',
  success: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-400',
  warning: 'bg-amber-100 text-amber-700 dark:bg-amber-500/20 dark:text-amber-400',
  error: 'bg-red-100 text-red-700 dark:bg-red-500/20 dark:text-red-400',
  neutral: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
};

export const Badge: React.FC<BadgeProps> = ({
  variant = 'default',
  children,
  className,
  pill = false,
}) => {
  return (
    <span
      className={cn(
        'inline-flex items-center px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider',
        pill ? 'rounded-full' : 'rounded-[3px]',
        variantClassMap[variant],
        className,
      )}
    >
      {children}
    </span>
  );
};
