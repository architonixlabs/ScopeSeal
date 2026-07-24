# Privacy Risk Assessment

> Status: Loop 0 draft.

| ID | Risk | Likelihood | Impact | Mitigation | Residual |
|----|------|------------|--------|------------|----------|
| P-01 | Incorrect Data Fiduciary classification | Medium | High | Legal review matrix; documented roles | Medium until legal sign-off |
| P-02 | Cross-border AI transfer without valid basis | Medium | High | ManualOnly default; provider approval; notice; kill switch | Low after controls |
| P-03 | Over-collection of personal data | Medium | Medium | Data inventory; minimisation; upload warnings | Low |
| P-04 | Invitation link exposes excessive data | Medium | High | Token scope; expiry; revocation; OTP option | Low after Loop 7 |
| P-05 | Deletion incomplete across systems | Medium | High | Orchestrated DeletionJob; reconciliation | Medium until Loop 11 tested |
| P-06 | Consent bundling | Low | High | Separate notices for AI, marketing, analytics | Low with UI design |
| P-07 | Children's data in uploads | Medium | Medium | 18+ gate; reporting workflow; no AI age inference | Medium |
| P-08 | Sensitive docs uploaded unnecessarily | High | Medium | Pre/post upload warnings; redaction assistance (Pro) | Medium |
| P-09 | Privacy rights behind paywall | Low | Critical | Free plan full rights — policy enforced in code | Low |
| P-10 | Subprocessor change without notice | Medium | Medium | Subprocessor register; versioned notices | Low after Loop 11 |

See `docs/privacy/india-privacy-readiness-checklist.md` for launch checklist.
