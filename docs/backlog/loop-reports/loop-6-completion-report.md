# Loop Completion Report — Loop 6

**Date:** 2026-07-25  
**Loop:** 6 — Manual Agreement Snapshot  
**Status:** Complete

---

## Objective

Deliver manual Agreement Snapshot creation and editing without AI: draft snapshot API with scope sections, deliverables, exclusions, milestones, commitments, questions, draft-only state machine, autosave concurrency handling, entitlement gating, and tenant-isolated endpoints.

## Implemented

### AgreementSnapshots module (`ScopeSeal.AgreementSnapshots`)

- Domain: `AgreementSnapshot`, `ScopeItem`, `Exclusion`, `Deliverable`, `Commitment`, `PaymentMilestone`, `TimelineMilestone`, `SnapshotDependency`, `Assumption`, `OpenQuestion`
- Enum: `SnapshotStatus` (Loop 6 uses `Draft` only for mutations)
- Service: `IAgreementSnapshotService` with create/list/get/update contracts and section input records

### Infrastructure

- EF Core migration `AgreementSnapshots`
- `AgreementSnapshotService` — entitlement checks, section upsert sync, millisecond UTC timestamp concurrency, audit events
- Payment milestones store amount in minor units + ISO currency code

### API (`ScopeSeal.Api`)

- `POST /api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots`
- `GET /api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots`
- `GET /api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}`
- `PUT /api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}`

### Tests

- Create, list, get, and update snapshot with all section types
- Cross-tenant snapshot isolation (404)
- Free plan blocks sixth snapshot creation (403)
- Stale `expectedUpdatedAtUtc` returns concurrency conflict (409)

## Commands Executed

```powershell
dotnet build ScopeSeal.slnx
dotnet test ScopeSeal.slnx
dotnet ef migrations add AgreementSnapshots --project modules/ScopeSeal.Infrastructure --startup-project hosts/ScopeSeal.Api
```

## Test Results

| Suite | Result |
|-------|--------|
| ScopeSeal.Architecture.Tests | 2 passed |
| ScopeSeal.Api.Tests | 25 passed |
| **Total** | **27 passed** |

## Known Limitations

- No Angular product UI for snapshot editor yet (API foundation only)
- Only `Draft` snapshots are editable; review/approval transitions deferred to Loop 7
- No PDF export yet (Loop 6+)
- No canonical JSON hash generation until approval workflow (Loop 7)
- Autosave uses client-supplied `expectedUpdatedAtUtc`; no ETag header yet

## Security Review

- All endpoints require tenant membership; cross-tenant access returns 404
- Mutations require `TenantEditor` policy
- Snapshot creation gated by `CanCreateSnapshot` and `SnapshotsCreatedThisMonth` usage
- Approved/immutable behaviour enforced by draft-only edit guard (no AI auto-approval)

## Recommended Next Loop

**Loop 7: Review and approval** — share snapshots, reviewer invitations, approval records, immutability hashes.
