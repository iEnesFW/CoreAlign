import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  useInvoiceQuery,
  useInvoicesByOrderQuery,
  useCreditNotesForInvoice,
} from '@/features/invoices/hooks/useInvoiceQueries';
import { useOrderQuery, useShipmentsByOrderQuery } from '@/features/orders/hooks/useOrderQueries';
import { useQuoteQuery } from '@/features/quotes/hooks/useQuoteQueries';
import type { InvoiceSummary } from '@/features/invoices/model/invoice.types';
import type { ChainEntity, ChainNode, ChainNodeState } from '../model/chainNode.types';

interface PaymentLike {
  status: string;
  amountDue: number;
  amountPaid: number;
}

const derivePaymentState = (invoices: PaymentLike[]): ChainNodeState => {
  if (invoices.length === 0) return 'pending';
  const paid = invoices.filter((i) => i.amountDue <= 0.0001).length;
  const started = invoices.filter((i) => i.amountPaid > 0).length;
  if (paid === invoices.length) return 'done';
  if (started > 0) return 'partial';
  return 'pending';
};

export const useDocumentChain = ({ entity, id }: { entity: ChainEntity; id: string | null }) => {
  const { t } = useTranslation();

  const invoiceQuery = useInvoiceQuery(entity === 'invoice' ? id : null);
  const invoice = invoiceQuery.data?.data ?? null;

  const quoteQuery = useQuoteQuery(entity === 'quote' ? id : null);
  const quote = quoteQuery.data?.data ?? null;

  const orderId = entity === 'order' ? id : (invoice?.orderId ?? quote?.convertedOrderId ?? null);

  const orderQuery = useOrderQuery(orderId);
  const order = orderQuery.data?.data ?? null;

  const shipmentsQuery = useShipmentsByOrderQuery(orderId);
  const orderInvoicesQuery = useInvoicesByOrderQuery(orderId);
  const creditNotesQuery = useCreditNotesForInvoice(entity === 'invoice' ? id : null);

  const nodes = useMemo<ChainNode[]>(() => {
    const shipments = shipmentsQuery.data?.data ?? [];
    const orderInvoices = orderInvoicesQuery.data?.data ?? [];
    const creditNotes = creditNotesQuery.data?.data ?? [];
    const result: ChainNode[] = [];

    if (quote) {
      result.push({
        kind: 'quote',
        id: quote.id,
        label: quote.quoteNumber,
        statusLabel: t(`quotes.status.${quote.status}` as const, { defaultValue: quote.status }),
        state: 'done',
        to: '/dashboard/quotes',
        isCurrent: entity === 'quote' && quote.id === id,
      });
    }

    if (order) {
      result.push({
        kind: 'order',
        id: order.id,
        label: order.orderNumber,
        statusLabel: t(`orders.status.${order.status}` as const, { defaultValue: order.status }),
        state: 'done',
        to: `/dashboard/orders?focus=${order.id}`,
        isCurrent: entity === 'order' && order.id === id,
      });
    }

    shipments.forEach((s) => {
      result.push({
        kind: 'shipment',
        id: s.id,
        label: s.shipmentNumber,
        statusLabel: t(`orders.shipmentStatus.${s.status}` as const, { defaultValue: s.status }),
        state: 'done',
        to: null,
        isCurrent: false,
      });
    });

    const invoiceList: InvoiceSummary[] =
      orderInvoices.length > 0
        ? orderInvoices
        : invoice
          ? [
              {
                id: invoice.id,
                invoiceNumber: invoice.invoiceNumber,
                type: invoice.type,
                orderId: invoice.orderId,
                customerName: invoice.customerName,
                issueDate: invoice.issueDate,
                dueDate: invoice.dueDate,
                status: invoice.status,
                currency: invoice.currency,
                total: invoice.total,
                amountPaid: invoice.amountPaid,
                amountDue: invoice.amountDue,
                isOverdue: false,
              },
            ]
          : [];

    invoiceList.forEach((inv) => {
      result.push({
        kind: 'invoice',
        id: inv.id,
        label: inv.invoiceNumber,
        statusLabel: t(`invoices.status.${inv.status}` as const, { defaultValue: inv.status }),
        state: inv.amountDue <= 0.0001 ? 'done' : inv.amountPaid > 0 ? 'partial' : 'pending',
        to: `/dashboard/invoices?selected=${inv.id}`,
        isCurrent: entity === 'invoice' && inv.id === id,
      });
    });

    if (invoiceList.length > 0) {
      const state = derivePaymentState(invoiceList);
      result.push({
        kind: 'payment',
        id: '',
        label: t('DocumentChain.nodes.payment', { defaultValue: 'Tahsilat' }),
        statusLabel: t(`DocumentChain.state.${state}` as const, { defaultValue: state }),
        state,
        to: null,
        isCurrent: false,
      });
    }

    creditNotes.forEach((cn) => {
      result.push({
        kind: 'creditNote',
        id: cn.id,
        label: cn.invoiceNumber,
        statusLabel: t(`invoices.status.${cn.status}` as const, { defaultValue: cn.status }),
        state: 'done',
        to: `/dashboard/invoices?selected=${cn.id}`,
        isCurrent: false,
      });
    });

    return result;
  }, [
    quote,
    order,
    invoice,
    shipmentsQuery.data,
    orderInvoicesQuery.data,
    creditNotesQuery.data,
    entity,
    id,
    t,
  ]);

  const isLoading =
    invoiceQuery.isPending ||
    quoteQuery.isPending ||
    orderQuery.isPending ||
    shipmentsQuery.isPending ||
    orderInvoicesQuery.isPending;

  return { nodes, isLoading };
};
