import { useEffect, useState, useCallback, useRef } from 'react';
import { logger } from '@/shared/lib/logger';
import { offlineQueueDb } from './offlineQueueDb';
import { flushOfflineQueue, type FlushResult } from './offlineFlush';

export interface OfflineState {
  isOnline: boolean;
  queueSize: number;
  failedQueueSize: number;
  isFlushing: boolean;
  lastFlush: FlushResult | null;
  flush: () => Promise<FlushResult>;
  refreshSize: () => Promise<void>;
}

const FLUSH_LOCK_KEY = 'corealign.offlineSync.flushLock';
const FLUSH_LOCK_TTL_MS = 5 * 60 * 1000;
const BROADCAST_CHANNEL = 'corealign-offline-sync';

type SyncBroadcast =
  | { kind: 'flush:start'; tabId: string; lockTs: number }
  | { kind: 'flush:end'; tabId: string; result: FlushResult }
  | { kind: 'queue:size'; size: number; failedSize: number };

const getInitialOnline = (): boolean => {
  if (typeof navigator === 'undefined') return true;
  return navigator.onLine;
};

interface FlushLockRecord {
  tabId: string;
  ts: number;
}

const readLock = (): FlushLockRecord | null => {
  if (typeof window === 'undefined') return null;
  try {
    const raw = window.localStorage.getItem(FLUSH_LOCK_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as FlushLockRecord;
    if (!parsed.tabId || typeof parsed.ts !== 'number') return null;
    if (Date.now() - parsed.ts > FLUSH_LOCK_TTL_MS) {
      window.localStorage.removeItem(FLUSH_LOCK_KEY);
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
};

const acquireLock = (tabId: string): boolean => {
  if (typeof window === 'undefined') return true;
  const existing = readLock();
  if (existing && existing.tabId !== tabId) return false;
  try {
    window.localStorage.setItem(
      FLUSH_LOCK_KEY,
      JSON.stringify({ tabId, ts: Date.now() } satisfies FlushLockRecord),
    );
    return true;
  } catch {
    return false;
  }
};

const releaseLock = (tabId: string): void => {
  if (typeof window === 'undefined') return;
  const existing = readLock();
  if (!existing || existing.tabId === tabId) {
    try {
      window.localStorage.removeItem(FLUSH_LOCK_KEY);
    } catch {
      void 0;
    }
  }
};

const generateTabId = (): string => {
  const cryptoSource: Crypto | undefined = typeof crypto !== 'undefined' ? crypto : undefined;
  if (cryptoSource?.randomUUID) {
    return cryptoSource.randomUUID();
  }
  return `tab-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
};

export const useOfflineSync = (): OfflineState => {
  const [isOnline, setIsOnline] = useState<boolean>(getInitialOnline);
  const [queueSize, setQueueSize] = useState<number>(0);
  const [failedQueueSize, setFailedQueueSize] = useState<number>(0);
  const [isFlushing, setIsFlushing] = useState<boolean>(false);
  const [lastFlush, setLastFlush] = useState<FlushResult | null>(null);
  const tabIdRef = useRef<string>(generateTabId());
  const channelRef = useRef<BroadcastChannel | null>(null);

  const refreshSize = useCallback(async () => {
    const size = await offlineQueueDb.size();
    const failed = await offlineQueueDb.failedSize();
    setQueueSize(size);
    setFailedQueueSize(failed);
    if (channelRef.current) {
      channelRef.current.postMessage({
        kind: 'queue:size',
        size,
        failedSize: failed,
      } satisfies SyncBroadcast);
    }
  }, []);

  const flush = useCallback(async (): Promise<FlushResult> => {
    const tabId = tabIdRef.current;
    if (!acquireLock(tabId)) {
      logger.info('offline.flush.skipped.lock_held_by_other_tab', { tabId });
      return {
        flushed: 0,
        failed: 0,
        permanentlyFailed: 0,
        remaining: await offlineQueueDb.size(),
      };
    }

    setIsFlushing(true);
    if (channelRef.current) {
      channelRef.current.postMessage({
        kind: 'flush:start',
        tabId,
        lockTs: Date.now(),
      } satisfies SyncBroadcast);
    }

    try {
      const result = await flushOfflineQueue();
      setLastFlush(result);
      setQueueSize(result.remaining);
      const failed = await offlineQueueDb.failedSize();
      setFailedQueueSize(failed);
      if (channelRef.current) {
        channelRef.current.postMessage({
          kind: 'flush:end',
          tabId,
          result,
        } satisfies SyncBroadcast);
      }
      return result;
    } finally {
      releaseLock(tabId);
      setIsFlushing(false);
    }
  }, []);

  useEffect(() => {
    void refreshSize();

    let channel: BroadcastChannel | null = null;
    if (typeof BroadcastChannel !== 'undefined') {
      try {
        channel = new BroadcastChannel(BROADCAST_CHANNEL);
        channel.onmessage = (event: MessageEvent<SyncBroadcast>) => {
          const data = event.data;
          if (!data || typeof data.kind !== 'string') return;
          if (data.kind === 'flush:start' && data.tabId !== tabIdRef.current) {
            setIsFlushing(true);
          } else if (data.kind === 'flush:end' && data.tabId !== tabIdRef.current) {
            setIsFlushing(false);
            setLastFlush(data.result);
            setQueueSize(data.result.remaining);
            void offlineQueueDb.failedSize().then(setFailedQueueSize);
          } else if (data.kind === 'queue:size') {
            setQueueSize(data.size);
            setFailedQueueSize(data.failedSize);
          }
        };
        channelRef.current = channel;
      } catch {
        channelRef.current = null;
      }
    }

    const handleOnline = () => {
      setIsOnline(true);
      void flush();
    };

    const handleOffline = () => {
      setIsOnline(false);
    };

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    const interval = window.setInterval(() => {
      void refreshSize();
    }, 5000);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
      window.clearInterval(interval);
      if (channel) {
        try {
          channel.close();
        } catch {
          void 0;
        }
      }
      channelRef.current = null;
    };
  }, [flush, refreshSize]);

  return {
    isOnline,
    queueSize,
    failedQueueSize,
    isFlushing,
    lastFlush,
    flush,
    refreshSize,
  };
};
