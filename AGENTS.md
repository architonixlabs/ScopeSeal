# ScopeSeal Repository Agent Instructions

## 1. Repository authority

This file governs autonomous work in the repository:

```text
architonixlabs/ScopeSeal
```

Before changing anything, read:

1. This `AGENTS.md`
2. `.cursor/rules/`
3. `.cursor/skills/`
4. `README.md`
5. Product documentation
6. Architecture decision records
7. Security and privacy documentation
8. Product backlog
9. Implementation ledger
10. Previous completion reports
11. Existing source code and test results

When documentation and code conflict, investigate the discrepancy. Determine the intended behaviour, record the decision, correct the implementation or documentation, and continue.

Priority order:

1. Prevention of data loss or unauthorised access
2. Tenant isolation
3. Authentication and authorization
4. Privacy and data minimisation
5. Billing and entitlement integrity
6. Store-policy safety
7. Build correctness
8. Product correctness
9. Accessibility and usability
10. Performance
11. Development convenience

Do not ask for routine architectural, technical, design, refactoring, testing, or implementation approval. Select the safest maintainable option, document significant decisions, implement them, validate them, and continue.

---

# 2. Product definition

ScopeSeal is a neutral communication-clarity, approval-record, scope-management, and change-control platform.

It converts conversations and uploaded material into:

* Agreement Snapshots
* Parties and responsibilities
* Included scope
* Excluded scope
* Deliverables
* Commitments
* Prices and payment milestones
* Timeline milestones
* Dependencies
* Assumptions
* Open questions
* Review comments
* Approval records
* Immutable approved versions
* Change requests
* Version comparisons
* Reapproval history
* Downloadable record packages

ScopeSeal is not:

* A law firm
* A lawyer
* A legal-advice service
* A court or arbitration service
* A notary
* A statutory digital-signature authority
* A government system
* A guarantee of enforceability
* A guarantee that uploaded material is genuine
* A guarantee that AI extraction is accurate
* A certified-evidence provider

Never use statements such as:

* Legally guaranteed
* Court-proof
* Certified evidence
* Government approved
* Completely tamper-proof
* Automatically enforceable

Use accurate terms such as:

* Approval record
* Recorded timestamp
* Version history
* Integrity hash
* Agreement Snapshot
* Supporting material
* Change Ledger
* Record package

---

# 3. Required product surfaces

The repository must ultimately contain five coordinated product surfaces.

## 3.1 Backend platform

A modular ASP.NET Core application providing:

* Public API
* Authentication and authorization
* Tenant management
* Product domain services
* Document processing
* AI-processing abstractions
* Billing
* Entitlements
* Notifications
* Privacy workflows
* Administration
* Audit and observability
* Background processing

## 3.2 Product web application

The authenticated ScopeSeal application used in desktop and mobile browsers.

It must provide the complete product workflow and remain usable without AI.

## 3.3 Android application

A Capacitor native application wrapping the shared Angular product client.

It must include platform adapters for Android-specific functionality and must not become a separately duplicated product codebase.

## 3.4 iOS application

A Capacitor native application wrapping the same Angular product client.

It must include platform adapters for iOS-specific functionality and must not become a separately duplicated product codebase.

## 3.5 Dedicated marketing website

A separate public Angular SSR/SSG application for:

* Home page
* Product explanation
* Use cases
* Features
* Pricing
* Free, Pro, and Business comparison
* Security overview
* Privacy overview
* AI-processing explanation
* Subprocessor information
* Help and documentation
* Frequently asked questions
* Contact and support
* Blog or learning resources
* App-download pages
* Terms and policies
* System-status link
* Login and registration links

The marketing website must not contain authenticated product functionality or directly process sensitive customer documents.

## 3.6 Administration portal

A separate restricted Angular application for authorised ScopeSeal platform operators.

It must not be bundled into or exposed through the public marketing application.

---

# 4. Recommended repository structure

Prefer the following structure unless an existing valid structure is already established:

```text
/
├── AGENTS.md
├── README.md
├── SECURITY.md
├── CONTRIBUTING.md
├── .editorconfig
├── .env.example
├── .github/
│   ├── workflows/
│   ├── dependabot.yml
│   └── CODEOWNERS
├── docs/
│   ├── architecture/
│   ├── product/
│   ├── security/
│   ├── privacy/
│   ├── mobile/
│   ├── billing/
│   ├── operations/
│   ├── testing/
│   ├── deployment/
│   └── backlog/
├── src/
│   ├── backend/
│   │   ├── ScopeSeal.sln
│   │   ├── modules/
│   │   ├── hosts/
│   │   └── tests/
│   └── clients/
│       ├── angular.json
│       ├── package.json
│       ├── capacitor.config.ts
│       ├── projects/
│       │   ├── product-app/
│       │   ├── marketing-site/
│       │   ├── admin-portal/
│       │   ├── shared-ui/
│       │   ├── shared-auth/
│       │   ├── shared-api/
│       │   ├── shared-domain/
│       │   └── shared-platform/
│       ├── android/
│       └── ios/
├── infrastructure/
└── tools/
```

Use a standard Angular multi-project workspace unless the repository already uses a well-configured alternative.

Do not introduce Nx merely because multiple applications exist. Introduce it only when measurable workspace requirements justify its operational complexity.

---

# 5. Core technology choices

Use stable supported versions that are mutually compatible when implementation begins.

Preferred technologies:

## Backend

* Current supported .NET LTS
* ASP.NET Core
* Entity Framework Core
* PostgreSQL
* OpenAPI
* Problem Details
* OpenTelemetry
* Database-backed background jobs
* Transactional outbox
* Structured logging
* Health checks
* Built-in rate limiting
* Policy-based authorization

## Frontend

* Current supported Angular release
* Strict TypeScript
* Angular Material and CDK
* Angular SSR/SSG for the marketing site
* Responsive PWA support
* Accessible components
* Route-level lazy loading
* Internationalisation-ready structure

## Mobile

* Current stable Capacitor release compatible with the Angular workspace
* Native Android project generated and maintained through Capacitor
* Native iOS project generated and maintained through Capacitor
* Platform adapters rather than direct native API calls throughout feature code
* Minimal, audited native plugins
* Native Swift or Kotlin plugins only when a maintained secure plugin is unavailable

## Testing

* xUnit
* FluentAssertions
* Testcontainers
* Angular unit and component tests
* Playwright
* Android unit and instrumentation-test foundations
* iOS unit and simulator-test foundations
* API contract tests
* Accessibility tests
* Architecture tests
* Security tests

---

# 6. Shared client architecture

Use one shared Angular product application for:

* Browser application
* Progressive Web App
* Android Capacitor application
* iOS Capacitor application

Do not create separate feature implementations for web, Android, and iOS.

Separate platform behaviour behind interfaces such as:

```text
PlatformService
SecureStorageService
DocumentPickerService
CameraCaptureService
ShareService
DeepLinkService
NotificationService
BiometricService
NetworkStatusService
AppLifecycleService
FileCacheService
```

Provide implementations for:

```text
Browser
Android
iOS
```

Feature modules must depend on the interfaces, not directly on Capacitor APIs.

Platform-specific code must remain isolated and tested.

---

# 7. Product web application rules

The product web application is the complete functional version of ScopeSeal.

It must support:

* Registration
* Authentication
* Free-plan onboarding
* Workspace management
* Secure uploads
* Agreement Snapshot creation
* Review and approval
* Change Ledger
* AI-assisted extraction
* Razorpay billing
* Privacy centre
* Data export
* Account deletion
* Notifications
* Responsive desktop and mobile-browser experiences

Web authentication should use secure HttpOnly cookies or a secure backend-for-frontend pattern.

Do not store long-lived authentication credentials in browser local storage.

---

# 8. Native mobile application rules

The Android and iOS applications are first-class clients, but they share the product client.

## 8.1 Initial native scope

The initial native applications should support:

* Registration for the Free plan
* Sign-in
* Secure session persistence
* Dashboard
* Workspace list
* Workspace creation and editing
* Agreement Snapshot editing
* Camera capture
* Photo selection
* Document selection
* Secure upload
* Review invitation handling
* Deep-link opening
* Approval and change-request workflows
* Record viewing
* PDF viewing and sharing
* Push-notification abstraction
* Offline connectivity status
* Safe retry of interrupted operations
* Privacy centre
* Data export request
* Account deletion request
* Current-plan and entitlement display

## 8.2 Online-first design

Use an online-first model for the initial release.

Do not implement broad offline replication of customer documents in the first release.

Allow only:

* Temporary encrypted upload queues
* Minimal encrypted draft recovery
* Expiring local previews
* Explicitly downloaded exports
* Safe retry metadata

Do not permanently cache private conversations, uploaded files, AI output, or approval records on the device unless the user explicitly requests a download.

Clear temporary material after:

* Successful upload
* Expiration
* Logout
* Account removal
* App privacy reset

## 8.3 Native authentication

Prefer standards-based authorization with:

* Authorization Code flow
* PKCE
* System browser authentication
* Verified deep-link callback
* Short-lived access tokens
* Rotating refresh tokens
* Revocation support

Store mobile credentials only in:

* iOS Keychain
* Android Keystore-backed secure storage

Never store long-lived tokens in:

* Plain preferences
* Local storage
* IndexedDB
* Unencrypted SQLite
* Log output
* Crash-report metadata

## 8.4 Native permissions

Request permissions only at the moment they are needed.

Examples:

* Camera permission only when capturing an image
* Photo-library access only when selecting a photo
* Notification permission only after explaining its purpose
* Biometric permission only when the user enables biometric unlock

Do not request:

* Contacts
* Precise location
* Microphone
* Calendar
* Full storage access
* Advertising identifiers

unless a documented product requirement, privacy review, and user-facing explanation exist.

Prefer modern system pickers that avoid broad storage permissions.

## 8.5 Native security

Implement:

* Certificate and hostname validation
* Secure deep-link validation
* Screenshot-obscuring assessment for sensitive screens
* Clipboard-data minimisation
* Rooted or jailbroken device risk documentation
* Secure logout
* Token revocation
* Local-cache deletion
* App-background privacy protection where practical
* Safe file opening
* No JavaScript bridge exposure beyond required plugins

Do not implement certificate pinning unless the operational rotation and outage risks are fully addressed.

## 8.6 Native feature restraint

Do not add native plugins merely for convenience.

Every plugin must be reviewed for:

* Maintenance status
* Licence
* Permissions
* Data collection
* Network behaviour
* Native dependencies
* Security history
* Store-policy implications
* Compatibility
* Removal strategy

---

# 9. Billing and app-store policy boundary

## 9.1 Web billing

Razorpay is the initial payment provider for the web application.

Razorpay billing may appear only in:

* The authenticated web product
* Approved web checkout routes
* Approved website pricing and purchase flows

Razorpay must be integrated behind:

```text
IPaymentGateway
```

The backend is the source of truth for verified payments and resulting entitlements.

Never grant paid access from browser checkout results alone.

## 9.2 Native mobile billing

The first Android and iOS releases must operate as free companion applications.

They may allow users to:

* Register for the Free plan
* Sign in
* Use Free-plan features
* Use paid entitlements already assigned to their ScopeSeal account
* View their current plan
* View entitlement availability

They must not:

* Open Razorpay Checkout
* Embed Razorpay in a WebView
* Link to a Razorpay checkout page
* Link to a website upgrade page
* Display an external-purchase button
* Display instructions intended to bypass store billing
* Accept card, UPI, wallet, or subscription payments
* Unlock paid features based on client-side input

Use neutral mobile copy such as:

```text
Your account is currently on the Pro plan.
```

For unavailable functionality, use neutral copy such as:

```text
This feature is not available for this account.
```

Do not include a purchase call to action in the native applications.

## 9.3 Future native billing

Prepare, but do not activate, a provider-neutral native billing abstraction:

```text
INativeBillingProvider
```

Possible future adapters:

* Apple StoreKit
* Google Play Billing
* Approved regional alternative billing

Native billing must be a separate reviewed workstream.

Before implementation or activation:

1. Recheck current Apple rules.
2. Recheck current Google Play rules.
3. Determine target storefronts.
4. Review tax handling.
5. Review entitlement reconciliation.
6. Review refund handling.
7. Review subscription restoration.
8. Review account-transfer behaviour.
9. Obtain product-owner approval.
10. Complete store-policy review.

Razorpay remains the web billing provider unless a later architecture decision changes this.

## 9.4 Unified entitlements

All platforms must consume one server-side entitlement model.

Entitlements may originate from:

* Free plan
* Razorpay-verified web subscription
* Future Apple purchase
* Future Google purchase
* Approved administrator grant
* Time-bound trial
* Promotional entitlement

The entitlement system must record:

* Source
* External transaction or subscription identifier
* Plan version
* Effective dates
* Verification status
* Revocation status
* Reconciliation status
* Audit record

Never trust a mobile-device claim that a purchase succeeded.

---

# 10. Plans

Implement centrally managed, configurable, versioned plans.

## Free

The Free plan must remain useful.

Suggested configurable defaults:

* One user
* Three active workspaces
* Five new Agreement Snapshots per month
* Manual snapshot creation
* Limited uploads
* Limited AI extraction
* Basic external review
* Basic approval history
* Basic Change Ledger
* ScopeSeal-branded PDF
* Standard retention
* Privacy centre
* Data export
* Account deletion

## Pro

Suggested capabilities:

* Higher limits
* OCR
* AI-assisted extraction
* Voice-transcription abstraction
* Multiple reviewers
* Full Change Ledger
* Templates
* Advanced exports
* Branding
* Reminders
* Contradiction detection
* Longer configurable retention
* Advanced reports

## Business

Suggested capabilities:

* Multiple members
* Team roles
* Shared templates
* Organisation branding
* Approval policies
* Team dashboards
* Advanced audit reports
* Billing administration
* Configurable retention
* Higher limits
* API foundation
* Webhook foundation
* Usage reporting

Suggested web price defaults:

```text
Free: ₹0
Pro monthly: ₹399
Pro annual: ₹3,999
Business monthly: ₹1,499
Business annual: ₹14,999
```

These are configurable hypotheses, not hard-coded permanent prices.

Do not place privacy rights behind a paid plan.

---

# 11. Dedicated marketing website

Create the marketing website as a separate Angular application.

Prefer:

* Static generation for stable public pages
* SSR only where dynamic server rendering is justified
* Minimal client JavaScript
* Strong Core Web Vitals
* Semantic HTML
* Accessible navigation
* SEO metadata
* Canonical URLs
* Sitemap
* Robots configuration
* Open Graph metadata
* Structured product information
* Secure contact forms
* Consent-aware analytics
* No unnecessary trackers

Required pages:

```text
/
/features
/how-it-works
/use-cases
/use-cases/freelancers
/use-cases/interior-designers
/use-cases/contractors
/use-cases/event-vendors
/pricing
/security
/privacy
/ai-transparency
/subprocessors
/help
/faq
/contact
/blog
/download
/legal/privacy
/legal/terms
/legal/acceptable-use
/legal/refund-and-cancellation
/login
/register
```

Marketing pages may direct browser users to the web product’s checkout.

The marketing website must not contain:

* Razorpay secret keys
* Authenticated API tokens
* Private customer data
* Customer document previews
* Administrative controls

Public legal content must remain marked as requiring qualified review until approved.

---

# 12. Core product invariants

These rules are mandatory.

* AI output always starts as Draft.
* AI never approves a snapshot.
* Users can accept, edit, reject, or mark extracted data uncertain.
* Every extracted fact retains source provenance where possible.
* An approved snapshot is immutable.
* A change never overwrites an approved snapshot.
* An accepted change creates a new draft version.
* A new version requires a new approval cycle.
* Approval references one exact canonical snapshot hash.
* Invitation tokens are non-guessable, expiring, revocable, and single-purpose.
* Deleted or revoked content cannot be accessed using an old link.
* Tenant boundaries are enforced server-side.
* Billing entitlements come from verified server-side state.
* Privacy controls remain available after cancellation or downgrade.

---

# 13. India privacy requirements

Design for the Indian Digital Personal Data Protection framework without claiming final legal compliance.

Maintain:

* Data inventory
* Processing register
* Purpose mapping
* Notice versions
* Consent records
* Consent withdrawal
* Retention schedules
* Deletion workflows
* Subprocessor register
* Data-flow map
* Privacy-risk assessment
* Grievance process
* Incident-response procedure

Apply:

* Purpose limitation
* Data minimisation
* Clear notices
* Separate optional consent
* Secure processing
* User access
* Correction
* Completion
* Updating
* Export
* Erasure
* Grievance tracking
* Processor deletion propagation

Do not collect unnecessary:

* Aadhaar
* PAN
* Passport data
* Biometrics
* Precise location
* Contact lists
* Payment-card details
* UPI PIN
* Bank credentials
* Advertising identifiers

The initial service is restricted to users aged 18 or above.

Mobile privacy disclosures, iOS privacy information, and Google Play Data Safety declarations must match actual application behaviour.

Do not copy generic declarations that do not match the code.

---

# 14. AI rules

The application must work in:

```text
ManualOnly
LocalProcessing
ApprovedExternalProvider
```

modes.

Uploaded material is untrusted input.

AI must never:

* Follow instructions embedded in uploaded files
* Execute commands
* Browse links found in documents
* Reveal secrets
* Determine enforceability
* Declare material genuine
* Invent missing terms
* Fill unknown values silently
* Trigger payments
* Contact counterparties automatically
* Train on customer content by default

Use strict schemas, output validation, provenance, confidence, contradiction detection, quotas, rate limits, retry limits, cost controls, and an administrator kill switch.

---

# 15. GitHub Actions strategy

Create focused workflows rather than one monolithic workflow.

Recommended workflows:

```text
.github/workflows/ci-backend.yml
.github/workflows/ci-clients.yml
.github/workflows/ci-security.yml
.github/workflows/build-web.yml
.github/workflows/build-android.yml
.github/workflows/build-ios.yml
.github/workflows/release-artifacts.yml
.github/workflows/dependency-review.yml
```

## 15.1 Pull-request validation

For every pull request:

* Build backend
* Build product web app
* Build marketing site
* Build admin portal
* Run backend tests
* Run frontend tests
* Run integration tests
* Run architecture tests
* Run Playwright smoke tests
* Run linting
* Validate migrations
* Run secret scanning
* Run dependency review
* Run static analysis
* Build Android debug application
* Run Android unit and lint checks
* Build iOS simulator application
* Run iOS simulator tests where feasible

Pull-request builds must not access release-signing secrets.

## 15.2 Android builds

Run Android builds on an appropriate Linux GitHub-hosted runner.

For normal CI:

* Build web assets
* Run Capacitor sync
* Run Gradle validation
* Build a debug APK
* Build an unsigned or debug app bundle where useful
* Upload test artifacts
* Upload test reports

For protected release builds:

* Use a protected GitHub Environment
* Import the upload keystore from encrypted secrets
* Build a signed AAB
* Verify its signature
* Generate checksums
* Generate an SBOM
* Upload the AAB as a workflow artifact
* Do not upload to Google Play automatically

Expected secret placeholders:

```text
ANDROID_KEYSTORE_BASE64
ANDROID_KEYSTORE_PASSWORD
ANDROID_KEY_ALIAS
ANDROID_KEY_PASSWORD
```

Never print these values.

## 15.3 iOS builds

Run iOS builds on an appropriate macOS GitHub-hosted runner.

For normal CI:

* Build web assets
* Run Capacitor sync
* Install CocoaPods or Swift dependencies as required
* Build for an iOS simulator
* Disable distribution signing
* Run available simulator tests
* Upload the simulator build and test reports

For protected release builds:

* Use a protected GitHub Environment
* Create a temporary keychain
* Import the distribution certificate
* Install the provisioning profile
* Archive the application
* Export an IPA
* Verify the archive
* Generate checksums
* Generate an SBOM where supported
* Upload the IPA as a workflow artifact
* Delete temporary signing material
* Do not upload to App Store Connect automatically

Expected secret placeholders:

```text
APPLE_CERTIFICATE_P12_BASE64
APPLE_CERTIFICATE_PASSWORD
APPLE_PROVISIONING_PROFILE_BASE64
APPLE_TEAM_ID
APPLE_BUNDLE_ID
APPLE_EXPORT_OPTIONS_PLIST_BASE64
APPSTORE_ISSUER_ID
APPSTORE_KEY_ID
APPSTORE_PRIVATE_KEY_BASE64
```

App Store Connect secrets are not required merely to compile an unsigned simulator build.

## 15.4 Workflow security

* Grant minimum `GITHUB_TOKEN` permissions.
* Pin release workflows and third-party actions to reviewed immutable versions or commit SHAs.
* Use Dependabot for action updates.
* Do not expose secrets to fork pull requests.
* Do not use untrusted pull-request code in a privileged signing job.
* Never use `pull_request_target` to build untrusted code with secrets.
* Use separate protected environments for Android and iOS release signing.
* Use concurrency controls.
* Set artifact-retention periods.
* Generate checksums.
* Consider artifact attestations.
* Do not commit generated certificates, profiles, keystores, or private keys.

---

# 16. Versioning

Use one product version across web, Android, and iOS.

Maintain:

* Semantic product version
* Android version code
* Android version name
* iOS bundle version
* iOS marketing version
* API version
* Database migration version

Derive CI build numbers deterministically from a release tag or GitHub run number.

Do not mutate committed version files during a normal pull-request build.

Release tags should follow a documented format such as:

```text
v1.2.3
```

Do not publish a store release merely because a tag exists.

---

# 17. Testing requirements

## Backend

Test:

* Domain rules
* State machines
* Tenant isolation
* Authorization
* Upload safety
* Snapshot immutability
* Hash generation
* Change Ledger
* Razorpay verification
* Webhook idempotency
* Entitlements
* Privacy workflows
* Deletion and retention

## Web

Test:

* Registration
* Sign-in
* Workspace workflow
* Upload
* Snapshot editing
* Review
* Approval
* Change request
* Version comparison
* Razorpay test-mode upgrade
* Privacy centre
* Responsive behaviour
* Accessibility

## Android

Test:

* Debug build
* App startup
* Authentication callback
* Secure storage adapter
* Deep links
* Camera and document-picker adapters
* Upload retry
* Logout cleanup
* No Razorpay checkout
* No external upgrade links
* Permission timing
* Back navigation
* Rotation and lifecycle behaviour

## iOS

Test:

* Simulator build
* App startup
* Authentication callback
* Keychain adapter
* Universal/deep links
* Camera and document-picker adapters
* Upload retry
* Logout cleanup
* No Razorpay checkout
* No external upgrade links
* Permission timing
* Background and foreground transitions
* Safe-area layouts

## Marketing website

Test:

* SSR/SSG build
* Sitemap
* Metadata
* Structured data
* Accessibility
* Broken links
* Forms
* Performance budgets
* Cookie and analytics consent
* No private API exposure

---

# 18. Documentation requirements

Maintain:

```text
/docs/architecture/system-context.md
/docs/architecture/container-design.md
/docs/architecture/mobile-architecture.md
/docs/architecture/client-sharing-strategy.md
/docs/architecture/authentication-flows.md
/docs/mobile/android-build.md
/docs/mobile/ios-build.md
/docs/mobile/store-policy-boundaries.md
/docs/mobile/deep-linking.md
/docs/mobile/native-permissions.md
/docs/mobile/release-signing.md
/docs/billing/razorpay.md
/docs/billing/mobile-billing-strategy.md
/docs/privacy/data-inventory.md
/docs/privacy/processing-register.md
/docs/privacy/retention-policy.md
/docs/privacy/mobile-privacy-disclosures.md
/docs/security/threat-model.md
/docs/security/mobile-threat-model.md
/docs/operations/runbook.md
/docs/deployment/github-actions.md
/docs/backlog/product-backlog.md
/docs/backlog/implementation-ledger.md
```

Every major workstream must produce a completion report.

---

# 19. Autonomous execution rules

The agent may autonomously:

* Create code
* Refactor
* Create migrations
* Add tests
* Add documentation
* Add Docker configuration
* Add GitHub Actions
* Build unsigned mobile artifacts
* Integrate Razorpay test mode on web
* Implement provider abstractions
* Create a feature branch
* Commit coherent checkpoints
* Push to a non-protected feature branch when authenticated
* Open or update a draft pull request
* Inspect and fix GitHub Actions failures

The agent must not autonomously:

* Push directly to a protected main branch
* Merge a pull request
* Deploy production
* Enable Razorpay live mode
* Collect real payments
* Issue real refunds
* Upload an app to Google Play
* Upload an app to App Store Connect
* Submit an app for review
* Publish legal policies as approved
* Claim legal compliance
* Use real customer data
* Purchase services
* Modify live DNS
* Delete production data
* Commit secrets
* Print signing credentials
* Run irreversible production migrations

Missing credentials are not a reason to stop.

Instead:

1. Implement the abstraction.
2. Add safe test or local behaviour.
3. Add configuration placeholders.
4. Add validation.
5. Add automated tests.
6. Add setup documentation.
7. Record the external dependency.
8. Continue with all independent work.

---

# 20. Definition of done

A work item is complete only when:

* Acceptance criteria are satisfied.
* Server-side authorization exists.
* Tenant isolation is tested.
* Inputs are validated.
* Errors are handled safely.
* Privacy effects are documented.
* Security effects are reviewed.
* Tests exist.
* Builds pass.
* Mobile effects are considered.
* Billing effects are considered.
* Store-policy effects are considered.
* Loading, empty, success, and failure states exist.
* Accessibility is checked.
* Logs contain no sensitive material.
* Documentation is updated.
* The implementation ledger is accurate.
* No placeholder is represented as production-complete.
* No unresolved repository-controlled critical or high-severity issue remains.

---

# 21. Completion classification

Use one of:

```text
Not Ready
Development Complete
Staging Ready
Production Preparation Complete
Blocked by External Approvals
```

Do not use `Production Ready` until authorised humans have completed:

* Independent security review
* Indian privacy and legal review
* CA and tax review
* Razorpay live verification
* Apple signing and App Store review preparation
* Google Play signing and policy preparation
* Backup restoration exercise
* Incident-response exercise
* Production configuration review
* Final launch approval
