import { logger } from '@/shared/lib/logger';

export type OfflineMutationType =
  | 'updateChecklist'
  | 'addPhoto'
  | 'captureSignature'
  | 'acceptInstallation'
  | 'rejectInstallation'
  | 'addPunchListItem'
  | 'resolvePunchListItem';

export interface OfflineMutation<TPayload = unknown> {
  id?: number;
  type: OfflineMutationType;
  payload: TPayload;
  timestamp: number;
  retryCount: number;
  lastError?: string;
  tempFileId?: string;
  tempFileField?: string;
  idempotencyKey?: string;
}

export interface FailedOfflineMutation<TPayload = unknown> {
  id?: number;
  type: OfflineMutationType;
  payload: TPayload;
  originalTimestamp: number;
  failedAtTimestamp: number;
  retryCount: number;
  errorMessage: string;
  idempotencyKey?: string;
}

export const MAX_RETRIES = 5;

const DB_NAME = 'corealign-offline';
const DB_VERSION = 2;
const STORE_NAME = 'pending_mutations';
const FAILED_STORE_NAME = 'failed_mutations';
const BLOB_STORE_NAME = 'pending_blobs';

let dbPromise: Promise<IDBDatabase> | null = null;

const openDb = (): Promise<IDBDatabase> => {
  if (dbPromise) return dbPromise;

  dbPromise = new Promise((resolve, reject) => {
    if (typeof indexedDB === 'undefined') {
      reject(new Error('IndexedDB unavailable'));
      return;
    }

    const request = indexedDB.open(DB_NAME, DB_VERSION);

    request.onupgradeneeded = (event) => {
      const db = (event.target as IDBOpenDBRequest).result;
      if (!db.objectStoreNames.contains(STORE_NAME)) {
        const store = db.createObjectStore(STORE_NAME, {
          keyPath: 'id',
          autoIncrement: true,
        });
        store.createIndex('type', 'type', { unique: false });
        store.createIndex('timestamp', 'timestamp', { unique: false });
      }
      if (!db.objectStoreNames.contains(FAILED_STORE_NAME)) {
        const failedStore = db.createObjectStore(FAILED_STORE_NAME, {
          keyPath: 'id',
          autoIncrement: true,
        });
        failedStore.createIndex('type', 'type', { unique: false });
        failedStore.createIndex('failedAtTimestamp', 'failedAtTimestamp', { unique: false });
      }
      if (!db.objectStoreNames.contains(BLOB_STORE_NAME)) {
        db.createObjectStore(BLOB_STORE_NAME, { keyPath: 'tempFileId' });
      }
    };

    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error ?? new Error('IndexedDB open failed'));
  });

  return dbPromise;
};

const withStore = async <T>(
  storeName: string,
  mode: IDBTransactionMode,
  fn: (store: IDBObjectStore) => IDBRequest<T> | Promise<T>,
): Promise<T> => {
  const db = await openDb();
  return new Promise<T>((resolve, reject) => {
    const tx = db.transaction(storeName, mode);
    const store = tx.objectStore(storeName);
    let result: T;
    try {
      const maybeRequest = fn(store);
      if (maybeRequest instanceof IDBRequest) {
        maybeRequest.onsuccess = () => {
          result = maybeRequest.result;
        };
        maybeRequest.onerror = () => reject(maybeRequest.error);
      } else {
        Promise.resolve(maybeRequest).then((r) => {
          result = r;
        });
      }
    } catch (err) {
      reject(err);
      return;
    }
    tx.oncomplete = () => resolve(result);
    tx.onerror = () => reject(tx.error);
    tx.onabort = () => reject(tx.error ?? new Error('Transaction aborted'));
  });
};

export interface PendingBlobEntry {
  tempFileId: string;
  blob: Blob;
  filename: string;
  contentType: string;
  createdAt: number;
}

export const offlineQueueDb = {
  async add<TPayload>(
    mutation: Omit<OfflineMutation<TPayload>, 'id' | 'timestamp' | 'retryCount'>,
  ): Promise<number> {
    const entry: Omit<OfflineMutation<TPayload>, 'id'> = {
      ...mutation,
      timestamp: Date.now(),
      retryCount: 0,
    };
    try {
      const id = await withStore<IDBValidKey>(STORE_NAME, 'readwrite', (store) => store.add(entry));
      logger.info('offline.queue.add', { type: mutation.type, id });
      return Number(id);
    } catch (err) {
      logger.error('offline.queue.add.failed', err, { type: mutation.type });
      throw err;
    }
  },

  async all(): Promise<OfflineMutation[]> {
    try {
      return await withStore<OfflineMutation[]>(STORE_NAME, 'readonly', (store) => store.getAll());
    } catch (err) {
      logger.error('offline.queue.all.failed', err);
      return [];
    }
  },

  async remove(id: number): Promise<void> {
    try {
      await withStore<undefined>(STORE_NAME, 'readwrite', (store) => store.delete(id));
    } catch (err) {
      logger.error('offline.queue.remove.failed', err, { id });
    }
  },

  async update(entry: OfflineMutation): Promise<void> {
    try {
      await withStore<IDBValidKey>(STORE_NAME, 'readwrite', (store) => store.put(entry));
    } catch (err) {
      logger.error('offline.queue.update.failed', err, { id: entry.id });
    }
  },

  async size(): Promise<number> {
    try {
      return await withStore<number>(STORE_NAME, 'readonly', (store) => store.count());
    } catch {
      return 0;
    }
  },

  async clear(): Promise<void> {
    try {
      await withStore<undefined>(STORE_NAME, 'readwrite', (store) => store.clear());
    } catch (err) {
      logger.error('offline.queue.clear.failed', err);
    }
  },

  async moveToFailed(entry: OfflineMutation, errorMessage: string): Promise<void> {
    if (typeof entry.id !== 'number') return;
    const failed: Omit<FailedOfflineMutation, 'id'> = {
      type: entry.type,
      payload: entry.payload,
      originalTimestamp: entry.timestamp,
      failedAtTimestamp: Date.now(),
      retryCount: entry.retryCount,
      errorMessage,
      idempotencyKey: entry.idempotencyKey,
    };
    try {
      await withStore<IDBValidKey>(FAILED_STORE_NAME, 'readwrite', (store) => store.add(failed));
      await withStore<undefined>(STORE_NAME, 'readwrite', (store) => store.delete(entry.id!));
      logger.warn('offline.queue.moved_to_failed', {
        id: entry.id,
        type: entry.type,
        retries: entry.retryCount,
      });
    } catch (err) {
      logger.error('offline.queue.move_failed.failed', err, { id: entry.id });
    }
  },

  async listFailed(): Promise<FailedOfflineMutation[]> {
    try {
      return await withStore<FailedOfflineMutation[]>(FAILED_STORE_NAME, 'readonly', (store) =>
        store.getAll(),
      );
    } catch (err) {
      logger.error('offline.queue.list_failed.failed', err);
      return [];
    }
  },

  async failedSize(): Promise<number> {
    try {
      return await withStore<number>(FAILED_STORE_NAME, 'readonly', (store) => store.count());
    } catch {
      return 0;
    }
  },

  async discardFailed(id: number): Promise<void> {
    try {
      await withStore<undefined>(FAILED_STORE_NAME, 'readwrite', (store) => store.delete(id));
      logger.info('offline.queue.failed.discarded', { id });
    } catch (err) {
      logger.error('offline.queue.failed.discard.failed', err, { id });
    }
  },

  async retryFailed(id: number): Promise<number | null> {
    try {
      const failed = await withStore<FailedOfflineMutation | undefined>(
        FAILED_STORE_NAME,
        'readonly',
        (store) => store.get(id) as IDBRequest<FailedOfflineMutation | undefined>,
      );
      if (!failed) return null;
      const newId = await this.add({
        type: failed.type,
        payload: failed.payload,
        idempotencyKey: failed.idempotencyKey,
      });
      await this.discardFailed(id);
      return newId;
    } catch (err) {
      logger.error('offline.queue.failed.retry.failed', err, { id });
      return null;
    }
  },

  async addBlob(entry: PendingBlobEntry): Promise<void> {
    try {
      await withStore<IDBValidKey>(BLOB_STORE_NAME, 'readwrite', (store) => store.put(entry));
    } catch (err) {
      logger.error('offline.blob.add.failed', err, { tempFileId: entry.tempFileId });
      throw err;
    }
  },

  async getBlob(tempFileId: string): Promise<PendingBlobEntry | null> {
    try {
      const result = await withStore<PendingBlobEntry | undefined>(
        BLOB_STORE_NAME,
        'readonly',
        (store) => store.get(tempFileId) as IDBRequest<PendingBlobEntry | undefined>,
      );
      return result ?? null;
    } catch (err) {
      logger.error('offline.blob.get.failed', err, { tempFileId });
      return null;
    }
  },

  async removeBlob(tempFileId: string): Promise<void> {
    try {
      await withStore<undefined>(BLOB_STORE_NAME, 'readwrite', (store) => store.delete(tempFileId));
    } catch (err) {
      logger.error('offline.blob.remove.failed', err, { tempFileId });
    }
  },
};
