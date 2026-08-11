import { useEffect, useState } from "react";
import {
  Box, Button, Card, CardContent, FormControlLabel, Grid, MenuItem, Snackbar, Stack,
  Switch, TextField, Typography
} from "@mui/material";
import { apiClient } from "@/api/client";
import type { ApiResponse } from "@/types/api";
import type { SystemSettingsResponse } from "@/types/domain";

// Section 21: admin configuration screens.
export function SettingsPage() {
  const [settings, setSettings] = useState<SystemSettingsResponse | null>(null);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiClient.get<ApiResponse<SystemSettingsResponse>>("/admin/settings")
      .then((res) => setSettings(res.data.data ?? null));
  }, []);

  const update = <K extends keyof SystemSettingsResponse>(key: K, value: SystemSettingsResponse[K]) => {
    if (settings) setSettings({ ...settings, [key]: value });
  };

  const handleSave = async () => {
    if (!settings) return;
    setSaving(true);
    setError(null);
    try {
      await apiClient.put("/admin/settings", settings);
      setSaved(true);
    } catch (err: any) {
      setError(err?.response?.data?.message ?? "Failed to save settings.");
    } finally {
      setSaving(false);
    }
  };

  if (!settings) return null;

  return (
    <Box>
      <Typography variant="h5" gutterBottom>Admin Configuration</Typography>

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Location Tracking (section 15)</Typography>
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <TextField select fullWidth label="Tracking Mode" value={settings.locationTrackingMode}
                onChange={(e) => update("locationTrackingMode", e.target.value)}>
                <MenuItem value="VisitBased">Visit Based</MenuItem>
                <MenuItem value="Periodic">Periodic Tracking</MenuItem>
                <MenuItem value="ContinuousDuty">Continuous Duty Tracking</MenuItem>
              </TextField>
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField fullWidth type="number" label="Periodic Interval (seconds)"
                value={settings.periodicTrackingIntervalSeconds}
                onChange={(e) => update("periodicTrackingIntervalSeconds", parseInt(e.target.value, 10))} />
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Geofence &amp; Devices</Typography>
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <TextField fullWidth type="number" label="Default Geofence Radius (meters)"
                value={settings.defaultGeofenceRadiusMeters}
                onChange={(e) => update("defaultGeofenceRadiusMeters", parseInt(e.target.value, 10))} />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField fullWidth label="Minimum Supported App Version"
                value={settings.minimumSupportedAppVersion}
                onChange={(e) => update("minimumSupportedAppVersion", e.target.value)} />
            </Grid>
            <Grid item xs={12} md={6}>
              <FormControlLabel
                control={<Switch checked={settings.deviceReplacementRequiresApproval}
                  onChange={(e) => update("deviceReplacementRequiresApproval", e.target.checked)} />}
                label="Device replacement requires approval"
              />
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Session &amp; Login (section 19)</Typography>
          <Grid container spacing={2}>
            <Grid item xs={12} md={4}>
              <TextField fullWidth type="number" label="Session Timeout (minutes)"
                value={settings.sessionTimeoutMinutes}
                onChange={(e) => update("sessionTimeoutMinutes", parseInt(e.target.value, 10))} />
            </Grid>
            <Grid item xs={12} md={4}>
              <TextField fullWidth type="number" label="Max Failed Login Attempts"
                value={settings.maxFailedLoginAttempts}
                onChange={(e) => update("maxFailedLoginAttempts", parseInt(e.target.value, 10))} />
            </Grid>
            <Grid item xs={12} md={4}>
              <TextField fullWidth type="number" label="Lockout Duration (minutes)"
                value={settings.lockoutMinutes}
                onChange={(e) => update("lockoutMinutes", parseInt(e.target.value, 10))} />
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Notification Rules (section 20)</Typography>
          <Stack>
            <FormControlLabel
              control={<Switch checked={settings.notifyEmployeeOnNewAssignment}
                onChange={(e) => update("notifyEmployeeOnNewAssignment", e.target.checked)} />}
              label="Notify employee on new field assignment"
            />
            <FormControlLabel
              control={<Switch checked={settings.notifyAdminsOnUnregisteredDeviceAttempt}
                onChange={(e) => update("notifyAdminsOnUnregisteredDeviceAttempt", e.target.checked)} />}
              label="Raise alert on unregistered device login attempt"
            />
            <FormControlLabel
              control={<Switch checked={settings.notifyAdminsOnDeviceNonCompliance}
                onChange={(e) => update("notifyAdminsOnDeviceNonCompliance", e.target.checked)} />}
              label="Raise alert on device non-compliance"
            />
          </Stack>
        </CardContent>
      </Card>

      {error && <Typography color="error" sx={{ mb: 2 }}>{error}</Typography>}
      <Button variant="contained" onClick={handleSave} disabled={saving}>
        {saving ? "Saving..." : "Save Settings"}
      </Button>

      <Snackbar open={saved} autoHideDuration={3000} onClose={() => setSaved(false)} message="Settings saved" />
    </Box>
  );
}
