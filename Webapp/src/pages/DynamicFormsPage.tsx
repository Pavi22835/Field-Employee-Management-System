import { useEffect, useState } from "react";
import {
  Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, FormControlLabel,
  IconButton, MenuItem, Stack, Switch, TextField, Typography, Divider
} from "@mui/material";
import { DataGrid, GridColDef } from "@mui/x-data-grid";
import { Trash2, ArrowUp, ArrowDown, Plus } from "lucide-react";
import { apiClient } from "@/api/client";
import type { ApiResponse, PagedResult } from "@/types/api";
import type { DynamicFormResponse, DynamicFormFieldDto } from "@/types/domain";

// Section 10: field types supported end to end (Webapp builder + MobileApp renderer).
const FIELD_TYPES = [
  "Text", "Number", "Dropdown", "Checkbox", "RadioButton", "Date",
  "Photo", "Video", "DocumentAttachment", "GpsCoordinates", "Signature"
];

// Dropdown/RadioButton options are stored in the existing FormFieldDto.optionsJson slot
// as a plain JSON string array, e.g. ["Option A","Option B"] — the simplest encoding of
// "a list of choices" that both this builder and the MobileApp renderer can share.
const OPTIONS_FIELD_TYPES = new Set(["Dropdown", "RadioButton"]);

type EditableField = DynamicFormFieldDto & { optionsText?: string };

function toEditableField(f: DynamicFormFieldDto): EditableField {
  let optionsText = "";
  if (f.optionsJson) {
    try {
      const arr = JSON.parse(f.optionsJson);
      if (Array.isArray(arr)) optionsText = arr.join(", ");
    } catch { /* leave blank if unparsable */ }
  }
  return { ...f, optionsText };
}

function newField(displayOrder: number): EditableField {
  return { label: "", fieldType: "Text", isRequired: false, displayOrder, optionsText: "" };
}

const emptyForm = { name: "", description: "", isActive: true, fields: [newField(0)] as EditableField[] };

export function DynamicFormsPage() {
  const [rows, setRows] = useState<DynamicFormResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [open, setOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadForms = () => {
    setLoading(true);
    setLoadError(false);
    apiClient.get<ApiResponse<PagedResult<DynamicFormResponse>>>("/dynamic-forms", { params: { pageSize: 100, activeOnly: false } })
      .then((res) => setRows(res.data.data?.items ?? []))
      .catch(() => setLoadError(true))
      .finally(() => setLoading(false));
  };

  useEffect(loadForms, []);

  const openCreate = () => {
    setEditingId(null);
    setForm(emptyForm);
    setError(null);
    setOpen(true);
  };

  const openEdit = (f: DynamicFormResponse) => {
    setEditingId(f.id);
    setForm({
      name: f.name, description: f.description ?? "", isActive: f.isActive,
      fields: f.fields.length > 0 ? f.fields.map(toEditableField) : [newField(0)]
    });
    setError(null);
    setOpen(true);
  };

  const updateField = (index: number, patch: Partial<EditableField>) => {
    setForm((prev) => ({ ...prev, fields: prev.fields.map((f, i) => (i === index ? { ...f, ...patch } : f)) }));
  };

  const addField = () => {
    setForm((prev) => ({ ...prev, fields: [...prev.fields, newField(prev.fields.length)] }));
  };

  const removeField = (index: number) => {
    setForm((prev) => ({ ...prev, fields: prev.fields.filter((_, i) => i !== index) }));
  };

  const moveField = (index: number, direction: -1 | 1) => {
    setForm((prev) => {
      const target = index + direction;
      if (target < 0 || target >= prev.fields.length) return prev;
      const fields = [...prev.fields];
      [fields[index], fields[target]] = [fields[target], fields[index]];
      return { ...prev, fields };
    });
  };

  const handleSave = async () => {
    setSaving(true);
    setError(null);
    try {
      const payload = {
        name: form.name,
        description: form.description || null,
        isActive: form.isActive,
        fields: form.fields.map((f, i) => ({
          id: f.id,
          label: f.label,
          fieldType: f.fieldType,
          isRequired: f.isRequired,
          displayOrder: i,
          optionsJson: OPTIONS_FIELD_TYPES.has(f.fieldType) && f.optionsText
            ? JSON.stringify(f.optionsText.split(",").map((o) => o.trim()).filter(Boolean))
            : null,
          validationRulesJson: f.validationRulesJson ?? null
        }))
      };

      if (editingId) {
        await apiClient.put(`/dynamic-forms/${editingId}`, payload);
      } else {
        await apiClient.post("/dynamic-forms", payload);
      }
      setOpen(false);
      loadForms();
    } catch (err: any) {
      setError(err?.response?.data?.message ?? "Failed to save form.");
    } finally {
      setSaving(false);
    }
  };

  const columns: GridColDef<DynamicFormResponse>[] = [
    { field: "name", headerName: "Name", flex: 1.2, minWidth: 160 },
    { field: "description", headerName: "Description", flex: 1.5, minWidth: 200, renderCell: (p) => p.value ?? "—" },
    { field: "version", headerName: "Version", flex: 0.5, minWidth: 80 },
    {
      field: "fields", headerName: "Fields", flex: 0.6, minWidth: 80,
      renderCell: (p) => p.row.fields.length
    },
    { field: "isActive", headerName: "Active", flex: 0.5, minWidth: 80, renderCell: (p) => (p.value ? "Yes" : "No") },
    {
      field: "actions", headerName: "Actions", flex: 0.6, minWidth: 90, sortable: false,
      renderCell: (p) => <Button size="small" onClick={() => openEdit(p.row)}>Edit</Button>
    }
  ];

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h5">Dynamic Forms</Typography>
        <Button variant="contained" onClick={openCreate}>Add Form</Button>
      </Stack>

      {loadError ? (
        <Box sx={{ textAlign: "center", mt: 4 }}>
          <Typography color="error" sx={{ mb: 2 }}>Failed to load dynamic forms.</Typography>
          <Button variant="contained" onClick={loadForms}>Retry</Button>
        </Box>
      ) : (
        <Box sx={{ height: 600, bgcolor: "background.paper", maxWidth: "100%" }}>
          <DataGrid rows={rows} columns={columns} loading={loading} getRowId={(r) => r.id} />
        </Box>
      )}

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>{editingId ? "Edit Form" : "Add Form"}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Typography color="error">{error}</Typography>}
            <TextField label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
            <TextField label="Description" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} multiline rows={2} />
            {editingId && (
              <FormControlLabel
                control={<Switch checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />}
                label="Active"
              />
            )}

            <Divider />
            <Typography variant="subtitle1">Fields</Typography>

            {form.fields.map((f, i) => (
              <Stack key={i} direction="row" spacing={1} alignItems="flex-start" sx={{ border: "1px solid", borderColor: "divider", borderRadius: 1, p: 1.5 }}>
                <Stack spacing={1} sx={{ flexGrow: 1 }}>
                  <Stack direction="row" spacing={1}>
                    <TextField
                      label="Label" value={f.label} onChange={(e) => updateField(i, { label: e.target.value })}
                      required fullWidth size="small"
                    />
                    <TextField
                      select label="Type" value={f.fieldType} onChange={(e) => updateField(i, { fieldType: e.target.value })}
                      sx={{ minWidth: 180 }} size="small"
                    >
                      {FIELD_TYPES.map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
                    </TextField>
                  </Stack>
                  {OPTIONS_FIELD_TYPES.has(f.fieldType) && (
                    <TextField
                      label="Options (comma-separated)" value={f.optionsText ?? ""}
                      onChange={(e) => updateField(i, { optionsText: e.target.value })}
                      size="small" fullWidth placeholder="e.g. Good, Fair, Poor"
                    />
                  )}
                  <FormControlLabel
                    control={<Switch size="small" checked={f.isRequired} onChange={(e) => updateField(i, { isRequired: e.target.checked })} />}
                    label="Required"
                  />
                </Stack>
                <Stack>
                  <IconButton size="small" onClick={() => moveField(i, -1)} disabled={i === 0}><ArrowUp size={16} /></IconButton>
                  <IconButton size="small" onClick={() => moveField(i, 1)} disabled={i === form.fields.length - 1}><ArrowDown size={16} /></IconButton>
                  <IconButton size="small" color="error" onClick={() => removeField(i)} disabled={form.fields.length <= 1}>
                    <Trash2 size={16} />
                  </IconButton>
                </Stack>
              </Stack>
            ))}

            <Button startIcon={<Plus size={16} />} onClick={addField} sx={{ alignSelf: "flex-start" }}>
              Add Field
            </Button>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={handleSave} disabled={saving}>{saving ? "Saving..." : "Save"}</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
