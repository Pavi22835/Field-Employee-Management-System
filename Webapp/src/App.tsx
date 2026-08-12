import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider } from "@/auth/AuthContext";
import { ProtectedRoute } from "@/routes/ProtectedRoute";
import { AppLayout } from "@/layouts/AppLayout";
import { LoginPage } from "@/pages/LoginPage";
import { DashboardPage } from "@/pages/DashboardPage";
import { EmployeesPage } from "@/pages/EmployeesPage";
import { DevicesPage } from "@/pages/DevicesPage";
import { FieldAreasPage } from "@/pages/FieldAreasPage";
import { AssignmentsPage } from "@/pages/AssignmentsPage";
import { AlertsPage } from "@/pages/AlertsPage";
import { SettingsPage } from "@/pages/SettingsPage";
import { SystemUsersPage } from "@/pages/SystemUsersPage";
import { DynamicFormsPage } from "@/pages/DynamicFormsPage";

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />

          <Route element={<ProtectedRoute />}>
            <Route element={<AppLayout />}>
              <Route path="/" element={<DashboardPage />} />
              <Route path="/employees" element={<ProtectedRoute roles={["SuperAdmin", "Admin", "Supervisor"]} />}>
                <Route index element={<EmployeesPage />} />
              </Route>
              <Route path="/devices" element={<ProtectedRoute roles={["SuperAdmin", "Admin"]} />}>
                <Route index element={<DevicesPage />} />
              </Route>
              <Route path="/field-areas" element={<FieldAreasPage />} />
              <Route path="/dynamic-forms" element={<ProtectedRoute roles={["SuperAdmin", "Admin"]} />}>
                <Route index element={<DynamicFormsPage />} />
              </Route>
              <Route path="/assignments" element={<ProtectedRoute roles={["SuperAdmin", "Admin", "Supervisor"]} />}>
                <Route index element={<AssignmentsPage />} />
              </Route>
              <Route path="/alerts" element={<ProtectedRoute roles={["SuperAdmin", "Admin"]} />}>
                <Route index element={<AlertsPage />} />
              </Route>
              <Route path="/settings" element={<ProtectedRoute roles={["SuperAdmin", "Admin"]} />}>
                <Route index element={<SettingsPage />} />
              </Route>
              <Route path="/system-users" element={<ProtectedRoute roles={["SuperAdmin"]} />}>
                <Route index element={<SystemUsersPage />} />
              </Route>
            </Route>
          </Route>

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
