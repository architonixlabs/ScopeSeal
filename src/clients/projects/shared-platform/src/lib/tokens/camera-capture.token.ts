import { InjectionToken } from '@angular/core';
import type { CameraCaptureService } from '../interfaces/camera-capture-service.interface';

export const PLATFORM_CAMERA_CAPTURE = new InjectionToken<CameraCaptureService>('PLATFORM_CAMERA_CAPTURE');
