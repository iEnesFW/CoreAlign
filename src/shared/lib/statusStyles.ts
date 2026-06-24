export type StatusTone =
  | 'primary'
  | 'success'
  | 'warning'
  | 'danger'
  | 'info'
  | 'neutral'
  | 'accent';

export const statusToneClass: Record<StatusTone, string> = {
  primary: 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300',
  success: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
  warning: 'bg-warning-100 text-warning-700 dark:bg-warning-500/20 dark:text-warning-300',
  danger: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
  info: 'bg-info-100 text-info-700 dark:bg-info-500/20 dark:text-info-300',
  neutral: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
  accent: 'bg-accent-100 text-accent-700 dark:bg-accent-500/20 dark:text-accent-300',
};
