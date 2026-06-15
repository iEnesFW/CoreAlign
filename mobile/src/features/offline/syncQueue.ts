import { getDatabase } from '@/shared/db/database';
import { getDeviceId } from '@/shared/native/deviceId';

export type MutationStatus = 'pending' | 'in_flight' | 'failed' | 'completed';

export interface MutationRecord<T = unknown> {
  id: string;
  type: string;
  refId: string | null;
  payload: T;
  idempotencyKey: string;
  retryCount: number;
  status: MutationStatus;
  lastError: string | null;
  createdAt: number;
  updatedAt: number;
  deviceId: string | null;
}

interface MutationRow {
  id: string;
  type: string;
  ref_id: string | null;
  payload: string;
  idempotency_key: string;
  retry_count: number;
  status: MutationStatus;
  last_error: string | null;
  created_at: number;
  updated_at: number;
  device_id: string | null;
}

export interface EnqueueInput<T = unknown> {
  type: string;
  payload: T;
  refId?: string | null;
  idempotencyKey?: string;
}

export interface PendingMutationsSummary {
  total: number;
  failed: number;
  oldestCreatedAt: number | null;
}

export type MutationHandler<T = unknown> = (record: MutationRecord<T>) => Promise<void>;

const handlers = new Map<string, MutationHandler>();

const generateId = (): string => {
  const random = Math.random().toString(36).slice(2, 10);
  const ts = Date.now().toString(36);
  return `${ts}-${random}`;
};

const newIdempotencyKey = (): string => `idem-${generateId()}`;

const mapRow = <T>(row: MutationRow): MutationRecord<T> => ({
  id: row.id,
  type: row.type,
  refId: row.ref_id,
  payload: JSON.parse(row.payload) as T,
  idempotencyKey: row.idempotency_key,
  retryCount: row.retry_count,
  status: row.status,
  lastError: row.last_error,
  createdAt: row.created_at,
  updatedAt: row.updated_at,
  deviceId: row.device_id,
});

export const registerMutationHandler = <T>(type: string, handler: MutationHandler<T>): void => {
  handlers.set(type, handler as MutationHandler);
};

export const clearMutationHandlers = (): void => {
  handlers.clear();
};

export const enqueueMutation = async <T>(input: EnqueueInput<T>): Promise<MutationRecord<T>> => {
  const db = await getDatabase();
  const id = generateId();
  const idempotencyKey = input.idempotencyKey ?? newIdempotencyKey();
  const now = Date.now();
  const deviceId = await getDeviceId();
  await db.runAsync(
    `INSERT INTO pending_mutations
     (id, type, ref_id, payload, idempotency_key, retry_count, status, last_error, created_at, updated_at, device_id)
     VALUES (?, ?, ?, ?, ?, 0, 'pending', NULL, ?, ?, ?)`,
    [
      id,
      input.type,
      input.refId ?? null,
      JSON.stringify(input.payload),
      idempotencyKey,
      now,
      now,
      deviceId,
    ],
  );
  return {
    id,
    type: input.type,
    refId: input.refId ?? null,
    payload: input.payload,
    idempotencyKey,
    retryCount: 0,
    status: 'pending',
    lastError: null,
    createdAt: now,
    updatedAt: now,
    deviceId,
  };
};

export const listMutations = async (
  filter: { status?: MutationStatus } = {},
): Promise<MutationRecord[]> => {
  const db = await getDatabase();
  if (filter.status) {
    const rows = await db.getAllAsync<MutationRow>(
      'SELECT id, type, ref_id, payload, idempotency_key, retry_count, status, last_error, created_at, updated_at, device_id FROM pending_mutations WHERE status = ? ORDER BY created_at ASC',
      [filter.status],
    );
    return rows.map((r) => mapRow(r));
  }
  const rows = await db.getAllAsync<MutationRow>(
    'SELECT id, type, ref_id, payload, idempotency_key, retry_count, status, last_error, created_at, updated_at, device_id FROM pending_mutations ORDER BY created_at ASC',
  );
  return rows.map((r) => mapRow(r));
};

export const getPendingSummary = async (): Promise<PendingMutationsSummary> => {
  const db = await getDatabase();
  const totalRow = await db.getFirstAsync<{ total: number; oldest: number | null }>(
    "SELECT COUNT(*) as total, MIN(created_at) as oldest FROM pending_mutations WHERE status IN ('pending','failed','in_flight')",
  );
  const failedRow = await db.getFirstAsync<{ failed: number }>(
    "SELECT COUNT(*) as failed FROM pending_mutations WHERE status = 'failed'",
  );
  return {
    total: totalRow?.total ?? 0,
    failed: failedRow?.failed ?? 0,
    oldestCreatedAt: totalRow?.oldest ?? null,
  };
};

const setStatus = async (
  id: string,
  status: MutationStatus,
  patch: { retryCountDelta?: number; lastError?: string | null } = {},
): Promise<void> => {
  const db = await getDatabase();
  const now = Date.now();
  if (patch.retryCountDelta) {
    await db.runAsync(
      'UPDATE pending_mutations SET status = ?, retry_count = retry_count + ?, last_error = ?, updated_at = ? WHERE id = ?',
      [status, patch.retryCountDelta, patch.lastError ?? null, now, id],
    );
    return;
  }
  await db.runAsync(
    'UPDATE pending_mutations SET status = ?, last_error = ?, updated_at = ? WHERE id = ?',
    [status, patch.lastError ?? null, now, id],
  );
};

export const removeMutation = async (id: string): Promise<void> => {
  const db = await getDatabase();
  await db.runAsync('DELETE FROM pending_mutations WHERE id = ?', [id]);
};

export const MAX_MUTATION_ATTEMPTS = 5;

export interface FlushReport {
  attempted: number;
  succeeded: number;
  failed: number;
  skipped: number;
}

let flushInFlight: Promise<FlushReport> | null = null;

const doFlush = async (): Promise<FlushReport> => {
  const report: FlushReport = { attempted: 0, succeeded: 0, failed: 0, skipped: 0 };
  const db = await getDatabase();
  const rows = await db.getAllAsync<MutationRow>(
    "SELECT id, type, ref_id, payload, idempotency_key, retry_count, status, last_error, created_at, updated_at, device_id FROM pending_mutations WHERE status IN ('pending','failed') AND retry_count < ? ORDER BY created_at ASC",
    [MAX_MUTATION_ATTEMPTS],
  );
  for (const row of rows) {
    const record = mapRow(row);
    const handler = handlers.get(record.type);
    if (!handler) {
      report.skipped += 1;
      continue;
    }
    report.attempted += 1;
    await setStatus(record.id, 'in_flight');
    try {
      await handler(record);
      await removeMutation(record.id);
      report.succeeded += 1;
    } catch (err) {
      const message = err instanceof Error ? err.message : String(err);
      await setStatus(record.id, 'failed', { retryCountDelta: 1, lastError: message });
      report.failed += 1;
    }
  }
  return report;
};

export const flushMutations = async (): Promise<FlushReport> => {
  if (flushInFlight) return flushInFlight;
  flushInFlight = doFlush().finally(() => {
    flushInFlight = null;
  });
  return flushInFlight;
};

export const syncQueue = {
  enqueue: enqueueMutation,
  flush: flushMutations,
  list: listMutations,
  summary: getPendingSummary,
  remove: removeMutation,
  registerHandler: registerMutationHandler,
};
