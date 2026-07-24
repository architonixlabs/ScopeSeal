# Subprocessor Register

> Status: Loop 0 draft — update before any production processing.

| Subprocessor | Purpose | Data Processed | Location | Contract Status | DPA Status | Kill Switch |
|--------------|---------|----------------|----------|-----------------|------------|-------------|
| Microsoft Azure | Hosting, DB, blob | All service data | India (target) | TBD | TBD | Region config |
| Razorpay | Payments | Billing metadata | India | TBD | TBD | Disable checkout |
| Email provider (TBD) | Transactional email | Email, name | TBD | TBD | TBD | Queue pause |
| Malware scanner (TBD) | File scan | File bytes in quarantine | TBD | TBD | TBD | Reject uploads |
| AI provider (TBD) | Extraction | Document content | TBD — approval required | Not active | Not active | ManualOnly mode |

No customer content sent to external AI until provider row shows approved contract, accurate notice, and security review.

Users must have subprocessor visibility in Privacy centre (Loop 11).
