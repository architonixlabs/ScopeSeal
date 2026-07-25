import { InjectionToken } from '@angular/core';
import type { DocumentPickerService } from '../interfaces/document-picker-service.interface';

export const PLATFORM_DOCUMENT_PICKER = new InjectionToken<DocumentPickerService>('PLATFORM_DOCUMENT_PICKER');
