import type { InvoiceListParams } from '../model/invoice.types';

export const invoiceKeys = {
  all: ['invoices'] as const,
  lists: () => [...invoiceKeys.all, 'list'] as const,
  list: (params: InvoiceListParams) => [...invoiceKeys.lists(), params] as const,
  details: () => [...invoiceKeys.all, 'detail'] as const,
  detail: (id: string | null) => [...invoiceKeys.details(), id] as const,
  byOrder: (orderId: string | null) => [...invoiceKeys.all, 'by-order', orderId] as const,
  aggregates: (search?: string, fiscalYear?: number) =>
    [...invoiceKeys.all, 'aggregates', search ?? '', fiscalYear ?? ''] as const,
};
