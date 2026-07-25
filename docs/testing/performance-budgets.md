# Performance budgets

ScopeSeal marketing and product surfaces target strong Core Web Vitals. Budgets below are **development targets** for Loop 14; production CDN tuning may adjust thresholds after real-user monitoring.

## Marketing site (SSR/prerender)

| Metric | Target | Notes |
|--------|--------|-------|
| LCP | ≤ 2.5 s | Hero content on `/` and `/pricing` |
| INP | ≤ 200 ms | Primary navigation |
| CLS | ≤ 0.1 | Stable layout for SSR pages |
| Initial JS (gzip) | ≤ 180 kB | Per route prerender bundle |
| Total page weight | ≤ 500 kB | Excluding optional analytics |

## Product app shell

| Metric | Target | Notes |
|--------|--------|-------|
| Initial JS (gzip) | ≤ 250 kB | Shell only; feature routes lazy-loaded in future |
| Time to interactive | ≤ 3.5 s | Mid-tier mobile, 4G |

## API

| Metric | Target | Notes |
|--------|--------|-------|
| P95 auth endpoints | ≤ 300 ms | Register/login under rate limits |
| P95 workspace list | ≤ 400 ms | Tenant-scoped queries |
| P95 upload session create | ≤ 500 ms | Excludes blob transfer |

## Enforcement

- Angular build budgets configured in `angular.json` for product and marketing apps
- Playwright marketing smoke tests validate page availability
- Lighthouse CI not yet wired — recommended before staging promotion

## Monitoring

Enable OpenTelemetry and real-user monitoring in staging before go/no-go review.
