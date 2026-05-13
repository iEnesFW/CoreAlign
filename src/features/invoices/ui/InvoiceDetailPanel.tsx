import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  CheckCircle2,
  FileText,
  ListOrdered,
  Printer,
  ShoppingCart,
  StickyNote,
  XCircle,
} from 'lucide-react';
import { DetailPanel, PanelTabs } from '@/shared/ui/DetailPanel/DetailPanel';
import { useInvoiceQuery } from '@/features/invoices/hooks/useInvoiceQueries';
import type { Invoice, InvoiceStatus } from '@/features/invoices/model/invoice.types';

interface Props {
  invoiceId: string | null;
  onClose: () => void;
  onMarkPaid?: (invoiceId: string) => void;
  onCancel?: (invoiceId: string) => void;
}

type Tab = 'overview' | 'lines' | 'activity' | 'notes';

const statusStyles: Record<InvoiceStatus, string> = {
  Draft: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
  Issued: 'bg-blue-100 text-blue-700 dark:bg-blue-500/20 dark:text-blue-300',
  Sent: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
  PartiallyPaid: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  Paid: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Overdue: 'bg-red-100 text-red-800 dark:bg-red-500/20 dark:text-red-300',
  Void: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
  Cancelled: 'bg-red-100 text-red-700 dark:bg-red-500/20 dark:text-red-300',
};

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

export const InvoiceDetailPanel = ({ invoiceId, onClose, onMarkPaid, onCancel }: Props) => {
  const { t, i18n } = useTranslation();
  const [tab, setTab] = useState<Tab>('overview');

  const invoiceQuery = useInvoiceQuery(invoiceId);
  const invoice = invoiceQuery.data?.data ?? null;

  const tabs: { id: Tab; label: string; icon: React.ReactNode }[] = [
    { id: 'overview', label: t('invoices.detail.tabs.overview'), icon: <FileText size={12} /> },
    { id: 'lines', label: t('invoices.detail.tabs.lines'), icon: <ListOrdered size={12} /> },
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
          <OverviewTab
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
          />
        )}
        {tab === 'lines' && invoice && <LinesTab invoice={invoice} locale={i18n.language} />}
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

const OverviewTab = ({
  invoice,
  locale,
  onMarkPaid,
  onCancel,
}: {
  invoice: Invoice;
  locale: string;
  onMarkPaid?: () => void;
  onCancel?: () => void;
}) => {
  const { t } = useTranslation();
  return (
    <>
      <div className="grid grid-cols-2 gap-2">
        <Stat
          label={t('invoices.fields.total')}
          value={fmtCurrency(invoice.total, invoice.currency, locale)}
          highlight="indigo"
        />
        <Stat
          label={t('invoices.detail.metrics.lines')}
          value={String(invoice.lines.length)}
          sub={`${fmtNumber(
            invoice.lines.reduce((s, l) => s + l.quantity, 0),
            locale,
          )} ${t('orders.detail.metrics.units')}`}
          highlight="blue"
        />
      </div>
      <div className="space-y-2 rounded-lg border border-slate-200 p-3 text-sm dark:border-slate-800">
        <Row label={t('orders.fields.status')}>
          <span
            className={`inline-flex rounded-full px-2 py-0.5 text-[10px] font-medium ${statusStyles[invoice.status]}`}
          >
            {t(`invoices.status.${invoice.status}` as never)}
          </span>
        </Row>
        <Row label={t('invoices.fields.issueDate')}>{fmtDate(invoice.issueDate, locale)}</Row>
        <Row label={t('invoices.fields.dueDate')}>{fmtDate(invoice.dueDate, locale)}</Row>
        <Row label={t('invoices.fields.currency')}>{invoice.currency}</Row>
        {invoice.orderId && (
          <Row label={t('invoices.detail.linkedOrder')}>
            <Link
              to={`/dashboard/orders?focus=${invoice.orderId}`}
              className="inline-flex items-center gap-1 text-indigo-600 hover:underline dark:text-indigo-400"
            >
              <ShoppingCart size={10} />
              {t('common.view')}
            </Link>
          </Row>
        )}
      </div>
      <div className="flex flex-col gap-2 sm:flex-row">
        <Link
          to={`/invoices/${invoice.id}/print`}
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex flex-1 items-center justify-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
        >
          <Printer size={14} />
          {t('invoices.actions.print')}
        </Link>
        {onMarkPaid && (
          <button
            type="button"
            onClick={onMarkPaid}
            className="inline-flex flex-1 items-center justify-center gap-2 rounded-lg border border-emerald-300 bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-700 hover:bg-emerald-100 dark:border-emerald-500/40 dark:bg-emerald-500/10 dark:text-emerald-300 dark:hover:bg-emerald-500/20"
          >
            <CheckCircle2 size={14} />
            {t('invoices.actions.markPaid')}
          </button>
        )}
        {onCancel && (
          <button
            type="button"
            onClick={onCancel}
            className="inline-flex flex-1 items-center justify-center gap-2 rounded-lg border border-red-300 bg-red-50 px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-100 dark:border-red-500/40 dark:bg-red-500/10 dark:text-red-300 dark:hover:bg-red-500/20"
          >
            <XCircle size={14} />
            {t('invoices.actions.cancel')}
          </button>
        )}
      </div>
    </>
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

const Row = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <div className="flex items-center justify-between gap-2">
    <span className="text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </span>
    <span className="truncate text-sm text-slate-700 dark:text-slate-200">{children}</span>
  </div>
);

const highlightClass: Record<'indigo' | 'blue', string> = {
  indigo: 'border-indigo-200 dark:border-indigo-500/30',
  blue: 'border-blue-200 dark:border-blue-500/30',
};

const Stat = ({
  label,
  value,
  sub,
  highlight,
}: {
  label: string;
  value: string;
  sub?: string;
  highlight: keyof typeof highlightClass;
}) => (
  <div className={`rounded border bg-white p-2.5 dark:bg-slate-900 ${highlightClass[highlight]}`}>
    <div className="text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </div>
    <div className="mt-0.5 text-base font-bold text-slate-900 dark:text-slate-100">{value}</div>
    {sub && <div className="mt-0.5 text-[10px] text-slate-500 dark:text-slate-400">{sub}</div>}
  </div>
);
