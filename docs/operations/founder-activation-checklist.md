# Founder activation checklist

Human actions required before external launch.

## Repository and CI

- [ ] Review and merge draft PR `feature/scopeseal-autonomous-build` → `main`
- [ ] Configure GitHub protected environments for Android/iOS signing secrets
- [ ] Enable branch protection on `main`

## Legal and privacy

- [ ] Qualified legal review of Terms, Privacy, Refund, and Acceptable Use drafts
- [ ] Approve privacy notice version for publication
- [ ] Confirm subprocessor register accuracy
- [ ] Do **not** claim DPDP Act compliance until counsel approves

## Billing

- [ ] Complete Razorpay KYC and live-mode verification
- [ ] Configure live webhook secret in production vault
- [ ] Verify entitlement reconciliation with test purchases in staging

## Infrastructure

- [ ] Provision PostgreSQL, blob storage, and container hosting
- [ ] Configure Key Vault / secret management
- [ ] Set production JWT secret and cookie settings
- [ ] Configure OTLP exporter and log aggregation
- [ ] Execute backup restore test (`docs/operations/backup-restore-test.md`)

## Security

- [ ] Commission independent penetration test
- [ ] Review go/no-go report blockers
- [ ] Assign incident-response owner and on-call rotation

## Mobile stores

- [ ] Generate Android upload keystore and iOS distribution certificates
- [ ] Complete Google Play Data Safety and Apple privacy nutrition labels
- [ ] Confirm companion-app copy contains no purchase CTAs

## Launch comms

- [ ] Approve marketing copy (no "court-proof" or "legally guaranteed" language)
- [ ] Publish system status page link
- [ ] Prepare support inbox and grievance workflow owner
