export type AsyncState<T> =
  | { status: 'idle' | 'loading'; data?: T }
  | { status: 'success'; data: T; updatedAt: number }
  | { status: 'error'; error: Error; data?: T };

export const toError = (value: unknown): Error =>
  value instanceof Error ? value : new Error('The request failed.');
