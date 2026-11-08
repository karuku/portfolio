import type { DriverUpdate } from './driverUpdates';

export function createUpdateSender(baseUrl: string | undefined) {
  const normalizedBaseUrl = baseUrl?.trim().replace(/\/+$/, '');
  if (!normalizedBaseUrl) return null;

  return async (update: DriverUpdate): Promise<void> => {
    const response = await fetch(`${normalizedBaseUrl}/api/driver-updates`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Idempotency-Key': update.id,
      },
      body: JSON.stringify({
        id: update.id,
        tripId: update.tripId,
        status: update.status,
        occurredAt: update.createdAt,
      }),
    });

    if (!response.ok) throw new Error(`Server returned ${response.status}`);
  };
}