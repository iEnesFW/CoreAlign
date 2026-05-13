import { Suspense } from 'react';
import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { queryClient } from '@/shared/query/queryClient';
import { GoogleReCaptchaProvider } from 'react-google-recaptcha-v3';
import { ThemeProvider } from '@/app/providers/ThemeProvider';
import { lazyNamed } from '@/app/router/lazyNamed';
import { ConfirmDialogProvider } from '@/shared/ui/ConfirmDialog/ConfirmDialog';
import { ErrorBoundary } from '@/shared/ui/ErrorBoundary/ErrorBoundary';
import { AppToaster } from '@/shared/ui/Toast/Toaster';
import { RouteFallback } from '@/shared/ui/RouteFallback/RouteFallback';
import { ProtectedRoute } from '@/features/auth/ui/ProtectedRoute';
import { env } from '@/shared/lib/env';

const LoginPage = lazyNamed(() => import('@/pages/login/LoginPage'), 'LoginPage');
const RegisterPage = lazyNamed(() => import('@/pages/register/RegisterPage'), 'RegisterPage');
const ForgotPasswordPage = lazyNamed(
  () => import('@/pages/forgot-password/ForgotPasswordPage'),
  'ForgotPasswordPage',
);
const VerifyEmailPage = lazyNamed(
  () => import('@/pages/verify-email/VerifyEmailPage'),
  'VerifyEmailPage',
);
const ResetPasswordPage = lazyNamed(
  () => import('@/pages/reset-password/ResetPasswordPage'),
  'ResetPasswordPage',
);
const DashboardLayout = lazyNamed(
  () => import('@/widgets/Layout/DashboardLayout/DashboardLayout'),
  'DashboardLayout',
);
const DashboardPage = lazyNamed(() => import('@/pages/dashboard/DashboardPage'), 'DashboardPage');
const CustomersPage = lazyNamed(() => import('@/pages/customers/CustomersPage'), 'CustomersPage');
const CustomerDetailPage = lazyNamed(
  () => import('@/pages/customers/CustomerDetailPage'),
  'CustomerDetailPage',
);
const ProductsPage = lazyNamed(() => import('@/pages/products/ProductsPage'), 'ProductsPage');
const OrdersPage = lazyNamed(() => import('@/pages/orders/OrdersPage'), 'OrdersPage');
const InvoicesPage = lazyNamed(() => import('@/pages/invoices/InvoicesPage'), 'InvoicesPage');
const InvoicePrintView = lazyNamed(
  () => import('@/pages/invoices/InvoicePrintView'),
  'InvoicePrintView',
);
const ActivityPage = lazyNamed(() => import('@/pages/activity/ActivityPage'), 'ActivityPage');
const ProfilePage = lazyNamed(() => import('@/pages/profile/ProfilePage'), 'ProfilePage');
const AccountingPeriodsPage = lazyNamed(
  () => import('@/pages/accounting/AccountingPeriodsPage'),
  'AccountingPeriodsPage',
);

const RecaptchaWrapper = () => (
  <GoogleReCaptchaProvider reCaptchaKey={env.VITE_RECAPTCHA_SITE_KEY}>
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
                      <Route path="orders" element={<OrdersPage />} />
                      <Route path="invoices" element={<InvoicesPage />} />
                      <Route path="activity" element={<ActivityPage />} />
                      <Route path="profile" element={<ProfilePage />} />
                      <Route path="accounting/periods" element={<AccountingPeriodsPage />} />
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
