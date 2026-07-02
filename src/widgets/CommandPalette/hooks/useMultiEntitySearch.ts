import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import { useCustomersQuery } from '@/features/customers/hooks/useCustomerQueries';
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import { useOrdersQuery } from '@/features/orders/hooks/useOrderQueries';
import { useInvoicesQuery } from '@/features/invoices/hooks/useInvoiceQueries';
import { useQuotesQuery } from '@/features/quotes/hooks/useQuoteQueries';
import { buildGroups, flattenGroups } from '../model/resultGrouping';
import type { PaletteKind, PaletteResult } from '../model/paletteTypes';

const PAGE = { page: 1, pageSize: 5 } as const;
const MIN_CHARS = 2;

export const useMultiEntitySearch = (query: string, enabled: boolean) => {
  const { t } = useTranslation();
  const debounced = useDebouncedValue(query.trim(), 250);
  const shouldSearch = enabled && debounced.length >= MIN_CHARS;

  const customers = useCustomersQuery({ ...PAGE, search: debounced }, { enabled: shouldSearch });
  const products = useProductsQuery({ ...PAGE, search: debounced }, { enabled: shouldSearch });
  const orders = useOrdersQuery({ ...PAGE, search: debounced }, { enabled: shouldSearch });
  const invoices = useInvoicesQuery({ ...PAGE, search: debounced }, { enabled: shouldSearch });
  const quotes = useQuotesQuery({ ...PAGE, search: debounced }, { enabled: shouldSearch });

  const groups = useMemo(() => {
    if (!shouldSearch) return [];
    const byKind: Partial<Record<PaletteKind, PaletteResult[]>> = {
      customer: (customers.data?.data?.items ?? []).map((c) => ({
        id: c.id,
        kind: 'customer' as const,
        label: c.code ? `${c.code} · ${c.name}` : c.name,
        sublabel: c.email ?? undefined,
        to: '/dashboard/customers',
      })),
      order: (orders.data?.data?.items ?? []).map((o) => ({
        id: o.id,
        kind: 'order' as const,
        label: t('CommandPalette.order', {
          defaultValue: 'Sipariş {{number}}',
          number: o.orderNumber,
        }),
        sublabel: o.customerName,
        to: `/dashboard/orders?focus=${o.id}`,
      })),
      invoice: (invoices.data?.data?.items ?? []).map((inv) => ({
        id: inv.id,
        kind: 'invoice' as const,
        label: t('CommandPalette.invoice', {
          defaultValue: 'Fatura {{number}}',
          number: inv.invoiceNumber,
        }),
        sublabel: inv.customerName,
        to: '/dashboard/invoices',
      })),
      quote: (quotes.data?.data?.items ?? []).map((q) => ({
        id: q.id,
        kind: 'quote' as const,
        label: t('CommandPalette.quote', {
          defaultValue: 'Teklif {{number}}',
          number: q.quoteNumber,
        }),
        sublabel: q.customerName,
        to: '/dashboard/quotes',
      })),
      product: (products.data?.data?.items ?? []).map((p) => ({
        id: p.id,
        kind: 'product' as const,
        label: `${p.sku} · ${p.name}`,
        to: '/dashboard/products',
      })),
    };
    return buildGroups(byKind);
  }, [shouldSearch, customers.data, orders.data, invoices.data, quotes.data, products.data, t]);

  const flat = useMemo(() => flattenGroups(groups), [groups]);

  const isFetching =
    shouldSearch &&
    (customers.isFetching ||
      products.isFetching ||
      orders.isFetching ||
      invoices.isFetching ||
      quotes.isFetching);

  return { groups, flat, isFetching, shouldSearch };
};
