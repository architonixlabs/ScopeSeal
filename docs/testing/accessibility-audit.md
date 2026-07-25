# Accessibility Audit Foundations

> Status: Loop 13 — automated foundations; full WCAG audit deferred to Loop 14.

## Goals

- Establish repeatable accessibility checks in CI and local development
- Cover critical product, marketing, and admin shell routes
- Align with Angular Material defaults and semantic HTML

## Tooling

- **axe-core** — automated rule checks (foundation script; full axe integration in Loop 14)
- **npm script** — `audit:a11y` runs foundation checks against static HTML fixtures
- **Manual** — keyboard navigation and screen reader spot checks before release

## Initial Scope

| Surface | Priority routes | Loop 13 coverage |
|---------|-----------------|------------------|
| Product app | Login, dashboard, workspace list | Foundation script + component test stub |
| Marketing site | Home, pricing, privacy | Build-time HTML fixture scan |
| Admin portal | Login, tenant search | Foundation script |

## CI Integration

- `ci-clients.yml` runs `npm run audit:a11y` after client builds
- Fail on **critical** axe violations in foundation fixtures
- Full Playwright + axe E2E deferred to Loop 14

## Component Guidelines

- Form inputs require associated labels
- Focus order matches visual order
- Error messages linked via `aria-describedby`
- Loading states use `aria-busy` where applicable
- Colour contrast meets WCAG AA for primary text (verify in Loop 14)

## Definition of Done (Accessibility)

Loop 13: foundation script passes in CI.  
Loop 14: key user journeys pass axe in Playwright smoke tests.
