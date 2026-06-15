import { useEffect, useRef, useState } from 'react';
import * as Notifications from 'expo-notifications';
import { useRouter } from 'expo-router';
import { registerDeviceToken, type RegisteredDeviceToken } from './registerDeviceToken';

export type PushNotificationKind =
  | 'installation.assigned'
  | 'installation.acceptanceRequired'
  | 'ticket.assigned'
  | 'ticket.statusChanged'
  | 'unknown';

interface PushPayload {
  kind?: PushNotificationKind;
  installationId?: string;
  ticketId?: string;
  projectId?: string;
}

const readPayload = (notification: Notifications.Notification): PushPayload => {
  const data = (notification.request?.content?.data ?? {}) as Record<string, unknown>;
  const kind = typeof data.kind === 'string' ? (data.kind as PushNotificationKind) : 'unknown';
  return {
    kind,
    installationId: typeof data.installationId === 'string' ? data.installationId : undefined,
    ticketId: typeof data.ticketId === 'string' ? data.ticketId : undefined,
    projectId: typeof data.projectId === 'string' ? data.projectId : undefined,
  };
};

export interface UsePushNotificationsResult {
  token: RegisteredDeviceToken | null;
  permissionDenied: boolean;
}

export const usePushNotifications = (): UsePushNotificationsResult => {
  const router = useRouter();
  const [token, setToken] = useState<RegisteredDeviceToken | null>(null);
  const [permissionDenied, setPermissionDenied] = useState<boolean>(false);
  const receivedSub = useRef<Notifications.Subscription | null>(null);
  const responseSub = useRef<Notifications.Subscription | null>(null);

  useEffect(() => {
    let cancelled = false;
    const register = async (): Promise<void> => {
      const result = await registerDeviceToken();
      if (cancelled) return;
      if (!result) {
        setPermissionDenied(true);
        return;
      }
      setToken(result);
    };
    void register();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    receivedSub.current = Notifications.addNotificationReceivedListener(() => {
      // hook for future toast surface
    });
    responseSub.current = Notifications.addNotificationResponseReceivedListener((response) => {
      const payload = readPayload(response.notification);
      if (payload.installationId) {
        router.push(`/installation/${payload.installationId}`);
        return;
      }
      if (payload.ticketId) {
        router.push(`/ticket/${payload.ticketId}`);
        return;
      }
      if (payload.projectId) {
        router.push(`/project/${payload.projectId}`);
      }
    });
    return () => {
      receivedSub.current?.remove();
      responseSub.current?.remove();
      receivedSub.current = null;
      responseSub.current = null;
    };
  }, [router]);

  return { token, permissionDenied };
};
