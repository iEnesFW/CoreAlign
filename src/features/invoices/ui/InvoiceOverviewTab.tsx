import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { ExternalLink, MapPin, Receipt, ShoppingCart } from 'lucide-react';
import type { Invoice, InvoiceStatus } from '@/features/invoices/model/invoice.types';
import { daysFromNow } from './invoiceOverview/format';
import { KpiRow, MetaChips, PaymentProgressBar } from './invoiceOverview/InvoiceKpis';
import {
  EInvoicePanel,
  FinancialBreakdown,
  TaxBreakdownCard,
} from './invoiceOverview/InvoiceBreakdown';
import { AddressSnapshotCard, CustomerSnapshotCard } from './invoiceOverview/InvoiceSnapshots';
import { ActionsBar } from './invoiceOverview/InvoiceActions';

interface Props {
  invoice: Invoice;
  locale: string;
  onMarkPaid?: () => void;
  onCancel?: () => void;
  onRecordPayment?: () => void;
  onIssueCreditNote?: () => void;
}

const statusStyles: Record<InvoiceStatus, string> = {
  Draft: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
  Issued: 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300',
  Sent: 'bg-info-100 text-info-700 dark:bg-info-500/20 dark:text-info-300',
  PartiallyPaid: 'bg-warning-100 text-warning-800 dark:bg-warning-500/20 dark:text-warning-300',
  Paid: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
  Overdue: 'bg-danger-100 text-danger-800 dark:bg-danger-500/20 dark:text-danger-300',
  Void: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
  Cancelled: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
};

export const InvoiceOverviewTab = ({
  invoice,
  locale,
  onMarkPaid,
  onCancel,
  onRecordPayment,
  onIssueCreditNote,
}: Props) => {
  const { t } = useTranslation();
  const dueIn = daysFromNow(invoice.dueDate);
  const paidPct = invoice.total > 0 ? (invoice.amountPaid / invoice.total) * 100 : 0;
  const showRecordPayment =
    !!onRecordPayment &&
    (invoice.status === 'Issued' ||
      invoice.status === 'Sent' ||
      invoice.status === 'PartiallyPaid' ||
      invoice.status === 'Overdue');

  return (
    <div className="space-y-3">
      <KpiRow invoice={invoice} locale={locale} paidPct={paidPct} dueIn={dueIn} />

      <MetaChips invoice={invoice} dueIn={dueIn} locale={locale} />

      <PaymentProgressBar
        total={invoice.total}
        paid={invoice.amountPaid}
        due={invoice.amountDue}
        currency={invoice.currency}
        locale={locale}
        status={invoice.status}
      />

      <FinancialBreakdown invoice={invoice} locale={locale} />

      {invoice.taxBreakdown && invoice.taxBreakdown.length > 0 && (
        <TaxBreakdownCard
          items={invoice.taxBreakdown}
          currency={invoice.currency}
          locale={locale}
        />
      )}

      <EInvoicePanel invoice={invoice} locale={locale} />

      {invoice.customerSnapshot && <CustomerSnapshotCard snapshot={invoice.customerSnapshot} />}

      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
        <AddressSnapshotCard
          icon={<Receipt size={12} />}
          title={t('orders.detail.billingAddress')}
          snapshot={invoice.billingAddressSnapshot}
          empty={t('orders.detail.noBillingAddress')}
        />
        <AddressSnapshotCard
          icon={<MapPin size={12} />}
          title={t('orders.detail.shippingAddress')}
          snapshot={invoice.shippingAddressSnapshot}
          empty={t('orders.detail.noShippingAddress')}
        />
      </div>

      <div className="flex items-center gap-2 rounded-lg border border-slate-200 bg-white p-2.5 text-[11px] dark:border-slate-800 dark:bg-slate-900">
        <span className="text-slate-500 dark:text-slate-400">{t('orders.fields.status')}</span>
        <span
          className={`inline-flex rounded-full px-2 py-0.5 text-[10px] font-medium ${statusStyles[invoice.status]}`}
        >
          {t(`invoices.status.${invoice.status}` as never)}
        </span>
        {invoice.orderId && (
          <Link
            to={`/dashboard/orders?selected=${invoice.orderId}`}
            className="ml-auto inline-flex items-center gap-1 text-[10px] text-primary-600 hover:underline dark:text-primary-400"
          >
            <ShoppingCart size={11} />
            {t('invoices.detail.linkedOrder')}
            <ExternalLink size={9} />
          </Link>
        )}
      </div>

      <ActionsBar
        invoice={invoice}
        showRecordPayment={showRecordPayment}
        onRecordPayment={onRecordPayment}
        onMarkPaid={onMarkPaid}
        onCancel={onCancel}
        onIssueCreditNote={onIssueCreditNote}
      />
    </div>
  );
};
