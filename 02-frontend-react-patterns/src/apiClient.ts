export type TokenProvider = {
  getAccessToken: () => string | undefined;
  refresh: () => Promise<string | undefined>;
};

export class ApiClient {
  private refreshInFlight: Promise<string | undefined> | undefined;

  public constructor(private readonly baseUrl: string, private readonly tokens: TokenProvider) {}

  public async get<T>(path: string, signal?: AbortSignal): Promise<T> {
    return this.request<T>(path, signal, true);
  }

  private async request<T>(path: string, signal: AbortSignal | undefined, canRefresh: boolean): Promise<T> {
    const response = await fetch(`${this.baseUrl}${path}`, {
      headers: { Accept: 'application/json', ...(this.tokens.getAccessToken() ? { Authorization: `Bearer ${this.tokens.getAccessToken()}` } : {}) },
      ...(signal ? { signal } : {}),
    });
    if (response.status === 401 && canRefresh) {
      this.refreshInFlight ??= this.tokens.refresh().finally(() => { this.refreshInFlight = undefined; });
      await this.refreshInFlight;
      return this.request<T>(path, signal, false);
    }
    if (!response.ok) throw new Error(`Request failed with status ${response.status}.`);
    return response.json() as Promise<T>;
  }
}
