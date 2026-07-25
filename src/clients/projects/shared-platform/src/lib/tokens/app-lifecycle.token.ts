import { InjectionToken } from '@angular/core';
import type { AppLifecycleService } from '../interfaces/app-lifecycle-service.interface';

export const PLATFORM_APP_LIFECYCLE = new InjectionToken<AppLifecycleService>('PLATFORM_APP_LIFECYCLE');
