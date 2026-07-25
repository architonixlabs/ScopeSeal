import type { PlatformKind } from './interfaces/platform-service.interface';

declare global {
  interface Window {
    Capacitor?: { getPlatform(): string };
  }
}

/** Detect runtime platform from Capacitor or user agent. */
export function detectPlatformKind(): PlatformKind {
  const capPlatform = typeof window !== 'undefined' ? window.Capacitor?.getPlatform() : undefined;
  if (capPlatform === 'android') {
    return 'android';
  }
  if (capPlatform === 'ios') {
    return 'ios';
  }
  return 'browser';
}

export function isNativePlatform(): boolean {
  const kind = detectPlatformKind();
  return kind === 'android' || kind === 'ios';
}
