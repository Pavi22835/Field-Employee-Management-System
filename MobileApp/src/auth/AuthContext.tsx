import { createContext, useContext, useEffect, useMemo, useState, ReactNode } from "react";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { apiClient } from "@/api/client";
import { secureTokenStorage } from "@/api/secureTokenStorage";
import { getOrCreateInstallationId } from "@/device/deviceIdentity";
import { enrollDevice } from "@/device/enrollDevice";
import type { ApiResponse } from "@/types/api";
import type { LoginResponseDto } from "@/types/domain";

interface AuthUser {
  userId: string;
  username: string;
  roles: string[];
  employeeId?: string;
  deviceVerified: boolean;
}

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);
const USER_KEY = "fems.user";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    AsyncStorage.getItem(USER_KEY).then((raw) => {
      if (raw) setUser(JSON.parse(raw));
      setIsLoading(false);
    });
  }, []);

  useEffect(() => {
    if (user) AsyncStorage.setItem(USER_KEY, JSON.stringify(user));
    else AsyncStorage.removeItem(USER_KEY);
  }, [user]);

  const login = async (username: string, password: string) => {
    // Section 19: device binding — the app's installation GUID is sent with login so
    // the backend can mark the session as coming from a verified company device.
    const deviceAppInstallationId = await getOrCreateInstallationId();

    const { data } = await apiClient.post<ApiResponse<LoginResponseDto>>("/auth/login", {
      username,
      password,
      deviceAppInstallationId
    });
    if (!data.data) throw new Error(data.message ?? "Login failed.");

    // This app only implements the Employee field-worker experience (section 5.3) — an
    // Admin/Supervisor/SuperAdmin account can authenticate successfully but has no
    // employeeId, so every screen's API calls would silently 403. Reject here instead
    // of leaving the user on a Dashboard that never loads any data.
    if (!data.data.roles.includes("Employee")) {
      throw new Error("This app is for field employees only. Please use the Admin Web portal.");
    }

    await secureTokenStorage.setTokens(data.data.accessToken, data.data.refreshToken);
    setUser({
      userId: data.data.userId,
      username: data.data.username,
      roles: data.data.roles,
      employeeId: data.data.employeeId,
      deviceVerified: data.data.deviceVerified
    });

    // Section 6: (re-)enroll this device now that we're authenticated, so the backend
    // has current model/OS/app-version/push-token info even if nothing changed.
    if (data.data.employeeId) enrollDevice().catch(() => undefined);
  };

  const logout = async () => {
    const refreshToken = await secureTokenStorage.getRefreshToken();
    if (refreshToken) apiClient.post("/auth/logout", { refreshToken }).catch(() => undefined);
    await secureTokenStorage.clear();
    setUser(null);
  };

  const value = useMemo(
    () => ({ user, isAuthenticated: !!user, isLoading, login, logout }),
    [user, isLoading]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
