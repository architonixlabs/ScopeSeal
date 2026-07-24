# ScopeSeal User Journeys

> Status: Loop 0 draft — maps to delivery loops in AGENTS.md.

## Journey 1: Create Workspace (Loop 4)

1. User signs in and lands on dashboard
2. Creates workspace, selects type (e.g. Interior-design project)
3. Adds parties and roles
4. States purpose; chooses template or blank
5. Reviews privacy notice for workspace processing
6. Adds material manually or uploads files (Loop 5)
7. Sets expected retention where configurable
8. Begins manual snapshot or extraction (Loops 6–9)

**Success:** Draft workspace with parties and at least one material source or manual section.

## Journey 2: Upload and Process Material (Loop 5)

1. User selects allowed file types (PDF, PNG, JPEG, WEBP, text, selected audio)
2. System validates size and content type, stores in quarantine, scans for malware
3. User sees processing status; unsafe files rejected or isolated
4. Approved material moves to private storage with hash recorded

**Success:** Linked supporting document available for snapshot sections.

## Journey 3: Build Agreement Snapshot — Manual (Loop 6)

1. User edits section cards: scope, deliverables, inclusions, exclusions, milestones, commitments, questions
2. Autosave with concurrency handling
3. Snapshot remains in Draft until explicitly shared

**Success:** Complete draft snapshot without AI (ManualOnly mode viable).

## Journey 4: AI-Assisted Extraction (Loop 9)

1. User triggers extraction on approved material (entitlement-checked)
2. AI suggests fields with confidence, source, and provenance — all Draft
3. User accepts, edits, rejects, or marks uncertain
4. No automatic approval regardless of confidence

**Success:** User-confirmed facts merged into draft snapshot.

## Journey 5: Share for Review (Loop 7)

1. Creator selects recipient, role, expiry; previews shared view
2. Optional OTP for higher-risk actions
3. Reviewer opens secure link, reads snapshot and authorised supporting material
4. Reviewer comments, suggests corrections, approves, requests changes, or declines — all actions equally visible

**Success:** Review recorded; invitation audit trail complete.

## Journey 6: Approval (Loop 7)

1. Reviewer sees confirmation disclaimer (not legal advice; not enforceability guarantee)
2. System records approval with canonical snapshot hash, timestamp, auth method, notice versions
3. Approved snapshot becomes immutable

**Success:** Approved version with hash linked to approval record.

## Journey 7: Change Request (Loop 8)

1. Party proposes change with reason, impacts, and source
2. Counterparty responds; states progress through workflow
3. On acceptance: previous approved snapshot preserved; new draft created with diff; re-approval required

**Success:** New version linked to change request; no silent overwrite.

## Journey 8: Export Record Package (Loop 6+)

1. User selects export scope (versions, comments per policy)
2. System generates package with hashes, disclaimer, verification instructions
3. Download available — marketed as record package, not certified legal evidence

**Success:** Verifiable export without exposing other tenants' data.

## Journey 9: Upgrade to Pro (Loop 10)

1. User hits free limit or chooses upgrade
2. Backend creates Razorpay subscription; Checkout in browser
3. Server verifies signature; webhooks confirm state; entitlements update idempotently

**Success:** Paid capabilities active only after server-verified billing state.

## Journey 10: Privacy Rights (Loop 11)

1. User opens Privacy centre
2. Submits access, export, correction, erasure, or grievance
3. Identity verification where required; request tracked
4. Optional consent withdrawn without paywall

**Success:** Documented response within configured SLA (TBD by operations).

## Journey 11: Account Deletion (Loop 11)

1. User requests deletion from settings
2. Immediate access revocation; orchestrated deletion across DB, storage, caches, derived AI outputs
3. Deletion receipt issued; backup expiry explained accurately

**Success:** No access via old links; audit metadata without preserved content.
