import { useState } from "react";
import { View, Text, TextInput, TouchableOpacity, StyleSheet, ActivityIndicator } from "react-native";
import { useAuth } from "@/auth/AuthContext";

export function LoginScreen() {
  const { login } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleLogin = async () => {
    setError(null);
    setLoading(true);
    try {
      await login(username, password);
    } catch (err: any) {
      setError(err?.response?.data?.message ?? "Login failed. Check your credentials.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>FEMS Field App</Text>
      <Text style={styles.subtitle}>Sign in with your company account</Text>

      {error && <Text style={styles.error}>{error}</Text>}

      <TextInput style={styles.input} placeholder="Username" autoCapitalize="none" value={username} onChangeText={setUsername} />
      <TextInput style={styles.input} placeholder="Password" secureTextEntry value={password} onChangeText={setPassword} />

      <TouchableOpacity style={styles.button} onPress={handleLogin} disabled={loading}>
        {loading ? <ActivityIndicator color="#fff" /> : <Text style={styles.buttonText}>Sign In</Text>}
      </TouchableOpacity>

      <Text style={styles.notice}>
        This app must be used only on your company-provided device.
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: "center", padding: 24, backgroundColor: "#F4F6F8" },
  title: { fontSize: 26, fontWeight: "700", textAlign: "center", marginBottom: 4, color: "#1565C0" },
  subtitle: { fontSize: 14, textAlign: "center", marginBottom: 24, color: "#666" },
  input: { backgroundColor: "#fff", borderRadius: 8, padding: 14, marginBottom: 12, borderWidth: 1, borderColor: "#ddd" },
  button: { backgroundColor: "#1565C0", borderRadius: 8, padding: 14, alignItems: "center", marginTop: 8 },
  buttonText: { color: "#fff", fontWeight: "600", fontSize: 16 },
  error: { color: "#C62828", marginBottom: 12, textAlign: "center" },
  notice: { fontSize: 12, color: "#888", textAlign: "center", marginTop: 24 }
});
