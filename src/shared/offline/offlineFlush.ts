import { logger } from '@/shared/lib/logger';
import {
  MAX_RETRIES,
  offlineQueueDb,
  type OfflineMutation,
  type OfflineMutationType,
} from './offlineQueueDb';

type OfflineExecutor = (payload: unknown) => Promise<void>;

const executors = new Map<OfflineMutationType, OfflineExecutor>();

export const registerOfflineExecutor = (
  type: OfflineMutationType,
  executor: OfflineExecutor,
): void => {
  executors.set(type, executor);
};

const executeMutation = async (mutation: OfflineMutation): Promise<void> => {
  const executor = executors.get(mutation.type);
  if (!executor) {
    throw new Error(`No offline executor registered for mutation type: ${mutation.type}`);
  }
  await executor(mutation.payload);
};

export interface FlushResult {
  flushed: number;
  failed: number;
  permanentlyFailed: number;
  remaining: number;
}

let flushing = false;

const cleanupBlobForMutation = async (entry: OfflineMutation): Promise<void> => {
  if (!entry.tempFileId) return;
  await offlineQueueDb.removeBlob(entry.tempFileId);
};

export const flushOfflineQueue = async (): Promise<FlushResult> => {
  if (flushing) {
    return {
      flushed: 0,
      failed: 0,
      permanentlyFailed: 0,
      remaining: await offlineQueueDb.size(),
    };
  }
  flushing = true;

  let flushed = 0;
  let failed = 0;
  let permanentlyFailed = 0;

  try {
    const queue = await offlineQueueDb.all();
    queue.sort((a, b) => a.timestamp - b.timestamp);

    for (const entry of queue) {
      if (typeof entry.id !== 'number') continue;
      try {
        await executeMutation(entry);
        await offlineQueueDb.remove(entry.id);
        await cleanupBlobForMutation(entry);
        flushed += 1;
      } catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        const nextRetry = entry.retryCount + 1;
        if (nextRetry >= MAX_RETRIES) {
          logger.error('offline.queue.flush.max_retries_exceeded', err, {
            id: entry.id,
            type: entry.type,
            retries: nextRetry,
          });
          await offlineQueueDb.moveToFailed(entry, message);
          permanentlyFailed += 1;
        } else {
          await offlineQueueDb.update({
            ...entry,
            retryCount: nextRetry,
            lastError: message,
          });
          failed += 1;
        }
      }
    }
  } finally {
    flushing = false;
  }

  const remaining = await offlineQueueDb.size();
  if (flushed > 0 || failed > 0 || permanentlyFailed > 0) {
    logger.info('offline.queue.flush.done', {
      flushed,
      failed,
      permanentlyFailed,
      remaining,
    });
  }
  return { flushed, failed, permanentlyFailed, remaining };
};
