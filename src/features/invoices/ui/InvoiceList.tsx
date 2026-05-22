import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  AlertTriangle,
  CalendarClock,
  CheckCircle2,
  Eye,
  FileText,
  Printer,
  XCircle,
} from 'lucide-react';
import { DataTable, RowActionButton } from '@/shared/ui/DataTable/DataTable';
import { cn } from '@/shared/lib/cn';
import type { InvoiceStatus, InvoiceSummary } from '../model/invoice.types';
import { InvoiceStatusCell } from './InvoiceStatusCell';

interface Props {
  invoices: InvoiceSummary[];
  isLoading: boolean;
  selectedId?: string | null;
  onView: (invoice: InvoiceSummary) => void;
  onMarkPaid: (invoice: InvoiceSummary) => void;
  onCancel: (invoice: InvoiceSummary) => void;
}

const statusTone: Record<InvoiceStatus, string> = {
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

const daysFromNow = (iso: string) => {
  const target = new Date(iso).getTime();
  if (Number.isNaN(target)) return 0;
  return Math.round((target - Date.now()) / (1000 * 60 * 60 * 24));
};

export const InvoiceList = ({
  invoices,
  isLoading,
  selectedId,
  onView,
  onMarkPaid,
  onCancel,
}: Props) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;

  return (
    <DataTable
      rows={invoices}
      getRowId={(i) => i.id}
      isLoading={isLoading}
      selectedId={selectedId ?? null}
      onRowClick={onView}
      emptyIcon={<FileText size={20} />}
      emptyTitle={t('invoices.empty')}
      emptyDescription={t('invoices.emptyHint', {
        defaultValue: 'Generate invoices from orders or create new ones manually.',
      })}
      columns={[
        {
          key: 'invoiceNumber',
          label: t('invoices.columns.invoiceNumber'),
          sortable: true,
          sortValue: (i) => i.invoiceNumber,
          cell: (i) => (
            <div className="flex items-center gap-2">
              <span className="inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-gradient-to-br from-blue-500/15 to-sky-500/15 text-blue-600 ring-1 ring-blue-200/40 dark:text-blue-300 dark:ring-blue-500/30">
                <FileText size={11} />
              </span>
              <span className="font-mono text-xs font-semibold text-slate-900 dark:text-slate-100">
                {i.invoiceNumber}
              </span>
            </div>
          ),
        },
        {
          key: 'customer',
          label: t('invoices.columns.customer'),
          sortable: true,
          sortValue: (i) => i.customerName.toLowerCase(),
          cell: (i) => (
            <span className="font-medium text-slate-900 dark:text-slate-100">{i.customerName}</span>
          ),
        },
        {
          key: 'issueDate',
          label: t('invoices.columns.issueDate'),
          sortable: true,
          sortValue: (i) => i.issueDate,
          hideOnMobile: true,
          cell: (i) => (
            <span className="text-[11px] text-slate-600 dark:text-slate-400">
              {fmtDate(i.issueDate, locale)}
            </span>
          ),
        },
        {
          key: 'dueDate',
          label: t('invoices.columns.dueDate'),
          sortable: true,
          sortValue: (i) => i.dueDate,
          hideOnMobile: true,
          cell: (i) => {
            const days = daysFromNow(i.dueDate);
            const isOverdue = i.isOverdue || (i.amountDue > 0 && days < 0);
            const isDueSoon = !isOverdue && i.amountDue > 0 && days >= 0 && days <= 7;
            return (
              <div className="text-[11px]">
                <div
                  className={cn(
                    'tabular-nums',
                    isOverdue
                      ? 'font-semibold text-rose-600 dark:text-rose-400'
                      : isDueSoon
                        ? 'font-semibold text-amber-600 dark:text-amber-400'
                        : 'text-slate-600 dark:text-slate-400',
                  )}
                >
                  {fmtDate(i.dueDate, locale)}
                </div>
                {(isOverdue || isDueSoon) && (
                  <div
                    className={cn(
                      'mt-0.5 inline-flex items-center gap-0.5 text-[9px] font-semibold uppercase tracking-wider',
                      isOverdue
                        ? 'text-rose-600 dark:text-rose-400'
                        : 'text-amber-600 dark:text-amber-400',
                    )}
                  >
                    {isOverdue ? <AlertTriangle size={9} /> : <CalendarClock size={9} />}
                    {isOverdue
                      ? t('orders.overdueBy', { count: -days })
                      : t('orders.dueIn', { count: days })}
                  </div>
                )}
              </div>
            );
          },
        },
        {
          key: 'status',
          label: t('invoices.columns.status'),
          sortable: true,
          sortValue: (i) => i.status,
          cell: (i) => (
            <InvoiceStatusCell
              status={i.status}
              toneClass={statusTone[i.status]}
              onMarkPaid={() => onMarkPaid(i)}
              onCancel={() => onCancel(i)}
            />
          ),
        },
        {
          key: 'total',
          label: t('invoices.columns.total'),
          align: 'right',
          sortable: true,
          sortValue: (i) => i.total,
          cell: (i) => {
            const paid = i.total > 0 ? Math.min(100, (i.amountPaid / i.total) * 100) : 0;
            return (
              <div className="text-right">
                <div className="font-mono text-xs font-semibold tabular-nums text-slate-900 dark:text-slate-100">
                  {fmtCurrency(i.total, i.currency, locale)}
                </div>
                {i.amountPaid > 0 && i.amountPaid < i.total && (
                  <div className="mt-1 flex items-center justify-end gap-1.5">
                    <div className="h-1 w-12 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
                      <div className="h-full bg-emerald-500" style={{ width: `${paid}%` }} />
                    </div>
                    <span className="text-[9px] tabular-nums text-slate-500 dark:text-slate-400">
                      {paid.toFixed(0)}%
                    </span>
                  </div>
                )}
              </div>
            );
          },
        },
      ]}
      rowActionsHeader={
        <span className="text-[10px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {t('invoices.columns.actions')}
        </span>
      }
      rowActions={(i) => (
        <>
          <RowActionButton
            icon={<Eye size={14} />}
            label={t('common.details', { defaultValue: 'Details' })}
            onClick={() => onView(i)}
          />
          <Link
            to={`/invoices/${i.id}/print`}
            target="_blank"
            rel="noopener noreferrer"
            className="rounded-md p-1.5 text-slate-500 transition-colors hover:bg-slate-100 hover:text-indigo-600 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-indigo-300"
            aria-label={t('invoices.actions.print')}
            title={t('invoices.actions.print')}
          >
            <Printer size={14} />
          </Link>
          {i.status === 'Issued' && (
            <button
              type="button"
              onClick={() => onMarkPaid(i)}
              className="rounded-md p-1.5 text-slate-500 transition-colors hover:bg-emerald-50 hover:text-emerald-600 dark:text-slate-400 dark:hover:bg-emerald-500/10 dark:hover:text-emerald-300"
              aria-label={t('invoices.actions.markPaid')}
              title={t('invoices.actions.markPaid')}
            >
              <CheckCircle2 size={14} />
            </button>
          )}
          {(i.status === 'Draft' || i.status === 'Issued') && (
            <RowActionButton
              icon={<XCircle size={14} />}
              label={t('invoices.actions.cancel')}
              tone="danger"
              onClick={() => onCancel(i)}
            />
          )}
        </>
      )}
    />
  );
};
