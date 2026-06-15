import { logger } from '@/shared/lib/logger';
import { installationAcceptanceApi } from '@/features/installation-acceptance/api/installationAcceptanceApi';
import type {
  AcceptInstallationInput,
  AddPunchListItemInput,
  CaptureSignatureInput,
  RejectInstallationInput,
  ResolvePunchListItemInput,
  UpdateChecklistItemInput,
  UploadPhotoInput,
} from '@/features/installation-acceptance/model/installationAcceptance.types';
import { MAX_RETRIES, offlineQueueDb, type OfflineMutation } from './offlineQueueDb';

const executeMutation = async (mutation: OfflineMutation): Promise<void> => {
  switch (mutation.type) {
    case 'updateChecklist':
      await installationAcceptanceApi.updateChecklist(mutation.payload as UpdateChecklistItemInput);
      return;
    case 'addPhoto':
      await installationAcceptanceApi.addPhoto(mutation.payload as UploadPhotoInput);
      return;
    case 'captureSignature':
      await installationAcceptanceApi.captureSignature(mutation.payload as CaptureSignatureInput);
      return;
    case 'acceptInstallation':
      await installationAcceptanceApi.accept(mutation.payload as AcceptInstallationInput);
      return;
    case 'rejectInstallation':
      await installationAcceptanceApi.reject(mutation.payload as RejectInstallationInput);
      return;
    case 'addPunchListItem':
      await installationAcceptanceApi.addPunchListItem(mutation.payload as AddPunchListItemInput);
      return;
    case 'resolvePunchListItem':
      await installationAcceptanceApi.resolvePunchListItem(
        mutation.payload as ResolvePunchListItemInput,
      );
      return;
    default: {
      const exhaustive: never = mutation.type;
      throw new Error(`Unknown offline mutation type: ${String(exhaustive)}`);
    }
  }
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
