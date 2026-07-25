import { InjectionToken } from '@angular/core';
import type { NotificationService } from '../interfaces/notification-service.interface';

export const PLATFORM_NOTIFICATION = new InjectionToken<NotificationService>('PLATFORM_NOTIFICATION');
