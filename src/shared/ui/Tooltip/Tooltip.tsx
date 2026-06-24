import React from 'react';
import { cn } from '@/shared/lib/cn';

type TooltipSide = 'top' | 'bottom' | 'left' | 'right';

interface TooltipProps {
  label: React.ReactNode;
  children: React.ReactNode;
  side?: TooltipSide;
  className?: string;
}

const sideClasses: Record<TooltipSide, string> = {
  top: 'bottom-full left-1/2 mb-1.5 -translate-x-1/2',
  bottom: 'top-full left-1/2 mt-1.5 -translate-x-1/2',
  left: 'right-full top-1/2 mr-1.5 -translate-y-1/2',
  right: 'left-full top-1/2 ml-1.5 -translate-y-1/2',
};

export const Tooltip = ({ label, children, side = 'top', className }: TooltipProps) => (
  <span className="group/tooltip relative inline-flex">
    {children}
    <span
      role="tooltip"
      className={cn(
        'pointer-events-none absolute z-50 whitespace-nowrap rounded-md bg-slate-900 px-2 py-1 text-[11px] font-medium text-white opacity-0 shadow-lg transition-opacity duration-150 group-hover/tooltip:opacity-100 group-focus-within/tooltip:opacity-100 dark:bg-slate-700',
        sideClasses[side],
        className,
      )}
    >
      {label}
    </span>
  </span>
);
