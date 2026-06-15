import { useAuthStore } from '@/features/auth/model/authStore';

const TENANT_ADMIN_ROLE = 'TenantAdmin';

export const useIsTenantAdmin = (): boolean => {
  const roles = useAuthStore((s) => s.user?.roles ?? []);
  return roles.includes(TENANT_ADMIN_ROLE);
};
