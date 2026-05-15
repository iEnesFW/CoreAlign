import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CheckCircle2, CreditCard, FileText, ListOrdered, StickyNote } from 'lucide-react';
import { DetailPanel, PanelTabs } from '@/shared/ui/DetailPanel/DetailPanel';
import { useInvoiceQuery } from '@/features/invoices/hooks/useInvoiceQueries';
import { InvoiceOverviewTab } from '@/features/invoices/ui/InvoiceOverviewTab';
import { PaymentsAppliedTab } from '@/features/invoices/ui/PaymentsAppliedTab';
import type { Invoice } from '@/features/invoices/model/invoice.types';

interface Props {
  invoiceId: string | null;
  onClose: () => void;
  onMarkPaid?: (invoiceId: string) => void;
  onCancel?: (invoiceId: string) => void;
  onRecordPayment?: (invoiceId: string) => void;
}

type Tab = 'overview' | 'lines' | 'payments' | 'activity' | 'notes';

const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

const fmtDate = (iso: string, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(new Date(iso));
  } catch {
    return iso.slice(0, 10);
  }
};

const fmtDateTime = (iso: string, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short' }).format(
      new Date(iso),
    );
  } catch {
    return iso;
  }
};

const fmtNumber = (value: number, locale: string) => new Intl.NumberFormat(locale).format(value);

export const InvoiceDetailPanel = ({
  invoiceId,
  onClose,
  onMarkPaid,
  onCancel,
  onRecordPayment,
}: Props) => {
  const { t, i18n } = useTranslation();
  const [tab, setTab] = useState<Tab>('overview');

  const invoiceQuery = useInvoiceQuery(invoiceId);
  const invoice = invoiceQuery.data?.data ?? null;

  const tabs: { id: Tab; label: string; icon: React.ReactNode }[] = [
    { id: 'overview', label: t('invoices.detail.tabs.overview'), icon: <FileText size={12} /> },
    { id: 'lines', label: t('invoices.detail.tabs.lines'), icon: <ListOrdered size={12} /> },
    {
      id: 'payments',
      label: t('invoices.detail.tabs.payments', { defaultValue: 'Payments' }),
      icon: <CreditCard size={12} />,
    },
    {
      id: 'activity',
      label: t('invoices.detail.tabs.activity'),
      icon: <CheckCircle2 size={12} />,
    },
    { id: 'notes', label: t('invoices.detail.tabs.notes'), icon: <StickyNote size={12} /> },
  ];

  return (
    <DetailPanel
      open={invoiceId !== null}
      title={invoice?.invoiceNumber ?? t('common.loading')}
      subtitle={invoice?.customerName}
      onClose={onClose}
    >
      <PanelTabs tabs={tabs} active={tab} onSelect={setTab} />

      <div className="space-y-4 p-4">
        {tab === 'overview' && invoice && (
          <InvoiceOverviewTab
            invoice={invoice}
            locale={i18n.language}
            onMarkPaid={
              onMarkPaid && invoice.status === 'Issued' ? () => onMarkPaid(invoice.id) : undefined
            }
            onCancel={
              onCancel && (invoice.status === 'Draft' || invoice.status === 'Issued')
                ? () => onCancel(invoice.id)
                : undefined
            }
            onRecordPayment={onRecordPayment ? () => onRecordPayment(invoice.id) : undefined}
          />
        )}
        {tab === 'lines' && invoice && <LinesTab invoice={invoice} locale={i18n.language} />}
        {tab === 'payments' && invoice && (
          <PaymentsAppliedTab
            invoiceId={invoice.id}
            currency={invoice.currency}
            locale={i18n.language}
            amountPaid={invoice.amountPaid}
            amountDue={invoice.amountDue}
            total={invoice.total}
            onRecordPayment={onRecordPayment ? () => onRecordPayment(invoice.id) : undefined}
          />
        )}
        {tab === 'activity' && invoice && <ActivityTab invoice={invoice} locale={i18n.language} />}
        {tab === 'notes' && (
          <div className="rounded border border-slate-200 bg-slate-50/50 p-3 text-sm text-slate-700 dark:border-slate-800 dark:bg-slate-800/30 dark:text-slate-300">
            {invoice?.notes || (
              <span className="italic text-slate-500">{t('invoices.detail.noNotes')}</span>
            )}
          </div>
        )}
      </div>
    </DetailPanel>
  );
};

const LinesTab = ({ invoice, locale }: { invoice: Invoice; locale: string }) => {
  const { t } = useTranslation();
  if (invoice.lines.length === 0) {
    return (
      <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800">
        {t('orders.detail.noLines')}
      </div>
    );
  }
  return (
    <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
      <table className="w-full text-left text-xs">
        <thead className="bg-slate-50 dark:bg-slate-800/50">
          <tr>
            <th className="px-2 py-1.5 font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {t('invoices.fields.product')}
            </th>
            <th className="px-2 py-1.5 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {t('invoices.fields.quantity')}
            </th>
            <th className="px-2 py-1.5 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {t('invoices.fields.unitPrice')}
            </th>
            <th className="px-2 py-1.5 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {t('invoices.fields.lineTotal')}
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
          {invoice.lines.map((line) => (
            <tr key={line.id}>
              <td className="px-2 py-1.5">
                <div className="font-medium text-slate-900 dark:text-slate-100">
                  {line.productName}
                </div>
                <div className="font-mono text-[10px] text-slate-500">{line.productSku}</div>
              </td>
              <td className="px-2 py-1.5 text-right tabular-nums text-slate-700 dark:text-slate-300">
                {fmtNumber(line.quantity, locale)}
              </td>
              <td className="px-2 py-1.5 text-right tabular-nums text-slate-700 dark:text-slate-300">
                {fmtCurrency(line.unitPrice, invoice.currency, locale)}
              </td>
              <td className="px-2 py-1.5 text-right font-medium tabular-nums text-slate-900 dark:text-slate-100">
                {fmtCurrency(line.lineTotal, invoice.currency, locale)}
              </td>
            </tr>
          ))}
        </tbody>
        <tfoot className="bg-slate-50 dark:bg-slate-800/50">
          <tr>
            <td
              colSpan={3}
              className="px-2 py-2 text-right text-[10px] font-semibold uppercase text-slate-500 dark:text-slate-400"
            >
              {t('invoices.fields.total')}
            </td>
            <td className="px-2 py-2 text-right text-sm font-bold tabular-nums text-slate-900 dark:text-slate-100">
              {fmtCurrency(invoice.total, invoice.currency, locale)}
            </td>
          </tr>
        </tfoot>
      </table>
    </div>
  );
};

const ActivityTab = ({ invoice, locale }: { invoice: Invoice; locale: string }) => {
  const { t } = useTranslation();
  const events: { key: string; label: string; at: string; tone: 'blue' | 'emerald' | 'red' }[] = [];
  events.push({
    key: 'created',
    label: t('invoices.detail.activity.created'),
    at: fmtDateTime(invoice.createdAtUtc, locale),
    tone: 'blue',
  });
  if (invoice.status !== 'Draft') {
    events.push({
      key: 'issued',
      label: t('invoices.detail.activity.issued'),
      at: fmtDate(invoice.issueDate, locale),
      tone: 'blue',
    });
  }
  if (invoice.paidAtUtc) {
    events.push({
      key: 'paid',
      label: t('invoices.detail.activity.paid'),
      at: fmtDateTime(invoice.paidAtUtc, locale),
      tone: 'emerald',
    });
  }
  if (invoice.cancelledAtUtc) {
    events.push({
      key: 'cancelled',
      label: t('invoices.detail.activity.cancelled'),
      at: fmtDateTime(invoice.cancelledAtUtc, locale),
      tone: 'red',
    });
  }

  const tones: Record<'blue' | 'emerald' | 'red', string> = {
    blue: 'bg-blue-100 text-blue-700 dark:bg-blue-500/20 dark:text-blue-300',
    emerald: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
    red: 'bg-red-100 text-red-700 dark:bg-red-500/20 dark:text-red-300',
  };

  return (
    <ol className="space-y-2">
      {events.map((ev) => (
        <li
          key={ev.key}
          className="flex items-center justify-between gap-2 rounded-lg border border-slate-200 px-3 py-2 dark:border-slate-800"
        >
          <span className={`rounded px-2 py-0.5 text-[10px] font-semibold ${tones[ev.tone]}`}>
            {ev.label}
          </span>
          <span className="text-[10px] tabular-nums text-slate-500 dark:text-slate-400">
            {ev.at}
          </span>
        </li>
      ))}
    </ol>
  );
};
