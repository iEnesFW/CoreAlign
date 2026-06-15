import * as SQLite from 'expo-sqlite';
import { runMigrations } from './migrations';

const DATABASE_NAME = 'corealign-offline.db';

let dbInstance: SQLite.SQLiteDatabase | null = null;
let openPromise: Promise<SQLite.SQLiteDatabase> | null = null;

const openInternal = async (): Promise<SQLite.SQLiteDatabase> => {
  const db = await SQLite.openDatabaseAsync(DATABASE_NAME);
  await runMigrations(db);
  return db;
};

export const getDatabase = async (): Promise<SQLite.SQLiteDatabase> => {
  if (dbInstance) return dbInstance;
  if (!openPromise) {
    openPromise = openInternal()
      .then((db) => {
        dbInstance = db;
        return db;
      })
      .catch((err) => {
        openPromise = null;
        throw err;
      });
  }
  return openPromise;
};

export const closeDatabase = async (): Promise<void> => {
  if (!dbInstance) return;
  await dbInstance.closeAsync();
  dbInstance = null;
  openPromise = null;
};

export const DATABASE_FILE_NAME = DATABASE_NAME;
