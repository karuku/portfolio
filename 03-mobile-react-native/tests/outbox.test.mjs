import assert from 'node:assert/strict';
import test from 'node:test';
import { flushOutbox, isNetworkReady, nextTripStatus } from '../src/driverUpdates.ts';

function createRepository(updates) {
  const remaining = [...updates];
  const failures = [];

  return {
    remaining,
    failures,
    async listPending() {
      return [...remaining];
    },
    async remove(id) {
      const index = remaining.findIndex((update) => update.id === id);
      if (index >= 0) remaining.splice(index, 1);
    },
    async recordFailure(id, message) {
      failures.push({ id, message });
    },
  };
}

const updates = [
  { id: 'one', tripId: 'DEMO-0142', status: 'accepted', createdAt: '2026-01-01T10:00:00Z', attempts: 0 },
  { id: 'two', tripId: 'DEMO-0142', status: 'en-route', createdAt: '2026-01-01T10:01:00Z', attempts: 0 },
];

test('trip statuses advance in order and stop after completion', () => {
  assert.equal(nextTripStatus(null), 'accepted');
  assert.equal(nextTripStatus('accepted'), 'en-route');
  assert.equal(nextTripStatus('completed'), null);
});

test('network readiness requires a connection and no confirmed reachability failure', () => {
  assert.equal(isNetworkReady({ isConnected: true, isInternetReachable: true }), true);
  assert.equal(isNetworkReady({ isConnected: true, isInternetReachable: undefined }), true);
  assert.equal(isNetworkReady({ isConnected: true, isInternetReachable: false }), false);
  assert.equal(isNetworkReady({ isConnected: false }), false);
});

test('outbox sends in order and removes acknowledged updates', async () => {
  const repository = createRepository(updates);
  const sent = [];

  const result = await flushOutbox(repository, async (update) => sent.push(update.id));

  assert.deepEqual(result, { sent: 2, failed: false });
  assert.deepEqual(sent, ['one', 'two']);
  assert.deepEqual(repository.remaining, []);
});

test('outbox stops at the first failure and retains later updates', async () => {
  const repository = createRepository(updates);
  const sent = [];

  const result = await flushOutbox(repository, async (update) => {
    sent.push(update.id);
    if (update.id === 'one') throw new Error('Temporary outage');
  });

  assert.deepEqual(result, { sent: 0, failed: true });
  assert.deepEqual(sent, ['one']);
  assert.deepEqual(repository.remaining.map((update) => update.id), ['one', 'two']);
  assert.deepEqual(repository.failures, [{ id: 'one', message: 'Temporary outage' }]);
});