# ADR-001: Modular Monolith Architecture

## Status

Accepted (Loop 0)

## Context

ScopeSeal is an early-stage India-first SaaS. The initial business does not justify distributed microservices complexity, operational overhead, or cross-service transaction challenges.

## Decision

Build a **modular monolith** with clear domain modules, explicit application use cases, and infrastructure adapters behind interfaces.

## Consequences

**Positive:**

- Simpler deployment and debugging for small team
- ACID transactions across modules where needed
- Faster iteration in delivery loops

**Negative:**

- Must enforce module boundaries through architecture tests
- Scaling requires vertical scaling or selective extraction later
- Discipline required to avoid "big ball of mud"

## Alternatives Considered

- Microservices: rejected for MVP
- Serverless-only: rejected due to long-running jobs and EF Core patterns

---

# ADR-002: PostgreSQL as Primary Database

## Status

Accepted (Loop 0)

## Context

Need relational integrity, JSON support, mature EF Core tooling, and Testcontainers for integration tests.

## Decision

Use PostgreSQL with EF Core migrations, foreign keys, check constraints, and tenant-participating indexes.

## Consequences

Evaluate row-level security as defence in depth in Loop 13; document outcome in follow-up ADR.

---

# ADR-003: Entitlement Policy Engine

## Status

Accepted (Loop 0)

## Context

Plan checks scattered in code lead to billing bugs and inconsistent UI.

## Decision

Single entitlement service with typed capabilities, configuration-driven limits, server-side enforcement, and auditable plan versions.

## Consequences

All feature gates go through `IEntitlementService` (name TBD in Loop 3). UI reflects server state only.

---

# ADR-004: External Identifiers

## Status

Accepted (Loop 0)

## Context

Sequential IDs enable enumeration and IDOR probing.

## Decision

Expose non-guessable external identifiers (GUIDs or similar) in APIs and invitation links; keep internal surrogate keys where useful.

---

# ADR-005: ManualOnly AI Default

## Status

Accepted (Loop 0)

## Context

AI processing introduces cost, privacy, and prompt-injection risks. Product must remain usable without AI.

## Decision

System supports `ManualOnly`, `LocalProcessing`, and `ApprovedExternalProvider` modes. Default deployment configuration starts ManualOnly until Loop 9 controls verified.

## Consequences

Loop 6 delivers full manual snapshot value before AI integration.

---

# ADR-006: Razorpay Behind IPaymentGateway

## Status

Accepted (Loop 0)

## Context

Payment integration must be testable, swappable, and free of Razorpay types in domain layer.

## Decision

Domain depends on `IPaymentGateway` interface; Razorpay adapter in infrastructure; webhooks authoritative over browser callbacks.

See Razorpay risk checklist in `docs/security/razorpay-integration-checklist.md`.
