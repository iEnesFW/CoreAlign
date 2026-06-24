import { registerAuthBridge } from '@/shared/api/authBridge';
import { authApi } from '../api/authApi';
import { useAuthStore } from '@/shared/lib/store/authStore';

registerAuthBridge({
  getAccessToken: () => useAuthStore.getState().accessToken,
  applyToken: (accessToken) => useAuthStore.getState().setAccessToken(accessToken),
  signOut: () => useAuthStore.getState().clearAuth(),
  refresh: async () => {
    const response = await authApi.refreshToken();
    if (response.isSuccess && response.data) {
      useAuthStore.getState().setAuth(response.data.accessToken, response.data.user);
      return response.data.accessToken;
    }
    return null;
  },
});
