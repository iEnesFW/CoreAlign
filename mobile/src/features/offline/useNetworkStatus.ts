import { useCallback, useEffect, useRef, useState } from 'react';
import * as Network from 'expo-network';

export interface NetworkStatus {
  isOnline: boolean;
  isInternetReachable: boolean | null;
  type: Network.NetworkStateType | null;
  lastCheckedAt: number | null;
}

const INITIAL: NetworkStatus = {
  isOnline: true,
  isInternetReachable: null,
  type: null,
  lastCheckedAt: null,
};

const POLL_INTERVAL_MS = 8000;

const toStatus = (state: Network.NetworkState): NetworkStatus => ({
  isOnline: Boolean(state.isConnected && state.isInternetReachable !== false),
  isInternetReachable: state.isInternetReachable ?? null,
  type: state.type ?? null,
  lastCheckedAt: Date.now(),
});

export interface UseNetworkStatusOptions {
  pollIntervalMs?: number;
  onReconnect?: () => void | Promise<void>;
}

export const useNetworkStatus = (
  options: UseNetworkStatusOptions = {},
): NetworkStatus & { refresh: () => Promise<void> } => {
  const [status, setStatus] = useState<NetworkStatus>(INITIAL);
  const lastOnlineRef = useRef<boolean>(true);
  const { pollIntervalMs = POLL_INTERVAL_MS, onReconnect } = options;
  const onReconnectRef = useRef(onReconnect);

  useEffect(() => {
    onReconnectRef.current = onReconnect;
  }, [onReconnect]);

  const refresh = useCallback(async (): Promise<void> => {
    const next = await Network.getNetworkStateAsync();
    const mapped = toStatus(next);
    setStatus(mapped);
    if (!lastOnlineRef.current && mapped.isOnline) {
      try {
        await onReconnectRef.current?.();
      } catch {
        // swallow — surfaces in queue retry counter
      }
    }
    lastOnlineRef.current = mapped.isOnline;
  }, []);

  useEffect(() => {
    let cancelled = false;
    const tick = async (): Promise<void> => {
      if (cancelled) return;
      await refresh();
    };
    void tick();
    const handle = setInterval(() => {
      void tick();
    }, pollIntervalMs);
    return () => {
      cancelled = true;
      clearInterval(handle);
    };
  }, [pollIntervalMs, refresh]);

  return { ...status, refresh };
};

export const probeIsOnline = async (): Promise<boolean> => {
  const state = await Network.getNetworkStateAsync();
  return Boolean(state.isConnected && state.isInternetReachable !== false);
};
