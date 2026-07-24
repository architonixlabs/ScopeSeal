# Operations Runbook

> Status: Loop 0 placeholder — expand in Loop 1 and Loop 14.

## Environments

| Environment | Purpose | Data |
|-------------|---------|------|
| Local | Developer Docker Compose | Synthetic |
| Staging | Pre-production validation | Sanitised |
| Production | Customer traffic | Real — India region target |

Never clone production DB into development without sanitisation.

## Health Checks

- API `/health` — database, blob, queue depth (Loop 1)
- Worker heartbeat — job processing lag

## Common Procedures (TBD)

### Database migration

1. Review migration in staging
2. Backup production
3. Apply with rollback plan

### Secret rotation

- Razorpay webhook secret: dual-secret validation period
- API keys: Key Vault rotation documented Loop 1

### Feature kill switches

- AI provider: `Ai:Mode=ManualOnly`
- External invitations: feature flag
- Checkout: disable plan in config

## Monitoring Alerts (Initial)

- Repeated authorization failures
- Webhook signature failures
- Payment state mismatch
- Malware detection
- AI cost spike
- Deletion job backlog

## Support Escalation

- S1: Page on-call
- Privacy request SLA breach: Privacy Administrator queue

## Backups

- RPO/RTO TBD Loop 13
- Restore test required before production readiness
