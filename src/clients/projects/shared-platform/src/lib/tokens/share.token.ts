import { InjectionToken } from '@angular/core';
import type { ShareService } from '../interfaces/share-service.interface';

export const PLATFORM_SHARE = new InjectionToken<ShareService>('PLATFORM_SHARE');
