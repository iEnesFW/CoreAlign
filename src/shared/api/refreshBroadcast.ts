import { logger } from '@/shared/lib/logger';

/**
 * Cross-tab broadcast for auth-token rotation.
 *
 * The access token lives in memory (never localStorage) so a refresh in tab A
 * doesn't reach tab B by itself. Without coordination, tab B's queued requests
 * after `waitForRefreshLock` would retry with their stale token → 401 loop.
 *
 * BroadcastChannel is same-origin only, so the token never leaves the browser
 * profile. We fall back to a localStorage "ping" channel for browsers without
 * BroadcastChannel support (older Safari < 15.4).
 */

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
  // Fallback for browsers without BroadcastChannel — localStorage 'storage' event
  // fires across tabs of the same origin. We never persist anything; we write a
  // single payload then immediately remove it to keep the token off-disk.
  window.addEventListener('storage', (e) => {
    if (e.key !== STORAGE_KEY || !e.newValue) return;
    try {
      const parsed = JSON.parse(e.newValue) as AuthBroadcastMessage;
      fanout(parsed);
    } catch {
      /* ignore malformed payload */
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
  // Storage-event fallback
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(msg));
    // Remove immediately so the token doesn't sit on disk.
    window.localStorage.removeItem(STORAGE_KEY);
  } catch {
    /* localStorage unavailable */
  }
};
