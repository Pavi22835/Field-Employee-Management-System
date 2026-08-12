import { useCallback, useEffect, useState } from "react";
import {
  View, Text, TextInput, TouchableOpacity, StyleSheet, ScrollView, ActivityIndicator,
  Image, Switch, Modal, PanResponder, GestureResponderEvent
} from "react-native";
import * as ImagePicker from "expo-image-picker";
import * as DocumentPicker from "expo-document-picker";
import * as Location from "expo-location";
import { useNavigation, useRoute } from "@react-navigation/native";
import { generateUuidV4 } from "@/utils/uuid";
import { apiClient } from "@/api/client";
import type { ApiResponse } from "@/types/api";
import type { DynamicFormResponse, FieldAssignmentResponse, FieldVisitResponse, FormFieldDto } from "@/types/domain";

interface CapturedFile {
  formFieldId: string;
  uri: string;
  fileName: string;
  mimeType: string;
  latitude?: number;
  longitude?: number;
}

// Field types whose answer is a captured file (uploaded via multipart, section 10/11),
// as opposed to a plain text value carried in `values`.
const FILE_FIELD_TYPES = new Set(["Photo", "Video", "DocumentAttachment"]);

function parseOptions(optionsJson?: string): string[] {
  if (!optionsJson) return [];
  try {
    const parsed = JSON.parse(optionsJson);
    return Array.isArray(parsed) ? parsed.filter((o) => typeof o === "string") : [];
  } catch {
    return [];
  }
}

// A small number of digits typed as "YYYYMMDD" is auto-formatted to "YYYY-MM-DD" as the
// user types, without pulling in a native date-picker dependency.
function formatDateInput(raw: string): string {
  const digits = raw.replace(/\D/g, "").slice(0, 8);
  const parts = [digits.slice(0, 4), digits.slice(4, 6), digits.slice(6, 8)].filter(Boolean);
  return parts.join("-");
}

type Point = { x: number; y: number };

// Section 11: a dependency-free signature pad — each stroke is captured as a list of
// touch points and rendered as connecting line segments (no react-native-svg/canvas
// available). The stroke data itself (not a rasterized image) is submitted as the
// field's text value, since it's small and the backend already accepts free-form text
// per field via `valuesJson`.
function SignaturePad({ value, onChange }: { value?: string; onChange: (paths: Point[][]) => void }) {
  const [paths, setPaths] = useState<Point[][]>(() => {
    if (!value) return [];
    try {
      const parsed = JSON.parse(value);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  });
  // Deliberately not memoized: PanResponder.create is cheap, and re-creating it every
  // render means each handler closes over that render's own `paths` — so
  // onPanResponderRelease/Terminate always see the latest strokes without needing a ref.
  const panResponder = PanResponder.create({
    onStartShouldSetPanResponder: () => true,
    onMoveShouldSetPanResponder: () => true,
    onPanResponderGrant: (evt: GestureResponderEvent) => {
      const { locationX, locationY } = evt.nativeEvent;
      setPaths((prev) => [...prev, [{ x: locationX, y: locationY }]]);
    },
    onPanResponderMove: (evt: GestureResponderEvent) => {
      const { locationX, locationY } = evt.nativeEvent;
      setPaths((prev) => {
        const next = prev.slice();
        const lastStroke = next[next.length - 1] ?? [];
        next[next.length - 1] = [...lastStroke, { x: locationX, y: locationY }];
        return next;
      });
    },
    onPanResponderRelease: () => onChange(paths),
    onPanResponderTerminate: () => onChange(paths)
  });

  const clear = () => {
    setPaths([]);
    onChange([]);
  };

  return (
    <View>
      <View style={styles.signaturePad} {...panResponder.panHandlers}>
        {paths.length === 0 && <Text style={styles.signatureHint}>Sign here</Text>}
        {paths.map((stroke, strokeIndex) =>
          stroke.slice(1).map((point, i) => {
            const prev = stroke[i];
            const dx = point.x - prev.x;
            const dy = point.y - prev.y;
            const length = Math.sqrt(dx * dx + dy * dy);
            const angle = (Math.atan2(dy, dx) * 180) / Math.PI;
            return (
              <View
                key={`${strokeIndex}-${i}`}
                style={{
                  position: "absolute",
                  left: prev.x,
                  top: prev.y,
                  width: length,
                  height: 2,
                  backgroundColor: "#1A1A1A",
                  transform: [{ rotate: `${angle}deg` }],
                  transformOrigin: "0 0"
                }}
              />
            );
          })
        )}
      </View>
      <TouchableOpacity style={styles.clearButton} onPress={clear}>
        <Text style={styles.clearButtonText}>Clear Signature</Text>
      </TouchableOpacity>
    </View>
  );
}

// Section 10 & 11: renders the assignment's dynamic form and enforces in-app-camera-only
// capture for Photo/Video fields (no gallery picker), then submits data + files together.
export function DynamicFormScreen() {
  const navigation = useNavigation<any>();
  const route = useRoute<any>();
  const assignment: FieldAssignmentResponse = route.params.assignment;
  const visit: FieldVisitResponse = route.params.visit;

  const [form, setForm] = useState<DynamicFormResponse | null>(null);
  const [values, setValues] = useState<Record<string, string>>({});
  const [files, setFiles] = useState<CapturedFile[]>([]);
  // Explicit states so a failed fetch is never confused with "no form required": "none"
  // (assignment.dynamicFormId is unset — nothing to load), "loading", "loaded", "error"
  // (dynamicFormId is set but the GET failed — must not let the user silently bypass it).
  const [formStatus, setFormStatus] = useState<"none" | "loading" | "loaded" | "error">(
    assignment.dynamicFormId ? "loading" : "none"
  );
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [dropdownFieldId, setDropdownFieldId] = useState<string | null>(null);

  // Kicks off the fetch only — no synchronous setState here, so the initial mount can
  // call this from an effect without tripping react-hooks/set-state-in-effect. The
  // "loading"/"none" transition is instead handled by formStatus's initial value below
  // and by retryLoadForm (a plain event handler, not an effect) for the Retry button.
  const fetchForm = useCallback(() => {
    apiClient.get<ApiResponse<DynamicFormResponse>>(`/dynamic-forms/${assignment.dynamicFormId}`)
      .then((res) => {
        if (res.data.data) { setForm(res.data.data); setFormStatus("loaded"); }
        else setFormStatus("error");
      })
      .catch(() => setFormStatus("error"));
  }, [assignment.dynamicFormId]);

  useEffect(() => {
    if (!assignment.dynamicFormId) return;
    fetchForm();
  }, [assignment.dynamicFormId, fetchForm]);

  const retryLoadForm = () => {
    setFormStatus("loading");
    fetchForm();
  };

  const setValue = (fieldId: string, value: string) => setValues((prev) => ({ ...prev, [fieldId]: value }));

  const setSignatureValue = (fieldId: string, paths: Point[][]) => {
    setValues((prev) => {
      if (paths.length === 0) {
        const { [fieldId]: _removed, ...rest } = prev;
        return rest;
      }
      return { ...prev, [fieldId]: JSON.stringify(paths) };
    });
  };

  const addFile = (file: CapturedFile) => {
    setFiles((prev) => [...prev.filter((f) => f.formFieldId !== file.formFieldId), file]);
  };

  const captureLocation = async (fieldId: string) => {
    setError(null);
    const { status } = await Location.requestForegroundPermissionsAsync();
    if (status !== "granted") { setError("Location permission is required for this field."); return; }
    const pos = await Location.getCurrentPositionAsync({});
    setValue(fieldId, `${pos.coords.latitude},${pos.coords.longitude}`);
  };

  const capturePhoto = async (fieldId: string) => {
    const { status } = await ImagePicker.requestCameraPermissionsAsync();
    if (status !== "granted") { setError("Camera permission is required to capture photos."); return; }

    // launchCameraAsync opens the device camera directly — gallery selection is not offered,
    // satisfying "captured via the in-app camera rather than selected from the gallery" (section 11).
    const result = await ImagePicker.launchCameraAsync({ quality: 0.6 });
    if (result.canceled || !result.assets?.[0]) return;

    const loc = await Location.getCurrentPositionAsync({}).catch(() => null);
    const asset = result.assets[0];

    addFile({
      formFieldId: fieldId,
      uri: asset.uri,
      fileName: asset.fileName ?? `photo_${generateUuidV4()}.jpg`,
      mimeType: "image/jpeg",
      latitude: loc?.coords.latitude,
      longitude: loc?.coords.longitude
    });
  };

  const captureVideo = async (fieldId: string) => {
    const { status } = await ImagePicker.requestCameraPermissionsAsync();
    if (status !== "granted") { setError("Camera permission is required to record video."); return; }

    const result = await ImagePicker.launchCameraAsync({ mediaTypes: ["videos"], quality: 0.6 });
    if (result.canceled || !result.assets?.[0]) return;

    const loc = await Location.getCurrentPositionAsync({}).catch(() => null);
    const asset = result.assets[0];

    addFile({
      formFieldId: fieldId,
      uri: asset.uri,
      fileName: asset.fileName ?? `video_${generateUuidV4()}.mp4`,
      mimeType: "video/mp4",
      latitude: loc?.coords.latitude,
      longitude: loc?.coords.longitude
    });
  };

  const pickDocument = async (fieldId: string) => {
    setError(null);
    const result = await DocumentPicker.getDocumentAsync({ type: "*/*", copyToCacheDirectory: true });
    if (result.canceled || !result.assets?.[0]) return;

    const asset = result.assets[0];
    addFile({
      formFieldId: fieldId,
      uri: asset.uri,
      fileName: asset.name,
      mimeType: asset.mimeType ?? "application/octet-stream"
    });
  };

  const renderField = (field: FormFieldDto) => {
    const fieldId = field.id!;
    switch (field.fieldType) {
      case "Photo": {
        const photo = files.find((f) => f.formFieldId === fieldId && f.mimeType.startsWith("image/"));
        return (
          <View key={fieldId} style={styles.fieldBlock}>
            <Text style={styles.label}>{field.label}{field.isRequired ? " *" : ""}</Text>
            {photo && <Image source={{ uri: photo.uri }} style={styles.photoPreview} />}
            <TouchableOpacity style={styles.actionButton} onPress={() => capturePhoto(fieldId)}>
              <Text style={styles.actionButtonText}>{photo ? "Retake Photo" : "Capture Photo"}</Text>
            </TouchableOpacity>
          </View>
        );
      }
      case "Video": {
        const video = files.find((f) => f.formFieldId === fieldId && f.mimeType.startsWith("video/"));
        return (
          <View key={fieldId} style={styles.fieldBlock}>
            <Text style={styles.label}>{field.label}{field.isRequired ? " *" : ""}</Text>
            {video && <Text style={styles.fileCaptured}>Video captured: {video.fileName}</Text>}
            <TouchableOpacity style={styles.actionButton} onPress={() => captureVideo(fieldId)}>
              <Text style={styles.actionButtonText}>{video ? "Re-record Video" : "Record Video"}</Text>
            </TouchableOpacity>
          </View>
        );
      }
      case "DocumentAttachment": {
        const doc = files.find((f) => f.formFieldId === fieldId && !f.mimeType.startsWith("image/") && !f.mimeType.startsWith("video/"));
        return (
          <View key={fieldId} style={styles.fieldBlock}>
            <Text style={styles.label}>{field.label}{field.isRequired ? " *" : ""}</Text>
            {doc && <Text style={styles.fileCaptured}>Attached: {doc.fileName}</Text>}
            <TouchableOpacity style={styles.actionButton} onPress={() => pickDocument(fieldId)}>
              <Text style={styles.actionButtonText}>{doc ? "Replace Document" : "Attach Document"}</Text>
            </TouchableOpacity>
          </View>
        );
      }
      case "GpsCoordinates": {
        const captured = values[fieldId];
        return (
          <View key={fieldId} style={styles.fieldBlock}>
            <Text style={styles.label}>{field.label}{field.isRequired ? " *" : ""}</Text>
            {captured && <Text style={styles.fileCaptured}>Location: {captured}</Text>}
            <TouchableOpacity style={styles.actionButton} onPress={() => captureLocation(fieldId)}>
              <Text style={styles.actionButtonText}>{captured ? "Recapture Location" : "Capture Current Location"}</Text>
            </TouchableOpacity>
          </View>
        );
      }
      case "Signature":
        return (
          <View key={fieldId} style={styles.fieldBlock}>
            <Text style={styles.label}>{field.label}{field.isRequired ? " *" : ""}</Text>
            <SignaturePad value={values[fieldId]} onChange={(paths) => setSignatureValue(fieldId, paths)} />
          </View>
        );
      case "Dropdown": {
        const options = parseOptions(field.optionsJson);
        return (
          <View key={fieldId} style={styles.fieldBlock}>
            <Text style={styles.label}>{field.label}{field.isRequired ? " *" : ""}</Text>
            <TouchableOpacity style={styles.selectInput} onPress={() => setDropdownFieldId(fieldId)}>
              <Text style={values[fieldId] ? styles.selectValue : styles.selectPlaceholder}>
                {values[fieldId] ?? "Select..."}
              </Text>
            </TouchableOpacity>
            <Modal visible={dropdownFieldId === fieldId} transparent animationType="fade" onRequestClose={() => setDropdownFieldId(null)}>
              <TouchableOpacity style={styles.modalBackdrop} activeOpacity={1} onPress={() => setDropdownFieldId(null)}>
                <View style={styles.modalCard}>
                  {options.length === 0 && <Text style={styles.meta}>No options configured for this field.</Text>}
                  {options.map((opt) => (
                    <TouchableOpacity
                      key={opt}
                      style={styles.modalOption}
                      onPress={() => { setValue(fieldId, opt); setDropdownFieldId(null); }}
                    >
                      <Text style={styles.modalOptionText}>{opt}</Text>
                    </TouchableOpacity>
                  ))}
                </View>
              </TouchableOpacity>
            </Modal>
          </View>
        );
      }
      case "RadioButton": {
        const options = parseOptions(field.optionsJson);
        return (
          <View key={fieldId} style={styles.fieldBlock}>
            <Text style={styles.label}>{field.label}{field.isRequired ? " *" : ""}</Text>
            {options.length === 0 && <Text style={styles.meta}>No options configured for this field.</Text>}
            {options.map((opt) => {
              const selected = values[fieldId] === opt;
              return (
                <TouchableOpacity key={opt} style={styles.radioRow} onPress={() => setValue(fieldId, opt)}>
                  <View style={[styles.radioCircle, selected && styles.radioCircleSelected]}>
                    {selected && <View style={styles.radioDot} />}
                  </View>
                  <Text style={styles.radioLabel}>{opt}</Text>
                </TouchableOpacity>
              );
            })}
          </View>
        );
      }
      case "Date":
        return (
          <View key={fieldId} style={styles.fieldBlock}>
            <Text style={styles.label}>{field.label}{field.isRequired ? " *" : ""}</Text>
            <TextInput
              style={styles.input}
              placeholder="YYYY-MM-DD"
              keyboardType="number-pad"
              value={values[fieldId] ?? ""}
              onChangeText={(t) => setValue(fieldId, formatDateInput(t))}
            />
          </View>
        );
      case "Checkbox":
        return (
          <View key={fieldId} style={[styles.fieldBlock, styles.row]}>
            <Text style={styles.label}>{field.label}{field.isRequired ? " *" : ""}</Text>
            <Switch value={values[fieldId] === "true"} onValueChange={(v) => setValue(fieldId, String(v))} />
          </View>
        );
      case "Number":
        return (
          <View key={fieldId} style={styles.fieldBlock}>
            <Text style={styles.label}>{field.label}{field.isRequired ? " *" : ""}</Text>
            <TextInput style={styles.input} keyboardType="numeric" value={values[fieldId] ?? ""} onChangeText={(t) => setValue(fieldId, t)} />
          </View>
        );
      default:
        // Text
        return (
          <View key={fieldId} style={styles.fieldBlock}>
            <Text style={styles.label}>{field.label}{field.isRequired ? " *" : ""}</Text>
            <TextInput style={styles.input} value={values[fieldId] ?? ""} onChangeText={(t) => setValue(fieldId, t)} />
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
        if (!f.isRequired || FILE_FIELD_TYPES.has(f.fieldType)) return false;
        if (f.fieldType === "Checkbox") return values[f.id!] !== "true";
        return !values[f.id!];
      });
      const missingFiles = form.fields.filter((f) => f.isRequired && FILE_FIELD_TYPES.has(f.fieldType) && !files.find((file) => file.formFieldId === f.id));
      if (missingRequired.length > 0 || missingFiles.length > 0) {
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
          files.map((f) => ({ formFieldId: f.formFieldId, latitude: f.latitude, longitude: f.longitude }))
        ));
        files.forEach((f) => {
          fd.append("files", { uri: f.uri, name: f.fileName, type: f.mimeType } as any);
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

  if (formStatus === "loading") return <View style={styles.center}><ActivityIndicator size="large" /></View>;

  if (formStatus === "error") {
    return (
      <View style={styles.center}>
        <Text style={styles.error}>Could not load the field information form. Check your connection and try again.</Text>
        <TouchableOpacity style={styles.button} onPress={retryLoadForm}>
          <Text style={styles.buttonText}>RETRY</Text>
        </TouchableOpacity>
      </View>
    );
  }

  return (
    <ScrollView style={styles.container}>
      <Text style={styles.title}>{form?.name ?? "Field Information"}</Text>
      {form?.description && <Text style={styles.description}>{form.description}</Text>}

      {formStatus === "none" && (
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
  meta: { fontSize: 13, color: "#888" },
  input: { borderWidth: 1, borderColor: "#ddd", borderRadius: 6, padding: 10 },
  photoPreview: { width: "100%", height: 180, borderRadius: 8, marginBottom: 8 },
  fileCaptured: { fontSize: 13, color: "#2E7D32", marginBottom: 8 },
  actionButton: { backgroundColor: "#1565C0", borderRadius: 6, padding: 10, alignItems: "center" },
  actionButtonText: { color: "#fff", fontWeight: "600" },
  selectInput: { borderWidth: 1, borderColor: "#ddd", borderRadius: 6, padding: 10 },
  selectValue: { color: "#1A1A1A" },
  selectPlaceholder: { color: "#999" },
  modalBackdrop: { flex: 1, backgroundColor: "rgba(0,0,0,0.4)", justifyContent: "center", padding: 32 },
  modalCard: { backgroundColor: "#fff", borderRadius: 8, paddingVertical: 8, maxHeight: "60%" },
  modalOption: { paddingVertical: 14, paddingHorizontal: 20 },
  modalOptionText: { fontSize: 15 },
  radioRow: { flexDirection: "row", alignItems: "center", paddingVertical: 8 },
  radioCircle: {
    width: 20, height: 20, borderRadius: 10, borderWidth: 2, borderColor: "#999",
    alignItems: "center", justifyContent: "center", marginRight: 10
  },
  radioCircleSelected: { borderColor: "#1565C0" },
  radioDot: { width: 10, height: 10, borderRadius: 5, backgroundColor: "#1565C0" },
  radioLabel: { fontSize: 15 },
  signaturePad: {
    height: 160, borderWidth: 1, borderColor: "#ddd", borderRadius: 6,
    backgroundColor: "#FAFAFA", overflow: "hidden", alignItems: "center", justifyContent: "center"
  },
  signatureHint: { color: "#bbb", fontSize: 14 },
  clearButton: { alignSelf: "flex-end", marginTop: 6 },
  clearButtonText: { color: "#C62828", fontSize: 13, fontWeight: "600" },
  button: { backgroundColor: "#2E7D32", borderRadius: 8, padding: 16, alignItems: "center", marginVertical: 24 },
  buttonText: { color: "#fff", fontWeight: "700" },
  error: { color: "#C62828", marginTop: 8, textAlign: "center" }
});
