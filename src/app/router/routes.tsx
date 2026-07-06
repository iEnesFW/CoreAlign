import { lazyNamed } from '@/app/router/lazyNamed';

export const LandingPage = lazyNamed(() => import('@/pages/public/LandingPage'), 'LandingPage');

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
export const NewOrderPage = lazyNamed(() => import('@/pages/orders/NewOrderPage'), 'NewOrderPage');
export const QuotesPage = lazyNamed(() => import('@/pages/Quotes/QuotesPage'), 'QuotesPage');
export const ReturnsPage = lazyNamed(() => import('@/pages/returns/ReturnsPage'), 'ReturnsPage');
export const ReturnDetailPage = lazyNamed(
  () => import('@/pages/returns/ReturnDetailPage'),
  'ReturnDetailPage',
);
export const InventoryPage = lazyNamed(
  () => import('@/pages/inventory/InventoryPage'),
  'InventoryPage',
);
export const StockCountsPage = lazyNamed(
  () => import('@/pages/inventory/StockCounts/StockCountsPage'),
  'StockCountsPage',
);
export const StockCountDetailPage = lazyNamed(
  () => import('@/pages/inventory/StockCounts/StockCountDetailPage'),
  'StockCountDetailPage',
);
export const InvoicesPage = lazyNamed(
  () => import('@/pages/invoices/InvoicesPage'),
  'InvoicesPage',
);
export const NewInvoicePage = lazyNamed(
  () => import('@/pages/invoices/NewInvoicePage'),
  'NewInvoicePage',
);
export const RecurringInvoicesPage = lazyNamed(
  () => import('@/pages/invoices/RecurringInvoicesPage'),
  'RecurringInvoicesPage',
);
export const IncomingInvoicesPage = lazyNamed(
  () => import('@/pages/incoming-invoices/IncomingInvoicesPage'),
  'IncomingInvoicesPage',
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
export const ReconciliationPage = lazyNamed(
  () => import('@/pages/accounting/ReconciliationPage'),
  'ReconciliationPage',
);
export const YearEndClosePage = lazyNamed(
  () => import('@/pages/accounting/YearEndClosePage'),
  'YearEndClosePage',
);
export const BankAccountsPage = lazyNamed(
  () => import('@/pages/accounting/BankAccountsPage'),
  'BankAccountsPage',
);
export const CashPositionPage = lazyNamed(
  () => import('@/pages/accounting/CashPositionPage'),
  'CashPositionPage',
);
export const VendorsPage = lazyNamed(() => import('@/pages/vendors/VendorsPage'), 'VendorsPage');
export const VendorDetailPage = lazyNamed(
  () => import('@/pages/vendors/VendorDetailPage'),
  'VendorDetailPage',
);
export const PurchaseOrdersPage = lazyNamed(
  () => import('@/pages/purchasing/PurchaseOrdersPage'),
  'PurchaseOrdersPage',
);
export const VendorBillsPage = lazyNamed(
  () => import('@/pages/purchasing/VendorBillsPage'),
  'VendorBillsPage',
);
export const GoodsReceiptsPage = lazyNamed(
  () => import('@/pages/purchasing/GoodsReceiptsPage'),
  'GoodsReceiptsPage',
);
export const ThreeWayMatchReport = lazyNamed(
  () => import('@/pages/purchasing/ThreeWayMatchReport'),
  'ThreeWayMatchReport',
);
export const PayablesAgingPage = lazyNamed(
  () => import('@/pages/purchasing/PayablesAgingPage'),
  'PayablesAgingPage',
);
export const EmployeesPage = lazyNamed(() => import('@/pages/hr/EmployeesPage'), 'EmployeesPage');
export const EmployeeDetailPage = lazyNamed(
  () => import('@/pages/hr/EmployeeDetailPage'),
  'EmployeeDetailPage',
);
export const PayrollRunsPage = lazyNamed(
  () => import('@/pages/hr/PayrollRunsPage'),
  'PayrollRunsPage',
);
export const PayrollRunDetailPage = lazyNamed(
  () => import('@/pages/hr/PayrollRunDetailPage'),
  'PayrollRunDetailPage',
);
export const PayslipPrintView = lazyNamed(
  () => import('@/pages/hr/PayslipPrintView'),
  'PayslipPrintView',
);
export const PayrollParametersPage = lazyNamed(
  () => import('@/pages/hr/PayrollParametersPage'),
  'PayrollParametersPage',
);
export const SettingsPage = lazyNamed(
  () => import('@/pages/settings/SettingsPage'),
  'SettingsPage',
);
export const DunningSettingsPage = lazyNamed(
  () => import('@/pages/settings/dunning/DunningSettingsPage'),
  'DunningSettingsPage',
);
export const ReportsPage = lazyNamed(() => import('@/pages/reports/ReportsPage'), 'ReportsPage');
export const DuplicateDetectionPage = lazyNamed(
  () => import('@/pages/reports/DuplicateDetectionPage'),
  'DuplicateDetectionPage',
);
export const DocumentNumberGapPage = lazyNamed(
  () => import('@/pages/reports/DocumentNumberGapPage'),
  'DocumentNumberGapPage',
);
export const SerialLookupPage = lazyNamed(
  () => import('@/pages/inventory/SerialLookupPage'),
  'SerialLookupPage',
);
export const ReportLibraryPage = lazyNamed(
  () => import('@/pages/reports/ReportLibraryPage'),
  'ReportLibraryPage',
);
export const CustomReportBuilderPage = lazyNamed(
  () => import('@/pages/reports/CustomReportBuilder'),
  'CustomReportBuilder',
);
export const ReportSchedulesPage = lazyNamed(
  () => import('@/pages/reports/SchedulesPage'),
  'SchedulesPage',
);
export const GlassProjectsPage = lazyNamed(
  () => import('@/pages/glass-enclosure/GlassProjectsPage'),
  'GlassProjectsPage',
);
export const NewProjectWizardPage = lazyNamed(
  () => import('@/pages/glass-enclosure/NewProjectWizardPage'),
  'NewProjectWizardPage',
);
export const GlassProjectDesignerPage = lazyNamed(
  () => import('@/pages/glass-enclosure/GlassProjectDesignerPage'),
  'GlassProjectDesignerPage',
);
export const ProvidersAdminPage = lazyNamed(
  () => import('@/pages/admin/ProvidersAdminPage'),
  'ProvidersAdminPage',
);
export const SmtpSettingsPage = lazyNamed(
  () => import('@/pages/admin/SmtpSettingsPage'),
  'SmtpSettingsPage',
);
export const ErrorLogsPage = lazyNamed(
  () => import('@/pages/admin/ErrorLogsPage'),
  'ErrorLogsPage',
);
export const TenantIdpAdminPage = lazyNamed(
  () => import('@/pages/admin/TenantIdpAdminPage'),
  'TenantIdpAdminPage',
);
export const WarrantyContractsPage = lazyNamed(
  () => import('@/pages/warranty/WarrantyContractsPage'),
  'WarrantyContractsPage',
);
export const WarrantyContractDetailPage = lazyNamed(
  () => import('@/pages/warranty/WarrantyContractDetailPage'),
  'WarrantyContractDetailPage',
);
export const ServiceTicketsPage = lazyNamed(
  () => import('@/pages/warranty/ServiceTicketsPage'),
  'ServiceTicketsPage',
);
export const AcceptanceListPage = lazyNamed(
  () => import('@/pages/installation/AcceptanceListPage'),
  'AcceptanceListPage',
);
export const AcceptanceFormPage = lazyNamed(
  () => import('@/pages/installation/AcceptanceFormPage'),
  'AcceptanceFormPage',
);
export const MrpDashboardPage = lazyNamed(
  () => import('@/pages/mrp/MrpDashboardPage'),
  'MrpDashboardPage',
);
export const PurchaseRequisitionsPage = lazyNamed(
  () => import('@/pages/mrp/PurchaseRequisitionsPage'),
  'PurchaseRequisitionsPage',
);
export const MrpWorkbenchPage = lazyNamed(
  () => import('@/pages/mrp/MrpWorkbenchPage'),
  'MrpWorkbenchPage',
);
export const MarketplaceListPage = lazyNamed(
  () => import('@/pages/marketplace/MarketplaceListPage'),
  'MarketplaceListPage',
);
export const MarketplaceDetailPage = lazyNamed(
  () => import('@/pages/marketplace/MarketplaceDetailPage'),
  'MarketplaceDetailPage',
);
export const MyMarketplaceSubmissionsPage = lazyNamed(
  () => import('@/pages/marketplace/MyMarketplaceSubmissionsPage'),
  'MyMarketplaceSubmissionsPage',
);
export const AdminReviewQueuePage = lazyNamed(
  () => import('@/pages/marketplace/AdminReviewQueuePage'),
  'AdminReviewQueuePage',
);

export const CustomerPortalLayout = lazyNamed(
  () => import('@/app/layouts/CustomerPortalLayout'),
  'CustomerPortalLayout',
);
export const CustomerPortalDashboardPage = lazyNamed(
  () => import('@/pages/customer-portal/DashboardPage'),
  'DashboardPage',
);
export const CustomerPortalWarrantyListPage = lazyNamed(
  () => import('@/pages/customer-portal/WarrantyListPage'),
  'WarrantyListPage',
);
export const CustomerPortalWarrantyDetailPage = lazyNamed(
  () => import('@/pages/customer-portal/WarrantyDetailPage'),
  'WarrantyDetailPage',
);
export const CustomerPortalServiceTicketListPage = lazyNamed(
  () => import('@/pages/customer-portal/ServiceTicketListPage'),
  'ServiceTicketListPage',
);
export const CustomerPortalServiceTicketDetailPage = lazyNamed(
  () => import('@/pages/customer-portal/ServiceTicketDetailPage'),
  'ServiceTicketDetailPage',
);
export const CustomerPortalNewServiceTicketPage = lazyNamed(
  () => import('@/pages/customer-portal/NewServiceTicketPage'),
  'NewServiceTicketPage',
);
export const CustomerPortalInvoiceListPage = lazyNamed(
  () => import('@/pages/customer-portal/InvoiceListPage'),
  'InvoiceListPage',
);
export const CustomerPortalInvoiceDetailPage = lazyNamed(
  () => import('@/pages/customer-portal/InvoiceDetailPage'),
  'InvoiceDetailPage',
);
export const CustomerPortalPaymentListPage = lazyNamed(
  () => import('@/pages/customer-portal/PaymentListPage'),
  'PaymentListPage',
);
export const CustomerPortalInitiatePaymentPage = lazyNamed(
  () => import('@/pages/customer-portal/InitiatePaymentPage'),
  'InitiatePaymentPage',
);
export const CustomerPortalProjectListPage = lazyNamed(
  () => import('@/pages/customer-portal/ProjectListPage'),
  'ProjectListPage',
);
export const CustomerPortalProjectDetailPage = lazyNamed(
  () => import('@/pages/customer-portal/ProjectDetailPage'),
  'ProjectDetailPage',
);
export const CustomerPortalProfilePage = lazyNamed(
  () => import('@/pages/customer-portal/ProfilePage'),
  'ProfilePage',
);
export const WhitelabelSettingsPage = lazyNamed(
  () => import('@/pages/admin/WhitelabelSettingsPage'),
  'WhitelabelSettingsPage',
);
