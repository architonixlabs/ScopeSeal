import { Injectable } from '@angular/core';
import type { AppLifecycleService, AppLifecycleState } from '../interfaces/app-lifecycle-service.interface';

@Injectable()
export class BrowserAppLifecycleService implements AppLifecycleService {
  readonly state: AppLifecycleState = 'active';

  onStateChange(callback: (state: AppLifecycleState) => void): () => void {
    const handler = () => {
      callback(document.hidden ? 'background' : 'active');
    };
    document.addEventListener('visibilitychange', handler);
    return () => document.removeEventListener('visibilitychange', handler);
  }
}
