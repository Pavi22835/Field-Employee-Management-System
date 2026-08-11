import { useEffect, useState } from "react";
import { View, Text, StyleSheet, TouchableOpacity, ActivityIndicator, Alert } from "react-native";
import * as Location from "expo-location";
import { useNavigation, useRoute } from "@react-navigation/native";
import { apiClient } from "@/api/client";
import { GeofenceCalculator } from "@/device/geofence";
import { startVisitLocationTracking } from "@/device/visitLocationTracker";
import type { ApiResponse } from "@/types/api";
import type { FieldAssignmentResponse, FieldVisitResponse } from "@/types/domain";

// Section 9: Field Check-In. Shows live distance to the assigned area, then validates
// identity/device/assignment/geofence server-side on "Start Visit".
export function VisitDetailScreen() {
  const navigation = useNavigation<any>();
  const route = useRoute<any>();
  const assignment: FieldAssignmentResponse = route.params.assignment;

  const [distance, setDistance] = useState<number | null>(null);
  const [checkingIn, setCheckingIn] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [locationEnabled, setLocationEnabled] = useState(false);

  useEffect(() => {
    (async () => {
      const { status } = await Location.requestForegroundPermissionsAsync();
      setLocationEnabled(status === "granted");
      if (status === "granted") {
        const pos = await Location.getCurrentPositionAsync({});
        setDistance(GeofenceCalculator.distanceMeters(
          pos.coords.latitude, pos.coords.longitude,
          assignment.fieldAreaLatitude, assignment.fieldAreaLongitude
        ));
      }
    })();
  }, [assignment]);

  const handleStartVisit = async () => {
    setError(null);
    setCheckingIn(true);
    try {
      let latitude: number | undefined;
      let longitude: number | undefined;

      if (locationEnabled) {
        const pos = await Location.getCurrentPositionAsync({});
        latitude = pos.coords.latitude;
        longitude = pos.coords.longitude;
      }

      const { data } = await apiClient.post<ApiResponse<FieldVisitResponse>>(
        `/field-visits/${assignment.id}/check-in`,
        { latitude, longitude }
      );

      if (!data.data) throw new Error(data.message ?? "Check-in failed.");

      const visit = data.data;

      // Section 15: begin periodic location reporting for the duration of the visit
      // (a no-op server-side if the org's tracking mode is Visit Based).
      startVisitLocationTracking(visit.id).catch(() => undefined);

      // Mandatory-mode geofence failures are already rejected server-side (422, caught
      // below). A successful response with geofenceSatisfied=false means the area's
      // enforcement mode is Warning Only: the check-in was still recorded, but the
      // employee must be told they're outside the geofence before proceeding.
      if (!visit.geofenceSatisfied) {
        Alert.alert(
          "Outside Assigned Area",
          visit.geofenceMessage || "You are outside the assigned field area.",
          [{ text: "Continue", onPress: () => navigation.navigate("DynamicForm", { assignment, visit }) }]
        );
        return;
      }

      navigation.navigate("DynamicForm", { assignment, visit });
    } catch (err: any) {
      setError(err?.response?.data?.message ?? err.message ?? "Check-in failed. Please try again.");
    } finally {
      setCheckingIn(false);
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>{assignment.fieldAreaName}</Text>
      <Text style={styles.meta}>{assignment.visitDate} · {assignment.startTime} - {assignment.expectedEndTime}</Text>
      {assignment.instructions && <Text style={styles.instructions}>{assignment.instructions}</Text>}

      <View style={styles.distanceCard}>
        {distance === null ? (
          <Text style={styles.meta}>{locationEnabled ? "Getting your location..." : "Location permission required for geofence check-in."}</Text>
        ) : (
          <>
            <Text style={styles.distanceValue}>{Math.round(distance)} m</Text>
            <Text style={styles.meta}>from assigned location (radius: {assignment.fieldAreaRadiusMeters} m)</Text>
          </>
        )}
      </View>

      {error && <Text style={styles.error}>{error}</Text>}

      <TouchableOpacity style={styles.button} onPress={handleStartVisit} disabled={checkingIn}>
        {checkingIn ? <ActivityIndicator color="#fff" /> : <Text style={styles.buttonText}>START VISIT</Text>}
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 20, backgroundColor: "#F4F6F8" },
  title: { fontSize: 22, fontWeight: "700" },
  meta: { fontSize: 14, color: "#555", marginTop: 4 },
  instructions: { fontSize: 14, color: "#777", marginTop: 12, fontStyle: "italic" },
  distanceCard: { backgroundColor: "#fff", borderRadius: 12, padding: 20, marginTop: 24, alignItems: "center" },
  distanceValue: { fontSize: 32, fontWeight: "700", color: "#1565C0" },
  button: { backgroundColor: "#2E7D32", borderRadius: 8, padding: 16, alignItems: "center", marginTop: 24 },
  buttonText: { color: "#fff", fontWeight: "700", fontSize: 16 },
  error: { color: "#C62828", marginTop: 16, textAlign: "center" }
});
