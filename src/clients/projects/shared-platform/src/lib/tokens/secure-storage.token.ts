import { InjectionToken } from '@angular/core';
import type { SecureStorageService } from '../interfaces/secure-storage-service.interface';

export const PLATFORM_SECURE_STORAGE = new InjectionToken<SecureStorageService>('PLATFORM_SECURE_STORAGE');
