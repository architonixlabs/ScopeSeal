# Processing Register

> Status: Loop 0 draft — DPDP readiness placeholder.

| Processing Activity | Data Categories | Purpose | Lawful Basis (review) | Recipients | Cross-Border | Retention | Safeguards |
|---------------------|-----------------|---------|----------------------|------------|--------------|-----------|------------|
| Account management | Identity, credentials | Provide service | Contract | None external | No default | Account life | Auth, hashing |
| Workspace & snapshot | Content, parties | Core product | Contract | Invited reviewers | No default | Policy-based | Tenant isolation |
| Document storage | Files, metadata | Supporting material | Contract | Malware scanner (TBD) | No default | Policy-based | Private blob, scan |
| AI extraction | Document text | Draft suggestions | Consent (separate) | Approved AI provider | Possible | Job + facts | Kill switch, schema validation |
| External review | Snapshot, comments | Counterparty review | Contract | Reviewer via link | No default | Invitation TTL | Token, expiry, OTP optional |
| Approval recording | Hash, audit metadata | Integrity record | Contract | None | No | Long TBD | Immutable append-only |
| Billing | Payment refs, GSTIN | Subscriptions | Contract / legal | Razorpay | India | Statutory TBD | No card storage |
| Notifications | Email, name | Transactional comms | Contract | Email provider | Possible | Delivery logs TBD | No doc content in email |
| Privacy requests | Request details | Rights fulfilment | Legal obligation | Internal only | No | Response TBD | Identity verification |
| Security logging | IP (minimal), correlation ID | Security | Legitimate interest | Internal | No | Short TBD | Minimisation |

Update with final lawful basis and DPDP roles (Data Fiduciary vs Processor matrix) after legal review.
