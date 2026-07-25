# iOS build guide

ScopeSeal ships a Capacitor wrapper around the shared Angular `product-app`.

## Prerequisites

- macOS with Xcode 16+
- CocoaPods (installed via `npx cap sync ios`)
- Node.js 22+
- Built web assets in `dist/product-app/browser`

## Local simulator build

```bash
cd src/clients
npm ci
npm run build:product
npx cap sync ios
cd ios/App
xcodebuild -workspace App.xcworkspace -scheme App -configuration Debug \
  -sdk iphonesimulator -destination 'platform=iOS Simulator,name=iPhone 16' \
  CODE_SIGNING_ALLOWED=NO build
```

## Store policy

- First iOS release is a **free companion app**
- No Razorpay checkout, external upgrade links, or StoreKit purchases in initial release
- Native billing abstraction (`INativeBillingProvider`) prepared but not activated

## CI

GitHub Actions workflow: `.github/workflows/build-ios.yml` (macOS runner, unsigned simulator build).

Distribution signing documented in `docs/mobile/release-signing.md`.
