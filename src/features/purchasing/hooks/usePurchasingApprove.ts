import { useAuthStore } from '@/shared/lib/store/authStore';

const APPROVE_ROLES = ['TenantAdmin', 'PurchasingManager', 'Purchasing.Approve'];

export const usePurchasingApprove = (): boolean => {
  const roles = useAuthStore((s) => s.user?.roles ?? []);
  return roles.some((role) => APPROVE_ROLES.includes(role));
};
