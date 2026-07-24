# Razorpay Integration Risk Checklist

> Status: Loop 0 — implement in Loop 10.

## Architecture

- [ ] Domain uses `IPaymentGateway` only — no Razorpay types in domain
- [ ] Plan IDs from configuration, not hard-coded
- [ ] Local subscription state machine mapped from Razorpay events
- [ ] Usage entitlements never from browser callback alone

## Checkout

- [ ] Server creates subscription before Checkout opens
- [ ] Only public checkout keys in browser
- [ ] Server verifies payment signature on return
- [ ] Provisional state until webhook confirmation

## Webhooks

- [ ] HTTPS only
- [ ] Read raw request bytes before JSON deserialisation
- [ ] Validate signature with configured secret(s)
- [ ] Support secret rotation
- [ ] Store provider event ID or fingerprint
- [ ] Reject invalid signatures (alert on repeated failures)
- [ ] Replay protection
- [ ] Acknowledge quickly; process async
- [ ] Idempotent processing (processed-event table)
- [ ] Dead-letter poison events
- [ ] Never log full webhook payload unredacted

## Events to Handle

- [ ] subscription.authenticated, activated, charged, updated, pending, halted, paused, resumed, cancelled, completed
- [ ] payment.captured, payment.failed
- [ ] refund.processed (where applicable)

## Reconciliation

- [ ] Periodic API reconciliation for uncertain states
- [ ] Admin visibility of mismatches (Loop 12)
- [ ] No double entitlement grants

## Failure & Cancellation

- [ ] Grace period on failed payment (configurable)
- [ ] Downgrade preserves data; blocks excess creation
- [ ] Clear cancellation UX — no dark patterns
- [ ] Invoice/receipt access documented

## Test Mode

- [ ] End-to-end tests in Razorpay test mode only
- [ ] No live keys in dev/staging
- [ ] Live-mode checklist separate (Loop 14)

## Tax & Invoicing

- [ ] GSTIN capture optional; tax rules configurable
- [ ] CA review before representing GST as final

## Security Tests

- [ ] Invalid webhook signature rejected
- [ ] Replayed webhook ignored
- [ ] Tampered client payment response rejected
