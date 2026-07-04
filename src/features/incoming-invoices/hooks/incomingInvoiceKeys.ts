import type { IncomingInvoiceListParams } from '../model/incomingInvoice.types';

export const incomingInvoiceKeys = {
  all: ['incoming-invoices'] as const,
  lists: () => [...incomingInvoiceKeys.all, 'list'] as const,
  list: (params: IncomingInvoiceListParams) => [...incomingInvoiceKeys.lists(), params] as const,
  details: () => [...incomingInvoiceKeys.all, 'detail'] as const,
  detail: (id: string | null) => [...incomingInvoiceKeys.details(), id] as const,
};
