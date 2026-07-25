# Loop Completion Report — Loop 8

**Date:** 2026-07-25  
**Loop:** 8 — Change Ledger  
**Status:** Complete

---

## Objective

Deliver change request workflow with state machine, scope/financial/schedule impacts, approved snapshot preservation, draft cloning on acceptance, visual diff API, reapproval with superseding, version linkage, tenant isolation, and entitlement gating.

## Implemented

### Change Ledger module (`ScopeSeal.ChangeLedger`)

- Domain: `ChangeRequest`, `ChangeImpact`, `ChangeDecision`, `ChangeRequestStatus`, `ChangeImpactType`
- Service contracts: `IChangeLedgerService`, `SnapshotDiffService` (section-level visual diff)
- DI module marker and registration

### Infrastructure

- EF Core migration `ChangeLedger`
- `ChangeLedgerService` — create, list, get, transition, accept (clone to draft), diff
- Extended `AgreementSnapshot` with `SourceSnapshotId` and `ChangeRequestId` lineage fields
- Extended `ReviewApprovalService` — on reapproval: supersede source approved snapshot, mark change request implemented
- Extended `AuditEventType` with change request lifecycle events

### API (`ScopeSeal.Api`)

**Tenant-scoped (authenticated):**

- `POST .../change-requests` — propose change against approved snapshot
- `GET .../change-requests` — list workspace change requests
- `GET .../change-requests/{id}` — get change request with impacts and decisions
- `POST .../change-requests/{id}/transition` — state machine transitions
- `POST .../change-requests/{id}/accept` — clone approved snapshot to new draft (v+1)
- `GET .../snapshots/{from}/diff/{to}` — visual section diff between versions

### Tests

- Full propose → discuss → accept → edit draft → reapprove → supersede → implemented flow
- Visual diff returns section changes
- Approved snapshot not mutated by change acceptance
- Cross-tenant change request isolation (404)
- Invalid state transition rejected (409)

## Commands Executed

```powershell
dotnet build ScopeSeal.slnx
dotnet test ScopeSeal.slnx
dotnet ef migrations add ChangeLedger --project modules/ScopeSeal.Infrastructure --startup-project hosts/ScopeSeal.Api
```

## Test Results

| Suite | Result |
|-------|--------|
| ScopeSeal.Architecture.Tests | 2 passed |
| ScopeSeal.Api.Tests | 36 passed |
| **Total** | **38 passed** |

## Known Limitations

- No Angular product UI for Change Ledger yet (API foundation only)
- Accept requires status UnderDiscussion, PricingRequired, or ScheduleReviewRequired
- Visual diff is structural (section items), not rich-text inline diff
- Free plan external invitation limit requires elevated test config for multi-approval flows

## Security Review

- Change requests scoped to tenant and workspace on every query
- Cross-tenant access returns 404
- Approved snapshots never overwritten — clone creates new draft with lineage links
- Reapproval supersedes prior approved version; canonical hash computed at approval time
- Change request workflow gated by `CanUseChangeRequestWorkflow` entitlement

## Recommended Next Loop

**Loop 9: AI extraction** — extraction runs, draft facts, provenance, ManualOnly default.
