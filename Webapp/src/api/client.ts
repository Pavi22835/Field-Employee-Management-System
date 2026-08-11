import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";
import type { ApiResponse } from "@/types/api";

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "/api";

const TOKEN_KEY = "fems.accessToken";
const REFRESH_KEY = "fems.refreshToken";

export const tokenStorage = {
  getAccessToken: () => sessionStorage.getItem(TOKEN_KEY),
  getRefreshToken: () => sessionStorage.getItem(REFRESH_KEY),
  setTokens: (accessToken: string, refreshToken: string) => {
    sessionStorage.setItem(TOKEN_KEY, accessToken);
    sessionStorage.setItem(REFRESH_KEY, refreshToken);
  },
  clear: () => {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(REFRESH_KEY);
  }
};

export const apiClient = axios.create({ baseURL: BASE_URL });

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = tokenStorage.getAccessToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

let isRefreshing = false;
let pendingQueue: Array<() => void> = [];

// Section 19: refresh token rotation is transparent to the caller — on a 401 we
// silently exchange the refresh token for a new access token and retry once.
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
        const refreshToken = tokenStorage.getRefreshToken();
        if (!refreshToken) throw error;

        const { data } = await axios.post<ApiResponse<{ accessToken: string; refreshToken: string }>>(
          `${BASE_URL}/auth/refresh`,
          { refreshToken }
        );

        if (data.data) {
          tokenStorage.setTokens(data.data.accessToken, data.data.refreshToken);
          pendingQueue.forEach((resolve) => resolve());
          pendingQueue = [];
          return apiClient(original);
        }
        throw error;
      } catch (refreshError) {
        tokenStorage.clear();
        window.location.href = "/login";
        throw refreshError;
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
