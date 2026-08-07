import { useAuthStore } from '@/shared/lib/store/authStore';

const TENANT_ADMIN_ROLE = 'TenantAdmin';

// WHY the selector returns a boolean and never `?? []`: a fresh array literal is a new snapshot on
// every store read, so a component mounted while `user` is still null re-renders without end.
export const useIsTenantAdmin = (): boolean =>
  useAuthStore((s) => s.user?.roles?.includes(TENANT_ADMIN_ROLE) ?? false);
