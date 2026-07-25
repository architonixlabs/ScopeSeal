# Android build guide

ScopeSeal ships a Capacitor wrapper around the shared Angular `product-app`.

## Prerequisites

- Node.js 22+
- Java 21 (Temurin recommended)
- Android SDK with API 35 platform tools
- Built web assets in `dist/product-app/browser`

## Local build

```bash
cd src/clients
npm ci
npm run build:product
npx cap sync android
cd android
./gradlew assembleDebug
```

Debug APK output:

```text
src/clients/android/app/build/outputs/apk/debug/app-debug.apk
```

## Store policy

- First Android release is a **free companion app**
- No Razorpay checkout, external upgrade links, or in-app purchases
- Paid entitlements display only when already assigned server-side

## CI

GitHub Actions workflow: `.github/workflows/build-android.yml`

Release signing uses protected environment secrets documented in `docs/mobile/release-signing.md`.
