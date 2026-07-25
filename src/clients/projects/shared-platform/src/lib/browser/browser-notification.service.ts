import { Injectable } from '@angular/core';
import type { NotificationService } from '../interfaces/notification-service.interface';

@Injectable()
export class BrowserNotificationService implements NotificationService {
  async requestPermission(): Promise<'granted' | 'denied' | 'default'> {
    if (typeof Notification === 'undefined') {
      return 'denied';
    }
    return Notification.requestPermission();
  }

  async registerDevice(): Promise<string | null> {
    return null;
  }
}
