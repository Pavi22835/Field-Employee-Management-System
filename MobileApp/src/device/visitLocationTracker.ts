import * as Location from "expo-location";
import { apiClient } from "@/api/client";
import type { ApiResponse } from "@/types/api";

interface TrackingPolicy {
  locationTrackingMode: "VisitBased" | "Periodic" | "ContinuousDuty";
  periodicTrackingIntervalSeconds: number;
}

// Section 15: reports location points at the org-configured interval while a visit is
// active. Lives at module scope (not tied to a screen's lifecycle) so it keeps running
// as the employee navigates from check-in through the dynamic form to completion. The
// backend independently enforces the tracking mode (VisitBased pings are dropped
// server-side), so this module is UX/battery-conscious plumbing, not the source of truth.
let intervalHandle: ReturnType<typeof setInterval> | null = null;

export async function startVisitLocationTracking(visitId: string) {
  stopVisitLocationTracking();

  const { status } = await Location.getForegroundPermissionsAsync();
  if (status !== "granted") return;

  let policy: TrackingPolicy;
  try {
    const { data } = await apiClient.get<ApiResponse<TrackingPolicy>>("/field-visits/tracking-policy");
    if (!data.data) return;
    policy = data.data;
  } catch {
    return;
  }

  if (policy.locationTrackingMode === "VisitBased") return;

  const intervalMs = Math.max(policy.periodicTrackingIntervalSeconds, 30) * 1000;

  intervalHandle = setInterval(async () => {
    try {
      const pos = await Location.getCurrentPositionAsync({});
      await apiClient.post(`/field-visits/${visitId}/locations`, {
        latitude: pos.coords.latitude,
        longitude: pos.coords.longitude,
        accuracyMeters: pos.coords.accuracy ?? undefined,
        capturedAt: new Date().toISOString()
      });
    } catch {
      // Best-effort; a missed ping isn't worth surfacing to the employee mid-visit.
    }
  }, intervalMs);
}

export function stopVisitLocationTracking() {
  if (intervalHandle) {
    clearInterval(intervalHandle);
    intervalHandle = null;
  }
}
