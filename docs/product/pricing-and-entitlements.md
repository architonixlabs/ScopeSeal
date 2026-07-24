# Pricing and Entitlements

> Status: Loop 0 draft. **All prices are configurable hypotheses — not hard-coded in source or tests.**

## Launch Price Hypotheses (INR)

| Plan | Monthly | Annual |
|------|---------|--------|
| Free | ₹0 | ₹0 |
| Pro | ₹399 | ₹3,999 |
| Business | ₹1,499 | ₹14,999 |

Requires founder validation and CA review for tax treatment before publication.

## Entitlement Model

Entitlements are evaluated by a single **entitlement service** using:

- Verified subscription state (never client-only checkout success)
- Plan version effective at evaluation time
- Usage counters (snapshots/month, storage, AI tokens, etc.)
- Capability flags (typed, not string plan names)

### Capability Examples

```text
CanCreateWorkspace
CanCreateSnapshot
CanUploadDocument
CanUseAiExtraction
CanUseOcr
CanTranscribeAudio
CanInviteExternalReviewer(count)
CanUseChangeRequestWorkflow
CanExportAdvancedPdf
CanUseCustomLogo
CanManageTeamMembers
CanUseSharedTemplates
CanConfigureRetention
CanAccessApi
```

### Downgrade Rules

1. Preserve existing records
2. Block creation beyond new limits
3. Configurable grace period for paid features
4. Clear UI explanation of affected features
5. Export and deletion always available
6. Never charge silently

### Free Plan Guarantees

- No payment card required
- Full privacy rights (access, export, erasure, grievance)
- Data export and account deletion
- Optional consent withdrawal

## Plan Versioning

- Price changes create new plan versions
- Existing subscriptions not silently migrated
- Record: old price, new price, effective date, notice, acceptance where required, Razorpay plan mapping, grandfathering policy

## Razorpay Mapping (Configuration)

Plan versions map to Razorpay plan IDs via configuration:

```text
Billing:Plans:Pro:Monthly:RazorpayPlanId
Billing:Plans:Pro:Annual:RazorpayPlanId
```

Webhook events are authoritative over browser callback success.

## Usage Metering

Track per tenant:

- Snapshots created (monthly window)
- Storage bytes
- AI extraction jobs / tokens
- External invitations sent
- Export downloads

Usage ledger append-only for billing disputes and limit enforcement.

## Assumptions for CA / Founder Review

- GST treatment and place of supply
- Invoice format and GSTIN capture
- Refund policy alignment with Razorpay
- Annual vs monthly proration on upgrade/downgrade
- Grace period duration after failed payment
