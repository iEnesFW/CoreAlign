import * as Notifications from 'expo-notifications';
import * as Device from 'expo-device';
import { Platform } from 'react-native';
import { apiClient } from '@/api/apiClient';

Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,
    shouldPlaySound: true,
    shouldSetBadge: true,
    shouldShowBanner: true,
    shouldShowList: true,
  }),
});

export const registerForPushNotificationsAsync = async (): Promise<string | null> => {
  if (!Device.isDevice) return null;

  if (Platform.OS === 'android') {
    await Notifications.setNotificationChannelAsync('default', {
      name: 'default',
      importance: Notifications.AndroidImportance.MAX,
      vibrationPattern: [0, 250, 250, 250],
      lightColor: '#0F172A',
    });
  }

  const existing = await Notifications.getPermissionsAsync();
  let status = existing.status;
  if (status !== 'granted') {
    const next = await Notifications.requestPermissionsAsync();
    status = next.status;
  }
  if (status !== 'granted') return null;

  const tokenResponse = await Notifications.getExpoPushTokenAsync();
  return tokenResponse.data;
};

export const submitPushToken = async (token: string): Promise<void> => {
  await apiClient.post('/api/v1/notifications/device-tokens', {
    token,
    platform: Platform.OS,
  });
};

export { registerDeviceToken, unregisterDeviceToken } from './registerDeviceToken';
export { usePushNotifications } from './usePushNotifications';
