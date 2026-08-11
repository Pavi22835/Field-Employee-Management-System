# Phase 1 — Foundation: Implementation Notes

Scope delivered in this pass (matches roadmap section 24, Phase 1):
Architecture, database schema (all 21 tables from section 17), authentication,
user/role management, device enrollment.

## What's implemented

- **Clean Architecture solution**: `FEMS.Domain`, `FEMS.Application`, `FEMS.Infrastructure`, `FEMS.Api`.
- **Domain**: all 21 tables from section 17 as EF Core entities, with the mandated
  `CreatedAt/UpdatedAt/CreatedBy/UpdatedBy/IsDeleted` audit columns on every table
  (`AuditableEntity` base class), plus soft-delete enforced via a global EF Core query filter.
- **Database**: PostgreSQL via Npgsql EF Core provider, Fluent API configurations for every
  entity (keys, indexes, unique constraints, cascade rules, decimal precision for lat/lng).
- **Auth**: JWT access tokens (short expiry) + rotating, hashed refresh tokens with reuse
  detection (section 19), BCrypt password hashing, account lockout after configurable failed
  attempts, policy-based RBAC for the four roles in section 4.
- **Device enrollment**: `POST /api/devices/enroll`, `GET /api/devices/me`,
  `POST /api/devices/events` implemented per section 6 (GUID + Android ID based identity,
  NOT IMEI). `POST /api/devices/heartbeat` stubbed as 501 — explicitly deferred per section 3.2.
- **API host**: global exception handling middleware with the consistent response envelope
  from section 18, Swagger/OpenAPI, Serilog (console + rolling file sinks), FluentValidation,
  IP rate limiting on `/api/auth/*`, CORS for the admin web origin.
- **Seed data**: the four roles (SuperAdmin/Admin/Supervisor/Employee) and a bootstrap
  Super Admin account (`MustChangePassword = true`) are seeded on first run.
- **Infra**: deployment target is **IIS on Windows Server** (on-prem), not Docker — per
  your direction, this replaces the Docker/Nginx setup originally scaffolded. `FEMS.Api.csproj`
  is configured for in-process IIS hosting (`AspNetCoreHostingModel=InProcess`).
- **Runtime**: all four projects target **.NET 8** (LTS), not the .NET 10 in section
  5.1 — per your direction. EF Core / ASP.NET Core packages are pinned to 8.0.x to match.

## Deliberately NOT built yet (per roadmap phases 2-6 / deferred scope, section 3.2)

- Admin Web Portal (React) — Phase 2.
- Mobile App (React Native) — Phase 3.
- Field area / assignment / visit / dynamic form CRUD endpoints — Phase 2-3. The full
  data model for these already exists in `FEMS.Domain.Entities` and is EF-configured, so
  these are additive (no re-architecture needed).
- Geofencing math, check-in/out flow, photo capture pipeline — Phase 3-4.
- Push notifications (FCM) — Phase 4.
- Heartbeat, MDM/Android Enterprise, SIM monitoring, second-device detection — Phase 6 /
  explicitly deferred. Data model placeholders exist (`Device.LastHeartbeatAt`,
  `Device.DeviceManagementId`, `SecurityAlerts` table).
- Formal reporting/export module — Phase 5.

## Before this can run

The sandbox this was authored in has no internet access and no .NET SDK installed, so the
solution has **not** been compiled or migration-generated in this environment. On your
on-prem/dev machine with the .NET 8 SDK installed, run:

```bash
cd Backend
dotnet restore
dotnet ef migrations add InitialCreate --project src/FEMS.Infrastructure --startup-project src/FEMS.Api
dotnet build
dotnet run --project src/FEMS.Api
```

`dotnet ef` requires the `dotnet-ef` tool (`dotnet tool install --global dotnet-ef`).
Update `appsettings.json` connection string, `Jwt:Secret`, and `Seed:SuperAdminPassword`
before first run (use environment-specific `appsettings.Production.json` or IIS
environment variables — see below — rather than committing real secrets).

## Deploying to IIS on Windows Server

1. Install prerequisites on the server: **.NET 8 Hosting Bundle** (installs the
   ASP.NET Core Module v2 for IIS) and **PostgreSQL** (or point at your existing instance).
2. In IIS Manager, create an Application Pool with **.NET CLR version: No Managed Code**
   (the app runs in-process via ANCM, not the classic CLR pipeline).
3. Publish the API: `dotnet publish src/FEMS.Api -c Release -o C:\inetpub\fems-api`.
   Publishing generates `web.config` automatically (hosting model is already set to
   `InProcess` in `FEMS.Api.csproj`).
4. Create an IIS site pointing at that publish folder, bound to the app pool from step 2.
   Set the site bindings for HTTPS (IIS terminates TLS here — bind your on-prem/internal
   CA certificate, per section 5.4/19: HTTPS only).
5. Configure secrets via `appsettings.Production.json` on the server or IIS
   Configuration Editor → `system.webServer/aspNetCore/environmentVariables`
   (`ConnectionStrings__DefaultConnection`, `Jwt__Secret`, `Seed__SuperAdminPassword`) —
   keep them out of source control (section 19).
6. Grant the app pool identity read/write access to the publish folder's `logs/` directory
   (Serilog file sink) and network access to PostgreSQL.
7. The app applies EF Core migrations and seeds roles/Super Admin automatically on startup
   (see `Program.cs`), so the database schema is created on first launch once the
   connection string is correct.

## Open items from section 25 that still need your decision

- Maps provider: Google Maps vs. Leaflet/OpenStreetMap (affects Admin Web, Phase 2).
- Confirm this Phase 1 scope split (Active vs. Deferred, section 3) before Phase 2 starts.
