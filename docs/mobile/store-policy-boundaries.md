# Mobile store policy boundaries

ScopeSeal mobile applications are **free companion clients** for the first release.

## Allowed

- Free plan registration and sign-in
- Use of Free-plan features
- Display of paid entitlements already assigned by the server
- Neutral plan status copy (e.g. "Your account is currently on the Pro plan.")
- Neutral unavailable-feature copy (e.g. "This feature is not available for this account.")

## Not allowed in native apps

- Razorpay Checkout or WebView checkout
- Links to web upgrade or purchase pages intended to bypass store billing
- External purchase buttons or upgrade CTAs
- Client-side unlocking of paid features
- Card, UPI, wallet, or subscription payment collection

## Web billing boundary

Razorpay remains the web billing provider behind `IPaymentGateway`. Verified webhooks and server-side entitlements are the source of truth.

## Future native billing

`INativeBillingProvider` may integrate Apple StoreKit or Google Play Billing only after store-policy review, tax review, entitlement reconciliation design, and product-owner approval.
