import { useEffect, useState } from "react";
import {
  Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem,
  Stack, TextField, Typography, Chip, FormControlLabel, Switch
} from "@mui/material";
import { DataGrid, GridColDef } from "@mui/x-data-grid";
import { apiClient } from "@/api/client";
import { useAuth } from "@/auth/AuthContext";
import type { ApiResponse, PagedResult } from "@/types/api";
import type { EmployeeResponse } from "@/types/domain";

const emptyCreateForm = {
  username: "", email: "", temporaryPassword: "", employeeCode: "",
  firstName: "", lastName: "", phoneNumber: "", designation: "", department: "",
  dateOfJoining: new Date().toISOString().slice(0, 10), role: "Employee"
};

export function EmployeesPage() {
  const { hasAnyRole } = useAuth();
  const canCreate = hasAnyRole("SuperAdmin", "Admin");
  const [rows, setRows] = useState<EmployeeResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState(emptyCreateForm);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [editTarget, setEditTarget] = useState<EmployeeResponse | null>(null);
  const [editForm, setEditForm] = useState({
    firstName: "", lastName: "", phoneNumber: "", designation: "", department: "",
    supervisorId: "", isActive: true
  });
  const [editSaving, setEditSaving] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);

  const loadEmployees = () => {
    setLoading(true);
    setLoadError(false);
    apiClient.get<ApiResponse<PagedResult<EmployeeResponse>>>("/employees", { params: { pageSize: 100 } })
      .then((res) => setRows(res.data.data?.items ?? []))
      .catch(() => setLoadError(true))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadEmployees();
  }, []);

  const handleCreate = async () => {
    setSaving(true);
    setError(null);
    try {
      // The Add Employee form provisions the Employee or Supervisor role — Admin/SuperAdmin
      // accounts have no Employee record and are created from the System Users page instead
      // (enforced again by the backend: EmployeeService only ever accepts these two roles).
      await apiClient.post("/employees", form);
      setOpen(false);
      setForm(emptyCreateForm);
      loadEmployees();
    } catch (err: any) {
      setError(err?.response?.data?.message ?? "Failed to create employee.");
    } finally {
      setSaving(false);
    }
  };

  const openEdit = (employee: EmployeeResponse) => {
    setEditTarget(employee);
    setEditError(null);
    setEditForm({
      firstName: employee.firstName,
      lastName: employee.lastName,
      phoneNumber: employee.phoneNumber ?? "",
      designation: employee.designation ?? "",
      department: employee.department ?? "",
      supervisorId: employee.supervisorId ?? "",
      isActive: employee.isActive
    });
  };

  const handleUpdate = async () => {
    if (!editTarget) return;
    setEditSaving(true);
    setEditError(null);
    try {
      await apiClient.put(`/employees/${editTarget.id}`, {
        ...editForm,
        supervisorId: editForm.supervisorId || null
      });
      setEditTarget(null);
      loadEmployees();
    } catch (err: any) {
      setEditError(err?.response?.data?.message ?? "Failed to update employee.");
    } finally {
      setEditSaving(false);
    }
  };

  const handleDeactivate = async (employee: EmployeeResponse) => {
    if (!window.confirm(`Deactivate ${employee.firstName} ${employee.lastName}? They will no longer be able to log in.`)) return;
    try {
      await apiClient.delete(`/employees/${employee.id}`);
      loadEmployees();
    } catch (err: any) {
      window.alert(err?.response?.data?.message ?? "Failed to deactivate employee.");
    }
  };

  const columns: GridColDef<EmployeeResponse>[] = [
    { field: "employeeCode", headerName: "Code", flex: 0.7, minWidth: 90 },
    { field: "firstName", headerName: "First Name", flex: 1, minWidth: 110 },
    { field: "lastName", headerName: "Last Name", flex: 1, minWidth: 110 },
    { field: "email", headerName: "Email", flex: 1.4, minWidth: 170 },
    { field: "designation", headerName: "Designation", flex: 1.1, minWidth: 130 },
    {
      field: "roles", headerName: "Roles", flex: 1.2, minWidth: 140,
      renderCell: (params) => params.row.roles.map((r) => <Chip key={r} label={r} size="small" sx={{ mr: 0.5 }} />)
    },
    {
      field: "isActive", headerName: "Active", flex: 0.6, minWidth: 80,
      renderCell: (params) => (params.value ? "Yes" : "No")
    },
    {
      field: "hasActiveDevice", headerName: "Device", flex: 0.7, minWidth: 90,
      renderCell: (params) => (params.value ? "Bound" : "None")
    },
    {
      field: "actions", headerName: "Actions", flex: 1.1, minWidth: 150, sortable: false,
      renderCell: (params) => (
        <Stack direction="row" spacing={1}>
          <Button size="small" onClick={() => openEdit(params.row)}>Edit</Button>
          <Button size="small" color="error" disabled={!params.row.isActive} onClick={() => handleDeactivate(params.row)}>
            Deactivate
          </Button>
        </Stack>
      )
    }
  ];

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h5">Employees</Typography>
        {canCreate && <Button variant="contained" onClick={() => setOpen(true)}>Add Employee</Button>}
      </Stack>

      {loadError ? (
        <Box sx={{ textAlign: "center", mt: 4 }}>
          <Typography color="error" sx={{ mb: 2 }}>Failed to load employees.</Typography>
          <Button variant="contained" onClick={loadEmployees}>Retry</Button>
        </Box>
      ) : (
        <Box
          sx={{
            height: 600,
            bgcolor: "background.paper",
            maxWidth: "100%",
            "& .MuiDataGrid-virtualScroller": { scrollbarWidth: "thin" },
            "& .MuiDataGrid-virtualScroller::-webkit-scrollbar": { height: 8 },
            "& .MuiDataGrid-virtualScroller::-webkit-scrollbar-thumb": {
              backgroundColor: "rgba(0, 0, 0, 0.3)",
              borderRadius: 4
            }
          }}
        >
          <DataGrid rows={rows} columns={columns} loading={loading} getRowId={(r) => r.id} />
        </Box>
      )}

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Employee</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Typography color="error">{error}</Typography>}
            <TextField label="Employee Code" value={form.employeeCode} onChange={(e) => setForm({ ...form, employeeCode: e.target.value })} required />
            <Stack direction="row" spacing={2}>
              <TextField label="First Name" value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} required fullWidth />
              <TextField label="Last Name" value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} required fullWidth />
            </Stack>
            <TextField label="Username" value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} required />
            <TextField label="Email" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} required />
            <TextField label="Temporary Password" type="password" value={form.temporaryPassword} onChange={(e) => setForm({ ...form, temporaryPassword: e.target.value })} required helperText="Employee will be required to change this on first login." />
            <TextField label="Designation" value={form.designation} onChange={(e) => setForm({ ...form, designation: e.target.value })} />
            <TextField label="Department" value={form.department} onChange={(e) => setForm({ ...form, department: e.target.value })} />
            <TextField label="Phone Number" value={form.phoneNumber} onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })} />
            <TextField label="Date of Joining" type="date" value={form.dateOfJoining} onChange={(e) => setForm({ ...form, dateOfJoining: e.target.value })} InputLabelProps={{ shrink: true }} />
            <TextField
              select label="Role" value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })}
              helperText="Admin/SuperAdmin accounts are provisioned separately, from System Users."
            >
              <MenuItem value="Employee">Employee</MenuItem>
              <MenuItem value="Supervisor">Supervisor</MenuItem>
            </TextField>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={handleCreate} disabled={saving}>{saving ? "Saving..." : "Create"}</Button>
        </DialogActions>
      </Dialog>

      <Dialog open={!!editTarget} onClose={() => setEditTarget(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Edit Employee</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {editError && <Typography color="error">{editError}</Typography>}
            <Stack direction="row" spacing={2}>
              <TextField label="First Name" value={editForm.firstName} onChange={(e) => setEditForm({ ...editForm, firstName: e.target.value })} required fullWidth />
              <TextField label="Last Name" value={editForm.lastName} onChange={(e) => setEditForm({ ...editForm, lastName: e.target.value })} required fullWidth />
            </Stack>
            <TextField label="Designation" value={editForm.designation} onChange={(e) => setEditForm({ ...editForm, designation: e.target.value })} />
            <TextField label="Department" value={editForm.department} onChange={(e) => setEditForm({ ...editForm, department: e.target.value })} />
            <TextField label="Phone Number" value={editForm.phoneNumber} onChange={(e) => setEditForm({ ...editForm, phoneNumber: e.target.value })} />
            <TextField
              select label="Supervisor" value={editForm.supervisorId}
              onChange={(e) => setEditForm({ ...editForm, supervisorId: e.target.value })}
              helperText="Determines which Supervisor can monitor this employee's assignments and visits."
            >
              <MenuItem value="">(none)</MenuItem>
              {rows.filter((r) => r.id !== editTarget?.id).map((r) => (
                <MenuItem key={r.id} value={r.id}>{r.firstName} {r.lastName} ({r.employeeCode})</MenuItem>
              ))}
            </TextField>
            <FormControlLabel
              control={<Switch checked={editForm.isActive} onChange={(e) => setEditForm({ ...editForm, isActive: e.target.checked })} />}
              label="Active"
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditTarget(null)}>Cancel</Button>
          <Button variant="contained" onClick={handleUpdate} disabled={editSaving}>{editSaving ? "Saving..." : "Save"}</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
