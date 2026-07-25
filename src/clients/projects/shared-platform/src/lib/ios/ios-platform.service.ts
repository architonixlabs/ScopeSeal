import { Injectable } from '@angular/core';
import type { PlatformKind, PlatformService } from '../interfaces/platform-service.interface';

/** iOS Capacitor platform adapter foundation. Native plugins wired in future loops. */
@Injectable()
export class IosPlatformService implements PlatformService {
  readonly kind: PlatformKind = 'ios';
  readonly isNative = true;
  readonly isOnline = typeof navigator !== 'undefined' ? navigator.onLine : true;
  readonly appVersion = '0.0.0-ios';
}
