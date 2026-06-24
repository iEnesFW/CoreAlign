const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

const TYPE_MAP: Record<string, string> = {
  orders: 'order',
  invoices: 'invoice',
  quotes: 'quote',
  customers: 'customer',
  products: 'product',
  payments: 'payment',
  returns: 'return',
  shipments: 'shipment',
  vendors: 'vendor',
  'purchase-orders': 'purchase_order',
  'vendor-bills': 'vendor_bill',
  'goods-receipts': 'goods_receipt',
  'gl-accounts': 'gl_account',
  'journal-entries': 'journal_entry',
  employees: 'employee',
  payslips: 'payslip',
  warranties: 'warranty_contract',
};

export interface PageEntity {
  pageEntityType: string;
  pageEntityId: string;
}

export const derivePageEntity = (pathname: string): PageEntity | null => {
  const parts = pathname.split('/').filter(Boolean);
  for (let i = parts.length - 1; i >= 1; i -= 1) {
    if (GUID_RE.test(parts[i])) {
      const type = TYPE_MAP[parts[i - 1].toLowerCase()];
      if (type) {
        return { pageEntityType: type, pageEntityId: parts[i] };
      }
    }
  }
  return null;
};
