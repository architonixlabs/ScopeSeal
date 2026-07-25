/** Detected runtime platform for the shared product client. */
export type PlatformKind = 'browser' | 'android' | 'ios';

/** Core platform metadata and capability flags. */
export interface PlatformService {
  readonly kind: PlatformKind;
  readonly isNative: boolean;
  readonly isOnline: boolean;
  readonly appVersion: string;
}
