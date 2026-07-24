# System Context

> Status: Loop 0 draft — C4 Level 1.

## Purpose

ScopeSeal helps parties convert fragmented communications into versioned Agreement Snapshots with approval records and a Change Ledger.

## Actors

| Actor | Description |
|-------|-------------|
| Tenant User | Registered user within an organisation/workspace |
| External Reviewer | Invited participant via expiring secure link |
| Platform Administrator | Internal ops, billing, privacy queues |
| Razorpay | Payment gateway (subscriptions, webhooks) |
| AI Provider (optional) | Approved external extraction when enabled |
| Email Provider | Transactional notifications |
| Object Storage | Private blob storage for documents |
| PostgreSQL | Primary transactional datastore |

## System Boundary

```text
┌─────────────────────────────────────────────────────────────┐
│                      ScopeSeal SaaS                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │ Angular PWA  │  │ ASP.NET API  │  │ Background Jobs  │  │
│  │  (Customer)  │  │ Modular Mono │  │ (DB-backed)      │  │
│  └──────┬───────┘  └──────┬───────┘  └────────┬─────────┘  │
│         │                 │                    │            │
│         └─────────────────┼────────────────────┘            │
│                           ▼                                 │
│                    PostgreSQL + Blob Storage                  │
└─────────────────────────────────────────────────────────────┘
         ▲              ▲              ▲              ▲
         │              │              │              │
    Tenant Users   External      Razorpay        AI Provider
                   Reviewers     (webhooks)      (optional)
```

## External Dependencies

- **TLS** termination at edge
- **Azure** (preferred deployment mapping): App Service/Container Apps, PostgreSQL, Blob, Key Vault, App Insights
- Core business logic must not bind directly to Azure SDKs — use infrastructure adapters

## Trust Boundaries

1. Browser ↔ API: authenticated sessions, CSRF protection, rate limits
2. External reviewer ↔ API: single-purpose tokens, minimal data exposure
3. API ↔ PostgreSQL: tenant-scoped queries, authorization policies
4. API ↔ Blob storage: private containers, signed short-lived URLs
5. API ↔ Razorpay: signature verification, idempotent webhook processing
6. API ↔ AI provider: untrusted upload content treated as data only (prompt-injection defence)

## Deployment Regions

Initial preference: **India region** for Indian customer data where commercially and technically feasible. Cross-border AI processing requires documented approval, notice accuracy, and kill switch.
