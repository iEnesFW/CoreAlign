import { Navigate, useLocation } from 'react-router-dom';
import { type ReactNode } from 'react';
import { useAuthStore } from './authStore';

interface RequirePersonaProps {
  persona: 'customer' | 'dealer' | 'tenant';
  children: ReactNode;
}

export const RequirePersona = ({ persona, children }: RequirePersonaProps) => {
  const location = useLocation();
  const user = useAuthStore((s) => s.user);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  if (!isAuthenticated || !user) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }
  if (user.persona !== persona) {
    return <Navigate to="/login" replace state={{ wrongPersona: true }} />;
  }
  return <>{children}</>;
};
