export interface NavCommand {
  key: string;
  labelKey: string;
  to: string;
}

export const NAV_COMMANDS: NavCommand[] = [
  { key: 'dashboard', labelKey: 'CommandPalette.nav.dashboard', to: '/dashboard' },
  { key: 'customers', labelKey: 'CommandPalette.nav.customers', to: '/dashboard/customers' },
  { key: 'orders', labelKey: 'CommandPalette.nav.orders', to: '/dashboard/orders' },
  { key: 'invoices', labelKey: 'CommandPalette.nav.invoices', to: '/dashboard/invoices' },
  { key: 'quotes', labelKey: 'CommandPalette.nav.quotes', to: '/dashboard/quotes' },
  { key: 'products', labelKey: 'CommandPalette.nav.products', to: '/dashboard/products' },
];
