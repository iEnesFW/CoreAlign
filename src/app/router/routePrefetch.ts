import {
  ActivityPage,
  AccountingPeriodsPage,
  ReconciliationPage,
  YearEndClosePage,
  CustomersPage,
  DashboardPage,
  InvoicesPage,
  OrdersPage,
  QuotesPage,
  ReturnsPage,
  ProductsPage,
  ProfilePage,
  ReportsPage,
  StockCountsPage,
  PurchaseOrdersPage,
  VendorBillsPage,
  GoodsReceiptsPage,
  ThreeWayMatchReport,
  PayablesAgingPage,
  EmployeesPage,
  PayrollRunsPage,
  PayrollParametersPage,
} from './routes';

export const routePreloaders: Record<string, () => Promise<unknown>> = {
  '/dashboard': DashboardPage.preload,
  '/dashboard/customers': CustomersPage.preload,
  '/dashboard/products': ProductsPage.preload,
  '/dashboard/inventory/stock-counts': StockCountsPage.preload,
  '/dashboard/quotes': QuotesPage.preload,
  '/dashboard/orders': OrdersPage.preload,
  '/dashboard/invoices': InvoicesPage.preload,
  '/dashboard/returns': ReturnsPage.preload,
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
  '/dashboard/reports': ReportsPage.preload,
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
