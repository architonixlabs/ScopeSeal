import { Injectable, inject } from '@angular/core';
import { AdminAuthService } from './admin-auth.service';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly auth = inject(AdminAuthService);

  async get<T>(path: string): Promise<T> {
    const response = await fetch(`${environment.apiBaseUrl}${path}`, {
      headers: this.headers(),
    });

    if (!response.ok) {
      throw new Error(`Admin API error ${response.status}`);
    }

    return response.json() as Promise<T>;
  }

  async put<T>(path: string, body: unknown): Promise<T> {
    const response = await fetch(`${environment.apiBaseUrl}${path}`, {
      method: 'PUT',
      headers: {
        ...this.headers(),
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      throw new Error(`Admin API error ${response.status}`);
    }

    return response.json() as Promise<T>;
  }

  private headers(): Record<string, string> {
    const key = this.auth.operatorKey();
    if (!key) {
      throw new Error('Operator key required');
    }

    return { 'X-Platform-Operator-Key': key };
  }
}
