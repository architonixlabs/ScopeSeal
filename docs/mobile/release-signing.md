# Mobile release signing

## Android (protected GitHub Environment)

Expected secrets:

```text
ANDROID_KEYSTORE_BASE64
ANDROID_KEYSTORE_PASSWORD
ANDROID_KEY_ALIAS
ANDROID_KEY_PASSWORD
```

Workflow builds signed AAB in protected environment only. CI pull-request builds produce unsigned debug APKs.

## iOS (protected GitHub Environment)

Expected secrets:

```text
APPLE_CERTIFICATE_P12_BASE64
APPLE_CERTIFICATE_PASSWORD
APPLE_PROVISIONING_PROFILE_BASE64
APPLE_TEAM_ID
APPLE_BUNDLE_ID
APPLE_EXPORT_OPTIONS_PLIST_BASE64
```

App Store Connect upload secrets are optional for simulator CI builds.

## Operational rules

- Never print signing credentials in logs
- Delete temporary keychain material after iOS release jobs
- Do not commit keystores, profiles, or certificates

Status: **documented placeholders** — release signing not configured in this repository yet.
