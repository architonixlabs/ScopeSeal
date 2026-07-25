import { Injectable } from '@angular/core';
import type { CameraCaptureService, CapturedImage } from '../interfaces/camera-capture-service.interface';

@Injectable()
export class BrowserCameraCaptureService implements CameraCaptureService {
  async capturePhoto(): Promise<CapturedImage | null> {
    return new Promise((resolve) => {
      const input = document.createElement('input');
      input.type = 'file';
      input.accept = 'image/*';
      input.capture = 'environment';
      input.onchange = () => {
        const file = input.files?.[0];
        if (!file) {
          resolve(null);
          return;
        }
        resolve({
          mimeType: file.type || 'image/jpeg',
          sizeBytes: file.size,
          data: file,
        });
      };
      input.click();
    });
  }
}
