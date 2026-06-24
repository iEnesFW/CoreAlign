import { useTranslation } from 'react-i18next';
import type { QuoteStatus } from '@/features/quotes/model/quote.types';

const toneByStatus: Record<QuoteStatus, string> = {
  Draft: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-200',
  Sent: 'bg-info-100 text-info-700 dark:bg-info-900/40 dark:text-info-200',
  Accepted: 'bg-success-100 text-success-700 dark:bg-success-900/40 dark:text-success-200',
  Rejected: 'bg-danger-100 text-danger-700 dark:bg-danger-900/40 dark:text-danger-200',
  Expired: 'bg-warning-100 text-warning-800 dark:bg-warning-900/40 dark:text-warning-200',
};

export const QuoteStatusBadge = ({ status }: { status: QuoteStatus }) => {
  const { t } = useTranslation();
  return (
    <span
      className={`inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-medium ${toneByStatus[status]}`}
    >
      {t(`quotes.status.${status}`)}
    </span>
  );
};
