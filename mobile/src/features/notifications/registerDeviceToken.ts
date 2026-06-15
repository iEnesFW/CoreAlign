import * as Notifications from 'expo-notifications';
import * as Device from 'expo-device';
import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';
import Constants from 'expo-constants';
import { apiClient } from '@/api/apiClient';

const TOKEN_CACHE_KEY = 'corealign.push.token';
const PLATFORM_CACHE_KEY = 'corealign.push.platform';

export type PushPlatform = 'ios' | 'android' | 'web';

export interface RegisteredDeviceToken {
  token: string;
  platform: PushPlatform;
  isDevicePushToken: boolean;
}

const resolvePlatform = (): PushPlatform => {
  if (Platform.OS === 'ios' || Platform.OS === 'android' || Platform.OS === 'web') {
    return Platform.OS;
  }
  return 'web';
};

const ensureAndroidChannel = async (): Promise<void> => {
  if (Platform.OS !== 'android') return;
  await Notifications.setNotificationChannelAsync('default', {
    name: 'default',
    importance: Notifications.AndroidImportance.MAX,
    vibrationPattern: [0, 250, 250, 250],
    lightColor: '#0F172A',
  });
};

const requestPermissionsIfNeeded = async (): Promise<boolean> => {
  const existing = await Notifications.getPermissionsAsync();
  if (existing.status === 'granted') return true;
  if (!existing.canAskAgain) return false;
  const next = await Notifications.requestPermissionsAsync();
  return next.status === 'granted';
};

const fetchDevicePushToken = async (): Promise<{
  token: string;
  isDevicePushToken: boolean;
} | null> => {
  try {
    const native = await Notifications.getDevicePushTokenAsync();
    if (native?.data && typeof native.data === 'string') {
      return { token: native.data, isDevicePushToken: true };
    }
  } catch {
    // fall through to expo token
  }

  try {
    const projectId =
      Constants.expoConfig?.extra?.eas?.projectId ?? Constants.easConfig?.projectId ?? undefined;
    const expoResp = await Notifications.getExpoPushTokenAsync(
      projectId ? { projectId } : undefined,
    );
    if (expoResp?.data) {
      return { token: expoResp.data, isDevicePushToken: false };
    }
  } catch {
    return null;
  }
  return null;
};

export const getCachedDeviceToken = async (): Promise<string | null> =>
  SecureStore.getItemAsync(TOKEN_CACHE_KEY);

const cacheToken = async (token: string, platform: PushPlatform): Promise<void> => {
  await SecureStore.setItemAsync(TOKEN_CACHE_KEY, token);
  await SecureStore.setItemAsync(PLATFORM_CACHE_KEY, platform);
};

const submitTokenToBackend = async (token: string, platform: PushPlatform): Promise<void> => {
  await apiClient.post('/api/v1/notifications/device-tokens', {
    token,
    platform,
    deviceName: Device.deviceName ?? null,
    osVersion: Device.osVersion ?? null,
  });
};

export const registerDeviceToken = async (): Promise<RegisteredDeviceToken | null> => {
  if (!Device.isDevice) return null;
  await ensureAndroidChannel();
  const granted = await requestPermissionsIfNeeded();
  if (!granted) return null;

  const result = await fetchDevicePushToken();
  if (!result) return null;

  const platform = resolvePlatform();
  const previous = await getCachedDeviceToken();

  if (previous !== result.token) {
    try {
      await submitTokenToBackend(result.token, platform);
      await cacheToken(result.token, platform);
    } catch {
      // backend may reject if endpoint not yet deployed; cache to retry later
    }
  }

  return {
    token: result.token,
    platform,
    isDevicePushToken: result.isDevicePushToken,
  };
};

export const unregisterDeviceToken = async (): Promise<void> => {
  const token = await getCachedDeviceToken();
  if (!token) return;
  try {
    await apiClient.delete(`/api/v1/notifications/device-tokens/${encodeURIComponent(token)}`);
  } catch {
    // ignore — token will rotate on next install
  }
  await SecureStore.deleteItemAsync(TOKEN_CACHE_KEY);
  await SecureStore.deleteItemAsync(PLATFORM_CACHE_KEY);
};
