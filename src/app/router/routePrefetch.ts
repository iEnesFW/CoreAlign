import {
  ActivityPage,
  AccountingPeriodsPage,
  ReconciliationPage,
  YearEndClosePage,
  BankAccountsPage,
  CashPositionPage,
  CustomersPage,
  DashboardPage,
  InvoicesPage,
  NewInvoicePage,
  RecurringInvoicesPage,
  OrdersPage,
  NewOrderPage,
  QuotesPage,
  GlassPlatesPage,
  GlassEnclosureCatalogPage,
  ProductionRoutingsPage,
  ReturnsPage,
  ProductsPage,
  ProfilePage,
  ReportsPage,
  DuplicateDetectionPage,
  DocumentNumberGapPage,
  SerialLookupPage,
  StockCountsPage,
  IncomingInvoicesPage,
  PurchaseOrdersPage,
  VendorBillsPage,
  GoodsReceiptsPage,
  ThreeWayMatchReport,
  PayablesAgingPage,
  EmployeesPage,
  PayrollRunsPage,
  PayrollParametersPage,
  DunningSettingsPage,
} from './routes';

export const routePreloaders: Record<string, () => Promise<unknown>> = {
  '/dashboard': DashboardPage.preload,
  '/dashboard/customers': CustomersPage.preload,
  '/dashboard/products': ProductsPage.preload,
  '/dashboard/inventory/stock-counts': StockCountsPage.preload,
  '/dashboard/quotes': QuotesPage.preload,
  '/dashboard/glass-plates': GlassPlatesPage.preload,
  '/dashboard/glass-enclosure/catalog': GlassEnclosureCatalogPage.preload,
  '/dashboard/production/routings': ProductionRoutingsPage.preload,
  '/dashboard/orders': OrdersPage.preload,
  '/dashboard/orders/new': NewOrderPage.preload,
  '/dashboard/invoices': InvoicesPage.preload,
  '/dashboard/invoices/new': NewInvoicePage.preload,
  '/dashboard/recurring-invoices': RecurringInvoicesPage.preload,
  '/dashboard/returns': ReturnsPage.preload,
  '/dashboard/incoming-invoices': IncomingInvoicesPage.preload,
  '/dashboard/purchasing/purchase-orders': PurchaseOrdersPage.preload,
  '/dashboard/purchasing/vendor-bills': VendorBillsPage.preload,
  '/dashboard/purchasing/goods-receipts': GoodsReceiptsPage.preload,
  '/dashboard/purchasing/three-way-match': ThreeWayMatchReport.preload,
  '/dashboard/purchasing/payables-aging': PayablesAgingPage.preload,
  '/dashboard/hr/employees': EmployeesPage.preload,
  '/dashboard/hr/payroll-runs': PayrollRunsPage.preload,
  '/dashboard/hr/payroll-parameters': PayrollParametersPage.preload,
  '/dashboard/activity': ActivityPage.preload,
  '/dashboard/profile': ProfilePage.preload,
  '/dashboard/accounting/periods': AccountingPeriodsPage.preload,
  '/dashboard/accounting/reconciliation': ReconciliationPage.preload,
  '/dashboard/accounting/year-end-close': YearEndClosePage.preload,
  '/dashboard/accounting/bank-accounts': BankAccountsPage.preload,
  '/dashboard/accounting/cash-position': CashPositionPage.preload,
  '/dashboard/reports': ReportsPage.preload,
  '/dashboard/reports/duplicates': DuplicateDetectionPage.preload,
  '/dashboard/reports/document-number-gaps': DocumentNumberGapPage.preload,
  '/dashboard/inventory/serial-lookup': SerialLookupPage.preload,
  '/dashboard/admin/dunning-settings': DunningSettingsPage.preload,
};

export const prefetchCommonDashboardPages = (): void => {
  const candidates = [
    DashboardPage.preload,
    CustomersPage.preload,
    OrdersPage.preload,
    InvoicesPage.preload,
  ];
  const run = () => {
    candidates.forEach((p, i) => setTimeout(() => p(), i * 150));
  };
  if (typeof window === 'undefined') return;
  const ric = (window as Window & { requestIdleCallback?: typeof requestIdleCallback })
    .requestIdleCallback;
  if (typeof ric === 'function') {
    ric(run, { timeout: 2000 });
  } else {
    window.setTimeout(run, 600);
  }
};
