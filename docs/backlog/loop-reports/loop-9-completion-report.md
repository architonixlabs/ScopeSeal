# Loop Completion Report — Loop 9

**Date:** 2026-07-25  
**Loop:** 9 — AI extraction  
**Status:** Complete

---

## Objective

Deliver AI extraction pipeline with provider abstraction, ManualOnly default, draft-only facts with provenance, prompt-injection validation, entitlement metering, kill switch, worker job processing, and human review before snapshot merge.

## Implemented

### Extraction module (`ScopeSeal.Extraction`)

- Domain: `ExtractionRun`, `ExtractedFact`, status enums, section types
- Services: `IExtractionService`, `IAiExtractionProvider`, `IExtractionSchemaValidator`, `IProcessingJobProcessor`
- DI module marker and registration

### Infrastructure

- EF Core migration `AiExtraction`
- `ExtractionService` — trigger run, get status, review facts, apply accepted facts to draft snapshot
- `ProcessingJobProcessor` — polls `TextExtraction` jobs, invokes provider, validates output, persists draft facts
- Providers: `ManualOnly`, `LocalProcessing` (fixture-driven for tests), `ApprovedExternalProvider` (stub — not configured)
- `ExtractionSchemaValidator` — max facts, confidence bounds, instruction-pattern rejection
- Extended `AiOptions` with kill switch and batch limits
- Extended `AuditEventType` with extraction lifecycle events
- Entitlement usage recorded on successful job completion

### Worker (`ScopeSeal.Worker`)

- Replaced heartbeat with `ProcessingJobWorker` polling pending extraction jobs every 5 seconds
- References Infrastructure for shared job processor

### API (`ScopeSeal.Api`)

- `POST .../documents/{id}/extraction-runs` — queue extraction (403 in ManualOnly / Free plan / kill switch)
- `GET .../extraction-runs/{id}` — run status and draft facts with provenance
- `POST .../extraction-runs/{id}/facts/{factId}/review` — accept / reject / uncertain
- `POST .../extraction-runs/{id}/apply/{snapshotId}` — merge accepted facts into draft snapshot

### Tests

- ManualOnly mode blocks trigger
- Free plan blocks trigger
- LocalProcessing full flow: trigger → process → review → apply
- Invalid provider output fails run (schema / injection defence)
- Cross-tenant isolation (404)
- Entitlement fix: atomic usage increment with retry; invitation usage reserved before create

## Commands Executed

```powershell
dotnet build ScopeSeal.slnx
dotnet test ScopeSeal.slnx
dotnet ef migrations add AiExtraction --project modules/ScopeSeal.Infrastructure --startup-project hosts/ScopeSeal.Api
```

## Test Results

| Suite | Result |
|-------|--------|
| ScopeSeal.Architecture.Tests | 2 passed |
| ScopeSeal.Api.Tests | 41 passed |
| **Total** | **43 passed** |

## Known Limitations

- No Angular product UI for extraction yet (API foundation only)
- `ApprovedExternalProvider` adapter not configured (throws if mode enabled without adapter)
- `LocalProcessing` uses deterministic fixture logic, not real OCR/LLM
- PreviewGeneration jobs still not processed (future hardening)
- Usage metering increments on successful completion, not at queue time

## Security Review

- Extraction runs scoped to tenant and workspace on every query
- Cross-tenant access returns 404
- All extracted facts start as Draft — never auto-applied or auto-approved
- Schema validator rejects instruction-like content and malformed confidence
- Kill switch disables new runs and worker processing
- ManualOnly default — product works without AI

## Recommended Next Loop

**Loop 10: Razorpay integration** — test-mode checkout, webhook verification, entitlement reconciliation.
