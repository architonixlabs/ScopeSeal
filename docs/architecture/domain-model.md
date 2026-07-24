# Domain Model

> Status: Loop 0 conceptual model — EF mappings in later loops.

## Core Aggregates

### Tenancy & Identity

- **User**, **Tenant**, **TenantMember**, **RoleAssignment**
- Platform roles: Platform Administrator, Support Operator, Privacy Administrator, Security Auditor, Billing Operator
- Tenant roles: Workspace Owner, Organisation Administrator, Billing Administrator, Editor, Reviewer, Read-only Member

### Commercial

- **Plan**, **PlanVersion**, **Entitlement**, **Subscription**, **BillingCustomer**, **PaymentRecord**, **WebhookEvent**, **UsageCounter**

### Workspace

- **Contact**, **Party**, **Workspace**, **WorkspaceParty**, **WorkspaceTemplate**, **WorkspaceAccessGrant**

### Documents

- **Document**, **DocumentVersion**, **DocumentBlob**, **DocumentHash**, **UploadSession**, **MalwareScanResult**, **ProcessingJob**, **ExtractionRun**, **ExtractedFact**, **FactSource**, **Redaction**

### Agreement

- **AgreementSnapshot**, **AgreementSection**, **Commitment**, **Deliverable**, **ScopeItem**, **Exclusion**, **PaymentMilestone**, **TimelineMilestone**, **Dependency**, **Assumption**, **OpenQuestion**

### Review & Change

- **ReviewInvitation**, **ReviewSession**, **Comment**, **ChangeSuggestion**, **Approval**, **ApprovalNoticeVersion**
- **ChangeRequest**, **ChangeImpact**, **ChangeDecision**

### Compliance & Ops

- **AuditEvent**, **Notification**, **NotificationPreference**
- **ConsentRecord**, **NoticeVersion**, **PrivacyRequest**, **Grievance**, **RetentionPolicy**, **DeletionJob**, **LegalHold**, **Subprocessor**, **Incident**

## Snapshot State Machine

```text
Draft → InternalReview → Shared → ChangesRequested → ReadyForApproval → Approved
Approved → Superseded (new version approved)
Any → Withdrawn | Archived
```

Approved snapshots are **immutable**. Changes require Change Request → new draft → re-approval.

## Change Request State Machine

```text
Proposed → UnderDiscussion → PricingRequired | ScheduleReviewRequired
→ Accepted | Rejected | Withdrawn → Implemented
```

## Integrity Rules

- Approved snapshot hash from deterministic canonical JSON
- Approval points to exact snapshot version and hash
- Change request cannot mutate approved snapshot
- Revoked/expired invitations unusable
- Webhook events processed at most once
- Entitlements from verified billing state only
- Tenant boundary on every query
- Public identifiers non-guessable (no sequential IDs exposed)

## Identifiers

Use strongly typed internal IDs; expose external GUIDs or similar non-sequential public IDs.

## Currency & Time

- Currency: minor units + ISO 4217 code
- All timestamps UTC; display Asia/Kolkata default for business context
