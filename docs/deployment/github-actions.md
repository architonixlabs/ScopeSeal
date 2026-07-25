# GitHub Actions deployment guide

## Workflows

| Workflow | Purpose |
|----------|---------|
| `ci-backend.yml` | Build, test backend |
| `ci-clients.yml` | Build Angular apps, a11y foundation |
| `ci-security.yml` | NuGet/npm audit |
| `ci-e2e.yml` | Playwright API + marketing smoke |
| `build-android.yml` | Capacitor sync, debug APK |
| `build-ios.yml` | Capacitor sync, simulator build |

## Pull-request validation

All workflows run on `feature/**` pushes. Release signing secrets are **not** available to PR builds.

## Release artifacts (future)

Protected environments required for:

- Android signed AAB
- iOS distribution IPA
- SBOM and checksum generation

## Deployment sequence (staging)

1. Merge to staging branch / environment
2. Apply database migrations
3. Deploy API + Worker containers
4. Deploy marketing SSR/static site
5. Deploy product web app
6. Configure OTLP, blob storage, Razorpay test mode
7. Run deployment checklist and smoke tests

Production deployment requires go/no-go approval — see `docs/operations/go-no-go-report.md`.
