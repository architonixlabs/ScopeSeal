# Loop Completion Report — Loop 7

**Date:** 2026-07-25  
**Loop:** 7 — Review and Approval  
**Status:** Complete

---

## Objective

Deliver review and approval workflow: share snapshots, secure expiring reviewer invitations, external review endpoints, comments, change suggestions, approval confirmation, immutable approved versions, canonical JSON integrity hash, revocation, expiration handling, approval audit events, and state transitions with tenant isolation.

## Implemented

### Approvals module (`ScopeSeal.Approvals`)

- Domain: `ReviewInvitation`, `ReviewComment`, `ChangeSuggestion`, `ApprovalRecord`, `InvitationStatus`
- Service: `IReviewApprovalService` with tenant-scoped and token-based external review contracts
- `CanonicalSnapshotHasher` — deterministic canonical JSON → SHA-256 integrity hash (not a truth or legal-signature claim)

### Infrastructure

- EF Core migration `ReviewAndApproval`
- `ReviewApprovalService` — state transitions, entitlement gating, audit events, token validation
- Extended `AgreementSnapshot` with `CanonicalHashSha256` and `ApprovedAtUtc`
- Extended `AuditEventType` with share, ready-for-approval, changes-requested, approved, invitation, comment, and suggestion events

### API (`ScopeSeal.Api`)

**Tenant-scoped (authenticated):**

- `POST .../snapshots/{id}/share` — Draft → Shared
- `POST .../snapshots/{id}/ready-for-approval` — Shared/ChangesRequested → ReadyForApproval
- `POST .../snapshots/{id}/invitations` — create expiring invitation (entitlement gated)
- `GET .../snapshots/{id}/invitations` — list invitations
- `POST .../snapshots/{id}/invitations/{invitationId}/revoke` — revoke invitation
- `GET .../snapshots/{id}/approval` — get approval record with integrity hash

**External (token-based, anonymous):**

- `GET /api/v1/external/review/{token}` — view snapshot for review
- `POST /api/v1/external/review/{token}/comments` — add review comment
- `POST /api/v1/external/review/{token}/change-suggestions` — suggest changes
- `POST /api/v1/external/review/{token}/request-changes` — Shared/ReadyForApproval → ChangesRequested
- `POST /api/v1/external/review/{token}/approve` — ReadyForApproval → Approved with confirmation

### Tests

- Full share → invite → comment → ready → approve flow with integrity hash
- Approved snapshot edit blocked (409)
- Revoked invitation returns 404
- Request changes state transition
- Cross-tenant invitation isolation (404)
- Free plan blocks second external invitation (403)

## Commands Executed

```powershell
dotnet build ScopeSeal.slnx
dotnet test ScopeSeal.slnx
dotnet ef migrations add ReviewAndApproval --project modules/ScopeSeal.Infrastructure --startup-project hosts/ScopeSeal.Api
```

## Test Results

| Suite | Result |
|-------|--------|
| ScopeSeal.Architecture.Tests | 2 passed |
| ScopeSeal.Api.Tests | 31 passed |
| **Total** | **33 passed** |

## Known Limitations

- No Angular product UI for review/approval yet (API foundation only)
- No OTP verification on external review links (optional future hardening)
- No email delivery of invitation links yet (token returned in API response)
- Change Ledger integration deferred to Loop 8
- Approved snapshot reapproval/version superseding deferred to Loop 8

## Security Review

- External review uses non-guessable GUID tokens with expiry and revocation
- Revoked/expired tokens return 404 (information hiding)
- Cross-tenant access returns 404 on all tenant-scoped endpoints
- External invitations gated by `CanInviteExternalReviewer` entitlement
- Approval requires explicit confirmation statement (min 10 characters)
- Canonical hash records snapshot integrity at approval time — not enforceability or legal signature
- Audit events recorded for share, approval, invitation, comment, and suggestion actions

## Recommended Next Loop

**Loop 8: Change Ledger** — change requests, version comparisons, reapproval after accepted changes.
