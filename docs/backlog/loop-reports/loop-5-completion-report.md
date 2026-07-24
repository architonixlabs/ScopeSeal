# Loop Completion Report — Loop 5

**Date:** 2026-07-25  
**Loop:** 5 — Secure document upload  
**Status:** Complete

---

## Objective

Deliver secure document upload with upload sessions, conservative content allowlist, magic-byte validation, quarantine and permanent blob storage, malware scan abstraction, SHA-256 integrity hashes, processing jobs, signed short-lived downloads, entitlement-gated storage usage, audit events, and tenant-isolated API endpoints.

## Implemented

### Documents module (`ScopeSeal.Documents`)

- Domain: `UploadSession`, `Document`, `DocumentVersion`, `DocumentBlob`, `DocumentHash`, `MalwareScanResult`, `ProcessingJob`, `DocumentDownloadToken`
- Enums: `UploadSessionStatus`, `DocumentStatus`, `MalwareScanStatus`, `ProcessingJobStatus`, `ProcessingJobType`
- Services: `IUploadSessionService`, `IDocumentService`, `IBlobStorageService`, `IMalwareScanner`, `IContentTypeValidator`

### Infrastructure

- EF Core migration `DocumentsAndUploadSessions`
- `UploadSessionService` — entitlement checks, quarantine upload, content validation, malware scan, permanent storage promotion, usage recording, audit events
- `DocumentService` — list/get documents, preview metadata, signed download tokens
- `ContentTypeValidator` — allowlist (PDF, PNG, JPEG, WEBP, text, selected audio), blocked extensions, magic-byte validation
- `DevelopmentMalwareScanner` — no-op scanner with EICAR test detection
- `InMemoryBlobStorageService` (Testing) and `AzuriteBlobStorageService` (Development/Production)
- `DocumentUploadOptions` in `ScopeSealOptions` — max file size, session/token expiry, container names

### API (`ScopeSeal.Api`)

- `POST /api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions`
- `PUT /api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions/{sessionPublicId}/content`
- `POST /api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions/{sessionPublicId}/complete`
- `GET /api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions/{sessionPublicId}`
- `GET /api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/documents`
- `GET /api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/documents/{documentPublicId}`
- `POST /api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/documents/{documentPublicId}/download-token`
- `GET /api/v1/tenants/{tenantPublicId}/documents/download?token={token}`

### Tests

- Full upload → complete → list → download flow with SHA-256 preview metadata
- Cross-tenant upload session and document isolation (404)
- Blocked file extension rejection
- Content-type spoofing rejection (magic-byte mismatch)
- EICAR malware signature rejection
- Per-file size limit enforcement (403)

## Commands Executed

```powershell
dotnet build ScopeSeal.slnx
dotnet test ScopeSeal.slnx
dotnet ef migrations add DocumentsAndUploadSessions --project modules/ScopeSeal.Infrastructure --startup-project hosts/ScopeSeal.Api
```

## Test Results

| Suite | Result |
|-------|--------|
| ScopeSeal.Architecture.Tests | 2 passed |
| ScopeSeal.Api.Tests | 21 passed |
| **Total** | **23 passed** |

## Known Limitations

- No Angular product UI for uploads yet (API foundation only)
- Worker does not yet process `ProcessingJob` records asynchronously
- Production malware scanner adapter not integrated (development no-op only)
- Azurite required for non-Testing blob storage; CI uses in-memory adapter
- Preview generation job created but not executed
- Storage usage decrement on document deletion deferred

## Security Review

- All endpoints require tenant membership; cross-tenant access returns 404
- Mutations require `TenantEditor` policy
- Upload gated by `IEntitlementService.CheckCapabilityAsync(CanUploadDocument)` and `StorageBytes` usage
- Conservative allowlist with magic-byte validation; blocked executable/archive extensions
- Server-generated blob paths; original filenames not used in storage keys
- Quarantine → scan → permanent storage pipeline
- Short-lived download tokens scoped to tenant with expiry
- Audit events for upload session creation, document upload, and rejected uploads

## Recommended Next Loop

**Loop 6: Manual Agreement Snapshot** — draft snapshot creation from workspace context and uploaded documents.

---

*End of Loop 5.*
