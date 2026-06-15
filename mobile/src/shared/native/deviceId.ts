import * as SecureStore from 'expo-secure-store';

const DEVICE_ID_KEY = 'corealign.deviceId';

const generateDeviceId = (): string => {
  const random = Math.random().toString(36).slice(2, 14);
  const ts = Date.now().toString(36);
  return `dev-${ts}-${random}`;
};

let cached: string | null = null;

export const getDeviceId = async (): Promise<string> => {
  if (cached) return cached;
  const existing = await SecureStore.getItemAsync(DEVICE_ID_KEY);
  if (existing) {
    cached = existing;
    return existing;
  }
  const fresh = generateDeviceId();
  await SecureStore.setItemAsync(DEVICE_ID_KEY, fresh);
  cached = fresh;
  return fresh;
};

export const resetDeviceIdCacheForTests = (): void => {
  cached = null;
};
