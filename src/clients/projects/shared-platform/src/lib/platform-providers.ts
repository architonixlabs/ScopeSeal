import { EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';
import { AndroidPlatformService } from './android/android-platform.service';
import { AndroidSecureStorageService } from './android/android-secure-storage.service';
import { BrowserAppLifecycleService } from './browser/browser-app-lifecycle.service';
import { BrowserCameraCaptureService } from './browser/browser-camera-capture.service';
import { BrowserDeepLinkService } from './browser/browser-deep-link.service';
import { BrowserDocumentPickerService } from './browser/browser-document-picker.service';
import { BrowserFileCacheService } from './browser/browser-file-cache.service';
import { BrowserNetworkStatusService } from './browser/browser-network-status.service';
import { BrowserNotificationService } from './browser/browser-notification.service';
import { BrowserPlatformService } from './browser/browser-platform.service';
import { BrowserSecureStorageService } from './browser/browser-secure-storage.service';
import { BrowserShareService } from './browser/browser-share.service';
import { IosPlatformService } from './ios/ios-platform.service';
import { IosSecureStorageService } from './ios/ios-secure-storage.service';
import { detectPlatformKind } from './platform-detection';
import { PLATFORM_APP_LIFECYCLE } from './tokens/app-lifecycle.token';
import { PLATFORM_CAMERA_CAPTURE } from './tokens/camera-capture.token';
import { PLATFORM_DEEP_LINK } from './tokens/deep-link.token';
import { PLATFORM_DOCUMENT_PICKER } from './tokens/document-picker.token';
import { PLATFORM_FILE_CACHE } from './tokens/file-cache.token';
import { PLATFORM_NETWORK_STATUS } from './tokens/network-status.token';
import { PLATFORM_NOTIFICATION } from './tokens/notification.token';
import { PLATFORM_SERVICE } from './tokens/platform.token';
import { PLATFORM_SECURE_STORAGE } from './tokens/secure-storage.token';
import { PLATFORM_SHARE } from './tokens/share.token';

export function providePlatformAdapters(): EnvironmentProviders {
  const kind = detectPlatformKind();

  const platformService =
    kind === 'android'
      ? AndroidPlatformService
      : kind === 'ios'
        ? IosPlatformService
        : BrowserPlatformService;

  const secureStorageService =
    kind === 'android'
      ? AndroidSecureStorageService
      : kind === 'ios'
        ? IosSecureStorageService
        : BrowserSecureStorageService;

  return makeEnvironmentProviders([
    { provide: PLATFORM_SERVICE, useClass: platformService },
    { provide: PLATFORM_SECURE_STORAGE, useClass: secureStorageService },
    { provide: PLATFORM_DOCUMENT_PICKER, useClass: BrowserDocumentPickerService },
    { provide: PLATFORM_CAMERA_CAPTURE, useClass: BrowserCameraCaptureService },
    { provide: PLATFORM_SHARE, useClass: BrowserShareService },
    { provide: PLATFORM_DEEP_LINK, useClass: BrowserDeepLinkService },
    { provide: PLATFORM_NOTIFICATION, useClass: BrowserNotificationService },
    { provide: PLATFORM_NETWORK_STATUS, useClass: BrowserNetworkStatusService },
    { provide: PLATFORM_APP_LIFECYCLE, useClass: BrowserAppLifecycleService },
    { provide: PLATFORM_FILE_CACHE, useClass: BrowserFileCacheService },
  ]);
}
