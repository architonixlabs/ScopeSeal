# Test Strategy

> Status: Loop 0 — implement with Loop 1 CI foundation.

## Pyramid

1. **Unit** — state machines, entitlements, hashing, validation, Razorpay mapping
2. **Integration** — PostgreSQL via Testcontainers, tenant isolation, webhooks, deletion
3. **E2E** — Playwright critical journeys
4. **Architecture** — module boundary tests
5. **Security** — IDOR, XSS, webhook replay fixtures
6. **Accessibility** — axe checks on key flows

## Backend Stack

- xUnit + FluentAssertions
- Testcontainers for PostgreSQL
- WebApplicationFactory for API integration tests

## Frontend Stack

- Jasmine/Karma or Jest (Angular default) unit tests
- Component tests for forms and accessibility
- Playwright for E2E

## CI Pipeline (Loop 1)

1. Restore
2. Format validation
3. Backend build
4. Frontend build
5. Unit tests
6. Integration tests
7. Architecture tests
8. E2E smoke (when app exists)
9. Dependency audit
10. Static analysis
11. Secret scan
12. Migration validation

## Mandatory Test Themes

| Theme | When |
|-------|------|
| Tenant isolation | Every module loop |
| Approval immutability | Loop 7 |
| Webhook idempotency | Loop 10 |
| Invitation expiry/revocation | Loop 7 |
| File type spoofing | Loop 5 |
| Downgrade behaviour | Loop 3, 10 |

## Definition of Done (Testing)

No feature complete without applicable automated tests and passing CI.
