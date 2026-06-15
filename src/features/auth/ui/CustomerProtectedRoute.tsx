import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuthStore } from '../model/authStore';
import { RouteFallback } from '@/shared/ui/RouteFallback/RouteFallback';

const CUSTOMER_ROLE = 'Customer';

export const CustomerProtectedRoute = () => {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const authReady = useAuthStore((state) => state.authReady);
  const user = useAuthStore((state) => state.user);
  const location = useLocation();

  if (!authReady) {
    return <RouteFallback />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  const hasCustomerRole = user?.roles?.some(
    (r) => r === CUSTOMER_ROLE || r.toLowerCase() === CUSTOMER_ROLE.toLowerCase(),
  );

  if (!hasCustomerRole) {
    return <Navigate to="/dashboard" replace />;
  }

  return <Outlet />;
};
