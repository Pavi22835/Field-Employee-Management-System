# Phase 3 — Mobile App: Implementation Notes

Scope delivered in this pass (matches roadmap section 24, Phase 3):
Login, device verification, field visit flow, check-in/check-out, dynamic forms, photo
upload. Offline support is explicitly deferred, per section 3.2 and the roadmap.

## Backend additions (Backend/src)

- **Dynamic Forms** (`Application/DynamicForms`, `DynamicFormService`,
  `DynamicFormsController`): admin CRUD for form templates + fields, readable by any
  authenticated role so the mobile app can fetch the template for an assignment. Field
  types are validated against the full list in section 10.
- **Geofence math** (`Domain/Common/GeofenceCalculator.cs`): haversine distance, used
  server-side to enforce the geofence per `FieldArea.EnforcementMode`
  (Mandatory/WarningOnly/Disabled) — section 9.
- **Field visit flow** (`Application/FieldVisits`, `FieldVisitService`,
  `FieldVisitsController`):
  - `POST /api/field-visits/{assignmentId}/check-in` — validates the assignment belongs
    to the caller, checks the geofence server-side (client-supplied coordinates are
    never trusted alone), creates the `FieldVisit` record, advances the assignment to
    `Started`.
  - `POST /api/field-visits/{visitId}/submit` — multipart/form-data: dynamic form values
    + captured photos. Files are hashed (SHA-256) and stored via `IFileStorageService`
    on local on-prem disk (section 11), with a `DeviceListItemResponse`-style audit trail
    (employee, device, GPS, timestamps) on every `FormSubmissionFile`.
  - `POST /api/field-visits/{visitId}/complete` — requires at least one form submission
    if the assignment specifies a required form, then closes out the visit (section 12).
  - `GET /api/field-visits/my-visits` (added in Phase 2, used here) — the employee's
    assignments for a given date, now includes the field area's lat/lng/radius and the
    assignment's `dynamicFormId` so the mobile app has everything it needs in one call.
- **File storage** (`FileStorageService.cs`): writes to `App:StorageRoot`
  (`appsettings.json`, defaults to `C:\FEMS\uploads` for the IIS deployment) — move this
  outside the IIS site's web root and lock it down via NTFS permissions to the app pool
  identity in production.

## Mobile App (MobileApp/) — new in this phase

Expo-based React Native + TypeScript (see `MobileApp/README.md` for why Expo instead of
a hand-authored bare RN native tree, and how to eject later if you need custom native
modules). Covers: device identity (GUID in Android Keystore via `expo-secure-store`),
login with device binding, the section 14 dashboard concept UI, check-in with a live
distance readout, a dynamic form renderer with camera-only photo capture (no gallery
picker, per section 11), and visit completion.

## Deliberately not built in this pass

- Rich field-type pickers (Dropdown/RadioButton/Date/Signature/GpsCoordinates/Video/
  Document) — render as text inputs for now; Photo is the one field type with full
  intended behavior.
- Push notifications (FCM) — Phase 4.
- Offline mode / local sync queue — explicitly deferred (section 3.2).
- Play Integrity API attestation — needs a bare-workflow eject; placeholder field exists
  on `DeviceEnrollment.AttestationResult`.

## Before this can run

Same caveat as Phases 1-2: no internet/SDKs in this sandbox.

```bash
# Backend
cd Backend && dotnet restore && dotnet build

# Mobile App
cd ../MobileApp && npm install && cp .env.example .env && npx expo start
```
