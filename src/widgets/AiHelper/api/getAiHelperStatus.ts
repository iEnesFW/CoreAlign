import { env } from '@/shared/lib/env';
import { authBridge } from '@/shared/api/authBridge';

export const getAiHelperStatus = async (): Promise<boolean> => {
  const token = authBridge.getAccessToken();
  try {
    const response = await fetch(`${env.VITE_API_URL}/api/v1/ai-helper/status`, {
      headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) },
      credentials: 'include',
    });
    if (!response.ok) {
      return true;
    }
    const data = (await response.json()) as { enabled?: boolean };
    return data.enabled !== false;
  } catch {
    return true;
  }
};
