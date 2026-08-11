import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";
import { secureTokenStorage } from "./secureTokenStorage";
import type { ApiResponse } from "@/types/api";

const BASE_URL = process.env.EXPO_PUBLIC_API_BASE_URL ?? "https://localhost:5443/api";

export const apiClient = axios.create({ baseURL: BASE_URL });

apiClient.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
  const token = await secureTokenStorage.getAccessToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

let isRefreshing = false;
let pendingQueue: Array<() => void> = [];

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

        const { data } = await axios.post<ApiResponse<{ accessToken: string; refreshToken: string }>>(
          `${BASE_URL}/auth/refresh`,
          { refreshToken }
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
        throw refreshError;
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
