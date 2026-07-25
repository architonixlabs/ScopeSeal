import { Injectable } from '@angular/core';
import type { DeepLinkPayload, DeepLinkService } from '../interfaces/deep-link-service.interface';

@Injectable()
export class BrowserDeepLinkService implements DeepLinkService {
  async getLaunchUrl(): Promise<DeepLinkPayload | null> {
    if (typeof window === 'undefined') {
      return null;
    }
    return { url: window.location.href, path: window.location.pathname };
  }

  onDeepLink(callback: (payload: DeepLinkPayload) => void): () => void {
    const handler = () => {
      callback({ url: window.location.href, path: window.location.pathname });
    };
    window.addEventListener('popstate', handler);
    return () => window.removeEventListener('popstate', handler);
  }
}
