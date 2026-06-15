import { useTranslation } from 'react-i18next';
import type { QuoteStatus } from '@/features/quotes/model/quote.types';

const toneByStatus: Record<QuoteStatus, string> = {
  Draft: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-200',
  Sent: 'bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-200',
  Accepted: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-200',
  Rejected: 'bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-200',
  Expired: 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-200',
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
