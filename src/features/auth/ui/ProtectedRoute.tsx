import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuthStore } from '../model/authStore';
import { RouteFallback } from '@/shared/ui/RouteFallback/RouteFallback';

export const ProtectedRoute = () => {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const authReady = useAuthStore((state) => state.authReady);
  const location = useLocation();

  if (!authReady) {
    return <RouteFallback />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <Outlet />;
};
