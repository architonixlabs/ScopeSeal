# Native permissions

ScopeSeal requests permissions **only when needed**.

| Permission | When requested | Platforms |
|------------|----------------|-----------|
| Camera | User taps capture photo | Android, iOS |
| Photo library | User selects existing image | Android, iOS |
| Notifications | After in-app explanation | Android, iOS |
| Biometric unlock | User enables setting | Android, iOS |

## Not collected without documented requirement

- Contacts, precise location, microphone, calendar, full storage access, advertising identifiers

## Implementation status

Platform adapters (`CameraCaptureService`, `DocumentPickerService`, `NotificationService`) are defined. Browser implementations use file inputs. Native Capacitor plugin wiring is deferred to post-launch mobile hardening.
