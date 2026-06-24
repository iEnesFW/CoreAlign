import { useTranslation } from 'react-i18next';
import { CheckCircle2, Mail, Send } from 'lucide-react';
import type { Invoice } from '@/features/invoices/model/invoice.types';
import { type DunningLevel, fmtDate } from './ledgerModel';

export const RemindersCard = ({
  invoice,
  dunningLevel,
  locale,
}: {
  invoice: Invoice;
  dunningLevel: DunningLevel;
  locale: string;
}) => {
  const { t } = useTranslation();
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <Mail size={12} />
        {t('invoices.ledger.reminders.title')}
      </header>
      <div className="mt-2 grid grid-cols-1 gap-2 sm:grid-cols-3 text-[11px]">
        <ReminderStep
          label={t('invoices.ledger.reminders.first')}
          sent={invoice.sentAtUtc !== null}
          dateLabel={invoice.sentAtUtc ? fmtDate(invoice.sentAtUtc, locale) : null}
        />
        <ReminderStep
          label={t('invoices.ledger.reminders.followUp')}
          sent={dunningLevel.level >= 2}
          dateLabel={
            dunningLevel.level >= 2
              ? t('invoices.ledger.reminders.suggested', { defaultValue: 'Suggested now' })
              : null
          }
        />
        <ReminderStep
          label={t('invoices.ledger.reminders.final')}
          sent={dunningLevel.level >= 3}
          dateLabel={
            dunningLevel.level >= 3
              ? t('invoices.ledger.reminders.escalate', { defaultValue: 'Escalate to collections' })
              : null
          }
        />
      </div>
      <div className="mt-2 flex items-center justify-between text-[10px] text-slate-500 dark:text-slate-400">
        <span>{t('invoices.ledger.reminders.note')}</span>
        <button
          type="button"
          disabled
          className="inline-flex items-center gap-1 rounded-md border border-slate-200 bg-slate-50 px-2 py-1 text-[10px] font-medium text-slate-400 dark:border-slate-800 dark:bg-slate-800/50"
        >
          <Send size={10} />
          {t('invoices.ledger.reminders.sendCta')}
        </button>
      </div>
    </section>
  );
};

const ReminderStep = ({
  label,
  sent,
  dateLabel,
}: {
  label: string;
  sent: boolean;
  dateLabel: string | null;
}) => (
  <div
    className={`flex items-start gap-2 rounded border p-1.5 ${sent ? 'border-success-200 bg-success-50/30 dark:border-success-500/30 dark:bg-success-500/10' : 'border-slate-200 dark:border-slate-800'}`}
  >
    <span
      className={`mt-0.5 inline-flex h-4 w-4 shrink-0 items-center justify-center rounded-full ${sent ? 'bg-success-500 text-white' : 'border border-slate-300 dark:border-slate-700'}`}
    >
      {sent && <CheckCircle2 size={10} />}
    </span>
    <div className="min-w-0">
      <div className="font-medium text-slate-900 dark:text-slate-100">{label}</div>
      {dateLabel && (
        <div className="text-[10px] text-slate-500 dark:text-slate-400">{dateLabel}</div>
      )}
    </div>
  </div>
);
