import { useEffect, useState } from "react";
import { View, Text, TextInput, TouchableOpacity, StyleSheet, ScrollView, ActivityIndicator, Image, Switch } from "react-native";
import * as ImagePicker from "expo-image-picker";
import * as Location from "expo-location";
import { useNavigation, useRoute } from "@react-navigation/native";
import { apiClient } from "@/api/client";
import type { ApiResponse } from "@/types/api";
import type { DynamicFormResponse, FieldAssignmentResponse, FieldVisitResponse, FormFieldDto } from "@/types/domain";

interface CapturedPhoto {
  formFieldId: string;
  uri: string;
  fileName: string;
  latitude?: number;
  longitude?: number;
}

// Section 10 & 11: renders the assignment's dynamic form and enforces in-app-camera-only
// capture for Photo fields (no gallery picker), then submits data + files together.
export function DynamicFormScreen() {
  const navigation = useNavigation<any>();
  const route = useRoute<any>();
  const assignment: FieldAssignmentResponse = route.params.assignment;
  const visit: FieldVisitResponse = route.params.visit;

  const [form, setForm] = useState<DynamicFormResponse | null>(null);
  const [values, setValues] = useState<Record<string, string>>({});
  const [photos, setPhotos] = useState<CapturedPhoto[]>([]);
  const [loading, setLoading] = useState(!!assignment.dynamicFormId);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!assignment.dynamicFormId) return;
    apiClient.get<ApiResponse<DynamicFormResponse>>(`/dynamic-forms/${assignment.dynamicFormId}`)
      .then((res) => setForm(res.data.data ?? null))
      .finally(() => setLoading(false));
  }, [assignment.dynamicFormId]);

  const capturePhoto = async (fieldId: string) => {
    const { status } = await ImagePicker.requestCameraPermissionsAsync();
    if (status !== "granted") { setError("Camera permission is required to capture photos."); return; }

    // launchCameraAsync opens the device camera directly — gallery selection is not offered,
    // satisfying "captured via the in-app camera rather than selected from the gallery" (section 11).
    const result = await ImagePicker.launchCameraAsync({ quality: 0.6 });
    if (result.canceled || !result.assets?.[0]) return;

    const loc = await Location.getCurrentPositionAsync({}).catch(() => null);
    const asset = result.assets[0];

    setPhotos((prev) => [
      ...prev.filter((p) => p.formFieldId !== fieldId),
      {
        formFieldId: fieldId,
        uri: asset.uri,
        fileName: asset.fileName ?? `photo_${Date.now()}.jpg`,
        latitude: loc?.coords.latitude,
        longitude: loc?.coords.longitude
      }
    ]);
  };

  const renderField = (field: FormFieldDto) => {
    const fieldId = field.id!;
    switch (field.fieldType) {
      case "Photo":
        const photo = photos.find((p) => p.formFieldId === fieldId);
        return (
          <View key={fieldId} style={styles.fieldBlock}>
            <Text style={styles.label}>{field.label}{field.isRequired ? " *" : ""}</Text>
            {photo && <Image source={{ uri: photo.uri }} style={styles.photoPreview} />}
            <TouchableOpacity style={styles.cameraButton} onPress={() => capturePhoto(fieldId)}>
              <Text style={styles.cameraButtonText}>{photo ? "Retake Photo" : "Capture Photo"}</Text>
            </TouchableOpacity>
          </View>
        );
      case "Checkbox":
        return (
          <View key={fieldId} style={[styles.fieldBlock, styles.row]}>
            <Text style={styles.label}>{field.label}{field.isRequired ? " *" : ""}</Text>
            <Switch value={values[fieldId] === "true"} onValueChange={(v) => setValues({ ...values, [fieldId]: String(v) })} />
          </View>
        );
      case "Number":
        return (
          <View key={fieldId} style={styles.fieldBlock}>
            <Text style={styles.label}>{field.label}{field.isRequired ? " *" : ""}</Text>
            <TextInput style={styles.input} keyboardType="numeric" value={values[fieldId] ?? ""} onChangeText={(t) => setValues({ ...values, [fieldId]: t })} />
          </View>
        );
      default:
        // Text, Dropdown, RadioButton, Date, GpsCoordinates, Signature, DocumentAttachment,
        // Video: rendered as free text for this phase; richer pickers are a follow-up.
        return (
          <View key={fieldId} style={styles.fieldBlock}>
            <Text style={styles.label}>{field.label}{field.isRequired ? " *" : ""}</Text>
            <TextInput style={styles.input} value={values[fieldId] ?? ""} onChangeText={(t) => setValues({ ...values, [fieldId]: t })} />
          </View>
        );
    }
  };

  const handleSubmit = async () => {
    setError(null);

    if (form) {
      // Checkbox values are stored as the string "true"/"false", so a plain falsy-string
      // check (`!values[id]`) would treat an unchecked-but-answered box as satisfied.
      const missingRequired = form.fields.filter((f) => {
        if (!f.isRequired || f.fieldType === "Photo") return false;
        if (f.fieldType === "Checkbox") return values[f.id!] !== "true";
        return !values[f.id!];
      });
      const missingPhotos = form.fields.filter((f) => f.isRequired && f.fieldType === "Photo" && !photos.find((p) => p.formFieldId === f.id));
      if (missingRequired.length > 0 || missingPhotos.length > 0) {
        setError("Please complete all required fields before submitting.");
        return;
      }
    }

    setSubmitting(true);
    try {
      if (form) {
        const fd = new FormData();
        fd.append("dynamicFormId", form.id);
        fd.append("valuesJson", JSON.stringify(
          Object.entries(values).map(([formFieldId, value]) => ({ formFieldId, value }))
        ));
        fd.append("fileMetaJson", JSON.stringify(
          photos.map((p) => ({ formFieldId: p.formFieldId, latitude: p.latitude, longitude: p.longitude }))
        ));
        photos.forEach((p) => {
          fd.append("files", { uri: p.uri, name: p.fileName, type: "image/jpeg" } as any);
        });

        await apiClient.post(`/field-visits/${visit.id}/submit`, fd, {
          headers: { "Content-Type": "multipart/form-data" }
        });
      }

      navigation.navigate("VisitComplete", { visit });
    } catch (err: any) {
      setError(err?.response?.data?.message ?? "Submission failed. Please try again.");
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) return <View style={styles.center}><ActivityIndicator size="large" /></View>;

  return (
    <ScrollView style={styles.container}>
      <Text style={styles.title}>{form?.name ?? "Field Information"}</Text>
      {form?.description && <Text style={styles.description}>{form.description}</Text>}

      {!assignment.dynamicFormId && (
        <Text style={styles.description}>No form is required for this visit — you can complete it directly.</Text>
      )}

      {form?.fields.map(renderField)}

      {error && <Text style={styles.error}>{error}</Text>}

      <TouchableOpacity style={styles.button} onPress={handleSubmit} disabled={submitting}>
        {submitting ? <ActivityIndicator color="#fff" /> : <Text style={styles.buttonText}>SUBMIT & CONTINUE</Text>}
      </TouchableOpacity>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#F4F6F8", padding: 16 },
  center: { flex: 1, justifyContent: "center", alignItems: "center" },
  title: { fontSize: 20, fontWeight: "700" },
  description: { fontSize: 14, color: "#666", marginTop: 4, marginBottom: 12 },
  fieldBlock: { marginBottom: 16, backgroundColor: "#fff", borderRadius: 8, padding: 12 },
  row: { flexDirection: "row", justifyContent: "space-between", alignItems: "center" },
  label: { fontSize: 14, fontWeight: "600", marginBottom: 6 },
  input: { borderWidth: 1, borderColor: "#ddd", borderRadius: 6, padding: 10 },
  photoPreview: { width: "100%", height: 180, borderRadius: 8, marginBottom: 8 },
  cameraButton: { backgroundColor: "#1565C0", borderRadius: 6, padding: 10, alignItems: "center" },
  cameraButtonText: { color: "#fff", fontWeight: "600" },
  button: { backgroundColor: "#2E7D32", borderRadius: 8, padding: 16, alignItems: "center", marginVertical: 24 },
  buttonText: { color: "#fff", fontWeight: "700" },
  error: { color: "#C62828", marginTop: 8, textAlign: "center" }
});
