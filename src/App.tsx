import { Suspense } from 'react';
import { BrowserRouter, Outlet, Route, Routes } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { queryClient } from '@/shared/query/queryClient';
import { GoogleReCaptchaProvider } from 'react-google-recaptcha-v3';
import { ThemeProvider } from '@/app/providers/ThemeProvider';
import { LocaleProvider } from '@/app/providers/LocaleProvider';
import { TenantThemeProvider } from '@/app/theme/TenantThemeProvider';
import { ConfirmDialogProvider } from '@/shared/ui/ConfirmDialog/ConfirmDialog';
import { ConflictResolutionHost } from '@/shared/ui/ConflictResolution/ConflictResolutionHost';
import { ErrorBoundary } from '@/shared/ui/ErrorBoundary/ErrorBoundary';
import { AppToaster } from '@/shared/ui/Toast/Toaster';
import { OfflineBanner } from '@/shared/ui/OfflineBanner/OfflineBanner';
import { RouteFallback } from '@/shared/ui/RouteFallback/RouteFallback';
import { ProtectedRoute } from '@/features/auth/ui/ProtectedRoute';
import { CustomerProtectedRoute } from '@/features/auth/ui/CustomerProtectedRoute';
import { AuthBootstrap } from '@/features/auth/ui/AuthBootstrap';
import { OnboardingTourHost } from '@/features/onboarding/ui/OnboardingTourHost';
import { env } from '@/shared/lib/env';
import {
  AccountingPeriodsPage,
  ActivityPage,
  BalanceSheetPage,
  ChartOfAccountsPage,
  CustomerDetailPage,
  CustomersPage,
  DashboardLayout,
  DashboardPage,
  ForgotPasswordPage,
  GlassProjectDesignerPage,
  GlassProjectsPage,
  IncomeStatementPage,
  InventoryPage,
  StockCountsPage,
  StockCountDetailPage,
  InvoicePrintView,
  InvoicesPage,
  JournalEntriesPage,
  LoginPage,
  NewProjectWizardPage,
  OrdersPage,
  ProductsPage,
  ProfilePage,
  ProvidersAdminPage,
  RegisterPage,
  CustomReportBuilderPage,
  ReportLibraryPage,
  ReportSchedulesPage,
  ReportsPage,
  ResetPasswordPage,
  ServiceTicketsPage,
  SettingsPage,
  TrialBalancePage,
  VendorDetailPage,
  VendorsPage,
  VerifyEmailPage,
  WarrantyContractDetailPage,
  WarrantyContractsPage,
  AcceptanceListPage,
  AcceptanceFormPage,
  MrpDashboardPage,
  PurchaseRequisitionsPage,
  MrpWorkbenchPage,
  MarketplaceListPage,
  MarketplaceDetailPage,
  MyMarketplaceSubmissionsPage,
  AdminReviewQueuePage,
  WhitelabelSettingsPage,
  CustomerPortalLayout,
  CustomerPortalDashboardPage,
  CustomerPortalWarrantyListPage,
  CustomerPortalWarrantyDetailPage,
  CustomerPortalServiceTicketListPage,
  CustomerPortalServiceTicketDetailPage,
  CustomerPortalNewServiceTicketPage,
  CustomerPortalInvoiceListPage,
  CustomerPortalInvoiceDetailPage,
  CustomerPortalPaymentListPage,
  CustomerPortalInitiatePaymentPage,
  CustomerPortalProjectListPage,
  CustomerPortalProjectDetailPage,
  CustomerPortalProfilePage,
  TenantIdpAdminPage,
  LandingPage,
} from '@/app/router/routes';

// useRecaptchaNet=true + defer script injection until the consumer actually
// renders, so the ~80KB script is loaded only on auth pages — and even there,
// only after the first React commit (asynchronously) so the LoginForm shell can
// paint before the recaptcha bundle blocks the main thread.
const RecaptchaWrapper = () => (
  <GoogleReCaptchaProvider
    reCaptchaKey={env.VITE_RECAPTCHA_SITE_KEY}
    scriptProps={{ async: true, defer: true, appendTo: 'body' }}
  >
    <Outlet />
  </GoogleReCaptchaProvider>
);

function App() {
  return (
    <ErrorBoundary>
      <ThemeProvider>
        <LocaleProvider>
          <QueryClientProvider client={queryClient}>
            <AuthBootstrap>
              <TenantThemeProvider>
                <BrowserRouter>
                  <AppToaster />
                  <OfflineBanner />
                  <ConflictResolutionHost />
                  <OnboardingTourHost />
                  <ConfirmDialogProvider>
                    <Suspense fallback={<RouteFallback />}>
                      <Routes>
                        <Route element={<RecaptchaWrapper />}>
                          <Route path="/" element={<LandingPage />} />
                          <Route path="/about" element={<LandingPage />} />
                          <Route path="/solutions" element={<LandingPage />} />
                          <Route path="/articles" element={<LandingPage />} />
                          <Route path="/contact" element={<LandingPage />} />
                          <Route path="/login" element={<LoginPage />} />
                          <Route path="/register" element={<RegisterPage />} />
                          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
                          <Route path="/reset-password" element={<ResetPasswordPage />} />
                          <Route path="/verify-email" element={<VerifyEmailPage />} />
                        </Route>

                        <Route element={<ProtectedRoute />}>
                          <Route path="/dashboard" element={<DashboardLayout />}>
                            <Route index element={<DashboardPage />} />
                            <Route path="customers" element={<CustomersPage />} />
                            <Route path="customers/:id" element={<CustomerDetailPage />} />
                            <Route path="products" element={<ProductsPage />} />
                            <Route path="inventory" element={<InventoryPage />} />
                            <Route path="inventory/stock-counts" element={<StockCountsPage />} />
                            <Route
                              path="inventory/stock-counts/:id"
                              element={<StockCountDetailPage />}
                            />
                            <Route path="orders" element={<OrdersPage />} />
                            <Route path="invoices" element={<InvoicesPage />} />
                            <Route path="vendors" element={<VendorsPage />} />
                            <Route path="vendors/:id" element={<VendorDetailPage />} />
                            <Route path="activity" element={<ActivityPage />} />
                            <Route path="profile" element={<ProfilePage />} />
                            <Route path="accounting/periods" element={<AccountingPeriodsPage />} />
                            <Route
                              path="accounting/chart-of-accounts"
                              element={<ChartOfAccountsPage />}
                            />
                            <Route
                              path="accounting/journal-entries"
                              element={<JournalEntriesPage />}
                            />
                            <Route path="accounting/trial-balance" element={<TrialBalancePage />} />
                            <Route path="accounting/balance-sheet" element={<BalanceSheetPage />} />
                            <Route
                              path="accounting/income-statement"
                              element={<IncomeStatementPage />}
                            />
                            <Route path="reports" element={<ReportsPage />} />
                            <Route path="reports/library" element={<ReportLibraryPage />} />
                            <Route path="reports/custom" element={<CustomReportBuilderPage />} />
                            <Route path="reports/schedules" element={<ReportSchedulesPage />} />
                            <Route path="settings" element={<SettingsPage />} />
                            <Route
                              path="glass-enclosure/projects"
                              element={<GlassProjectsPage />}
                            />
                            <Route
                              path="glass-enclosure/projects/new"
                              element={<NewProjectWizardPage />}
                            />
                            <Route
                              path="glass-enclosure/projects/:id"
                              element={<GlassProjectDesignerPage />}
                            />
                            <Route path="admin/providers" element={<ProvidersAdminPage />} />
                            <Route path="admin/providers/sso" element={<TenantIdpAdminPage />} />
                            <Route path="warranty/contracts" element={<WarrantyContractsPage />} />
                            <Route
                              path="warranty/contracts/:id"
                              element={<WarrantyContractDetailPage />}
                            />
                            <Route
                              path="warranty/service-tickets"
                              element={<ServiceTicketsPage />}
                            />
                            <Route
                              path="installation/acceptances"
                              element={<AcceptanceListPage />}
                            />
                            <Route
                              path="installation/acceptances/:id"
                              element={<AcceptanceFormPage />}
                            />
                            <Route path="mrp" element={<MrpDashboardPage />} />
                            <Route path="mrp/workbench" element={<MrpWorkbenchPage />} />
                            <Route path="mrp/requisitions" element={<PurchaseRequisitionsPage />} />
                            <Route path="marketplace" element={<MarketplaceListPage />} />
                            <Route path="marketplace/:id" element={<MarketplaceDetailPage />} />
                            <Route
                              path="my-submissions"
                              element={<MyMarketplaceSubmissionsPage />}
                            />
                            <Route
                              path="admin/marketplace-review"
                              element={<AdminReviewQueuePage />}
                            />
                            <Route path="admin/whitelabel" element={<WhitelabelSettingsPage />} />
                            <Route
                              path="customer-portal/my-warranties"
                              element={<WarrantyContractsPage />}
                            />
                          </Route>
                          <Route path="/invoices/:id/print" element={<InvoicePrintView />} />
                        </Route>

                        <Route element={<CustomerProtectedRoute />}>
                          <Route path="/customer-portal" element={<CustomerPortalLayout />}>
                            <Route index element={<CustomerPortalDashboardPage />} />
                            <Route path="warranties" element={<CustomerPortalWarrantyListPage />} />
                            <Route
                              path="warranties/:id"
                              element={<CustomerPortalWarrantyDetailPage />}
                            />
                            <Route
                              path="service-tickets"
                              element={<CustomerPortalServiceTicketListPage />}
                            />
                            <Route
                              path="service-tickets/new"
                              element={<CustomerPortalNewServiceTicketPage />}
                            />
                            <Route
                              path="service-tickets/:id"
                              element={<CustomerPortalServiceTicketDetailPage />}
                            />
                            <Route path="invoices" element={<CustomerPortalInvoiceListPage />} />
                            <Route
                              path="invoices/:id"
                              element={<CustomerPortalInvoiceDetailPage />}
                            />
                            <Route path="payments" element={<CustomerPortalPaymentListPage />} />
                            <Route
                              path="payments/initiate"
                              element={<CustomerPortalInitiatePaymentPage />}
                            />
                            <Route path="projects" element={<CustomerPortalProjectListPage />} />
                            <Route
                              path="projects/:id"
                              element={<CustomerPortalProjectDetailPage />}
                            />
                            <Route path="profile" element={<CustomerPortalProfilePage />} />
                          </Route>
                        </Route>
                      </Routes>
                    </Suspense>
                  </ConfirmDialogProvider>
                </BrowserRouter>
              </TenantThemeProvider>
            </AuthBootstrap>
          </QueryClientProvider>
        </LocaleProvider>
      </ThemeProvider>
    </ErrorBoundary>
  );
}

export default App;
