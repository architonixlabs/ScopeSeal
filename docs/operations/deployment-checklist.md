# Deployment checklist

Use before staging or production promotion.

## Pre-deploy

- [ ] All CI workflows green on target commit
- [ ] Database migration reviewed and reversible plan documented
- [ ] Configuration secrets present in target environment (no secrets in repo)
- [ ] Razorpay remains in **test mode** unless live verification complete
- [ ] OpenTelemetry exporter endpoint configured for staging
- [ ] Blob storage account and connection strings validated
- [ ] Backup retention and restore procedure documented (`docs/operations/backup-restore-test.md`)

## Deploy

- [ ] Apply migrations
- [ ] Deploy API host
- [ ] Deploy Worker host
- [ ] Deploy marketing site
- [ ] Deploy product web app
- [ ] Purge CDN caches if applicable
- [ ] Verify health endpoints (`/health/live`, `/health/ready`)

## Post-deploy smoke

- [ ] Register + login
- [ ] Create workspace
- [ ] Upload test PDF
- [ ] Create draft snapshot
- [ ] Privacy notice retrieval
- [ ] Admin portal login (operator account)
- [ ] Playwright smoke suite (`ci-e2e.yml`) passes against staging URL

## Mobile (when activated)

- [ ] Android debug/signed build artifact produced
- [ ] iOS simulator build produced
- [ ] Confirm no Razorpay or external purchase UI in native apps

## Rollback

- [ ] Previous container image tags recorded
- [ ] Migration rollback script or forward-fix plan identified
- [ ] Incident channel and owner assigned
