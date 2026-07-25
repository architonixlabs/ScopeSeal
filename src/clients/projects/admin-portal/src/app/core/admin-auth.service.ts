import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'ss-admin-operator-key';

@Injectable({ providedIn: 'root' })
export class AdminAuthService {
  readonly operatorKey = signal<string | null>(this.readStoredKey());

  isAuthenticated(): boolean {
    return !!this.operatorKey()?.trim();
  }

  signIn(key: string): void {
    const trimmed = key.trim();
    sessionStorage.setItem(STORAGE_KEY, trimmed);
    this.operatorKey.set(trimmed);
  }

  signOut(): void {
    sessionStorage.removeItem(STORAGE_KEY);
    this.operatorKey.set(null);
  }

  private readStoredKey(): string | null {
    return sessionStorage.getItem(STORAGE_KEY);
  }
}
