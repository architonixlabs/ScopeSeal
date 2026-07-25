import { InjectionToken } from '@angular/core';
import type { NetworkStatusService } from '../interfaces/network-status-service.interface';

export const PLATFORM_NETWORK_STATUS = new InjectionToken<NetworkStatusService>('PLATFORM_NETWORK_STATUS');
