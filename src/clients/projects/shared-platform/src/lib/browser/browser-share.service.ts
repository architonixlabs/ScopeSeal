import { Injectable } from '@angular/core';
import type { ShareService } from '../interfaces/share-service.interface';

@Injectable()
export class BrowserShareService implements ShareService {
  async share(options: { title?: string; text?: string; url?: string; files?: File[] }): Promise<boolean> {
    if (typeof navigator !== 'undefined' && navigator.share) {
      try {
        await navigator.share(options);
        return true;
      } catch {
        return false;
      }
    }
    if (options.url && typeof navigator !== 'undefined' && navigator.clipboard) {
      await navigator.clipboard.writeText(options.url);
      return true;
    }
    return false;
  }
}
