# Retention Policy

> Status: Loop 0 draft — all durations configurable, not hard-coded.

## Purpose-Based Retention (Initial Hypotheses)

| Data Category | Default Retention | Trigger | Notes |
|---------------|-------------------|---------|-------|
| Unverified upload sessions | 24–72 hours | Config | Auto-delete incomplete uploads |
| Malware quarantine | 7–30 days | Config | Isolated, no user access |
| Temporary extraction files | 24 hours post-job | Config | Even on failure |
| Draft workspaces (inactive) | 90–180 days | Config + notice | Notify before delete |
| Approved workspaces | User-selected tier | Plan entitlement | Pro/Business longer options |
| Expired invitations | 30 days post-expiry | Config | Audit metadata may persist |
| Audit records | 1–7 years | Config + legal | No deleted content in audit |
| Billing records | Statutory minimum | Legal / CA | TBD India requirements |
| Security logs | 90–365 days | Config | Minimised fields |
| Failed webhooks | 90 days | Config | Then dead-letter archive |
| Backups | RPO/RTO defined | Ops | Deletion via backup expiry — not instant |

## User Controls

- Export before automated deletion where appropriate
- Account deletion triggers orchestrated workflow
- Downgrade preserves records; may limit new retention options

## Legal Hold

Business plan optional workflow (Loop 12+): suspend automated deletion for named workspaces with documented authorization.

## Backup Disclaimer

Accurate messaging: deleted data may persist in encrypted backups until backup rotation expires — not claimed as instant erasure from all media.
