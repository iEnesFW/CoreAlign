import type { OrderStatus } from '@/features/orders/model/order.types';
import type { QuoteStatus } from '@/features/quotes/model/quote.types';
import type { InvoiceStatus } from '@/features/invoices/model/invoice.types';

export type NbaEntity = 'order' | 'quote' | 'invoice';

export interface NextActionDescriptor {
  action: string;
  labelKey: string;
}

const nba = (action: string, entity: NbaEntity): NextActionDescriptor => ({
  action,
  labelKey: `NextBestAction.${entity}.${action}`,
});

const ORDER_ACTIONS: Record<OrderStatus, NextActionDescriptor | null> = {
  Draft: nba('submit', 'order'),
  Submitted: nba('approve', 'order'),
  Approved: nba('allocate', 'order'),
  Allocated: nba('createShipment', 'order'),
  Picking: nba('createShipment', 'order'),
  Packed: nba('createShipment', 'order'),
  PartiallyShipped: nba('generateInvoice', 'order'),
  Shipped: nba('generateInvoice', 'order'),
  Delivered: nba('generateInvoice', 'order'),
  Confirmed: nba('generateInvoice', 'order'),
  Closed: null,
  Cancelled: null,
  Returned: null,
};

const QUOTE_ACTIONS: Record<QuoteStatus, NextActionDescriptor | null> = {
  Draft: nba('send', 'quote'),
  Sent: nba('accept', 'quote'),
  Accepted: nba('convertToOrder', 'quote'),
  Rejected: null,
  Expired: null,
};

const INVOICE_ACTIONS: Record<InvoiceStatus, NextActionDescriptor | null> = {
  Draft: null,
  Issued: nba('collectPayment', 'invoice'),
  Sent: nba('collectPayment', 'invoice'),
  PartiallyPaid: nba('collectPayment', 'invoice'),
  Overdue: nba('collectPayment', 'invoice'),
  Paid: null,
  Void: null,
  Cancelled: null,
  WrittenOff: null,
};

export const resolveNextAction = (
  entity: NbaEntity,
  status: string,
): NextActionDescriptor | null => {
  if (entity === 'order') return ORDER_ACTIONS[status as OrderStatus] ?? null;
  if (entity === 'quote') return QUOTE_ACTIONS[status as QuoteStatus] ?? null;
  return INVOICE_ACTIONS[status as InvoiceStatus] ?? null;
};
