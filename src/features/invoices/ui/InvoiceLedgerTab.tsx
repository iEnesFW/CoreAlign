import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  AlertTriangle,
  ArrowDownLeft,
  ArrowUpRight,
  BookOpen,
  Calendar,
  CalendarClock,
  CheckCircle2,
  CircleDot,
  ExternalLink,
  FileMinus,
  Mail,
  Send,
} from 'lucide-react';
import { Badge } from '@/shared/ui/Badge/Badge';
import { useCreditNotesForInvoice } from '@/features/invoices/hooks/useInvoiceQueries';
import type { Invoice } from '@/features/invoices/model/invoice.types';

interface Props {
  invoice: Invoice;
  locale: string;
}

interface GlEntry {
  account: string;
  description: string;
  debit: number;
  credit: number;
}

const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

const fmtDate = (iso: string | null, locale: string) => {
  if (!iso) return '—';
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(new Date(iso));
  } catch {
    return iso.slice(0, 10);
  }
};

const daysFromNow = (iso: string | null) => {
  if (!iso) return null;
  const target = new Date(iso).getTime();
  if (Number.isNaN(target)) return null;
  const dayMs = 1000 * 60 * 60 * 24;
  return Math.round((target - Date.now()) / dayMs);
};

const buildGlEntries = (invoice: Invoice): GlEntry[] => {
  const entries: GlEntry[] = [];
  const customerAcct = '120 — Accounts Receivable';
  const revenueAcct = '600 — Sales Revenue';
  const discountAcct = '611 — Sales Discounts';
  const taxAcct = '391 — VAT Output';
  const withholdingAcct = '193 — Withholding Tax';
  const shippingAcct = '623 — Shipping Revenue';

  entries.push({
    account: customerAcct,
    description: `${invoice.invoiceNumber} — ${invoice.customerName}`,
    debit: invoice.total,
    credit: 0,
  });

  const netRevenue =
    invoice.taxableTotal ||
    invoice.subtotal - invoice.lineDiscountTotal - invoice.headerDiscountAmount;
  if (netRevenue > 0) {
    entries.push({
      account: revenueAcct,
      description: 'Net of discounts',
      debit: 0,
      credit: netRevenue,
    });
  }

  if (invoice.lineDiscountTotal + invoice.headerDiscountAmount > 0) {
    entries.push({
      account: discountAcct,
      description: 'Discounts granted',
      debit: invoice.lineDiscountTotal + invoice.headerDiscountAmount,
      credit: 0,
    });
  }

  if (invoice.taxTotal > 0) {
    entries.push({
      account: taxAcct,
      description: 'Output VAT',
      debit: 0,
      credit: invoice.taxTotal,
    });
  }

  if (invoice.withholdingTotal > 0) {
    entries.push({
      account: withholdingAcct,
      description: 'Withholding tax',
      debit: invoice.withholdingTotal,
      credit: 0,
    });
  }

  if (invoice.shippingCost > 0) {
    entries.push({
      account: shippingAcct,
      description: 'Shipping revenue',
      debit: 0,
      credit: invoice.shippingCost,
    });
  }

  return entries;
};

export const InvoiceLedgerTab = ({ invoice, locale }: Props) => {
  const glEntries = useMemo(() => buildGlEntries(invoice), [invoice]);
  const creditNotesQuery = useCreditNotesForInvoice(invoice.id);
  const creditNotes = creditNotesQuery.data?.data ?? [];
  const totalDebit = glEntries.reduce((s, e) => s + e.debit, 0);
  const totalCredit = glEntries.reduce((s, e) => s + e.credit, 0);
  const dunningLevel = computeDunningLevel(invoice);

  return (
    <div className="space-y-3">
      <DunningCard invoice={invoice} dunningLevel={dunningLevel} locale={locale} />
      <GlPostingCard
        invoice={invoice}
        entries={glEntries}
        totalDebit={totalDebit}
        totalCredit={totalCredit}
        locale={locale}
      />
      <LinkedDocumentsCard invoice={invoice} creditNotes={creditNotes} locale={locale} />
      <RemindersCard invoice={invoice} dunningLevel={dunningLevel} locale={locale} />
    </div>
  );
};

interface DunningLevel {
  level: 0 | 1 | 2 | 3;
  daysPastDue: number;
  tone: 'slate' | 'amber' | 'orange' | 'red';
  label: string;
}

const computeDunningLevel = (invoice: Invoice): DunningLevel => {
  if (
    invoice.amountDue <= 0 ||
    invoice.status === 'Paid' ||
    invoice.status === 'Cancelled' ||
    invoice.status === 'Void'
  ) {
    return { level: 0, daysPastDue: 0, tone: 'slate', label: 'Clear' };
  }
  const days = -(daysFromNow(invoice.dueDate) ?? 0);
  if (days <= 0) return { level: 0, daysPastDue: 0, tone: 'slate', label: 'Current' };
  if (days <= 14) return { level: 1, daysPastDue: days, tone: 'amber', label: 'Friendly reminder' };
  if (days <= 30) return { level: 2, daysPastDue: days, tone: 'orange', label: 'Second notice' };
  return { level: 3, daysPastDue: days, tone: 'red', label: 'Final notice / Collection' };
};

const dunningToneBg: Record<DunningLevel['tone'], string> = {
  slate: 'border-slate-200 dark:border-slate-800',
  amber: 'border-amber-300 dark:border-amber-500/40',
  orange: 'border-orange-300 dark:border-orange-500/40',
  red: 'border-red-300 dark:border-red-500/40',
};

const dunningToneText: Record<DunningLevel['tone'], string> = {
  slate: 'text-slate-700 dark:text-slate-200',
  amber: 'text-amber-700 dark:text-amber-400',
  orange: 'text-orange-700 dark:text-orange-400',
  red: 'text-red-700 dark:text-red-400',
};

const dunningToneBadge: Record<DunningLevel['tone'], 'neutral' | 'warning' | 'error'> = {
  slate: 'neutral',
  amber: 'warning',
  orange: 'warning',
  red: 'error',
};

const DunningCard = ({
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
  amber: 'text-amber-600 dark:text-amber-400',
  orange: 'text-orange-600 dark:text-orange-400',
  red: 'text-red-600 dark:text-red-400',
  emerald: 'text-emerald-600 dark:text-emerald-400',
  indigo: 'text-indigo-600 dark:text-indigo-400',
};

const Stat = ({
  label,
  value,
  icon,
  tone = 'slate',
}: {
  label: string;
  value: string;
  icon?: React.ReactNode;
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

const GlPostingCard = ({
  invoice,
  entries,
  totalDebit,
  totalCredit,
  locale,
}: {
  invoice: Invoice;
  entries: GlEntry[];
  totalDebit: number;
  totalCredit: number;
  locale: string;
}) => {
  const { t } = useTranslation();
  const balanced = Math.abs(totalDebit - totalCredit) < 0.01;
  return (
    <section className="rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 border-b border-slate-100 px-3 py-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:border-slate-800 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <BookOpen size={12} />
          {t('invoices.ledger.glTitle')}
        </span>
        <Badge variant={invoice.isPostedToLedger ? 'success' : 'neutral'} pill>
          {invoice.isPostedToLedger ? t('invoices.ledger.posted') : t('invoices.ledger.notPosted')}
        </Badge>
      </header>
      <table className="w-full text-left text-[11px]">
        <thead className="bg-slate-50 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/40 dark:text-slate-400">
          <tr>
            <th className="px-3 py-1.5">{t('invoices.ledger.account')}</th>
            <th className="px-3 py-1.5">{t('invoices.ledger.description')}</th>
            <th className="px-3 py-1.5 text-right">{t('invoices.ledger.debit')}</th>
            <th className="px-3 py-1.5 text-right">{t('invoices.ledger.credit')}</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
          {entries.map((e, i) => (
            <tr key={`${e.account}-${i}`}>
              <td className="px-3 py-1.5 font-mono text-[11px] text-slate-900 dark:text-slate-100">
                {e.account}
              </td>
              <td className="px-3 py-1.5 text-slate-700 dark:text-slate-300">{e.description}</td>
              <td className="px-3 py-1.5 text-right tabular-nums text-red-600 dark:text-red-400">
                {e.debit > 0 ? (
                  <>
                    <ArrowUpRight size={9} className="mr-1 inline" />
                    {fmtCurrency(e.debit, invoice.currency, locale)}
                  </>
                ) : (
                  '—'
                )}
              </td>
              <td className="px-3 py-1.5 text-right tabular-nums text-emerald-600 dark:text-emerald-400">
                {e.credit > 0 ? (
                  <>
                    <ArrowDownLeft size={9} className="mr-1 inline" />
                    {fmtCurrency(e.credit, invoice.currency, locale)}
                  </>
                ) : (
                  '—'
                )}
              </td>
            </tr>
          ))}
        </tbody>
        <tfoot className="bg-slate-50 dark:bg-slate-800/40">
          <tr>
            <td
              colSpan={2}
              className="px-3 py-2 text-right text-[10px] font-semibold uppercase text-slate-500 dark:text-slate-400"
            >
              {t('invoices.ledger.total')}
            </td>
            <td className="px-3 py-2 text-right font-bold tabular-nums text-slate-900 dark:text-slate-100">
              {fmtCurrency(totalDebit, invoice.currency, locale)}
            </td>
            <td className="px-3 py-2 text-right font-bold tabular-nums text-slate-900 dark:text-slate-100">
              {fmtCurrency(totalCredit, invoice.currency, locale)}
            </td>
          </tr>
          <tr>
            <td
              colSpan={4}
              className="border-t border-slate-200 px-3 py-1.5 text-right text-[10px] dark:border-slate-800"
            >
              {balanced ? (
                <span className="inline-flex items-center gap-1 text-emerald-600 dark:text-emerald-400">
                  <CheckCircle2 size={10} /> {t('invoices.ledger.balanced')}
                </span>
              ) : (
                <span className="inline-flex items-center gap-1 text-red-600 dark:text-red-400">
                  <CircleDot size={10} />
                  {t('invoices.ledger.unbalanced')}:{' '}
                  {fmtCurrency(totalDebit - totalCredit, invoice.currency, locale)}
                </span>
              )}
            </td>
          </tr>
        </tfoot>
      </table>
    </section>
  );
};

const LinkedDocumentsCard = ({
  invoice,
  creditNotes,
  locale,
}: {
  invoice: Invoice;
  creditNotes: Array<{
    id: string;
    invoiceNumber: string;
    total: number;
    currency: string;
    issueDate: string;
    status: string;
  }>;
  locale: string;
}) => {
  const { t } = useTranslation();
  const hasLinks = !!invoice.orderId || creditNotes.length > 0 || !!invoice.originInvoiceId;
  if (!hasLinks) return null;
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <FileMinus size={12} />
          {t('invoices.ledger.linkedDocs')}
        </span>
        <span className="text-slate-400">
          {(invoice.orderId ? 1 : 0) + creditNotes.length + (invoice.originInvoiceId ? 1 : 0)}
        </span>
      </header>
      <ul className="mt-2 space-y-1">
        {invoice.orderId && (
          <li>
            <Link
              to={`/dashboard/orders?selected=${invoice.orderId}`}
              className="flex items-center justify-between gap-2 rounded border border-slate-200 px-2 py-1.5 text-[11px] transition hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/50"
            >
              <span className="inline-flex items-center gap-1.5">
                <ArrowUpRight size={11} className="text-indigo-500" />
                <span className="text-slate-700 dark:text-slate-300">
                  {t('invoices.detail.linkedOrder')}
                </span>
              </span>
              <ExternalLink size={10} className="text-slate-400" />
            </Link>
          </li>
        )}
        {invoice.originInvoiceId && (
          <li>
            <Link
              to={`/dashboard/invoices?selected=${invoice.originInvoiceId}`}
              className="flex items-center justify-between gap-2 rounded border border-slate-200 px-2 py-1.5 text-[11px] transition hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/50"
            >
              <span className="inline-flex items-center gap-1.5">
                <ArrowUpRight size={11} className="text-indigo-500" />
                <span className="text-slate-700 dark:text-slate-300">
                  {t('invoices.ledger.originInvoice')}
                </span>
              </span>
              <ExternalLink size={10} className="text-slate-400" />
            </Link>
          </li>
        )}
        {creditNotes.map((cn) => (
          <li key={cn.id}>
            <Link
              to={`/dashboard/invoices?selected=${cn.id}`}
              className="flex items-center justify-between gap-2 rounded border border-slate-200 px-2 py-1.5 text-[11px] transition hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/50"
            >
              <span className="inline-flex items-center gap-1.5">
                <ArrowDownLeft size={11} className="text-rose-500" />
                <span className="font-mono text-slate-900 dark:text-slate-100">
                  {cn.invoiceNumber}
                </span>
                <span className="rounded bg-rose-100 px-1 text-[9px] font-semibold uppercase text-rose-700 dark:bg-rose-500/20 dark:text-rose-300">
                  {t('invoices.ledger.creditNote')}
                </span>
              </span>
              <span className="font-mono tabular-nums text-slate-700 dark:text-slate-300">
                {fmtCurrency(cn.total, cn.currency, locale)}
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
};

const RemindersCard = ({
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
    className={`flex items-start gap-2 rounded border p-1.5 ${sent ? 'border-emerald-200 bg-emerald-50/30 dark:border-emerald-500/30 dark:bg-emerald-500/10' : 'border-slate-200 dark:border-slate-800'}`}
  >
    <span
      className={`mt-0.5 inline-flex h-4 w-4 shrink-0 items-center justify-center rounded-full ${sent ? 'bg-emerald-500 text-white' : 'border border-slate-300 dark:border-slate-700'}`}
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
