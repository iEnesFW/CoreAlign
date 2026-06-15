import type { SQLiteDatabase } from 'expo-sqlite';

export const SCHEMA_VERSION = 3;

const PRAGMA_USER_VERSION = 'PRAGMA user_version';

const readUserVersion = async (db: SQLiteDatabase): Promise<number> => {
  const row = await db.getFirstAsync<{ user_version: number }>(PRAGMA_USER_VERSION);
  return row?.user_version ?? 0;
};

const writeUserVersion = async (db: SQLiteDatabase, version: number): Promise<void> => {
  await db.execAsync(`PRAGMA user_version = ${version}`);
};

const migrateToV1 = async (db: SQLiteDatabase): Promise<void> => {
  await db.execAsync(`
    CREATE TABLE IF NOT EXISTS pending_mutations (
      id TEXT PRIMARY KEY NOT NULL,
      type TEXT NOT NULL,
      ref_id TEXT,
      payload TEXT NOT NULL,
      idempotency_key TEXT NOT NULL,
      retry_count INTEGER NOT NULL DEFAULT 0,
      status TEXT NOT NULL DEFAULT 'pending',
      last_error TEXT,
      created_at INTEGER NOT NULL,
      updated_at INTEGER NOT NULL
    );

    CREATE TABLE IF NOT EXISTS pending_acceptances (
      id TEXT PRIMARY KEY NOT NULL,
      installation_id TEXT NOT NULL,
      payload TEXT NOT NULL,
      created_at INTEGER NOT NULL,
      attempts INTEGER NOT NULL DEFAULT 0,
      last_error TEXT
    );

    CREATE TABLE IF NOT EXISTS pending_ticket_updates (
      id TEXT PRIMARY KEY NOT NULL,
      ticket_id TEXT NOT NULL,
      payload TEXT NOT NULL,
      created_at INTEGER NOT NULL,
      attempts INTEGER NOT NULL DEFAULT 0,
      last_error TEXT
    );

    CREATE TABLE IF NOT EXISTS cached_installations (
      id TEXT PRIMARY KEY NOT NULL,
      project_id TEXT,
      status TEXT,
      data_json TEXT NOT NULL,
      last_synced_at INTEGER NOT NULL
    );

    CREATE TABLE IF NOT EXISTS cached_tickets (
      id TEXT PRIMARY KEY NOT NULL,
      project_id TEXT,
      status TEXT,
      data_json TEXT NOT NULL,
      last_synced_at INTEGER NOT NULL
    );

    CREATE TABLE IF NOT EXISTS cached_projects (
      id TEXT PRIMARY KEY NOT NULL,
      data_json TEXT NOT NULL,
      last_synced_at INTEGER NOT NULL
    );
  `);
};

const migrateToV2 = async (db: SQLiteDatabase): Promise<void> => {
  await db.execAsync(`
    CREATE INDEX IF NOT EXISTS idx_pending_mutations_status
      ON pending_mutations (status);
    CREATE INDEX IF NOT EXISTS idx_pending_mutations_retry_count
      ON pending_mutations (retry_count);
    CREATE INDEX IF NOT EXISTS idx_pending_mutations_status_created
      ON pending_mutations (status, created_at);
    CREATE INDEX IF NOT EXISTS idx_cached_installations_project
      ON cached_installations (project_id);
    CREATE INDEX IF NOT EXISTS idx_cached_installations_status
      ON cached_installations (status);
    CREATE INDEX IF NOT EXISTS idx_cached_tickets_status
      ON cached_tickets (status);
  `);
};

const migrateToV3 = async (db: SQLiteDatabase): Promise<void> => {
  await db.execAsync(`
    ALTER TABLE pending_mutations ADD COLUMN device_id TEXT;
    CREATE INDEX IF NOT EXISTS idx_pending_mutations_device
      ON pending_mutations (device_id);
  `);
};

export const runMigrations = async (db: SQLiteDatabase): Promise<void> => {
  await db.execAsync('PRAGMA journal_mode = WAL');
  await db.execAsync('PRAGMA foreign_keys = ON');

  const current = await readUserVersion(db);

  if (current < 1) {
    await migrateToV1(db);
    await writeUserVersion(db, 1);
  }

  if (current < 2) {
    await migrateToV2(db);
    await writeUserVersion(db, 2);
  }

  if (current < 3) {
    await migrateToV3(db);
    await writeUserVersion(db, 3);
  }
};
