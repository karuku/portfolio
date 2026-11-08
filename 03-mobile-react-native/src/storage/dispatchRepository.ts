import * as Crypto from 'expo-crypto';
import type { SQLiteDatabase } from 'expo-sqlite';
import type { DriverUpdate, TripStatus } from '../driverUpdates';

export async function initializeDispatchDatabase(database: SQLiteDatabase): Promise<void> {
  await database.execAsync(`
    PRAGMA journal_mode = WAL;

    CREATE TABLE IF NOT EXISTS trip_state (
      trip_id TEXT PRIMARY KEY NOT NULL,
      status TEXT CHECK (status IN ('accepted', 'en-route', 'arrived', 'picked-up', 'completed')),
      updated_at TEXT NOT NULL
    );

    CREATE TABLE IF NOT EXISTS update_outbox (
      id TEXT PRIMARY KEY NOT NULL,
      trip_id TEXT NOT NULL,
      status TEXT NOT NULL CHECK (status IN ('accepted', 'en-route', 'arrived', 'picked-up', 'completed')),
      created_at TEXT NOT NULL,
      attempts INTEGER NOT NULL DEFAULT 0,
      last_error TEXT
    );

    CREATE INDEX IF NOT EXISTS ix_update_outbox_created_at_id
      ON update_outbox (created_at, id);
  `);

  await database.runAsync(
    'INSERT OR IGNORE INTO trip_state (trip_id, status, updated_at) VALUES (?, NULL, ?)',
    'DEMO-0142',
    new Date().toISOString(),
  );
}

export async function getTripStatus(database: SQLiteDatabase, tripId: string): Promise<TripStatus | null> {
  const row = await database.getFirstAsync<{ status: TripStatus | null }>(
    'SELECT status FROM trip_state WHERE trip_id = ?',
    tripId,
  );
  return row?.status ?? null;
}

export async function queueTripStatus(
  database: SQLiteDatabase,
  tripId: string,
  status: TripStatus,
): Promise<void> {
  const id = Crypto.randomUUID();
  const createdAt = new Date().toISOString();

  await database.withTransactionAsync(async () => {
    await database.runAsync(
      'UPDATE trip_state SET status = ?, updated_at = ? WHERE trip_id = ?',
      status,
      createdAt,
      tripId,
    );
    await database.runAsync(
      'INSERT INTO update_outbox (id, trip_id, status, created_at) VALUES (?, ?, ?, ?)',
      id,
      tripId,
      status,
      createdAt,
    );
  });
}

export async function listPendingUpdates(database: SQLiteDatabase): Promise<DriverUpdate[]> {
  return database.getAllAsync<DriverUpdate>(
    `SELECT id, trip_id AS tripId, status, created_at AS createdAt, attempts
     FROM update_outbox
     ORDER BY created_at ASC, id ASC
     LIMIT 100`,
  );
}

export async function removePendingUpdate(database: SQLiteDatabase, id: string): Promise<void> {
  await database.runAsync('DELETE FROM update_outbox WHERE id = ?', id);
}

export async function recordUpdateFailure(
  database: SQLiteDatabase,
  id: string,
  message: string,
): Promise<void> {
  await database.runAsync(
    'UPDATE update_outbox SET attempts = attempts + 1, last_error = ? WHERE id = ?',
    message,
    id,
  );
}

export async function getRecentFailures(database: SQLiteDatabase): Promise<number> {
  const row = await database.getFirstAsync<{ count: number }>(
    'SELECT COUNT(*) AS count FROM update_outbox WHERE attempts > 0',
  );
  return row?.count ?? 0;
}