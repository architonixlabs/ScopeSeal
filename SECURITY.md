# Security Policy

## Supported Versions

ScopeSeal is in pre-release development. Security reporting is welcome at any stage.

| Version | Supported |
|---------|-----------|
| Pre-release (Loop 0+) | Best-effort |

## Reporting a Vulnerability

Please report security issues responsibly:

1. **Do not** open public GitHub issues for undisclosed vulnerabilities.
2. Email the project maintainers (address TBD — configure before public launch).
3. Include: description, reproduction steps, impact assessment, and suggested fix if available.

We aim to acknowledge reports within **5 business days** and provide status updates as investigation proceeds.

## Scope

In scope:

- Tenant isolation failures
- Authentication and authorization bypass
- Invitation token weaknesses
- Unsafe file handling
- Webhook signature bypass
- Sensitive data in logs or exports
- Payment state manipulation

Out of scope (initially):

- Social engineering of users
- Denial of service at extreme scale without PoC
- Issues in third-party services (report to vendor directly)

## Safe Harbor

Good-faith security research that avoids privacy violations, data destruction, and service disruption is appreciated. Do not access other users' data.

## Security Practices

See:

- [docs/security/threat-model.md](docs/security/threat-model.md)
- [docs/security/secure-development-checklist.md](docs/security/secure-development-checklist.md)
- [docs/security/incident-response-plan.md](docs/security/incident-response-plan.md)
