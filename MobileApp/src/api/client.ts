import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";
import { secureTokenStorage } from "./secureTokenStorage";
import { getOrCreateInstallationId } from "@/device/deviceIdentity";
import type { ApiResponse } from "@/types/api";

const BASE_URL = process.env.EXPO_PUBLIC_API_BASE_URL ?? "https://localhost:5443/api";

// AuthContext registers a callback here so it can clear its `user` state (and let
// RootNavigator fall back to Login) when a refresh definitively fails — e.g. the app was
// closed for longer than the refresh token's lifetime. Without this, a stale cached
// session left the Dashboard rendered (from AsyncStorage) with every API call silently
// failing forever, and no way back to Login short of the user finding "Logout" themselves.
let onAuthExpired: (() => void) | null = null;
export function setOnAuthExpired(handler: () => void) {
  onAuthExpired = handler;
}

// axios's own type declarations only expose `create` via the default export (no matching
// named export to switch to), so this is a false-positive for import/no-named-as-default-member.
// eslint-disable-next-line import/no-named-as-default-member
export const apiClient = axios.create({ baseURL: BASE_URL });

apiClient.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
  const token = await secureTokenStorage.getAccessToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

let isRefreshing = false;
let pendingQueue: (() => void)[] = [];

// Section 19: transparent refresh-token rotation, mirroring the Admin Web client.
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as (InternalAxiosRequestConfig & { _retry?: boolean }) | undefined;

    if (error.response?.status === 401 && original && !original._retry) {
      original._retry = true;

      if (isRefreshing) {
        await new Promise<void>((resolve) => pendingQueue.push(resolve));
        return apiClient(original);
      }

      isRefreshing = true;
      try {
        const refreshToken = await secureTokenStorage.getRefreshToken();
        if (!refreshToken) throw error;

        // Section 19: resend the device installation id so the backend can re-attach the
        // device claim to the rotated access token — otherwise it's dropped after the first
        // refresh and /devices/events plus new visits/files lose their device audit link.
        const deviceAppInstallationId = await getOrCreateInstallationId();

        const { data } = await axios.post<ApiResponse<{ accessToken: string; refreshToken: string }>>(
          `${BASE_URL}/auth/refresh`,
          { refreshToken, deviceAppInstallationId }
        );

        if (data.data) {
          await secureTokenStorage.setTokens(data.data.accessToken, data.data.refreshToken);
          pendingQueue.forEach((resolve) => resolve());
          pendingQueue = [];
          return apiClient(original);
        }
        throw error;
      } catch (refreshError) {
        await secureTokenStorage.clear();
        onAuthExpired?.();
        throw refreshError;
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
