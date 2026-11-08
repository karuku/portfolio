# Field Dispatch Demo

A small Expo driver companion that demonstrates a persistent, offline-first update flow. It stores trip status and an ordered outbox in SQLite, exposes connectivity state with Expo Network, and sends updates with an idempotency key when an API URL is configured.

## Run

```powershell
npm ci
npm run typecheck
npm test
npm start
```

Use Expo Go to open the project on a device. For Android and iOS simulators, copy `.env.example` to `.env`. For a physical device, replace `localhost` with the development machine's LAN address. The API endpoint is `POST /api/driver-updates`.

The route and driver are demo data. Without `EXPO_PUBLIC_API_URL`, status changes remain in the local outbox and are not represented as delivered. A real deployment still needs authenticated driver identity, server-side authorization, durable server persistence, and a retention policy for locally stored trip data.

## Design

- `src/driverUpdates.ts` contains the status progression and ordered outbox flush policy; it is independently tested with Node's test runner.
- `src/storage/dispatchRepository.ts` uses parameterized SQLite queries, WAL mode, an index for chronological outbox reads, and a transaction to save status and its event together.
- `src/syncClient.ts` sends each event with its UUID as an `Idempotency-Key`. The receiving service must enforce that key before treating delivery as exactly-once.
- Expo Network supplies device connectivity state. A network connection is only a prerequisite; HTTP failures keep the event queued for retry.

## Verification

```powershell
npm run typecheck
npm test
npx expo export --platform android --platform ios
```

The project targets Expo SDK 58, React Native 0.88, and React 19.3. See the [versioned Expo SQLite documentation](https://docs.expo.dev/versions/v58.0.0/sdk/sqlite/) and [Expo Network documentation](https://docs.expo.dev/versions/v58.0.0/sdk/network/) for the platform APIs used here.