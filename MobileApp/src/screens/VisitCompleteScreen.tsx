import { useState } from "react";
import { View, Text, TextInput, TouchableOpacity, StyleSheet, ActivityIndicator } from "react-native";
import * as Location from "expo-location";
import { useNavigation, useRoute } from "@react-navigation/native";
import { apiClient } from "@/api/client";
import { stopVisitLocationTracking } from "@/device/visitLocationTracker";
import type { ApiResponse } from "@/types/api";
import type { FieldVisitResponse } from "@/types/domain";

// Section 12: "Complete Visit" — captures completion timestamp/location/remarks and
// closes out the visit for admin/supervisor review.
export function VisitCompleteScreen() {
  const navigation = useNavigation<any>();
  const route = useRoute<any>();
  const visit: FieldVisitResponse = route.params.visit;

  const [remarks, setRemarks] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleComplete = async () => {
    setError(null);
    setSubmitting(true);
    try {
      const loc = await Location.getCurrentPositionAsync({}).catch(() => null);

      await apiClient.post<ApiResponse<FieldVisitResponse>>(`/field-visits/${visit.id}/complete`, {
        latitude: loc?.coords.latitude,
        longitude: loc?.coords.longitude,
        remarks: remarks || undefined
      });

      stopVisitLocationTracking();
      navigation.popToTop();
    } catch (err: any) {
      setError(err?.response?.data?.message ?? "Could not complete the visit. Please try again.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Complete Visit</Text>
      <Text style={styles.meta}>Add any final remarks, then confirm completion.</Text>

      <TextInput
        style={styles.input}
        placeholder="Remarks (optional)"
        multiline
        numberOfLines={4}
        value={remarks}
        onChangeText={setRemarks}
      />

      {error && <Text style={styles.error}>{error}</Text>}

      <TouchableOpacity style={styles.button} onPress={handleComplete} disabled={submitting}>
        {submitting ? <ActivityIndicator color="#fff" /> : <Text style={styles.buttonText}>COMPLETE VISIT</Text>}
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 20, backgroundColor: "#F4F6F8" },
  title: { fontSize: 22, fontWeight: "700" },
  meta: { fontSize: 14, color: "#555", marginTop: 4, marginBottom: 16 },
  input: { backgroundColor: "#fff", borderRadius: 8, padding: 12, borderWidth: 1, borderColor: "#ddd", textAlignVertical: "top" },
  button: { backgroundColor: "#2E7D32", borderRadius: 8, padding: 16, alignItems: "center", marginTop: 24 },
  buttonText: { color: "#fff", fontWeight: "700", fontSize: 16 },
  error: { color: "#C62828", marginTop: 16, textAlign: "center" }
});
