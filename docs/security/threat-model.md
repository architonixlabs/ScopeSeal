# Threat Model

> Status: Loop 13 — STRIDE-oriented summary with hardening mitigations.

## Assets

- Tenant workspace and snapshot content
- Uploaded documents (untrusted input)
- Approval records and canonical hashes
- Invitation tokens
- Billing and subscription state
- Credentials and sessions
- Audit logs

## Threats & Mitigations

| Threat | Description | Mitigation | Status |
|--------|-------------|------------|--------|
| Broken tenant isolation | Cross-tenant data access | Auth policies; tenant context middleware; integration tests | Loop 2–13 tests |
| IDOR | Guessing resource IDs | External GUIDs; auth on every endpoint; 404 on cross-tenant | Loop 13 security tests |
| Invitation leakage | Token shared or logged | Short expiry; hash tokens; log redaction standards | Loop 7, 13 |
| Account takeover | Credential compromise | Rate-limited login; session revocation | Loop 13 rate limits |
| Malicious upload | Malware, polyglot files | Allowlist; quarantine; scan; magic-byte validation | Loop 5, 13 tests |
| Stored XSS | Extracted text in UI | JSON encoding; CSP headers; client sanitisation | Loop 13 |
| Prompt injection | Instructions in uploads | Schema-only output; ignore doc instructions | Loop 9 |
| CSRF | Forged state-changing requests | SameSite cookies; anti-forgery on forms | Loop 2 |
| SSRF | URL fetch from uploads | No browsing links from documents | Loop 5 |
| Webhook forgery | Fake Razorpay events | Signature on raw body; idempotency | Loop 10, 13 tests |
| Webhook replay | Duplicate events | Event ID dedup; processed-event table | Loop 10, 13 tests |
| Payment manipulation | Client claims paid | Server + webhook authoritative | Loop 10 |
| Privilege escalation | Role tampering | Server-side policies; no client tenant ID trust | Loop 2+ |
| Excessive admin access | Support sees all content | Metadata-only admin API; support grants | Loop 12 |
| Sensitive logging | PII/secrets in logs | SensitiveDataLogFilter; redaction standards | Loop 13 |
| DoS | Large uploads / API abuse | Rate limits; size limits; pagination | Loop 13 |
| Secret leakage | Keys in repo | Secret scanning; Key Vault; CI dependency audit | Loop 1, 13 |
| Backup exposure | Unencrypted backups | Encryption; access controls; restore test doc | Loop 13 doc |
| Missing security headers | Clickjacking, MIME sniff | SecurityHeadersMiddleware (CSP, XFO, HSTS) | Loop 13 |
| Blind observability gaps | Delayed incident response | OpenTelemetry wiring (opt-in export) | Loop 13 |

## Security Controls (Loop 13)

- **Headers:** CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy, HSTS (HTTPS)
- **Rate limits:** Auth (10/min), webhooks (120/min), API partition by IP (300/min)
- **Logging:** Sensitive property and Bearer token filtering
- **Telemetry:** OpenTelemetry traces/metrics when `ScopeSeal:Security:OpenTelemetry:Enabled=true`

## Out of Scope (Initial)

- Nation-state adversaries
- Hardware security module requirements
- Formal third-party penetration test (checklist in Loop 13)

Update after each hardening loop.
