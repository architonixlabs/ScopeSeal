import { Injectable } from '@angular/core';
import { BrowserSecureStorageService } from '../browser/browser-secure-storage.service';

/** Android secure storage uses Keystore-backed Capacitor plugin when integrated. */
@Injectable()
export class AndroidSecureStorageService extends BrowserSecureStorageService {}
