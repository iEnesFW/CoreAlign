import type * as SQLite from 'expo-sqlite';
import { getDatabase } from './database';

export const getDb = async (): Promise<SQLite.SQLiteDatabase> => getDatabase();
