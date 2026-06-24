import { logger } from '@/shared/lib/logger';

const CHANNEL_NAME = 'corealign:auth';
const STORAGE_KEY = 'corealign:auth:bc-fallback';

export type AuthBroadcastMessage =
  | { type: 'token-refreshed'; accessToken: string; at: number }
  | { type: 'signed-out'; at: number };

type Listener = (msg: AuthBroadcastMessage) => void;

let channel: BroadcastChannel | null = null;
const listeners = new Set<Listener>();

const ensureChannel = (): BroadcastChannel | null => {
  if (channel) return channel;
  if (typeof window === 'undefined' || typeof BroadcastChannel === 'undefined') return null;
  try {
    channel = new BroadcastChannel(CHANNEL_NAME);
    channel.onmessage = (e) => fanout(e.data as AuthBroadcastMessage);
  } catch (err) {
    logger.warn('refreshBroadcast.create-failed', { err: (err as Error)?.message });
    channel = null;
  }
  return channel;
};

const fanout = (msg: AuthBroadcastMessage) => {
  listeners.forEach((l) => {
    try {
      l(msg);
    } catch (err) {
      logger.warn('refreshBroadcast.listener-threw', { err: (err as Error)?.message });
    }
  });
};

if (typeof window !== 'undefined') {
  window.addEventListener('storage', (e) => {
    if (e.key !== STORAGE_KEY || !e.newValue) return;
    try {
      const parsed = JSON.parse(e.newValue) as AuthBroadcastMessage;
      fanout(parsed);
    } catch {
      // WHY: malformed cross-tab payloads are ignored by design
    }
  });
}

export const subscribeRefreshBroadcast = (listener: Listener): (() => void) => {
  ensureChannel();
  listeners.add(listener);
  return () => listeners.delete(listener);
};

export const broadcastRefresh = (msg: AuthBroadcastMessage): void => {
  const ch = ensureChannel();
  if (ch) {
    try {
      ch.postMessage(msg);
      return;
    } catch (err) {
      logger.warn('refreshBroadcast.post-failed', { err: (err as Error)?.message });
    }
  }
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(msg));
    window.localStorage.removeItem(STORAGE_KEY);
  } catch {
    // WHY: localStorage may be unavailable (private mode); broadcast is best-effort
  }
};
