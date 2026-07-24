# Threat Model

> Status: Loop 0 draft — STRIDE-oriented summary.

## Assets

- Tenant workspace and snapshot content
- Uploaded documents (untrusted input)
- Approval records and canonical hashes
- Invitation tokens
- Billing and subscription state
- Credentials and sessions
- Audit logs

## Threats & Mitigations

| Threat | Description | Mitigation Loop |
|--------|-------------|-----------------|
| Broken tenant isolation | Cross-tenant data access | Loop 2+ tests; auth policies; optional RLS Loop 13 |
| IDOR | Guessing resource IDs | External GUIDs; auth on every endpoint |
| Invitation leakage | Token shared or logged | Short expiry; hash tokens; no full token in logs |
| Account takeover | Credential compromise | MFA option; rate limits; session revocation |
| Malicious upload | Malware, polyglot files | Allowlist; quarantine; scan; safe preview |
| Stored XSS | Extracted text in UI | Encoding; CSP; sanitisation |
| Prompt injection | Instructions in uploads | Schema-only output; ignore doc instructions |
| CSRF | Forged state-changing requests | Anti-forgery tokens / SameSite |
| SSRF | URL fetch from uploads | No browsing links from documents |
| Webhook forgery | Fake Razorpay events | Signature on raw body; idempotency |
| Webhook replay | Duplicate events | Event ID dedup; processed-event table |
| Payment manipulation | Client claims paid | Server + webhook authoritative |
| Privilege escalation | Role tampering | Server-side policies; no client tenant ID trust |
| Excessive admin access | Support sees all content | Case-based elevation; audit; time limit |
| Sensitive logging | PII/secrets in logs | Redaction standards; structured logging |
| DoS | Large uploads / API abuse | Rate limits; size limits; pagination |
| Secret leakage | Keys in repo | Secret scanning; Key Vault |
| Backup exposure | Unencrypted backups | Encryption; access controls |

## Out of Scope (Initial)

- Nation-state adversaries
- Hardware security module requirements
- Formal penetration test (Loop 13 checklist)

Update after each hardening loop.
