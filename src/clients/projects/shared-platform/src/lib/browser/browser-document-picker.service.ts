import { Injectable } from '@angular/core';
import type { DocumentPickerService, PickedDocument } from '../interfaces/document-picker-service.interface';

@Injectable()
export class BrowserDocumentPickerService implements DocumentPickerService {
  async pickDocument(): Promise<PickedDocument | null> {
    return new Promise((resolve) => {
      const input = document.createElement('input');
      input.type = 'file';
      input.accept = '.pdf,.png,.jpg,.jpeg,.webp,.doc,.docx';
      input.onchange = () => {
        const file = input.files?.[0];
        if (!file) {
          resolve(null);
          return;
        }
        resolve({
          name: file.name,
          mimeType: file.type || 'application/octet-stream',
          sizeBytes: file.size,
          data: file,
        });
      };
      input.click();
    });
  }
}
