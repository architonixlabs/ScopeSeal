/** Temporary encrypted upload queue and preview cache abstraction. */
export interface FileCacheService {
  store(key: string, data: Blob, expiresAtUtc: Date): Promise<void>;
  retrieve(key: string): Promise<Blob | null>;
  remove(key: string): Promise<void>;
  clearExpired(): Promise<void>;
}
