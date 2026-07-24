# ScopeSeal Feature Matrix

> Status: Loop 0 draft. Capabilities enforced server-side via entitlement engine — never scattered `if (plan == "pro")` checks.

## Plan Overview

| Capability | Free | Pro | Business |
|------------|------|-----|----------|
| Users per tenant | 1 | 1 | Multiple (configurable) |
| Active workspaces | 3 | Higher (config) | Higher (config) |
| Agreement Snapshots / month | 5 | Higher | Higher |
| Manual snapshot entry | Yes | Yes | Yes |
| File upload (allowlist) | Limited | Higher limits | Higher limits |
| AI extraction allowance | Basic | Higher | Higher |
| OCR | No | Yes | Yes |
| Voice transcription | No | Yes | Yes |
| External reviewers per snapshot | 1 | Multiple | Multiple |
| Structured commitments & scope tracking | Basic | Full | Full |
| Change-request workflow | Basic history | Full workflow | Full + policies |
| PDF export | Branded basic | Advanced + custom logo | Org branding |
| Templates | No | Reusable personal | Shared team templates |
| Role-based access | N/A (solo) | N/A | Yes |
| Team dashboards | No | No | Yes |
| Approval policies | No | No | Configurable |
| Advanced audit log | Basic | Activity reports | Advanced |
| Retention configuration | Standard | Longer options | Configurable |
| Legal hold | No | No | Optional enable |
| API / webhooks | No | No | Optional |
| Support | Community/email | Priority | Priority |
| Privacy rights centre | Full (never paywalled) | Full | Full + admin export |

All numeric limits stored in configuration/database, versioned, auditable, and testable.

## MVP Boundary (Loops 0–8 + partial 11)

**In MVP:**

- Identity, tenancy, roles (Loop 2)
- Plans and entitlements without live Razorpay (Loop 3)
- Workspaces, parties, templates, dashboard (Loop 4)
- Secure document upload with conservative allowlist (Loop 5)
- Manual Agreement Snapshot editor and basic PDF export (Loop 6)
- External review, approval, canonical hash, change ledger (Loops 7–8)
- ManualOnly AI mode stubbed; extraction deferred to post-MVP core if schedule requires

**Explicitly out of MVP:**

- Live Razorpay (test mode only in Loop 10)
- Enterprise SSO / SCIM
- Native WhatsApp API integration
- Audio transcription (until Loop 9 stable)
- Public API and webhooks
- Legal hold workflow
- Certified evidence / court-proof marketing
- Automatic AI approval
- Office document formats (macro-enabled, archives)
- Row-level security (evaluate in Loop 13; document decision in ADR)

## Non-Goals (Product)

- Providing legal advice or enforceability opinions
- Statutory digital signatures without compliant integration
- Training AI on customer content by default
- Behavioural advertising or contact-list harvesting
- Guaranteeing uploaded material authenticity
- Instant deletion from immutable backups (accurate backup-expiry messaging only)

## Feature Flags (Future)

Administration portal (Loop 12) will gate experimental features: external AI provider, OCR, webhooks, legal hold.
