# Secure Development Checklist

> Status: Loop 0 — apply from Loop 1 onward.

## Design

- [ ] Threat model updated for feature
- [ ] Privacy impact considered (data inventory)
- [ ] Tenant boundary explicit in every new endpoint
- [ ] Authorization policy defined (not UI-only)

## Implementation

- [ ] Input validation (FluentValidation or equivalent)
- [ ] No secrets in source control
- [ ] Parameterised queries (EF Core)
- [ ] Idempotency for webhooks and sensitive creates
- [ ] Optimistic concurrency where collaborative
- [ ] UTC timestamps; no client-trusted tenant IDs

## Authentication & Sessions

- [ ] Rate-limited login
- [ ] Secure password hashing (Identity defaults)
- [ ] MFA for platform admins
- [ ] Reauthentication for destructive actions

## Files & Content

- [ ] Content-type validation beyond extension
- [ ] Size limits enforced server-side
- [ ] Private blob storage; signed URLs
- [ ] No executable formats in allowlist

## Logging & Audit

- [ ] No passwords, OTPs, tokens, doc content in logs
- [ ] Security events to append-only audit
- [ ] Correlation IDs

## Dependencies

- [ ] Vulnerability scan in CI
- [ ] Pin/update policy documented

## Testing

- [x] Unit tests for auth rules and state machines
- [x] Integration tests for tenant isolation
- [x] Security tests per test strategy (IDOR, XSS fixtures, webhook replay) — Loop 13
- [ ] Full penetration test checklist sign-off — Loop 14

## Release

- [ ] Migration safety review
- [ ] Feature flags for risky features
- [ ] Rollback plan documented
