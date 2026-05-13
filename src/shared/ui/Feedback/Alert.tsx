import React from 'react';
import { useTranslation } from 'react-i18next';
import { CheckCircle2, Info, AlertTriangle, AlertCircle } from 'lucide-react';
import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export type AlertVariant = 'success' | 'info' | 'warning' | 'error';

export interface AlertProps {
  variant: AlertVariant;
  title?: string;
  message: string;
  className?: string;
}

const variantConfig = {
  success: {
    icon: CheckCircle2,
    containerClass:
      'bg-emerald-50 dark:bg-emerald-500/10 border-emerald-200 dark:border-emerald-500/20',
    iconClass: 'text-emerald-600 dark:text-emerald-400',
    titleClass: 'text-emerald-800 dark:text-emerald-300',
    messageClass: 'text-emerald-600 dark:text-emerald-400/80',
    defaultTitleKey: 'common.success',
  },
  info: {
    icon: Info,
    containerClass: 'bg-blue-50 dark:bg-blue-500/10 border-blue-200 dark:border-blue-500/20',
    iconClass: 'text-blue-600 dark:text-blue-400',
    titleClass: 'text-blue-800 dark:text-blue-300',
    messageClass: 'text-blue-600 dark:text-blue-400/80',
    defaultTitleKey: 'common.info',
  },
  warning: {
    icon: AlertTriangle,
    containerClass: 'bg-amber-50 dark:bg-amber-500/10 border-amber-200 dark:border-amber-500/20',
    iconClass: 'text-amber-600 dark:text-amber-400',
    titleClass: 'text-amber-800 dark:text-amber-300',
    messageClass: 'text-amber-600 dark:text-amber-400/80',
    defaultTitleKey: 'common.warning',
  },
  error: {
    icon: AlertCircle,
    containerClass: 'bg-red-50 dark:bg-red-500/10 border-red-200 dark:border-red-500/20',
    iconClass: 'text-red-600 dark:text-red-400',
    titleClass: 'text-red-800 dark:text-red-300',
    messageClass: 'text-red-600 dark:text-red-400/80',
    defaultTitleKey: 'common.error',
  },
};

export const Alert: React.FC<AlertProps> = ({ variant, title, message, className }) => {
  const { t } = useTranslation();
  const config = variantConfig[variant];
  const Icon = config.icon;

  const displayTitle =
    title ||
    t(
      config.defaultTitleKey as
        | 'common.success'
        | 'common.info'
        | 'common.warning'
        | 'common.error',
    );

  return (
    <div
      className={cn(
        'flex items-start gap-2.5 p-3 rounded-[5px] border',
        config.containerClass,
        className,
      )}
    >
      <Icon size={16} className={cn('mt-0.5 shrink-0', config.iconClass)} />
      <div>
        <h4 className={cn('text-xs font-semibold', config.titleClass)}>{displayTitle}</h4>
        <p className={cn('text-[11px] mt-0.5', config.messageClass)}>{message}</p>
      </div>
    </div>
  );
};
