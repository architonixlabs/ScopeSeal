# Loop Workflow Skill

When the user runs `/loop`:

1. Read `docs/backlog/implementation-ledger.md` for the next unfinished loop
2. Read AGENTS.md, relevant ADRs, risk register, and prior loop report
3. Implement **only** that loop's smallest coherent scope
4. Run builds, tests, lint, security checks — fix failures
5. Update documentation and implementation ledger
6. Produce a Loop Completion Report and **stop** — do not start the next loop

For test repair: fix failures minimally without disabling tests.

For hardening: security/privacy/billing audit with evidence-based readiness report.
