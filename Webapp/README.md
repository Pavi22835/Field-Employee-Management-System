# FEMS Admin Web

React + TypeScript + Material UI admin portal (section 5.2), Phase 2.

## Setup

This sandbox has no internet access, so dependencies were never installed here — run
locally:

```bash
cd Webapp
npm install
cp .env.example .env   # point VITE_API_BASE_URL at your running API
npm run dev
```

## What's implemented

- JWT auth (login, silent refresh-token rotation via axios interceptor, logout) — section 19
- Role-aware routing/navigation (SuperAdmin/Admin/Supervisor/Employee) — section 4
- Dashboard: employee/visit/device stat cards — section 13.1-13.3
- Employees: list + create (creates the linked user account and assigns a role)
- Devices: list + admin actions (approve/revoke/unassign/mark lost) — section 16
- Field Areas: list + create with a Leaflet/OpenStreetMap picker (click-to-set lat/lng,
  geofence radius circle) — section 7
- Field Assignments: list + create — section 8

## Not yet built (Phase 2 continuation / later phases)

- Edit/delete flows for employees and field areas (create + list are done; PUT endpoints
  already exist on the backend)
- Field area map view showing live employee positions (section 13.4) — depends on
  location-tracking data that lands in Phase 4
- Dynamic form builder UI (section 10) — Phase 3 alongside the mobile app
- Toast/snackbar error handling (currently inline text) — polish pass
