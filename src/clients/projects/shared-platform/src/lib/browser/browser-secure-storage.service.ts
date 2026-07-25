import { Injectable } from '@angular/core';
import type { SecureStorageService } from '../interfaces/secure-storage-service.interface';

/** Browser uses sessionStorage for short-lived non-auth data only. Auth uses HttpOnly cookies. */
@Injectable()
export class BrowserSecureStorageService implements SecureStorageService {
  async get(key: string): Promise<string | null> {
    return sessionStorage.getItem(key);
  }

  async set(key: string, value: string): Promise<void> {
    sessionStorage.setItem(key, value);
  }

  async remove(key: string): Promise<void> {
    sessionStorage.removeItem(key);
  }

  async clear(): Promise<void> {
    sessionStorage.clear();
  }
}
