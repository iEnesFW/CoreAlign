import { registerAuthBridge } from '@/shared/api/authBridge';
import { useAuthStore } from './authStore';

registerAuthBridge({
  getAccessToken: () => useAuthStore.getState().accessToken,
  clearAuth: () => useAuthStore.getState().clearAuth(),
});
