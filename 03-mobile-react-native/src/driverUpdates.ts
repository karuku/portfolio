export const tripStatuses = ['accepted', 'en-route', 'arrived', 'picked-up', 'completed'] as const;

export type TripStatus = (typeof tripStatuses)[number];

export interface DriverUpdate {
  id: string;
  tripId: string;
  status: TripStatus;
  createdAt: string;
  attempts: number;
}

export interface OutboxRepository {
  listPending(): Promise<DriverUpdate[]>;
  remove(id: string): Promise<void>;
  recordFailure(id: string, message: string): Promise<void>;
}

export interface NetworkState {
  isConnected?: boolean;
  isInternetReachable?: boolean;
}

export function isNetworkReady(state: NetworkState): boolean {
  return state.isConnected === true && state.isInternetReachable !== false;
}

export function nextTripStatus(current: TripStatus | null): TripStatus | null {
  const currentIndex = current === null ? -1 : tripStatuses.indexOf(current);
  return tripStatuses[currentIndex + 1] ?? null;
}

export function tripStatusLabel(status: TripStatus | null): string {
  if (status === null) return 'Ready to accept';
  switch (status) {
    case 'accepted':
      return 'Trip accepted';
    case 'en-route':
      return 'En route to pickup';
    case 'arrived':
      return 'At pickup point';
    case 'picked-up':
      return 'Passenger on board';
    case 'completed':
      return 'Trip completed';
  }
}

export async function flushOutbox(
  repository: OutboxRepository,
  send: (update: DriverUpdate) => Promise<void>,
): Promise<{ sent: number; failed: boolean }> {
  const updates = await repository.listPending();
  let sent = 0;

  for (const update of updates) {
    try {
      await send(update);
      await repository.remove(update.id);
      sent += 1;
    } catch (error) {
      const message = error instanceof Error ? error.message.slice(0, 160) : 'Delivery failed';
      await repository.recordFailure(update.id, message);
      return { sent, failed: true };
    }
  }

  return { sent, failed: false };
}