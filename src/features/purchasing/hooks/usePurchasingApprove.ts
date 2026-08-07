import { useAuthStore } from '@/shared/lib/store/authStore';

const APPROVE_ROLES = ['TenantAdmin', 'PurchasingManager', 'Purchasing.Approve'];

// WHY the selector returns a boolean: `?? []` hands zustand a new array on every read, which is a
// fresh snapshot each time and re-renders the caller without end while `user` is still null.
export const usePurchasingApprove = (): boolean =>
  useAuthStore((s) => s.user?.roles?.some((role) => APPROVE_ROLES.includes(role)) ?? false);
