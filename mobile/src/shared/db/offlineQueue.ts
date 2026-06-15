import { getDb } from './sqlite';

export interface QueuedRecord<T> {
  id: string;
  refId: string;
  payload: T;
  createdAt: number;
  attempts: number;
  lastError: string | null;
}

interface RawRow {
  id: string;
  installation_id?: string;
  ticket_id?: string;
  payload: string;
  created_at: number;
  attempts: number;
  last_error: string | null;
}

const generateId = (): string => {
  const random = Math.random().toString(36).slice(2, 10);
  const ts = Date.now().toString(36);
  return `${ts}-${random}`;
};

const mapRow = <T>(row: RawRow, refField: 'installation_id' | 'ticket_id'): QueuedRecord<T> => ({
  id: row.id,
  refId: (row[refField] ?? '') as string,
  payload: JSON.parse(row.payload) as T,
  createdAt: row.created_at,
  attempts: row.attempts,
  lastError: row.last_error,
});

export const acceptanceQueue = {
  async enqueue<T>(installationId: string, payload: T): Promise<string> {
    const db = await getDb();
    const id = generateId();
    await db.runAsync(
      'INSERT INTO pending_acceptances (id, installation_id, payload, created_at, attempts) VALUES (?, ?, ?, ?, 0)',
      [id, installationId, JSON.stringify(payload), Date.now()],
    );
    return id;
  },
  async list<T>(): Promise<QueuedRecord<T>[]> {
    const db = await getDb();
    const rows = await db.getAllAsync<RawRow>(
      'SELECT id, installation_id, payload, created_at, attempts, last_error FROM pending_acceptances ORDER BY created_at ASC',
    );
    return rows.map((r) => mapRow<T>(r, 'installation_id'));
  },
  async remove(id: string): Promise<void> {
    const db = await getDb();
    await db.runAsync('DELETE FROM pending_acceptances WHERE id = ?', [id]);
  },
  async markFailure(id: string, message: string): Promise<void> {
    const db = await getDb();
    await db.runAsync(
      'UPDATE pending_acceptances SET attempts = attempts + 1, last_error = ? WHERE id = ?',
      [message, id],
    );
  },
};

export const ticketQueue = {
  async enqueue<T>(ticketId: string, payload: T): Promise<string> {
    const db = await getDb();
    const id = generateId();
    await db.runAsync(
      'INSERT INTO pending_ticket_updates (id, ticket_id, payload, created_at, attempts) VALUES (?, ?, ?, ?, 0)',
      [id, ticketId, JSON.stringify(payload), Date.now()],
    );
    return id;
  },
  async list<T>(): Promise<QueuedRecord<T>[]> {
    const db = await getDb();
    const rows = await db.getAllAsync<RawRow>(
      'SELECT id, ticket_id, payload, created_at, attempts, last_error FROM pending_ticket_updates ORDER BY created_at ASC',
    );
    return rows.map((r) => mapRow<T>(r, 'ticket_id'));
  },
  async remove(id: string): Promise<void> {
    const db = await getDb();
    await db.runAsync('DELETE FROM pending_ticket_updates WHERE id = ?', [id]);
  },
  async markFailure(id: string, message: string): Promise<void> {
    const db = await getDb();
    await db.runAsync(
      'UPDATE pending_ticket_updates SET attempts = attempts + 1, last_error = ? WHERE id = ?',
      [message, id],
    );
  },
};

interface CachedRow {
  data_json: string;
}

export const installationCache = {
  async upsert(
    id: string,
    payload: unknown,
    metadata?: { projectId?: string | null; status?: string | null },
  ): Promise<void> {
    const db = await getDb();
    await db.runAsync(
      'INSERT OR REPLACE INTO cached_installations (id, project_id, status, data_json, last_synced_at) VALUES (?, ?, ?, ?, ?)',
      [
        id,
        metadata?.projectId ?? null,
        metadata?.status ?? null,
        JSON.stringify(payload),
        Date.now(),
      ],
    );
  },
  async get<T>(id: string): Promise<T | null> {
    const db = await getDb();
    const row = await db.getFirstAsync<CachedRow>(
      'SELECT data_json FROM cached_installations WHERE id = ?',
      [id],
    );
    return row ? (JSON.parse(row.data_json) as T) : null;
  },
};

export const ticketCache = {
  async upsert(
    id: string,
    payload: unknown,
    metadata?: { projectId?: string | null; status?: string | null },
  ): Promise<void> {
    const db = await getDb();
    await db.runAsync(
      'INSERT OR REPLACE INTO cached_tickets (id, project_id, status, data_json, last_synced_at) VALUES (?, ?, ?, ?, ?)',
      [
        id,
        metadata?.projectId ?? null,
        metadata?.status ?? null,
        JSON.stringify(payload),
        Date.now(),
      ],
    );
  },
  async get<T>(id: string): Promise<T | null> {
    const db = await getDb();
    const row = await db.getFirstAsync<CachedRow>(
      'SELECT data_json FROM cached_tickets WHERE id = ?',
      [id],
    );
    return row ? (JSON.parse(row.data_json) as T) : null;
  },
};

export const projectCache = {
  async upsert(id: string, payload: unknown): Promise<void> {
    const db = await getDb();
    await db.runAsync(
      'INSERT OR REPLACE INTO cached_projects (id, data_json, last_synced_at) VALUES (?, ?, ?)',
      [id, JSON.stringify(payload), Date.now()],
    );
  },
  async get<T>(id: string): Promise<T | null> {
    const db = await getDb();
    const row = await db.getFirstAsync<CachedRow>(
      'SELECT data_json FROM cached_projects WHERE id = ?',
      [id],
    );
    return row ? (JSON.parse(row.data_json) as T) : null;
  },
};

export const newIdempotencyKey = (): string => `idem-${generateId()}`;
