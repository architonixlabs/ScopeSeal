# ScopeSeal Personas

> Status: Loop 0 draft.

## Primary Personas

### 1. Priya — Freelance Interior Designer

- **Role:** Workspace Owner on Pro plan
- **Context:** Runs a solo practice in Bengaluru; clients communicate via WhatsApp and email
- **Goals:** Capture scope from chat screenshots, share a clear snapshot for client approval, track change requests when clients add items mid-project
- **Pain:** Clients claim "that was never discussed"; no single version of agreed scope
- **Tech comfort:** Moderate; prefers mobile-friendly flows
- **Privacy concern:** Client phone numbers and home addresses in uploads

### 2. Rajesh — Homeowner (External Reviewer)

- **Role:** External participant without full account
- **Context:** Hired Priya for a 2BHK renovation; receives review link on phone
- **Goals:** Understand what is included/excluded, approve or request changes, download approved record
- **Pain:** Long WhatsApp threads; unclear payment milestones
- **Tech comfort:** Basic smartphone user
- **Privacy concern:** Wants to know what data is stored and who can see it

### 3. Ankit — Agency Operations Lead

- **Role:** Organisation Administrator on Business plan
- **Context:** Small design agency with 5 editors; multiple concurrent client projects
- **Goals:** Team workspaces, role-based access, templates, approval policies, usage reporting
- **Pain:** Inconsistent documentation across team members; billing disputes on scope creep
- **Tech comfort:** High
- **Privacy concern:** Employee access to client data; need audit trail

### 4. Meera — Freelance Consultant (Free Plan)

- **Role:** Single user exploring product before upgrade
- **Context:** Occasional small projects; manual entry sufficient
- **Goals:** Try product without payment card; export data if not continuing
- **Pain:** Cannot justify subscription for low volume
- **Tech comfort:** Moderate

## Secondary Personas

### Platform Support Operator

- Needs tenant metadata, billing status, job failures — **not** default access to uploaded content
- Requires case-based elevated access with audit

### Privacy Administrator

- Handles data-principal requests, grievances, retention configuration
- Works through admin portal queues

### Billing Administrator (Tenant)

- Manages subscription, invoices, GSTIN, payment recovery
- Needs clear downgrade impact messaging

## Persona Priorities for MVP

1. Priya (creator/editor)
2. Rajesh (external reviewer)
3. Meera (free tier validation)
4. Ankit (partial — team features deferred post-MVP where noted in backlog)
