import { Injectable } from '@angular/core';
import { BrowserSecureStorageService } from '../browser/browser-secure-storage.service';

/** iOS secure storage uses Keychain-backed Capacitor plugin when integrated. */
@Injectable()
export class IosSecureStorageService extends BrowserSecureStorageService {}
