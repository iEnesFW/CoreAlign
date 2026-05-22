import { lazyNamed } from '@/app/router/lazyNamed';

export const LoginPage = lazyNamed(() => import('@/pages/login/LoginPage'), 'LoginPage');
export const RegisterPage = lazyNamed(
  () => import('@/pages/register/RegisterPage'),
  'RegisterPage',
);
export const ForgotPasswordPage = lazyNamed(
  () => import('@/pages/forgot-password/ForgotPasswordPage'),
  'ForgotPasswordPage',
);
export const VerifyEmailPage = lazyNamed(
  () => import('@/pages/verify-email/VerifyEmailPage'),
  'VerifyEmailPage',
);
export const ResetPasswordPage = lazyNamed(
  () => import('@/pages/reset-password/ResetPasswordPage'),
  'ResetPasswordPage',
);
export const DashboardLayout = lazyNamed(
  () => import('@/widgets/Layout/DashboardLayout/DashboardLayout'),
  'DashboardLayout',
);
export const DashboardPage = lazyNamed(
  () => import('@/pages/dashboard/DashboardPage'),
  'DashboardPage',
);
export const CustomersPage = lazyNamed(
  () => import('@/pages/customers/CustomersPage'),
  'CustomersPage',
);
export const CustomerDetailPage = lazyNamed(
  () => import('@/pages/customers/CustomerDetailPage'),
  'CustomerDetailPage',
);
export const ProductsPage = lazyNamed(
  () => import('@/pages/products/ProductsPage'),
  'ProductsPage',
);
export const OrdersPage = lazyNamed(() => import('@/pages/orders/OrdersPage'), 'OrdersPage');
export const InventoryPage = lazyNamed(
  () => import('@/pages/inventory/InventoryPage'),
  'InventoryPage',
);
export const InvoicesPage = lazyNamed(
  () => import('@/pages/invoices/InvoicesPage'),
  'InvoicesPage',
);
export const InvoicePrintView = lazyNamed(
  () => import('@/pages/invoices/InvoicePrintView'),
  'InvoicePrintView',
);
export const ActivityPage = lazyNamed(
  () => import('@/pages/activity/ActivityPage'),
  'ActivityPage',
);
export const ProfilePage = lazyNamed(() => import('@/pages/profile/ProfilePage'), 'ProfilePage');
export const AccountingPeriodsPage = lazyNamed(
  () => import('@/pages/accounting/AccountingPeriodsPage'),
  'AccountingPeriodsPage',
);
export const ChartOfAccountsPage = lazyNamed(
  () => import('@/pages/accounting/ChartOfAccountsPage'),
  'ChartOfAccountsPage',
);
export const JournalEntriesPage = lazyNamed(
  () => import('@/pages/accounting/JournalEntriesPage'),
  'JournalEntriesPage',
);
export const TrialBalancePage = lazyNamed(
  () => import('@/pages/accounting/TrialBalancePage'),
  'TrialBalancePage',
);
export const BalanceSheetPage = lazyNamed(
  () => import('@/pages/accounting/BalanceSheetPage'),
  'BalanceSheetPage',
);
export const IncomeStatementPage = lazyNamed(
  () => import('@/pages/accounting/IncomeStatementPage'),
  'IncomeStatementPage',
);
export const VendorsPage = lazyNamed(() => import('@/pages/vendors/VendorsPage'), 'VendorsPage');
export const VendorDetailPage = lazyNamed(
  () => import('@/pages/vendors/VendorDetailPage'),
  'VendorDetailPage',
);
export const SettingsPage = lazyNamed(
  () => import('@/pages/settings/SettingsPage'),
  'SettingsPage',
);
export const ReportsPage = lazyNamed(() => import('@/pages/reports/ReportsPage'), 'ReportsPage');
