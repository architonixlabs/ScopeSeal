# Data Inventory

> Status: Loop 0 draft — machine-readable summary; expand in Loop 11.

| Category | Example Fields | Purpose | Source | Legal Basis (TBD) | Required | Storage | Encryption | Retention | Deletion |
|----------|----------------|---------|--------|-------------------|----------|---------|------------|-----------|----------|
| Account identity | Email, password hash, name | Authentication | User registration | Contract / consent TBD | Required | PostgreSQL | At rest + TLS | Account lifetime | Deletion workflow |
| Age declaration | Boolean 18+ | Eligibility | Registration | Legal obligation TBD | Required | PostgreSQL | At rest | Account lifetime | With account |
| Tenant metadata | Org name, plan | Tenancy | User input | Contract | Required | PostgreSQL | At rest | Tenant lifetime | Orchestrated |
| Workspace content | Snapshot sections, parties | Core product | User input | Contract | Required | PostgreSQL | At rest | Per retention policy | Orchestrated |
| Uploaded documents | Files, hashes | Supporting material | User upload | Contract | Optional | Blob + PostgreSQL metadata | At rest, private | Per retention policy | Blob + metadata |
| AI extraction output | Extracted facts, confidence | Draft suggestions | AI pipeline | Separate notice TBD | Optional | PostgreSQL | At rest | Until deleted | With source docs |
| Review invitations | Token hash, expiry, email | External review | System | Contract | Required for share | PostgreSQL | At rest | Invitation TTL + audit | Revoke + expire |
| Approval records | Hash, timestamp, role | Integrity record | User action | Contract | Required for approval | PostgreSQL append-only | At rest | Long retention TBD | Policy TBD legal review |
| Billing | Razorpay IDs, GSTIN | Subscriptions | User + Razorpay | Contract / legal | Paid plans | PostgreSQL | At rest | Statutory TBD | Anonymise TBD |
| Audit events | Actor, action, resource | Security | System | Legitimate interest TBD | Required | PostgreSQL append-only | At rest | Configured | No content preservation |
| Consent records | Notice version, purpose | Compliance | User action | Consent | Per purpose | PostgreSQL | At rest | Legal requirement TBD | Record retention TBD |
| Support tickets | Subject, metadata | Support | User | Contract | Optional | TBD Loop 12 | At rest | TBD | TBD |

**Not collected by default:** Aadhaar, PAN, passport, bank accounts, full card data, UPI PIN, biometrics, exact location, contact lists.

All legal basis columns require qualified Indian legal review before launch.
