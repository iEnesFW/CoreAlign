import React, { useEffect } from 'react';
import { registerCoreMutationHandlers } from './mutationHandlers';
import { useAppStateSync } from './useAppStateSync';
import { usePushNotifications } from '@/features/notifications/usePushNotifications';

export interface OfflineProviderProps {
  children: React.ReactNode;
  enablePush?: boolean;
}

export const OfflineProvider: React.FC<OfflineProviderProps> = ({
  children,
  enablePush = true,
}) => {
  useEffect(() => {
    registerCoreMutationHandlers();
  }, []);

  useAppStateSync();
  usePushNotifications();

  return <>{children}</>;
};

const NoopProvider: React.FC<OfflineProviderProps> = ({ children }) => {
  useEffect(() => {
    registerCoreMutationHandlers();
  }, []);
  useAppStateSync();
  return <>{children}</>;
};

export const OfflineProviderNoPush = NoopProvider;
