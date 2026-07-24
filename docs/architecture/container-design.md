# Container Design

> Status: Loop 0 draft — C4 Level 2 modular monolith.

## Containers

| Container | Technology | Responsibility |
|-----------|------------|----------------|
| Customer Web App | Angular 22 PWA | Dashboard, workspace UI, snapshot editor, review flows, privacy centre |
| Admin Portal | Angular (separate app or route tree) | Internal operations — Loop 12 |
| Web API | ASP.NET Core (.NET 10) | REST API, auth, authorization, use cases, webhooks |
| Worker | ASP.NET Core hosted service | Background jobs: extraction, notifications, deletion, reconciliation |
| Database | PostgreSQL | Transactional data, outbox, usage ledger |
| Blob Storage | Azure Blob (adapter) | Document originals, exports, quarantine |

## Module Boundaries (Modular Monolith)

```text
Identity          Tenancy           Contacts
Workspaces        Documents         Extraction
AgreementSnapshots Commitments      ChangeRequests
Approvals         Audit             Notifications
Billing           Entitlements      Privacy
Support           Administration    Reporting
```

Rules:

- Domain-oriented boundaries with clean dependency direction
- Application use cases explicit; infrastructure behind interfaces
- No cross-module DB writes except through defined contracts
- Transactional outbox for reliable external events
- Optimistic concurrency for collaborative editing

## API Surface

- Versioned REST (`/api/v1/...`)
- OpenAPI documentation
- Problem Details errors
- Correlation IDs on all requests

## Frontend Architecture

- Standalone Angular components
- Strict TypeScript, Angular Material
- Signal-based state where appropriate
- Route-level lazy loading
- i18n-ready (English first)

## Background Processing

Database-backed job abstraction (replaceable):

- Malware scan completion
- Document processing
- AI extraction runs
- Notification delivery
- Webhook async processing
- Deletion orchestration
- Billing reconciliation

## CI/CD Containers (Loop 1)

- Docker Compose for local dev (API + PostgreSQL)
- GitHub Actions / Azure DevOps pipeline (TBD in Loop 1)
