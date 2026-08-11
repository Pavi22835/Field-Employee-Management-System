# FEMS Mobile App

React Native + TypeScript employee field app (section 5.3). Phases 3-4.

Built with **Expo** (managed workflow) rather than a hand-generated bare RN native
project — this sandbox can't materially author the ~1000+ generated files in
`android/`/`ios/` native project trees by hand, and Expo still satisfies "React Native +
TypeScript, single codebase for Android." If you need custom native modules later (e.g.
Play Integrity API, a production Firebase SDK), run `npx expo prebuild` to eject to a
bare workflow with real native folders — the JS/TS layer here doesn't change.

## Setup

No internet/SDK in this sandbox, so nothing was installed or run here.

```bash
cd MobileApp
npm install
cp .env.example .env   # point EXPO_PUBLIC_API_BASE_URL at your running API
npx expo start
```

**Push notifications in production**: `expo-notifications`' `getDevicePushTokenAsync()`
only returns a real FCM token from a custom dev client / EAS build with
`google-services.json` wired up — it will not work in plain Expo Go. Run `npx expo
prebuild` (or an EAS build) and add your Firebase Android config before relying on push.

## What's implemented

- **Device identity** (`src/device/deviceIdentity.ts`): app-scoped GUID generated on
  first launch, stored via `expo-secure-store` (Android Keystore-backed) — section 6,
  deliberately not IMEI-based.
- **Auth** (`src/auth/AuthContext.tsx`, `src/api/client.ts`): login sends the device
  installation GUID for binding, tokens stored in Keystore-backed secure storage,
  transparent refresh-token rotation on 401 — section 19. On successful login the app
  also (re-)enrolls the device (`src/device/enrollDevice.ts`) so the backend always has
  current model/OS/app-version/push-token info.
- **Dashboard** (`src/screens/DashboardScreen.tsx`): matches the section 14 concept UI —
  greeting, today's assignment card with distance-aware Start Visit button, visit
  summary counts, device status panel, and an explicit "tracking is active" notice
  (section 15: no covert tracking).
- **Check-in** (`src/screens/VisitDetailScreen.tsx`): shows live distance to the area,
  requests location permission, posts to `/field-visits/{assignmentId}/check-in`, then
  starts periodic location reporting for the visit's duration (section 15). The backend
  re-validates the geofence server-side regardless of what the client shows.
- **Dynamic form + photo capture** (`src/screens/DynamicFormScreen.tsx`): renders the
  assignment's form fields, enforces required-field completion, and captures photos via
  `expo-image-picker`'s `launchCameraAsync` — camera only, no gallery picker, per
  section 11. Submits form values + files together to `/field-visits/{visitId}/submit`.
- **Complete visit** (`src/screens/VisitCompleteScreen.tsx`): remarks + completion
  location, posts to `/field-visits/{visitId}/complete`, and stops location reporting.
- **Push token registration** (`src/device/pushNotifications.ts`): requests notification
  permission and includes a device push token in enrollment, for FCM delivery of new
  assignment notifications and admin-sent messages.
- **Periodic location tracking** (`src/device/visitLocationTracker.ts`): reads the org's
  configured tracking mode/interval from `GET /field-visits/tracking-policy` and posts
  location points accordingly while a visit is active; a no-op when the mode is Visit
  Based.

## Not yet built in this pass

- Offline queueing — explicitly deferred per section 3.2.
- Rich pickers for Dropdown/RadioButton/Date/Signature/GpsCoordinates/Video/Document
  field types (currently render as plain text inputs); Photo is the only field type with
  its full intended UX in this pass.
- Play Integrity API attestation — placeholder exists in the backend's `DeviceEnrollment`
  entity (`AttestationResult`); wiring it up needs a bare-workflow eject.
- In-app handling of incoming push notifications (foreground listener / notification
  tap-to-navigate) — tokens are registered and the backend sends, but the client doesn't
  yet do anything special when a push arrives beyond the OS-level notification.
