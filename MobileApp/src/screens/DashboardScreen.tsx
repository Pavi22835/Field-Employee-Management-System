import { useCallback, useState } from "react";
import { View, Text, StyleSheet, TouchableOpacity, ScrollView, RefreshControl } from "react-native";
import { useFocusEffect, useNavigation } from "@react-navigation/native";
import { apiClient } from "@/api/client";
import { useAuth } from "@/auth/AuthContext";
import type { ApiResponse } from "@/types/api";
import type { DeviceStatusResponse, FieldAssignmentResponse } from "@/types/domain";

// Section 14: clean single-screen employee dashboard.
export function DashboardScreen() {
  const { user, logout } = useAuth();
  const navigation = useNavigation<any>();

  const [assignments, setAssignments] = useState<FieldAssignmentResponse[]>([]);
  const [deviceStatus, setDeviceStatus] = useState<DeviceStatusResponse | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    // Use the device's local calendar date, not UTC — toISOString() would request the
    // wrong day's assignments near local midnight in timezones ahead of/behind UTC.
    const now = new Date();
    const today = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(now.getDate()).padStart(2, "0")}`;
    const [assignmentsRes, deviceRes] = await Promise.allSettled([
      apiClient.get<ApiResponse<FieldAssignmentResponse[]>>("/field-visits/my-visits", { params: { date: today } }),
      apiClient.get<ApiResponse<DeviceStatusResponse>>("/devices/me")
    ]);

    if (assignmentsRes.status === "fulfilled") setAssignments(assignmentsRes.value.data.data ?? []);
    if (deviceRes.status === "fulfilled") setDeviceStatus(deviceRes.value.data.data ?? null);
  }, []);

  // Re-fetch whenever this screen regains focus (e.g. returning from VisitComplete via
  // popToTop()), not just on first mount — otherwise assignment status/counts go stale.
  useFocusEffect(useCallback(() => { load(); }, [load]));

  const onRefresh = async () => {
    setRefreshing(true);
    await load();
    setRefreshing(false);
  };

  const todaysPrimary = assignments.find((a) => a.status === "Assigned" || a.status === "Accepted" || a.status === "Started");
  const completedCount = assignments.filter((a) => a.status === "Completed").length;
  const pendingCount = assignments.filter((a) => a.status === "Assigned" || a.status === "Accepted").length;

  const greeting = (() => {
    const hour = new Date().getHours();
    if (hour < 12) return "Good Morning";
    if (hour < 17) return "Good Afternoon";
    return "Good Evening";
  })();

  return (
    <ScrollView style={styles.container} refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}>
      <View style={styles.header}>
        <View>
          <Text style={styles.greeting}>{greeting},</Text>
          <Text style={styles.name}>{user?.username}</Text>
        </View>
        <TouchableOpacity onPress={logout}><Text style={styles.logout}>Logout</Text></TouchableOpacity>
      </View>

      {/* Today's Assignment card */}
      {todaysPrimary ? (
        <View style={styles.card}>
          <Text style={styles.cardTitle}>Today's Assignment</Text>
          <Text style={styles.area}>{todaysPrimary.fieldAreaName}</Text>
          <Text style={styles.meta}>{todaysPrimary.startTime} - {todaysPrimary.expectedEndTime}</Text>
          {todaysPrimary.instructions && <Text style={styles.instructions}>{todaysPrimary.instructions}</Text>}
          <TouchableOpacity
            style={styles.startButton}
            onPress={() => navigation.navigate("VisitDetail", { assignment: todaysPrimary })}
          >
            <Text style={styles.startButtonText}>START VISIT</Text>
          </TouchableOpacity>
        </View>
      ) : (
        <View style={styles.card}><Text style={styles.meta}>No active assignment right now.</Text></View>
      )}

      {/* Today's Visits summary */}
      <View style={styles.rowCards}>
        <View style={[styles.smallCard, { backgroundColor: "#E8F5E9" }]}>
          <Text style={styles.smallCardValue}>{completedCount}</Text>
          <Text style={styles.smallCardLabel}>Completed</Text>
        </View>
        <View style={[styles.smallCard, { backgroundColor: "#FFF8E1" }]}>
          <Text style={styles.smallCardValue}>{pendingCount}</Text>
          <Text style={styles.smallCardLabel}>Pending</Text>
        </View>
      </View>

      {/* Device Status panel */}
      <View style={styles.card}>
        <Text style={styles.cardTitle}>Device Status</Text>
        <Text style={styles.meta}>Company Device: {deviceStatus?.status ?? "Not enrolled"}</Text>
        <Text style={styles.meta}>Compliant: {deviceStatus?.isCompliant ? "Yes" : "Pending review"}</Text>
        <Text style={styles.meta}>Last Sync: {new Date().toLocaleTimeString()}</Text>
      </View>

      <View style={styles.trackingNotice}>
        <Text style={styles.trackingNoticeText}>
          Location tracking is active only during check-in/check-out and field activities,
          per company policy. You will always be shown when tracking is active.
        </Text>
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#F4F6F8", padding: 16 },
  header: { flexDirection: "row", justifyContent: "space-between", alignItems: "center", marginBottom: 16 },
  greeting: { fontSize: 14, color: "#666" },
  name: { fontSize: 20, fontWeight: "700" },
  logout: { color: "#C62828", fontWeight: "600" },
  card: { backgroundColor: "#fff", borderRadius: 12, padding: 16, marginBottom: 12, elevation: 1 },
  cardTitle: { fontSize: 13, color: "#888", marginBottom: 6, textTransform: "uppercase" },
  area: { fontSize: 18, fontWeight: "700" },
  meta: { fontSize: 14, color: "#555", marginTop: 2 },
  instructions: { fontSize: 13, color: "#777", marginTop: 8, fontStyle: "italic" },
  startButton: { backgroundColor: "#2E7D32", borderRadius: 8, padding: 12, alignItems: "center", marginTop: 12 },
  startButtonText: { color: "#fff", fontWeight: "700" },
  rowCards: { flexDirection: "row", gap: 12, marginBottom: 12 },
  smallCard: { flex: 1, borderRadius: 12, padding: 16, alignItems: "center" },
  smallCardValue: { fontSize: 24, fontWeight: "700" },
  smallCardLabel: { fontSize: 12, color: "#555" },
  trackingNotice: { padding: 12, marginBottom: 24 },
  trackingNoticeText: { fontSize: 12, color: "#888", textAlign: "center" }
});
