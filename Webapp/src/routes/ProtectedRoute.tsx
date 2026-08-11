import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "@/auth/AuthContext";

export function ProtectedRoute({ roles }: { roles?: string[] }) {
  const { isAuthenticated, hasAnyRole } = useAuth();

  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (roles && roles.length > 0 && !hasAnyRole(...roles)) return <Navigate to="/" replace />;

  return <Outlet />;
}
