import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, Calendar, CalendarClock } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge/Badge';
import type { Invoice } from '@/features/invoices/model/invoice.types';
import {
  type DunningLevel,
  dunningToneBadge,
  dunningToneBg,
  dunningToneText,
  fmtCurrency,
  fmtDate,
} from './ledgerModel';

export const DunningCard = ({
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
    <section
      className={`rounded-lg border bg-white p-3 dark:bg-slate-900 ${dunningToneBg[dunningLevel.tone]}`}
    >
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <AlertTriangle size={12} />
          {t('invoices.ledger.dunning.title')}
        </span>
        <Badge variant={dunningToneBadge[dunningLevel.tone]} pill>
          {dunningLevel.level === 0
            ? t('invoices.ledger.dunning.clear')
            : t(`invoices.ledger.dunning.level${dunningLevel.level}`, {
                defaultValue: dunningLevel.label,
              })}
        </Badge>
      </header>
      <div className="mt-2 grid grid-cols-2 gap-2 sm:grid-cols-4 text-[11px]">
        <Stat
          label={t('invoices.fields.dueDate')}
          value={fmtDate(invoice.dueDate, locale)}
          icon={<CalendarClock size={11} />}
        />
        <Stat
          label={t('invoices.ledger.dunning.daysOverdue')}
          value={dunningLevel.daysPastDue > 0 ? `${dunningLevel.daysPastDue}` : '0'}
          tone={dunningLevel.tone}
          icon={<Calendar size={11} />}
        />
        <Stat
          label={t('invoices.detail.metrics.due')}
          value={fmtCurrency(invoice.amountDue, invoice.currency, locale)}
          tone={invoice.amountDue > 0 ? 'amber' : 'emerald'}
        />
        <Stat
          label={t('invoices.ledger.dunning.recommended')}
          value={
            dunningLevel.level === 0
              ? t('invoices.ledger.dunning.noActionNeeded')
              : t(`invoices.ledger.dunning.action${dunningLevel.level}`, {
                  defaultValue: 'Send reminder',
                })
          }
          tone={dunningLevel.tone}
        />
      </div>
      {dunningLevel.level > 0 && (
        <div
          className={`mt-2 rounded border border-dashed ${dunningToneBg[dunningLevel.tone]} p-2 text-[11px] ${dunningToneText[dunningLevel.tone]}`}
        >
          {t('invoices.ledger.dunning.guidance', {
            count: dunningLevel.daysPastDue,
            defaultValue:
              'Invoice is past due by {{count}} days. Consider issuing a reminder or escalating to collections.',
          })}
        </div>
      )}
    </section>
  );
};

const statToneText: Record<'slate' | 'amber' | 'orange' | 'red' | 'emerald' | 'indigo', string> = {
  slate: 'text-slate-900 dark:text-slate-100',
  amber: 'text-warning-600 dark:text-warning-400',
  orange: 'text-warning-600 dark:text-warning-400',
  red: 'text-danger-600 dark:text-danger-400',
  emerald: 'text-success-600 dark:text-success-400',
  indigo: 'text-primary-600 dark:text-primary-400',
};

const Stat = ({
  label,
  value,
  icon,
  tone = 'slate',
}: {
  label: string;
  value: string;
  icon?: ReactNode;
  tone?: keyof typeof statToneText;
}) => (
  <div className="rounded border border-slate-200 px-2 py-1.5 dark:border-slate-800">
    <div className="flex items-center gap-1 text-[9px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {icon}
      <span>{label}</span>
    </div>
    <div className={`mt-0.5 text-sm font-bold tabular-nums ${statToneText[tone]}`}>{value}</div>
  </div>
);
