import { Suspense } from 'react';
import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { queryClient } from '@/shared/query/queryClient';
import { GoogleReCaptchaProvider } from 'react-google-recaptcha-v3';
import { ThemeProvider } from '@/app/providers/ThemeProvider';
import { ConfirmDialogProvider } from '@/shared/ui/ConfirmDialog/ConfirmDialog';
import { ErrorBoundary } from '@/shared/ui/ErrorBoundary/ErrorBoundary';
import { AppToaster } from '@/shared/ui/Toast/Toaster';
import { RouteFallback } from '@/shared/ui/RouteFallback/RouteFallback';
import { ProtectedRoute } from '@/features/auth/ui/ProtectedRoute';
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
  IncomeStatementPage,
  InventoryPage,
  InvoicePrintView,
  InvoicesPage,
  JournalEntriesPage,
  LoginPage,
  OrdersPage,
  ProductsPage,
  ProfilePage,
  RegisterPage,
  ReportsPage,
  ResetPasswordPage,
  SettingsPage,
  TrialBalancePage,
  VendorDetailPage,
  VendorsPage,
  VerifyEmailPage,
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
        <QueryClientProvider client={queryClient}>
          <BrowserRouter>
            <AppToaster />
            <ConfirmDialogProvider>
              <Suspense fallback={<RouteFallback />}>
                <Routes>
                  <Route element={<RecaptchaWrapper />}>
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
                      <Route path="accounting/journal-entries" element={<JournalEntriesPage />} />
                      <Route path="accounting/trial-balance" element={<TrialBalancePage />} />
                      <Route path="accounting/balance-sheet" element={<BalanceSheetPage />} />
                      <Route path="accounting/income-statement" element={<IncomeStatementPage />} />
                      <Route path="reports" element={<ReportsPage />} />
                      <Route path="settings" element={<SettingsPage />} />
                    </Route>
                    <Route path="/invoices/:id/print" element={<InvoicePrintView />} />
                  </Route>

                  <Route path="/" element={<Navigate to="/login" replace />} />
                </Routes>
              </Suspense>
            </ConfirmDialogProvider>
          </BrowserRouter>
        </QueryClientProvider>
      </ThemeProvider>
    </ErrorBoundary>
  );
}

export default App;
