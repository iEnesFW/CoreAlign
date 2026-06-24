import React from 'react';
import { cn } from '@/shared/lib/cn';
import { statusToneClass, type StatusTone } from '@/shared/lib/statusStyles';

export type BadgeVariant =
  | 'default'
  | 'primary'
  | 'success'
  | 'warning'
  | 'danger'
  | 'error'
  | 'info'
  | 'neutral'
  | 'accent';

const variantTone: Record<BadgeVariant, StatusTone> = {
  default: 'primary',
  primary: 'primary',
  success: 'success',
  warning: 'warning',
  danger: 'danger',
  error: 'danger',
  info: 'info',
  neutral: 'neutral',
  accent: 'accent',
};

export interface BadgeProps {
  variant?: BadgeVariant;
  children: React.ReactNode;
  className?: string;
  pill?: boolean;
}

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
        pill ? 'rounded-full' : 'rounded-md',
        statusToneClass[variantTone[variant]],
        className,
      )}
    >
      {children}
    </span>
  );
};
