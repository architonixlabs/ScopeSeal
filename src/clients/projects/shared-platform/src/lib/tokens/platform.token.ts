import { InjectionToken } from '@angular/core';
import type { PlatformService } from '../interfaces/platform-service.interface';

export const PLATFORM_SERVICE = new InjectionToken<PlatformService>('PLATFORM_SERVICE');
