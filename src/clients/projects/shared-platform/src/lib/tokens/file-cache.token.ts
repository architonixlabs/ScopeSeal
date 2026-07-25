import { InjectionToken } from '@angular/core';
import type { FileCacheService } from '../interfaces/file-cache-service.interface';

export const PLATFORM_FILE_CACHE = new InjectionToken<FileCacheService>('PLATFORM_FILE_CACHE');
