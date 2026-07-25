import { Injectable } from '@angular/core';
import type { PlatformKind, PlatformService } from '../interfaces/platform-service.interface';

@Injectable()
export class BrowserPlatformService implements PlatformService {
  readonly kind: PlatformKind = 'browser';
  readonly isNative = false;
  readonly isOnline = typeof navigator !== 'undefined' ? navigator.onLine : true;
  readonly appVersion = '0.0.0-web';
}
