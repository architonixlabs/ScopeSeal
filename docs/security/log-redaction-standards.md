# Log Redaction Standards

> Status: Loop 13 — apply to API, worker, and client logging.

## Never Log

- Passwords, OTPs, or recovery codes
- Full session cookies or JWTs
- Razorpay key secrets or webhook secrets
- Operator API keys
- Invitation tokens (log prefix/hash only if needed)
- Download tokens
- Document or snapshot body content
- AI extraction raw source text
- Payment card or UPI details

## Allowed with Care

- Tenant public IDs and user public IDs (not internal DB keys in customer-facing logs)
- Correlation IDs and trace IDs
- Event types and HTTP status codes
- File names (sanitised; no path components)
- Plan codes and entitlement feature keys

## Implementation (Loop 13)

- `SensitiveDataLogFilter` excludes log events with sensitive property names or Bearer/password patterns
- Serilog request logging uses warning level for 4xx and error for 5xx
- Problem Details in production omit exception messages

## Review Checklist

- [ ] New endpoints do not log request bodies containing credentials
- [ ] Webhook handlers log event ID, not full payload secrets
- [ ] Admin actions audit metadata without customer content
- [ ] Mobile clients do not log tokens to console in release builds

Review when adding authentication, billing, upload, or admin features.
