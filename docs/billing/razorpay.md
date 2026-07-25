# Razorpay Billing (Web)

> ScopeSeal web billing uses Razorpay **test mode only** until Loop 14 live-mode checklist is completed.

## Architecture

- Domain module exposes `IPaymentGateway` and `IBillingService` only — no Razorpay SDK types in `ScopeSeal.Billing`
- Implementations live in `ScopeSeal.Infrastructure` (`LocalTestPaymentGateway`, `RazorpayPaymentGateway`)
- Entitlements are granted only after verified webhook processing via `IEntitlementService.AssignPlanAsync(..., EntitlementSource.WebSubscription)`
- Browser payment verification sets **provisional** subscription state until webhook confirmation

## Configuration

Non-secret defaults in `appsettings.json` under `ScopeSeal:Billing`. Secrets via environment:

```text
ScopeSeal__Billing__Razorpay__KeyId=rzp_test_...
ScopeSeal__Billing__Razorpay__KeySecret=...
ScopeSeal__Billing__Razorpay__WebhookSecret=...
ScopeSeal__Billing__Mode=Razorpay
```

Plan ID mapping:

```text
ScopeSeal__Billing__Plans__Pro__MonthlyRazorpayPlanId
ScopeSeal__Billing__Plans__Pro__AnnualRazorpayPlanId
ScopeSeal__Billing__Plans__Business__MonthlyRazorpayPlanId
ScopeSeal__Billing__Plans__Business__AnnualRazorpayPlanId
```

Startup validation rejects live keys (`rzp_live_*`) when `TestModeOnly=true`.

## Modes

| Mode | Purpose |
|------|---------|
| `Disabled` | Default; billing endpoints return unavailable |
| `LocalTest` | Integration tests; deterministic HMAC fixtures |
| `Razorpay` | Test-mode HTTP calls to Razorpay API |

## Webhook endpoint

```text
POST /api/v1/webhooks/razorpay
Header: X-Razorpay-Signature
Body: raw JSON (read before deserialization)
```

Idempotency: `processed_webhook_events` stores provider event ID and payload SHA-256 fingerprint.

## Mobile policy

Android and iOS companion apps must **not** embed Razorpay checkout or external upgrade links. Paid entitlements consumed from server-side account state only.

## Related docs

- `docs/security/razorpay-integration-checklist.md`
- `docs/product/pricing-and-entitlements.md`
