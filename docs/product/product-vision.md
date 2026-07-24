# ScopeSeal Product Vision

> Status: Loop 0 draft — subject to founder and legal review before public launch.

## Problem

Small businesses, freelancers, and service providers in India often agree scope, price, and timelines through fragmented channels: WhatsApp, email, phone calls, and informal quotations. When misunderstandings arise, parties lack a clear, versioned record of what was discussed, what changed, and who approved what.

## Solution

ScopeSeal converts fragmented business conversations and supporting material into a clear, reviewable record containing:

- What the parties discussed
- What each party committed to
- What is included and excluded
- Prices and payment milestones
- Important dates and dependencies
- Open questions and later changes
- Who reviewed or approved each version and when

The system produces an **Agreement Snapshot** and maintains an append-only **Change Ledger**.

## What ScopeSeal Is

- A communication-clarity utility
- A scope and commitment organiser
- A versioned approval-record system
- A change-control utility
- A secure document and conversation workspace
- A neutral record shared between participating parties

## What ScopeSeal Is Not

- A law firm, lawyer, or legal-advice service
- An arbitration service, court, or government system
- A digital-signature certificate authority or e-stamping platform
- A notary service
- A guarantee of legal enforceability or document authenticity
- A guarantee that AI extraction is correct

Use safer language: version history, integrity verification, approval record, recorded timestamp, conversation summary, supporting material. Always advise users to consult qualified legal professionals where necessary.

## Target Market (Initial)

India-first SaaS for:

- Freelancers and independent consultants
- Interior designers and renovation contractors
- Event and service vendors
- Small agencies coordinating client projects

## First Customer Vertical (Recommended)

**Interior design and home renovation projects** — high scope ambiguity, frequent change requests, WhatsApp-heavy communication, milestone-based payments, and strong need for included/excluded scope clarity. See implementation ledger for rationale.

## Success Metrics (Hypotheses)

- Time from workspace creation to first shared snapshot
- External reviewer approval rate without rework
- Change-request resolution time
- Free-to-paid conversion after hitting plan limits
- Data export and deletion completion within SLA
- Support tickets related to scope misunderstandings (qualitative)

## Non-Goals (Initial Release)

See `docs/product/feature-matrix.md` and product backlog for explicit MVP exclusions including: enterprise SSO/SCIM, native WhatsApp integration, live-mode Razorpay, certified legal evidence claims, automatic AI approval, and cross-border data residency commitments beyond documented subprocessors.

## Assumptions Requiring Review

| Assumption | Reviewer |
|------------|----------|
| Data Fiduciary vs Data Processor classification | Qualified Indian legal counsel |
| DPDP Act applicability and notice wording | Legal counsel |
| GST invoicing and tax treatment | Chartered Accountant |
| Marketing claims and disclaimers | Legal counsel |
| External AI provider terms and cross-border transfer | Legal + Security |
| Retention periods per data category | Legal + Privacy |
| Age gate at 18+ | Legal |
| Razorpay subscription and webhook handling | Billing engineer + CA |

## Competitive Differentiation

ScopeSeal focuses on **operational clarity and versioned approval records** for everyday service agreements—not contract lifecycle management for enterprises, not e-signatures with statutory presumptions, and not generic document storage. The Change Ledger and canonical snapshot hashing provide integrity verification without claiming court-proof certification.
