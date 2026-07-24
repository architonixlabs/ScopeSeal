# Loop Completion Report — Loop 3

**Date:** 2026-07-25  
**Loop:** 3 — Plans and entitlements  
**Status:** Complete

---

## Objective

Deliver a central entitlement service with configuration-driven plan definitions, versioned plan catalog, tenant plan assignments, usage counters, server-side capability checks, Free-plan defaults on registration, downgrade-safe behaviour, and tenant-isolated entitlements API — without Razorpay integration.

## Implemented

### Entitlements module (`ScopeSeal.Entitlements`)

- Domain: `PlanCode`, `Capability`, `UsageMetric`, `EntitlementSource`, `PlanVersion`, `TenantPlanAssignment`, `UsageCounter`
- Configuration: `PlansOptions` with Free, Pro, and Business definitions (limits and capability flags)
- `IEntitlementService` with capability checks, usage checks/recording, plan assignment, and entitlement summary
- `PlanLimitsSnapshot` for immutable versioned limit storage

### Infrastructure (`ScopeSeal.Infrastructure`)

- `EntitlementService` — server-side enforcement; privacy capabilities never paywalled
- `PlanCatalogSeeder` — seeds/updates plan versions from configuration on startup
- EF Core migration `PlansAndEntitlements`
- Registration assigns default Free plan after tenant creation

### API (`ScopeSeal.Api`)

- `GET /api/v1/tenants/{tenantPublicId}/entitlements` — member-only plan, capabilities, and usage summary

### Configuration

- `Plans` section in `appsettings.json` and `appsettings.Development.json` with AGENTS.md-aligned defaults

### Tests

- Free plan on registration
- Tenant isolation for entitlements endpoint
- Downgrade blocks paid capabilities while preserving privacy access
- Snapshot monthly usage limit enforcement on Free plan

## Commands Executed

```powershell
dotnet build ScopeSeal.slnx
dotnet test ScopeSeal.slnx
dotnet ef migrations add PlansAndEntitlements --project modules/ScopeSeal.Infrastructure --startup-project hosts/ScopeSeal.Api
```

## Test Results

| Suite | Result |
|-------|--------|
| ScopeSeal.Architecture.Tests | 2 passed |
| ScopeSeal.Api.Tests | 9 passed |
| **Total** | **11 passed** |

## Known Limitations

- Razorpay and webhook-driven plan changes deferred to Loop 10
- Administrator grant UI/API deferred to Loop 12
- Usage counters are foundation only; feature modules will call `RecordUsageAsync` in later loops
- Grace period after downgrade not yet configurable (immediate limit application)
- Plan price metadata not stored (billing amounts remain configuration hypotheses)

## Security Review

- All entitlement checks are server-side via `IEntitlementService`
- No scattered `if (plan == "pro")` checks
- Privacy capabilities (`CanAccessPrivacyCentre`, `CanRequestDataExport`, `CanRequestAccountDeletion`) always allowed
- Entitlements endpoint requires tenant membership — cross-tenant access returns 404
- No Razorpay keys or payment logic introduced

## Recommended Next Loop

**Loop 4: Workspace and contact management** — workspace CRUD with entitlement-gated creation limits.

---

*End of Loop 3.*
