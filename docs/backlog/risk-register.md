# Risk Register

> Status: Loop 0.

| ID | Category | Risk | Likelihood | Impact | Owner | Mitigation | Status |
|----|----------|------|------------|--------|-------|------------|--------|
| R-01 | Legal | Marketing overclaims legal enforceability | Medium | Critical | Founder + Legal | Safe language policy in AGENTS.md | Open |
| R-02 | Legal | Incorrect DPDP role classification | Medium | High | Legal | Fiduciary matrix; checklist | Open |
| R-03 | Privacy | Cross-border AI without valid transfer | Medium | High | Privacy + Legal | ManualOnly default; ADR-005 | Mitigating |
| R-04 | Security | Tenant isolation failure | Low | Critical | Engineering | Auth policies; tests every loop | Open |
| R-05 | Security | Invitation token leakage | Medium | High | Engineering | Expiry, OTP, revocation Loop 7 | Open |
| R-06 | Security | Malicious file upload | Medium | High | Engineering | Allowlist, quarantine Loop 5 | Open |
| R-07 | Security | Prompt injection via uploads | Medium | Medium | Engineering | Schema validation Loop 9 | Open |
| R-08 | Billing | Double entitlement grant | Medium | High | Engineering | Webhook idempotency Loop 10 | Open |
| R-09 | Billing | Client-side payment trust | Medium | Critical | Engineering | Server + webhook authoritative | Mitigating |
| R-10 | Product | MVP scope creep | High | Medium | PM | MVP boundary in feature matrix | Mitigating |
| R-11 | Ops | No verified backup restore | Medium | High | DevOps | Loop 13 DR exercise | Open |
| R-12 | Commercial | Wrong initial vertical | Medium | Medium | Founder | Interior design recommendation | Assumed |
| R-13 | AI | Runaway AI costs | Medium | Medium | Engineering | Quotas, kill switch Loop 9 | Open |
| R-14 | Privacy | Incomplete deletion | Medium | High | Engineering | Deletion orchestration Loop 11 | Open |
| R-15 | Regulatory | GST invoicing errors | Medium | High | CA | Configurable tax; CA review | Open |

Review at end of each delivery loop.
