import { useEffect, useState } from "react";
import {
  Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Grid, MenuItem,
  Stack, TextField, Typography, List, ListItemButton, ListItemText, Chip
} from "@mui/material";
import { MapContainer, TileLayer, Marker, Circle, useMapEvents } from "react-leaflet";
import { apiClient } from "@/api/client";
import type { ApiResponse, PagedResult } from "@/types/api";
import type { FieldAreaResponse } from "@/types/domain";

// Section 7: field areas with geofence radius. Leaflet + OpenStreetMap (no licensing cost).
const DEFAULT_CENTER: [number, number] = [11.0168, 76.9558]; // Coimbatore, per the doc's worked example

function LocationPicker({ onPick }: { onPick: (lat: number, lng: number) => void }) {
  useMapEvents({ click: (e) => onPick(e.latlng.lat, e.latlng.lng) });
  return null;
}

export function FieldAreasPage() {
  const [areas, setAreas] = useState<FieldAreaResponse[]>([]);
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({
    name: "", description: "", address: "",
    latitude: DEFAULT_CENTER[0], longitude: DEFAULT_CENTER[1],
    radiusMeters: 2000, enforcementMode: "Mandatory"
  });
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const loadAreas = () => {
    apiClient.get<ApiResponse<PagedResult<FieldAreaResponse>>>("/field-areas", { params: { pageSize: 100 } })
      .then((res) => setAreas(res.data.data?.items ?? []));
  };

  useEffect(loadAreas, []);

  const handleCreate = async () => {
    setSaving(true);
    setError(null);
    try {
      await apiClient.post("/field-areas", form);
      setOpen(false);
      loadAreas();
    } catch (err: any) {
      setError(err?.response?.data?.message ?? "Failed to create field area.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h5">Field Areas</Typography>
        <Button variant="contained" onClick={() => setOpen(true)}>Add Field Area</Button>
      </Stack>

      <Grid container spacing={2}>
        <Grid item xs={12} md={4}>
          <List sx={{ bgcolor: "background.paper" }}>
            {areas.map((a) => (
              <ListItemButton key={a.id} sx={{ alignItems: "flex-start" }}>
                <ListItemText
                  primary={a.name}
                  secondary={`${a.radiusMeters}m radius · ${a.assignedEmployeeCount} employees`}
                />
                <Chip label={a.enforcementMode} size="small" sx={{ mt: 0.5 }} />
              </ListItemButton>
            ))}
          </List>
        </Grid>
        <Grid item xs={12} md={8}>
          <Box sx={{ height: 500 }}>
            <MapContainer center={DEFAULT_CENTER} zoom={11} style={{ height: "100%", width: "100%" }}>
              <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" attribution="&copy; OpenStreetMap contributors" />
              {areas.map((a) => (
                <Circle key={a.id} center={[a.latitude, a.longitude]} radius={a.radiusMeters} />
              ))}
            </MapContainer>
          </Box>
        </Grid>
      </Grid>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Add Field Area</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 0.5 }}>
            <Grid item xs={12} md={6}>
              <Stack spacing={2}>
                {error && <Typography color="error">{error}</Typography>}
                <TextField label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
                <TextField label="Description" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} multiline rows={2} />
                <TextField label="Address" value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
                <Stack direction="row" spacing={2}>
                  <TextField label="Latitude" type="number" value={form.latitude} onChange={(e) => setForm({ ...form, latitude: parseFloat(e.target.value) })} fullWidth />
                  <TextField label="Longitude" type="number" value={form.longitude} onChange={(e) => setForm({ ...form, longitude: parseFloat(e.target.value) })} fullWidth />
                </Stack>
                <TextField label="Radius (meters)" type="number" value={form.radiusMeters} onChange={(e) => setForm({ ...form, radiusMeters: parseInt(e.target.value, 10) })} />
                <TextField select label="Geofence Enforcement" value={form.enforcementMode} onChange={(e) => setForm({ ...form, enforcementMode: e.target.value })}>
                  <MenuItem value="Mandatory">Mandatory</MenuItem>
                  <MenuItem value="WarningOnly">Warning Only</MenuItem>
                  <MenuItem value="Disabled">Disabled</MenuItem>
                </TextField>
              </Stack>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="caption" color="text.secondary">Click the map to set the area's coordinates.</Typography>
              <Box sx={{ height: 350, mt: 1 }}>
                <MapContainer center={[form.latitude, form.longitude]} zoom={12} style={{ height: "100%", width: "100%" }}>
                  <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" attribution="&copy; OpenStreetMap contributors" />
                  <Marker position={[form.latitude, form.longitude]} />
                  <Circle center={[form.latitude, form.longitude]} radius={form.radiusMeters} />
                  <LocationPicker onPick={(lat, lng) => setForm({ ...form, latitude: lat, longitude: lng })} />
                </MapContainer>
              </Box>
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={handleCreate} disabled={saving}>{saving ? "Saving..." : "Create"}</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
