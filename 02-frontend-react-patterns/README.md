# React and TypeScript patterns

These examples are intentionally framework-light so the ideas can move between React, React Native, and Next.js applications.

## Included patterns

- `apiClient.ts`: typed JSON requests with one coordinated token refresh for concurrent 401 responses.
- `usePolling.ts`: cancellable polling with backoff and visibility awareness.
- `asyncState.ts`: a discriminated union that makes loading, success, and error states explicit.

Compile the module with:

```powershell
npm install
npm run typecheck
```

The module does not store tokens in local storage. In a browser product, prefer secure, httpOnly cookies and a backend-for-frontend where the threat model allows it.
