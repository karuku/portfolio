export type PollingOptions = {
  intervalMs: number;
  onValue: (value: unknown) => void;
  onError?: (error: Error) => void;
};

export const startPolling = (load: (signal: AbortSignal) => Promise<unknown>, options: PollingOptions): (() => void) => {
  const controller = new AbortController();
  let stopped = false;
  const run = async (): Promise<void> => {
    try {
      options.onValue(await load(controller.signal));
    } catch (error) {
      if (!controller.signal.aborted) options.onError?.(error instanceof Error ? error : new Error('Polling failed.'));
    }
    if (!stopped) setTimeout(() => void run(), options.intervalMs);
  };
  void run();
  return () => { stopped = true; controller.abort(); };
};
