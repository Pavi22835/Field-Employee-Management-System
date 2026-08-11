# Phase 4 — Location & Alerts: Implementation Notes

Scope delivered in this pass (matches roadmap section 24, Phase 4):
Location tracking, geofencing, device events, basic notifications. Heartbeat and the
full real-time security-alert dashboard remain deferred, per the roadmap and section 3.2.

## A schema note before the rest of this doc

Section 17 enumerates 21 tables and doesn't include a settings/configuration table, but
section 21 ("Admin Configuration") clearly requires persisted, admin-editable settings
(geofence radius, tracking mode, session timeout, etc.). I added one additive table,
**`SystemSettings`** (single-row), rather than inventing per-field config elsewhere or
hardcoding these in `appsettings.json` where only a developer could change them. This is
additive, not a change to any of the original 21 tables — flagging it explicitly per your
"don't change or omit a requirement without discussing it" instruction. Happy to rename,
restructure (e.g. per-organization rows if you introduce multi-tenancy), or remove it if
you'd rather these stay as static config.

## Backend additions (Backend/src)

- **System Settings** (`Application/Settings`, `SystemSettingsService`,
  `SettingsController` at `GET/PUT /api/admin/settings`): location tracking mode +
  interval, default geofence radius, session/lockout policy, minimum app version,
  device-replacement-approval toggle, and three notification-rule toggles — covers every
  item listed in section 21. Seeded with sensible defaults on first run (`DbSeeder`).
- **Location tracking** (`FieldVisitService.RecordLocationAsync`,
  `POST /api/field-visits/{visitId}/locations`): records `FieldVisitLocation` points
  during an active visit. Section 15's "Visit Based" mode is enforced server-side by
  simply not persisting intermediate points (only check-in/check-out coordinates, already
  captured on `FieldVisit`, are kept) — the client never has to know the rule, it just
  gets told "not recorded" back.
- **Security alerts** (`Application/Admin/SecurityAlertDtos.cs`, `SecurityAlertService`,
  `GET/POST /api/admin/alerts` + `/acknowledge`): replaces the Phase 1 `501` stub with a
  real pull-based list + acknowledge workflow. Two generation hooks now populate it
  automatically:
  - `AuthService.LoginAsync` — section 20.2's "Employee John attempted login from an
    unregistered device," raised whenever a device GUID is presented at login that isn't
    an Active device assigned to that employee.
  - `DeviceService.EnrollAsync` → `EvaluateComplianceAsync` — compares the reported app
    version against `SystemSettings.MinimumSupportedAppVersion` on every enrollment,
    writes a `DeviceCompliance` snapshot either way, and raises section 20.2's "Device
    assigned to Employee John has become non-compliant" alert when it fails.

  A full real-time dashboard (live push to the admin screen) is still Phase 6 — this is
  the pull/refresh version, which is what section 3.1's "admin configuration screens" and
  13.3's "Device alerts" count were already implying existed.
- **FCM push notifications** (`IPushNotificationService` / `FcmPushNotificationService`,
  Firebase Admin SDK): dispatches section 20.1's "New field assignment has been created"
  on `FieldAssignmentService.CreateAsync`, and dispatches admin-initiated device
  notifications (`POST /api/admin/devices/{id}/notify`, section 16) for real this time —
  Phase 2 only persisted the `Notification` row. If `Fcm:ServiceAccountKeyPath` isn't
  configured, sends are logged and skipped rather than failing the request that
  triggered them — a missing push credential shouldn't block creating an assignment.
  The "visit starts in 30 minutes" reminder from section 20.1 needs a background
  scheduler (Hangfire/Quartz or a Windows Scheduled Task hitting an endpoint) that isn't
  wired up yet — noted as a gap below.

## Admin Web additions (Webapp/)

- **Alerts page** (`/alerts`): list + acknowledge, unacknowledged-only filter.
- **Settings page** (`/settings`): full form over every `SystemSettings` field, grouped
  to match section 21's list (location tracking, geofence/devices, session/login,
  notification rules).

## Mobile App additions (MobileApp/)

- **Device (re-)enrollment on login** (`src/device/enrollDevice.ts`): Phase 3 generated
  the device identity GUID but never actually called `POST /devices/enroll` — that gap
  is closed now. Called after every successful login so the backend always has current
  model/OS/app-version/push-token data.
- **Push token registration** (`src/device/pushNotifications.ts`): requests notification
  permission and registers a device push token via `expo-notifications`. Production
  caveat: `getDevicePushTokenAsync()` only returns a real FCM token on a custom dev
  client / EAS build with `google-services.json` configured — it won't work in Expo Go.
  Noted in `MobileApp/README.md`.
- **Periodic location reporting** (`src/device/visitLocationTracker.ts`): started after
  a successful check-in, stopped after visit completion. Fetches the org's tracking
  policy from `GET /api/field-visits/tracking-policy` (a minimal, non-admin-only subset
  of `SystemSettings`) and, if the mode isn't Visit Based, posts a location point on the
  configured interval. Lives at module scope so it survives navigation between the
  check-in, form, and completion screens.

## Known gaps in this pass

- **30-minutes-before reminder** (section 20.1): needs a background job scheduler that
  periodically scans upcoming assignments; not built. The infrastructure for sending the
  notification once triggered already exists (`IPushNotificationService`).
- **Heartbeat infrastructure**: still explicitly reserved (section 3.2) — no change.
- **Real-time alert push to the admin dashboard**: still Phase 6; today's `AlertsPage` is
  pull/refresh, not live.
- **SIM/network change alerts**: still reserved, per section 3.2 and the "0" hard-coded
  in `AdminDashboardService`.

## Before this can run

Same caveat as prior phases: no internet/SDKs in this sandbox.

```bash
cd Backend && dotnet restore && dotnet build
cd ../Webapp && npm install
cd ../MobileApp && npm install
```

If you want push notifications to actually deliver, you'll need a Firebase project +
service account JSON at the path configured in `Fcm:ServiceAccountKeyPath`
(`appsettings.json`), plus (for the mobile app) an EAS/dev-client build with
`google-services.json` wired up — Expo Go alone won't produce a usable FCM token.
