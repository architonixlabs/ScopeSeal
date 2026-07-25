import { InjectionToken } from '@angular/core';
import type { DeepLinkService } from '../interfaces/deep-link-service.interface';

export const PLATFORM_DEEP_LINK = new InjectionToken<DeepLinkService>('PLATFORM_DEEP_LINK');
