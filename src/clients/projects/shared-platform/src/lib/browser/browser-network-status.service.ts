import { Injectable } from '@angular/core';
import type { NetworkStatusService } from '../interfaces/network-status-service.interface';

@Injectable()
export class BrowserNetworkStatusService implements NetworkStatusService {
  readonly isOnline = typeof navigator !== 'undefined' ? navigator.onLine : true;

  onStatusChange(callback: (online: boolean) => void): () => void {
    const online = () => callback(true);
    const offline = () => callback(false);
    window.addEventListener('online', online);
    window.addEventListener('offline', offline);
    return () => {
      window.removeEventListener('online', online);
      window.removeEventListener('offline', offline);
    };
  }
}
