import { useEffect, useRef } from 'react';
import { AppState, type AppStateStatus } from 'react-native';
import { flushMutations } from './syncQueue';
import { probeIsOnline } from './useNetworkStatus';

export interface UseAppStateSyncOptions {
  enabled?: boolean;
}

export const useAppStateSync = (options: UseAppStateSyncOptions = {}): void => {
  const { enabled = true } = options;
  const previousStateRef = useRef<AppStateStatus>(AppState.currentState);

  useEffect(() => {
    if (!enabled) return undefined;

    const handle = async (next: AppStateStatus): Promise<void> => {
      const previous = previousStateRef.current;
      previousStateRef.current = next;
      if (next !== 'active') return;
      if (previous === 'active') return;
      const online = await probeIsOnline();
      if (!online) return;
      try {
        await flushMutations();
      } catch {
        // surfaces via OfflineBanner failed counter
      }
    };

    const subscription = AppState.addEventListener('change', (next) => {
      void handle(next);
    });

    void (async () => {
      if (AppState.currentState !== 'active') return;
      const online = await probeIsOnline();
      if (online) {
        try {
          await flushMutations();
        } catch {
          // ignore
        }
      }
    })();

    return () => {
      subscription.remove();
    };
  }, [enabled]);
};
