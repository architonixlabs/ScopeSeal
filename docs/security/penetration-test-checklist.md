# Penetration Test Checklist

> Status: Loop 13 — pre-staging checklist. Not a substitute for independent penetration testing.

## Scope

- ScopeSeal API (authenticated and anonymous routes)
- Product web application
- Admin portal (operator key)
- Razorpay webhook endpoint
- Document upload and download flows

## Authentication and Session

- [ ] Brute-force resistance on login (rate limits, lockout review)
- [ ] Session fixation and cookie flags (HttpOnly, Secure, SameSite)
- [ ] Logout invalidates session
- [ ] Password reset / registration abuse (enumeration, rate limits)

## Authorization and Tenant Isolation

- [ ] IDOR on workspaces, snapshots, documents, billing, privacy requests
- [ ] Cross-tenant access via swapped tenant GUID in URL
- [ ] Horizontal privilege escalation between tenant members
- [ ] Admin endpoints reject missing/invalid operator key
- [ ] Support access grants cannot read customer document content

## Input and Output

- [ ] Stored XSS fixtures in snapshot titles, comments, party names
- [ ] Reflected XSS in error messages and search parameters
- [ ] SQL injection via EF Core parameterised queries (spot check)
- [ ] Mass assignment on DTOs

## File Upload

- [ ] Content-type spoofing (magic bytes vs declared type)
- [ ] Polyglot / executable rejection
- [ ] Oversized upload rejection
- [ ] Path traversal in filenames
- [ ] Signed download token expiry and one-time use

## Billing and Webhooks

- [ ] Razorpay webhook signature validation
- [ ] Webhook replay (duplicate event ID)
- [ ] Tampered payment signature on verify-payment
- [ ] Client-side checkout success does not grant entitlements alone

## Infrastructure and Headers

- [ ] Security headers (CSP, X-Frame-Options, HSTS when TLS)
- [ ] CORS policy review
- [ ] TLS configuration (staging/production)
- [ ] Error responses do not leak stack traces in production

## Observability and Logging

- [ ] No passwords, tokens, or document content in logs
- [ ] Correlation IDs present on API responses
- [ ] OpenTelemetry export configured for staging

## Dependencies

- [ ] CI dependency audit passes (dotnet + npm)
- [ ] No known high/critical unmitigated CVEs in direct dependencies

## Backup and Recovery

- [ ] Backup restore test documented and executed in staging
- [ ] RPO/RTO recorded

## Sign-off

| Role | Name | Date | Result |
|------|------|------|--------|
| Engineering | | | |
| Security review | | | |
| Product owner | | | |

Update after each hardening loop and before production preparation.
