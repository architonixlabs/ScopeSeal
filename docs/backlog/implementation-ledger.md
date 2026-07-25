# Implementation Ledger

> Tracks delivery loop status. Update at end of each `/loop` run.

## Status Legend

| Status | Meaning |
|--------|---------|
| Complete | Acceptance criteria satisfied |
| Functionally complete | Works but hardening pending |
| Partially implemented | Started, not done |
| Placeholder | Documented only |
| Blocked | External dependency |
| Deferred | Consciously postponed |
| Rejected | Will not implement |

## Delivery Loops

| Loop | Name | Status | Notes |
|------|------|--------|-------|
| 0 | Discovery and risk framing | **Complete** | Docs, backlog, risk register, checklists |
| 1 | Architecture foundation | **Complete** | Solution, modules, API skeleton, Angular workspace, Docker, CI |
| 2 | Identity and tenancy | **Complete** | EF Core, Identity, tenants, cookie auth, isolation tests |
| 3 | Plans and entitlements | **Complete** | IEntitlementService, plan catalog, usage counters, Free defaults |
| 4 | Workspace and contact management | **Complete** | Workspaces, parties, contacts, templates, dashboard API, audit events |
| 5 | Secure document upload | **Complete** | Upload sessions, quarantine/permanent blob storage, content validation, malware scan abstraction, signed downloads |
| 6 | Manual Agreement Snapshot | **Complete** | Draft snapshot API, section editor, concurrency, entitlement gating |
| 7 | Review and approval | **Complete** | Invitations, external review, comments, approval hash, immutability |
| 8 | Change Ledger | **Complete** | Change requests, impacts, clone-to-draft, diff API, reapproval superseding |
| 9 | AI extraction | **Complete** | Provider abstraction, job pipeline, draft facts, provenance, ManualOnly default |
| 10 | Razorpay integration | **Complete** | Test-mode IPaymentGateway, checkout, webhooks, entitlement reconciliation |
| 11 | Privacy centre | **Complete** | Notice versioning, consent, requests, export/deletion foundations, subprocessor API |
| 12 | Administration and support | **Complete** | Admin API, operator portal shell, metadata-only support access |
| 13 | Hardening | **Complete** | Security headers, rate limits, OTel wiring, security tests, a11y CI foundations |
| 14 | Launch readiness | Placeholder | |

## Repository Scaffold Status

| Item | Status |
|------|--------|
| AGENTS.md | Complete |
| Product docs (`/docs/product/`) | Complete |
| Architecture docs (`/docs/architecture/`) | Complete |
| Privacy docs (`/docs/privacy/`) | Complete |
| Security docs (`/docs/security/`) | Complete |
| Operations & testing docs | Complete |
| Backlog & risk register | Complete |
| ADRs (initial set) | Complete |
| `.cursor/rules/` | Complete |
| Root README, SECURITY, CONTRIBUTING | Complete |
| `.editorconfig`, `.env.example` | Complete |
| Backend solution (`src/backend/ScopeSeal.slnx`) | **Complete** — Loop 1 |
| Modular domain projects (14 modules + Shared) | **Complete** — markers/DI stubs |
| Infrastructure module (`ScopeSeal.Infrastructure`) | **Complete** — Loop 2 |
| API host (health, OpenAPI, logging, config validation) | **Complete** — Loop 1 |
| Identity & tenancy (EF, auth endpoints, policies) | **Complete** — Loop 2 |
| Plans & entitlements (IEntitlementService, plan catalog) | **Complete** — Loop 3 |
| Workspaces & contacts (CRUD, parties, templates, dashboard) | **Complete** — Loop 4 |
| Secure document upload (sessions, blob storage, validation, scan) | **Complete** — Loop 5 |
| Manual Agreement Snapshot (draft editor API, sections, concurrency) | **Complete** — Loop 6 |
| Review and approval (invitations, external review, approval hash) | **Complete** — Loop 7 |
| Change Ledger (change requests, version diff, reapproval) | **Complete** — Loop 8 |
| AI extraction (provider abstraction, job pipeline, draft facts) | **Complete** — Loop 9 |
| Razorpay web billing (IPaymentGateway, webhooks, reconciliation) | **Complete** — Loop 10 |
| Privacy centre (consent, requests, export/deletion foundations) | **Complete** — Loop 11 |
| Administration and support (admin API, operator portal shell) | **Complete** — Loop 12 |
| Security hardening (headers, rate limits, OTel, security tests) | **Complete** — Loop 13 |
| Audit events (IAuditService foundation) | **Complete** — Loop 4–5 |
| Worker host | Partially implemented — extraction job polling; preview jobs deferred |
| Frontend workspace (product, marketing SSR, admin, shared libs) | **Complete** — Loop 1 shells |
| CI pipeline (backend, clients, security) | **Complete** — Loop 1 |
| Docker dev environment | **Complete** — PostgreSQL + Azurite |
| Capacitor Android/iOS shells | Not started — Loop 2+ |
| OpenTelemetry export | **Complete** — opt-in OTLP/console via Loop 13 config |

## Last Updated

2026-07-25 — Loop 13 completion

## Recommended Next Loop

**Loop 14: Launch readiness** — mobile shells, E2E Playwright, performance budgets, production config review.
