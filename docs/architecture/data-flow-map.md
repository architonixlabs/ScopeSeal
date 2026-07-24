# Data Flow Map

> Status: Loop 0 draft.

## 1. Registration & Tenancy

```text
User → Register → Email verification → Create Tenant → Default Free entitlements
```

Data: account credentials (hashed), email, age declaration, tenant metadata, consent records.

## 2. Document Upload

```text
Client → Upload session → Quarantine blob → Malware scan → Content validation
→ Permanent private blob + DocumentHash → ProcessingJob
```

Data flows: file bytes to private storage; metadata to PostgreSQL; hash for integrity.

## 3. AI Extraction (when enabled)

```text
ProcessingJob → Entitlement check → Approved provider (or ManualOnly skip)
→ Structured JSON → Schema validation → ExtractedFact (Draft) → User review
```

Untrusted document content never treated as system instructions. Customer content to external AI only after documented approval and accurate notice.

## 4. Snapshot Build & Share

```text
Editor → Draft snapshot sections → Share → ReviewInvitation token
→ External reviewer session → Comments / Approval
```

Minimal data on invitation link; full snapshot only after authorization.

## 5. Approval & Hashing

```text
Approval action → Canonical JSON → SHA-256 (or configured) hash
→ Immutable Approved snapshot + Approval audit record
```

## 6. Change Request

```text
Proposed change → Discussion → Accepted → Clone draft from approved + diff
→ New approval cycle → Supersede prior approved (linked, not overwritten)
```

## 7. Billing

```text
Upgrade → Server creates Razorpay subscription → Checkout
→ Signature verify → Provisional local state → Webhook (authoritative) → Entitlements
```

Never grant paid features from browser callback alone.

## 8. Privacy Deletion

```text
Deletion request → Access revoke → DeletionJob orchestration
→ DB + blob + cache + derived AI + exports → Receipt → Backup expiry documented
```

## 9. Export

```text
Export request → Authorised snapshot versions + change ledger + hashes
→ PDF/ZIP package → Signed download URL (short-lived)
```

## Cross-Border Flows

| Flow | Default | Control |
|------|---------|---------|
| Customer uploads | India region storage preferred | Configurable region |
| AI extraction | Block until provider approved | Kill switch, ManualOnly mode |
| Email notifications | Provider location in subprocessor register | Template without doc content |
| Razorpay | India payment processing | Webhook signature validation |

## Telemetry

Metrics and logs use correlation IDs — **no** raw document content, passwords, OTPs, or full webhook payloads in logs.
