# Phase 2 — Admin Portal: Implementation Notes

Scope delivered in this pass (matches roadmap section 24, Phase 2):
Admin portal shell, employee management, device management, field area management,
field assignments.

## Backend additions (Backend/src)

- **Employees** (`Application/Employees`, `Infrastructure/Services/EmployeeService.cs`,
  `Api/Controllers/EmployeesController.cs`): list/get/create/update. Creating an employee
  provisions the linked `User` account, hashes a temporary password (`MustChangePassword`
  is set), and assigns exactly one of Admin/Supervisor/Employee (SuperAdmin isn't
  assignable through this endpoint, per section 4).
- **Field Areas** (`Application/FieldAreas`, `FieldAreaService`, `FieldAreasController`):
  list/get/create/update, geofence radius + enforcement mode (Mandatory/WarningOnly/
  Disabled) per section 7.
- **Field Assignments** (`Application/FieldAssignments`, `FieldAssignmentService`,
  `FieldAssignmentsController`): admin/supervisor create + list assignments; status
  workflow update endpoint; `GET /api/field-visits/my-visits` for the employee side
  (wired now so the mobile app in Phase 3 has a working endpoint from day one).
- **Admin dashboard** (`Application/Admin`, `AdminDashboardService`): `GET
  /api/admin/dashboard` aggregates the exact stat groups from section 13.1-13.3
  (employees, today's visits, devices). SIM/network alert count is hard-zeroed with a
  comment — reserved per section 3.2.
- **Device admin actions** (`IDeviceAdminService`, `DeviceAdminService`,
  `AdminController`): device list, approve, revoke, assign, unassign, mark lost, send
  notification, lock employee account, force logout — covers every action listed in
  section 16 except "Initiate MDM action," which is explicitly reserved for Phase 6.
- **Roles** (`RolesController`): read-only list, used to populate the "Add Employee" role
  dropdown in the Admin Web.

All new endpoints use the same `ApiResponse<T>` envelope, FluentValidation, and
policy-based RBAC (`AdminOnly` / `ManagementOnly` / `EmployeeOnly`) already established
in Phase 1.

## Admin Web (Webapp/) — new in this phase

React 18 + TypeScript + Vite + Material UI (section 5.2). Structure:

- `src/api/client.ts` — axios instance with a request interceptor for the bearer token
  and a response interceptor that transparently rotates the refresh token on a 401
  (queues concurrent requests during the refresh so only one refresh call fires).
- `src/auth/AuthContext.tsx` + `src/routes/ProtectedRoute.tsx` — login/logout, role-gated
  routes matching the four roles in section 4.
- `src/layouts/AppLayout.tsx` — app shell: top bar, side nav, user menu.
- Pages: `LoginPage`, `DashboardPage` (stat cards per 13.1-13.3), `EmployeesPage` (grid +
  create dialog), `DevicesPage` (grid + approve/revoke/unassign/mark-lost actions),
  `FieldAreasPage` (list + Leaflet/OpenStreetMap picker — click the map to set lat/lng,
  live geofence-radius circle), `AssignmentsPage` (grid + create dialog).

**Maps provider**: Leaflet + OpenStreetMap, per your decision — no API key, no licensing
cost.

## Still open / not built in this pass

- Edit/delete UI for employees and field areas (list + create only; the backend PUT
  endpoints exist and are ready to wire up).
- Toast/snackbar-based error handling (currently inline text under the offending field).
- Map view of live employee positions on the dashboard (section 13.4) — needs Phase 4's
  location-tracking data to be meaningful.
- Dynamic form builder screens (section 10) — planned alongside Phase 3 (mobile app),
  since the form templates only matter once field visits can be executed end-to-end.

## Before this can run

Same caveat as Phase 1: no internet/SDK in this sandbox, so nothing was compiled,
`npm install`-ed, or built here.

```bash
# Backend
cd Backend
dotnet restore && dotnet build

# Admin Web
cd ../Webapp
npm install
cp .env.example .env
npm run dev
```
