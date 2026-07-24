# ScopeSeal Product Backlog

> Status: Loop 0 — prioritised for delivery loops 1–14. IDs map to AGENTS.md loops.

## P0 — Foundation (Blocks Everything)

| ID | Item | Loop | Notes |
|----|------|------|-------|
| PB-001 | Solution structure & modular monolith skeleton | 1 | .NET 10 + Angular 22 |
| PB-002 | Docker dev environment + PostgreSQL | 1 | |
| PB-003 | CI pipeline foundation | 1 | Build, test, secret scan |
| PB-004 | Configuration validation & health checks | 1 | |
| PB-005 | Structured logging & OpenTelemetry baseline | 1 | |

## P0 — Identity & Commercial Core

| ID | Item | Loop | Notes |
|----|------|------|-------|
| PB-010 | Registration, login, email verification | 2 | |
| PB-011 | Tenant creation & membership | 2 | |
| PB-012 | Role-based authorization policies | 2 | |
| PB-013 | Tenant isolation integration tests | 2 | |
| PB-014 | Plan model & entitlement engine | 3 | No Razorpay yet |
| PB-015 | Usage counters & limit enforcement | 3 | |
| PB-016 | Free plan defaults & downgrade-safe rules | 3 | |

## P0 — MVP Product Path

| ID | Item | Loop | Notes |
|----|------|------|-------|
| PB-020 | Workspaces, parties, templates | 4 | |
| PB-021 | Dashboard (draft, review, changes) | 4 | |
| PB-022 | Secure document upload & quarantine | 5 | Conservative allowlist |
| PB-023 | Malware scan abstraction | 5 | |
| PB-024 | Manual Agreement Snapshot editor | 6 | Usable without AI |
| PB-025 | Snapshot state machine & autosave | 6 | |
| PB-026 | Basic branded PDF export | 6 | |
| PB-027 | Review invitations & external review | 7 | |
| PB-028 | Approval + canonical hash | 7 | |
| PB-029 | Change requests & visual diff | 8 | |
| PB-030 | Version timeline | 8 | |

## P1 — Post-MVP Core

| ID | Item | Loop | Notes |
|----|------|------|-------|
| PB-040 | AI extraction pipeline | 9 | ManualOnly first |
| PB-041 | Razorpay test-mode integration | 10 | |
| PB-042 | Privacy centre & deletion orchestration | 11 | |
| PB-043 | Admin portal | 12 | |
| PB-044 | Security hardening & DR test | 13 | |
| PB-045 | Launch readiness | 14 | |

## P2 — Deferred

| ID | Item | Notes |
|----|------|-------|
| PB-050 | Enterprise SSO / SCIM | Design only |
| PB-051 | Native WhatsApp integration | Not MVP |
| PB-052 | Public API & webhooks | Business optional |
| PB-053 | Legal hold workflow | Business optional |
| PB-054 | Audio transcription | After PDF/text stable |
| PB-055 | OCR | Pro plan |
| PB-056 | Row-level security | Evaluate Loop 13 |

## Legal & Compliance Backlog (Non-Engineering)

| ID | Item | Owner |
|----|------|-------|
| LB-001 | Privacy notice legal review | Legal |
| LB-002 | Terms of service review | Legal |
| LB-003 | GST invoicing rules | CA |
| LB-004 | Data Fiduciary classification | Legal |
| LB-005 | Grievance officer appointment | Founder |
| LB-006 | Razorpay merchant agreement | Founder |

## MVP Boundary Summary

**Ship first:** Loops 1–8 with ManualOnly AI stub; privacy centre minimal in Loop 11 if required for deletion rights before public beta.

**Not in first public beta:** Live Razorpay, external AI, admin portal at scale, enterprise features.
