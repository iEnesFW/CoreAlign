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
      'bg-success-50 dark:bg-success-500/10 border-success-200 dark:border-success-500/20',
    iconClass: 'text-success-600 dark:text-success-400',
    titleClass: 'text-success-800 dark:text-success-300',
    messageClass: 'text-success-600 dark:text-success-400/80',
    defaultTitleKey: 'common.success',
  },
  info: {
    icon: Info,
    containerClass:
      'bg-primary-50 dark:bg-primary-500/10 border-primary-200 dark:border-primary-500/20',
    iconClass: 'text-primary-600 dark:text-primary-400',
    titleClass: 'text-primary-800 dark:text-primary-300',
    messageClass: 'text-primary-600 dark:text-primary-400/80',
    defaultTitleKey: 'common.info',
  },
  warning: {
    icon: AlertTriangle,
    containerClass:
      'bg-warning-50 dark:bg-warning-500/10 border-warning-200 dark:border-warning-500/20',
    iconClass: 'text-warning-600 dark:text-warning-400',
    titleClass: 'text-warning-800 dark:text-warning-300',
    messageClass: 'text-warning-600 dark:text-warning-400/80',
    defaultTitleKey: 'common.warning',
  },
  error: {
    icon: AlertCircle,
    containerClass:
      'bg-danger-50 dark:bg-danger-500/10 border-danger-200 dark:border-danger-500/20',
    iconClass: 'text-danger-600 dark:text-danger-400',
    titleClass: 'text-danger-800 dark:text-danger-300',
    messageClass: 'text-danger-600 dark:text-danger-400/80',
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
