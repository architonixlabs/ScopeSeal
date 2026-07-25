import { Injectable } from '@angular/core';
import type { FileCacheService } from '../interfaces/file-cache-service.interface';

interface CacheEntry {
  data: Blob;
  expiresAtUtc: number;
}

@Injectable()
export class BrowserFileCacheService implements FileCacheService {
  private readonly entries = new Map<string, CacheEntry>();

  async store(key: string, data: Blob, expiresAtUtc: Date): Promise<void> {
    this.entries.set(key, { data, expiresAtUtc: expiresAtUtc.getTime() });
  }

  async retrieve(key: string): Promise<Blob | null> {
    const entry = this.entries.get(key);
    if (!entry) {
      return null;
    }
    if (Date.now() > entry.expiresAtUtc) {
      this.entries.delete(key);
      return null;
    }
    return entry.data;
  }

  async remove(key: string): Promise<void> {
    this.entries.delete(key);
  }

  async clearExpired(): Promise<void> {
    const now = Date.now();
    for (const [key, entry] of this.entries) {
      if (now > entry.expiresAtUtc) {
        this.entries.delete(key);
      }
    }
  }
}
