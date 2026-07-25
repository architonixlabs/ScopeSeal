import { describe, expect, it } from 'vitest';
import { detectPlatformKind, isNativePlatform } from './platform-detection';

describe('platform detection', () => {
  it('defaults to browser when Capacitor is absent', () => {
    expect(detectPlatformKind()).toBe('browser');
    expect(isNativePlatform()).toBe(false);
  });
});
