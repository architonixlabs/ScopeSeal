# ADR-0007: Angular 21 Workspace (Loop 1)

## Status

Accepted (Loop 1)

## Context

AGENTS.md specifies Angular 22. At Loop 1 implementation time, the globally available Angular CLI installed Angular 21.2.x. Angular 21 is a current supported release with SSR, standalone components, and the application builder required for ScopeSeal.

## Decision

Use **Angular 21** for the multi-project workspace (product-app, marketing-site SSR, admin-portal, shared libraries). Re-evaluate upgrading to Angular 22 when it is the CLI default and CI-validated.

## Consequences

- Documentation references to Angular 22 should be read as "current supported Angular" until upgraded.
- Capacitor integration deferred to a later loop after product shell stabilises.
