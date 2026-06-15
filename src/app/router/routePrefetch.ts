import {
  ActivityPage,
  AccountingPeriodsPage,
  CustomersPage,
  DashboardPage,
  InvoicesPage,
  OrdersPage,
  ProductsPage,
  ProfilePage,
  ReportsPage,
  StockCountsPage,
} from './routes';

/** Map URL path → lazy component, used by Sidebar prefetch on hover/focus. */
export const routePreloaders: Record<string, () => Promise<unknown>> = {
  '/dashboard': DashboardPage.preload,
  '/dashboard/customers': CustomersPage.preload,
  '/dashboard/products': ProductsPage.preload,
  '/dashboard/inventory/stock-counts': StockCountsPage.preload,
  '/dashboard/orders': OrdersPage.preload,
  '/dashboard/invoices': InvoicesPage.preload,
  '/dashboard/activity': ActivityPage.preload,
  '/dashboard/profile': ProfilePage.preload,
  '/dashboard/accounting/periods': AccountingPeriodsPage.preload,
  '/dashboard/reports': ReportsPage.preload,
};

/** Idle-prefetch the most common dashboard pages once the session is hydrated.
 *  Narrowed to the four pages a user actually visits within the first minute
 *  (per usage analytics) — Reports / AccountingPeriods are dropped because they
 *  cost ~70KB+ each and are rarely visited on session start. */
export const prefetchCommonDashboardPages = (): void => {
  const candidates = [
    DashboardPage.preload,
    CustomersPage.preload,
    OrdersPage.preload,
    InvoicesPage.preload,
  ];
  const run = () => {
    // Stagger the preloads so we don't saturate the connection with 4 parallel
    // chunk fetches the moment the dashboard mounts.
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
