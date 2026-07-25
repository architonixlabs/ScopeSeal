import { Injectable } from '@angular/core';
import type { PlatformKind, PlatformService } from '../interfaces/platform-service.interface';

/** Android Capacitor platform adapter foundation. Native plugins wired in future loops. */
@Injectable()
export class AndroidPlatformService implements PlatformService {
  readonly kind: PlatformKind = 'android';
  readonly isNative = true;
  readonly isOnline = typeof navigator !== 'undefined' ? navigator.onLine : true;
  readonly appVersion = '0.0.0-android';
}
