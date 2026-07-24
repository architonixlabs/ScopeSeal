# Contributing to ScopeSeal

Thank you for your interest in ScopeSeal. The project is built through controlled delivery loops documented in [AGENTS.md](AGENTS.md).

## Before You Start

1. Read [AGENTS.md](AGENTS.md) and [docs/backlog/implementation-ledger.md](docs/backlog/implementation-ledger.md)
2. Identify which delivery loop your change belongs to
3. Implement the smallest coherent unit with tests

## Development Principles

- Modular monolith with clear domain boundaries
- Server-side authorization and tenant isolation on every resource
- No secrets in source control
- No hard-coded plan prices, tax rates, or retention periods
- Privacy rights never behind a paywall
- Safe language — no "court-proof" or "legally guaranteed" claims

## Pull Request Expectations

- [ ] Bounded scope matching a backlog item
- [ ] Tests for new behaviour
- [ ] No sensitive data in logs
- [ ] Documentation updated if behaviour or architecture changes
- [ ] Implementation ledger updated for loop progress

## Code Style

- `.editorconfig` for formatting baseline
- Backend: .NET conventions, FluentValidation for inputs
- Frontend: Angular strict mode, standalone components

## Legal Content

Do not merge customer-facing legal text without qualified Indian legal review. Mark drafts appropriately.

## Questions

Open a discussion issue for product or architecture questions. Security issues: see [SECURITY.md](SECURITY.md).
