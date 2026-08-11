# Field Employee Management System (FEMS)

Implementation of the Field Employee Management & Device-Controlled Field Visit App,
per `Field_Employee_Management_Requirement_and_Development_Plan.docx`.

## Repository layout

```
Backend/           ASP.NET Core Web API (.NET 8), Clean Architecture, EF Core, PostgreSQL
  src/FEMS.Domain          Entities, enums (no external dependencies)
  src/FEMS.Application     Interfaces, DTOs, application contracts
  src/FEMS.Infrastructure  EF Core DbContext, entity configs, JWT/BCrypt, services
  src/FEMS.Api             Controllers, middleware, Program.cs, appsettings
Webapp/            React + TypeScript + Material UI admin portal
MobileApp/         Expo (React Native) + TypeScript employee field app
Docs/              Phase notes and implementation decisions
```

## Deployment target: IIS on Windows Server (on-prem)

Per your direction, this project deploys to on-prem IIS rather than Docker. The API
project (`FEMS.Api`) is configured for in-process IIS hosting
(`AspNetCoreHostingModel=InProcess`). See `Docs/Phase1-Foundation-Notes.md` for the
step-by-step IIS setup.

## Runtime: .NET 8 (LTS)

Per your direction, the backend targets **.NET 8** rather than the .NET 10 originally
noted in section 5.1 of the requirements doc. All four projects
(`FEMS.Domain/Application/Infrastructure/Api`) target `net8.0`, and the EF Core /
ASP.NET Core package versions were pinned down to their 8.0.x releases to match (mixing
a net8.0 TFM with 9.0.x ASP.NET Core packages doesn't restore cleanly, since those
packages ship framework-specific assets). Install the **.NET 8 SDK** and **.NET 8
Hosting Bundle** (not 10) on your dev machine and the on-prem IIS server.

## Status

- **Phase 1 — Foundation**: done. Solution structure, full database schema (21 tables),
  JWT auth with refresh-token rotation and RBAC, device enrollment. See
  `Docs/Phase1-Foundation-Notes.md`.
- **Phase 2 — Admin Portal**: done. Employee/device/field-area/assignment management on
  the backend, plus the React admin web app (login, dashboard, employees, devices, field
  areas with a Leaflet map, assignments). See `Docs/Phase2-AdminPortal-Notes.md`.
- **Phase 3 — Mobile App**: done. Dynamic forms + geofenced check-in/submit/complete on
  the backend, plus the Expo/React Native employee app (login with device binding,
  dashboard, check-in with live distance, dynamic form + camera-only photo capture,
  visit completion). See `Docs/Phase3-MobileApp-Notes.md`.
- **Phase 4 — Location & Alerts**: done. Admin-configurable location tracking mode
  (section 21, via one additive `SystemSettings` table — flagged in the Phase 4 notes,
  please review), periodic location reporting during visits, security alerts
  (unregistered device login, device non-compliance) with a list/acknowledge admin
  screen, and FCM push notifications for new assignments and admin-sent messages. See
  `Docs/Phase4-LocationAndAlerts-Notes.md`.
- **Phase 5 — Reporting & Ops**: not started.

This environment has no internet access and no .NET/Node SDKs, so nothing has been
compiled, restored, or `npm install`-ed here — please build locally or in CI before
relying on it.

## Next steps (your call before I continue)

1. **Please review the `SystemSettings` table addition** described at the top of
   `Docs/Phase4-LocationAndAlerts-Notes.md` — it's the one schema change not explicitly
   listed in section 17, added to give section 21's admin configuration screens
   somewhere to persist values. Let me know if you'd rather structure this differently.
2. Run `dotnet restore && dotnet build` (Backend), `npm install && npm run build`
   (Webapp), and `npm install` (MobileApp) locally to confirm everything compiles clean.
3. Generate the initial EF Core migration and confirm the schema against section 17 +
   the `SystemSettings` addition.
4. I'll continue with Phase 5 (Reporting & Ops: dashboards, audit logs, monitoring,
   production deployment) next, unless you want to reprioritize.
