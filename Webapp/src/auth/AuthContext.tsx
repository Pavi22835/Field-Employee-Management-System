import { createContext, useContext, useEffect, useMemo, useState, ReactNode } from "react";
import { apiClient, tokenStorage } from "@/api/client";
import type { ApiResponse } from "@/types/api";

interface LoginResponseDto {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  userId: string;
  username: string;
  roles: string[];
  employeeId?: string;
  deviceVerified: boolean;
}

interface AuthUser {
  userId: string;
  username: string;
  roles: string[];
}

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
  hasAnyRole: (...roles: string[]) => boolean;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);
const USER_KEY = "fems.user";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => {
    const raw = sessionStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as AuthUser) : null;
  });

  useEffect(() => {
    if (user) sessionStorage.setItem(USER_KEY, JSON.stringify(user));
    else sessionStorage.removeItem(USER_KEY);
  }, [user]);

  const login = async (username: string, password: string) => {
    const { data } = await apiClient.post<ApiResponse<LoginResponseDto>>("/auth/login", {
      username,
      password,
      deviceAppInstallationId: null
    });
    if (!data.data) throw new Error(data.message ?? "Login failed.");

    tokenStorage.setTokens(data.data.accessToken, data.data.refreshToken);
    setUser({ userId: data.data.userId, username: data.data.username, roles: data.data.roles });
  };

  const logout = () => {
    const refreshToken = tokenStorage.getRefreshToken();
    if (refreshToken) apiClient.post("/auth/logout", { refreshToken }).catch(() => undefined);
    tokenStorage.clear();
    setUser(null);
  };

  const hasAnyRole = (...roles: string[]) => !!user && roles.some((r) => user.roles.includes(r));

  const value = useMemo(
    () => ({ user, isAuthenticated: !!user, login, logout, hasAnyRole }),
    [user]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
