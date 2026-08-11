import { apiClient } from "@/api/client";
import { getDeviceEnrollmentPayload } from "./deviceIdentity";
import { getOrRegisterPushToken } from "./pushNotifications";
import type { ApiResponse } from "@/types/api";
import type { DeviceStatusResponse } from "@/types/domain";

// Section 6: registers (or re-registers) this device against the logged-in employee.
// Safe to call on every login — the backend upserts by AppInstallationId.
export async function enrollDevice(): Promise<DeviceStatusResponse | null> {
  try {
    const payload = await getDeviceEnrollmentPayload();
    const pushToken = await getOrRegisterPushToken();

    const { data } = await apiClient.post<ApiResponse<DeviceStatusResponse>>("/devices/enroll", {
      ...payload,
      pushToken: pushToken ?? undefined
    });

    return data.data ?? null;
  } catch {
    // Enrollment failure shouldn't block login — the employee can still use the app
    // in a degraded state until an admin resolves the device's Pending/Suspended status.
    return null;
  }
}
