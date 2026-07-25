# Backup and Restore Test Procedure

> Status: Loop 13 — required before production preparation. Execute in staging with sanitised data.

## Objectives

- Verify PostgreSQL backups can be restored within target RPO/RTO
- Confirm blob storage recovery alignment with database restore point
- Document roles, timing, and rollback steps

## Preconditions

- Staging environment mirrors production topology (managed PostgreSQL, blob storage, Key Vault references)
- Backup policy enabled (automated daily minimum; point-in-time recovery where available)
- Runbook owner and on-call contact identified

## Test Steps

1. **Record baseline** — note latest backup timestamp, migration version, and sample tenant public IDs for verification queries.
2. **Create test marker** — insert a identifiable audit event or feature-flag change in staging after backup window starts.
3. **Simulate failure** — restore database to a new staging instance from backup (not in-place on production).
4. **Apply migrations** — run pending migrations only if restore point predates latest migration; document order.
5. **Verify data** — confirm tenants, workspaces, and entitlement rows for sample IDs; approved snapshots remain immutable.
6. **Verify blobs** — download a known document via signed URL; confirm hash matches pre-restore record.
7. **Verify billing** — subscription state matches Razorpay reconciliation query for test tenant.
8. **Record timing** — document restore duration vs RTO target.
9. **Tear down** — delete temporary restore instance; retain test report.

## Success Criteria

- Restore completes without manual schema edits
- Sample tenant data consistent with backup point
- No secrets printed in restore logs
- RTO/RTO values recorded in operations runbook

## RPO/RTO Hypotheses (Configurable)

| Component | RPO (target) | RTO (target) |
|-----------|--------------|--------------|
| PostgreSQL | 24 hours | 4 hours |
| Blob storage | 24 hours | 4 hours |
| Configuration | 0 (IaC) | 1 hour |

Adjust with infrastructure provider capabilities before production.

## Reporting

Store completion report under `docs/backlog/loop-reports/` with date, environment, duration, issues, and sign-off placeholders.

Not **Production Ready** until an authorised restore exercise is completed in staging.
