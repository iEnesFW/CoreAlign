import { createBrowserRouter, Navigate } from 'react-router-dom';
import { RequirePersona } from '@/features/auth/RequirePersona';
import { LoginPage } from '@/pages/LoginPage';
import { DashboardPage } from '@/pages/DashboardPage';
import { CustomersPage } from '@/pages/CustomersPage';
import { NewOrderPage } from '@/pages/NewOrderPage';
import { OrdersPage } from '@/pages/OrdersPage';
import { OrderDetailPage } from '@/pages/OrderDetailPage';
import { ProfilePage } from '@/pages/ProfilePage';
import { InvoicesPage } from '@/pages/InvoicesPage';
import { InvoiceDetailPage } from '@/pages/InvoiceDetailPage';
import { CommissionsPage } from '@/pages/CommissionsPage';
import { PortalLayout } from '@/widgets/PortalLayout';
import { AydinlatmaMetniPage } from '@/pages/legal/AydinlatmaMetniPage';
import { GizlilikPolitikasiPage } from '@/pages/legal/GizlilikPolitikasiPage';
import { KullanimKosullariPage } from '@/pages/legal/KullanimKosullariPage';
import { CerezPolitikasiPage } from '@/pages/legal/CerezPolitikasiPage';
import { KvkkBasvuruFormuPage } from '@/pages/legal/KvkkBasvuruFormuPage';

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  { path: '/legal/aydinlatma-metni', element: <AydinlatmaMetniPage /> },
  { path: '/legal/gizlilik-politikasi', element: <GizlilikPolitikasiPage /> },
  { path: '/legal/kullanim-kosullari', element: <KullanimKosullariPage /> },
  { path: '/legal/cerez-politikasi', element: <CerezPolitikasiPage /> },
  { path: '/legal/kvkk-basvuru-formu', element: <KvkkBasvuruFormuPage /> },
  {
    path: '/',
    element: (
      <RequirePersona persona="dealer">
        <PortalLayout />
      </RequirePersona>
    ),
    children: [
      { index: true, element: <DashboardPage /> },
      { path: 'customers', element: <CustomersPage /> },
      { path: 'orders/new', element: <NewOrderPage /> },
      { path: 'orders', element: <OrdersPage /> },
      { path: 'orders/:id', element: <OrderDetailPage /> },
      { path: 'invoices', element: <InvoicesPage /> },
      { path: 'invoices/:id', element: <InvoiceDetailPage /> },
      { path: 'commissions', element: <CommissionsPage /> },
      { path: 'profile', element: <ProfilePage /> },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
]);
