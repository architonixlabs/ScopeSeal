# Incident Response Plan

> Status: Loop 0 draft — operationalise in Loop 13.

## Severity Levels

| Level | Example | Response Time Target |
|-------|---------|---------------------|
| S1 Critical | Active data breach, tenant isolation failure | Immediate |
| S2 High | Webhook forgery success, malware bypass | < 4 hours |
| S3 Medium | Elevated failed auth rate, AI cost spike | < 24 hours |
| S4 Low | Single failed job, non-critical bug | Next business day |

## Roles

- **Incident Commander:** On-call engineer (rotate)
- **Privacy Lead:** Privacy Administrator
- **Comms:** Founder / designated spokesperson
- **Legal:** External counsel on retainer (founder action)

## Phases

1. **Detect** — Alerts, user report, audit anomaly
2. **Triage** — Severity, scope, tenants affected
3. **Contain** — Revoke tokens, disable feature flag, block IP, pause webhooks
4. **Eradicate** — Patch root cause
5. **Recover** — Restore service, reconcile billing if needed
6. **Notify** — Users/regulators per legal advice (not predetermined here)
7. **Post-incident** — Timeline, lessons, backlog items

## Immediate Actions (Examples)

- Tenant isolation breach: disable affected endpoint; preserve audit logs
- Webhook compromise: rotate webhook secret; replay reconciliation
- Malware detected: isolate blob; notify uploader; scan adjacent uploads
- AI cost anomaly: activate administrator kill switch

## Communication

- No document content in status pages or user emails
- Preserve evidence chain for investigation

## Testing

- Tabletop exercise required before production (Loop 14)
