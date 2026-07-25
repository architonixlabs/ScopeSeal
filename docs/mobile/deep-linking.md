# Deep linking

## Product deep links (planned)

| Platform | Scheme / host | Purpose |
|----------|---------------|---------|
| Android | `https://app.scopeseal.in/...` (App Links) | Review invitations, approval callbacks |
| iOS | `https://app.scopeseal.in/...` (Universal Links) | Review invitations, approval callbacks |
| Dev | `scopeseal://` custom scheme | Local OAuth callback testing |

## OAuth mobile flow

1. App opens system browser for Authorization Code + PKCE
2. Provider redirects to verified deep link
3. App exchanges code via backend BFF
4. Short-lived access token + rotating refresh token stored in secure storage only

## Validation rules

- Reject unknown hosts and paths
- Reject expired or revoked invitation tokens
- Single-purpose tokens only

Implementation status: **foundation only** — `DeepLinkService` interface and browser adapter exist; native handlers deferred until auth mobile flow is activated.
