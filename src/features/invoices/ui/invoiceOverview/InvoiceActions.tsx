import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { CheckCircle2, CreditCard, FileMinus, Printer, XCircle } from 'lucide-react';
import type { Invoice } from '@/features/invoices/model/invoice.types';

export const ActionsBar = ({
  invoice,
  showRecordPayment,
  onRecordPayment,
  onMarkPaid,
  onCancel,
  onIssueCreditNote,
}: {
  invoice: Invoice;
  showRecordPayment: boolean;
  onRecordPayment?: () => void;
  onMarkPaid?: () => void;
  onCancel?: () => void;
  onIssueCreditNote?: () => void;
}) => {
  const { t } = useTranslation();
  return (
    <div className="flex flex-col gap-2 sm:flex-row sm:flex-wrap">
      <Link
        to={`/invoices/${invoice.id}/print`}
        target="_blank"
        rel="noopener noreferrer"
        className="inline-flex flex-1 min-w-[140px] items-center justify-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
      >
        <Printer size={14} />
        {t('invoices.actions.print')}
      </Link>
      {showRecordPayment && (
        <button
          type="button"
          onClick={onRecordPayment}
          className="inline-flex flex-1 min-w-[140px] items-center justify-center gap-2 rounded-lg border border-violet-300 bg-violet-50 px-3 py-2 text-sm font-medium text-violet-700 hover:bg-violet-100 dark:border-violet-500/40 dark:bg-violet-500/10 dark:text-violet-300 dark:hover:bg-violet-500/20"
        >
          <CreditCard size={14} />
          {t('invoices.actions.recordPayment')}
        </button>
      )}
      {onMarkPaid && (
        <button
          type="button"
          onClick={onMarkPaid}
          className="inline-flex flex-1 min-w-[140px] items-center justify-center gap-2 rounded-lg border border-success-300 bg-success-50 px-3 py-2 text-sm font-medium text-success-700 hover:bg-success-100 dark:border-success-500/40 dark:bg-success-500/10 dark:text-success-300 dark:hover:bg-success-500/20"
        >
          <CheckCircle2 size={14} />
          {t('invoices.actions.markPaid')}
        </button>
      )}
      {onIssueCreditNote && (
        <button
          type="button"
          onClick={onIssueCreditNote}
          className="inline-flex flex-1 min-w-[140px] items-center justify-center gap-2 rounded-lg border border-amber-300 bg-amber-50 px-3 py-2 text-sm font-medium text-amber-700 hover:bg-amber-100 dark:border-amber-500/40 dark:bg-amber-500/10 dark:text-amber-300 dark:hover:bg-amber-500/20"
        >
          <FileMinus size={14} />
          {t('invoices.actions.issueCreditNote')}
        </button>
      )}
      {onCancel && (
        <button
          type="button"
          onClick={onCancel}
          className="inline-flex flex-1 min-w-[140px] items-center justify-center gap-2 rounded-lg border border-danger-300 bg-danger-50 px-3 py-2 text-sm font-medium text-danger-700 hover:bg-danger-100 dark:border-danger-500/40 dark:bg-danger-500/10 dark:text-danger-300 dark:hover:bg-danger-500/20"
        >
          <XCircle size={14} />
          {t('invoices.actions.cancel')}
        </button>
      )}
    </div>
  );
};
