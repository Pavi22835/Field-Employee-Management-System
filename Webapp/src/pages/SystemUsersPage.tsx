import { useEffect, useState } from "react";
import {
  Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem,
  Stack, TextField, Typography, Chip
} from "@mui/material";
import { DataGrid, GridColDef } from "@mui/x-data-grid";
import { apiClient } from "@/api/client";
import type { ApiResponse } from "@/types/api";
import type { SystemUserResponse } from "@/types/domain";

const emptyForm = { username: "", email: "", temporaryPassword: "", role: "Admin" };

// Section 4: SuperAdmin-only management of Admin/SuperAdmin system accounts — distinct
// from EmployeesPage, which provisions Employee/Supervisor accounts (see UsersController,
// gated end to end by PolicyNames.SuperAdminOnly so an Admin can never mint another Admin).
export function SystemUsersPage() {
  const [rows, setRows] = useState<SystemUserResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadUsers = () => {
    setLoading(true);
    setLoadError(false);
    apiClient.get<ApiResponse<SystemUserResponse[]>>("/admin/users")
      .then((res) => setRows(res.data.data ?? []))
      .catch(() => setLoadError(true))
      .finally(() => setLoading(false));
  };

  useEffect(loadUsers, []);

  const handleCreate = async () => {
    setSaving(true);
    setError(null);
    try {
      await apiClient.post("/admin/users", form);
      setOpen(false);
      setForm(emptyForm);
      loadUsers();
    } catch (err: any) {
      setError(err?.response?.data?.message ?? "Failed to create system user.");
    } finally {
      setSaving(false);
    }
  };

  const handleDeactivate = async (user: SystemUserResponse) => {
    if (!window.confirm(`Deactivate ${user.username}? They will no longer be able to log in.`)) return;
    try {
      await apiClient.delete(`/admin/users/${user.id}`);
      loadUsers();
    } catch (err: any) {
      window.alert(err?.response?.data?.message ?? "Failed to deactivate user.");
    }
  };

  const columns: GridColDef<SystemUserResponse>[] = [
    { field: "username", headerName: "Username", flex: 1, minWidth: 140 },
    { field: "email", headerName: "Email", flex: 1.3, minWidth: 170 },
    {
      field: "roles", headerName: "Roles", flex: 1, minWidth: 140,
      renderCell: (params) => params.row.roles.map((r) => (
        <Chip key={r} label={r} size="small" color={r === "SuperAdmin" ? "error" : "primary"} sx={{ mr: 0.5 }} />
      ))
    },
    { field: "isActive", headerName: "Active", flex: 0.6, minWidth: 80, renderCell: (p) => (p.value ? "Yes" : "No") },
    {
      field: "lastLoginAt", headerName: "Last Login", flex: 1, minWidth: 150,
      valueFormatter: (v) => v ? new Date(v as string).toLocaleString() : "—"
    },
    {
      field: "actions", headerName: "Actions", flex: 0.7, minWidth: 110, sortable: false,
      renderCell: (params) => (
        <Button size="small" color="error" disabled={!params.row.isActive} onClick={() => handleDeactivate(params.row)}>
          Deactivate
        </Button>
      )
    }
  ];

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h5">System Users</Typography>
        <Button variant="contained" onClick={() => setOpen(true)}>Add System User</Button>
      </Stack>

      {loadError ? (
        <Box sx={{ textAlign: "center", mt: 4 }}>
          <Typography color="error" sx={{ mb: 2 }}>Failed to load system users.</Typography>
          <Button variant="contained" onClick={loadUsers}>Retry</Button>
        </Box>
      ) : (
        <Box sx={{ height: 600, bgcolor: "background.paper", maxWidth: "100%" }}>
          <DataGrid rows={rows} columns={columns} loading={loading} getRowId={(r) => r.id} />
        </Box>
      )}

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add System User</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Typography color="error">{error}</Typography>}
            <TextField label="Username" value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} required />
            <TextField label="Email" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} required />
            <TextField
              label="Temporary Password" type="password" value={form.temporaryPassword}
              onChange={(e) => setForm({ ...form, temporaryPassword: e.target.value })}
              required helperText="User will be required to change this on first login."
            />
            <TextField select label="Role" value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })}>
              <MenuItem value="Admin">Admin</MenuItem>
              <MenuItem value="SuperAdmin">SuperAdmin</MenuItem>
            </TextField>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={handleCreate} disabled={saving}>{saving ? "Saving..." : "Create"}</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
