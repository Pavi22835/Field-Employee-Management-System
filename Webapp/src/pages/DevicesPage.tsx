import { useEffect, useState } from "react";
import { Box, Button, Chip, Menu, MenuItem, Stack, Typography } from "@mui/material";
import { DataGrid, GridColDef } from "@mui/x-data-grid";
import { apiClient } from "@/api/client";
import type { ApiResponse, PagedResult } from "@/types/api";
import type { DeviceListItemResponse } from "@/types/domain";

const statusColor: Record<string, "default" | "success" | "warning" | "error"> = {
  Pending: "warning", Active: "success", Suspended: "warning", Revoked: "error", Lost: "error"
};

export function DevicesPage() {
  const [rows, setRows] = useState<DeviceListItemResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null);
  const [selected, setSelected] = useState<DeviceListItemResponse | null>(null);

  const loadDevices = () => {
    setLoading(true);
    setLoadError(false);
    apiClient.get<ApiResponse<PagedResult<DeviceListItemResponse>>>("/admin/devices", { params: { pageSize: 100 } })
      .then((res) => setRows(res.data.data?.items ?? []))
      .catch(() => setLoadError(true))
      .finally(() => setLoading(false));
  };

  useEffect(loadDevices, []);

  const runAction = async (action: string) => {
    if (!selected) return;
    setMenuAnchor(null);
    try {
      if (action === "approve") await apiClient.post(`/admin/devices/${selected.id}/approve`);
      if (action === "revoke") await apiClient.post(`/admin/devices/${selected.id}/revoke`);
      if (action === "unassign") await apiClient.post(`/admin/devices/${selected.id}/unassign`);
      if (action === "mark-lost") await apiClient.post(`/admin/devices/${selected.id}/mark-lost`, {});
      loadDevices();
    } catch (err: any) {
      window.alert(err?.response?.data?.message ?? `Failed to ${action.replace("-", " ")} device.`);
    }
  };

  const columns: GridColDef<DeviceListItemResponse>[] = [
    { field: "employeeName", headerName: "Employee", flex: 1.3, minWidth: 150, renderCell: (p) => p.value ?? "Unassigned" },
    { field: "model", headerName: "Model", flex: 1.1, minWidth: 120 },
    { field: "manufacturer", headerName: "Manufacturer", flex: 1, minWidth: 110 },
    { field: "osVersion", headerName: "OS Version", flex: 0.8, minWidth: 90 },
    { field: "appVersion", headerName: "App Version", flex: 0.8, minWidth: 90 },
    {
      field: "status", headerName: "Status", flex: 0.9, minWidth: 100,
      renderCell: (p) => <Chip label={p.value} color={statusColor[p.value as string] ?? "default"} size="small" />
    },
    { field: "isCompliant", headerName: "Compliant", flex: 0.7, minWidth: 90, renderCell: (p) => (p.value ? "Yes" : "No") },
    {
      field: "actions", headerName: "Actions", flex: 0.7, minWidth: 90, sortable: false,
      renderCell: (p) => (
        <Button size="small" onClick={(e) => { setSelected(p.row); setMenuAnchor(e.currentTarget); }}>
          Actions
        </Button>
      )
    }
  ];

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h5">Devices (section 16)</Typography>
      </Stack>

      {loadError ? (
        <Box sx={{ textAlign: "center", mt: 4 }}>
          <Typography color="error" sx={{ mb: 2 }}>Failed to load devices.</Typography>
          <Button variant="contained" onClick={loadDevices}>Retry</Button>
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

      <Menu anchorEl={menuAnchor} open={!!menuAnchor} onClose={() => setMenuAnchor(null)}>
        <MenuItem onClick={() => runAction("approve")}>Approve</MenuItem>
        <MenuItem onClick={() => runAction("revoke")}>Revoke</MenuItem>
        <MenuItem onClick={() => runAction("unassign")}>Unassign</MenuItem>
        <MenuItem onClick={() => runAction("mark-lost")}>Mark Lost</MenuItem>
      </Menu>
    </Box>
  );
}
