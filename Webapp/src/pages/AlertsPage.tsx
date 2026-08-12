import { useEffect, useState } from "react";
import { Box, Button, Chip, Stack, Switch, FormControlLabel, Typography } from "@mui/material";
import { DataGrid, GridColDef } from "@mui/x-data-grid";
import { apiClient } from "@/api/client";
import type { ApiResponse, PagedResult } from "@/types/api";
import type { SecurityAlertResponse } from "@/types/domain";

// Section 13.3, 16, 20.2: pull-based alert list + acknowledge. A real-time push
// dashboard is reserved for Phase 6 per the roadmap.
const severityColor: Record<string, "default" | "warning" | "error" | "info"> = {
  Info: "info", Warning: "warning", Critical: "error"
};

export function AlertsPage() {
  const [rows, setRows] = useState<SecurityAlertResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [unacknowledgedOnly, setUnacknowledgedOnly] = useState(true);

  const loadAlerts = () => {
    setLoading(true);
    setLoadError(false);
    apiClient.get<ApiResponse<PagedResult<SecurityAlertResponse>>>("/admin/alerts", {
      params: { pageSize: 100, unacknowledgedOnly }
    })
      .then((res) => setRows(res.data.data?.items ?? []))
      .catch(() => setLoadError(true))
      .finally(() => setLoading(false));
  };

  useEffect(loadAlerts, [unacknowledgedOnly]);

  const acknowledge = async (id: string) => {
    try {
      await apiClient.post(`/admin/alerts/${id}/acknowledge`);
      loadAlerts();
    } catch (err: any) {
      window.alert(err?.response?.data?.message ?? "Failed to acknowledge alert.");
    }
  };

  const columns: GridColDef<SecurityAlertResponse>[] = [
    {
      field: "severity", headerName: "Severity", width: 100,
      renderCell: (p) => <Chip label={p.value} color={severityColor[p.value as string] ?? "default"} size="small" />
    },
    { field: "alertType", headerName: "Type", width: 180 },
    { field: "message", headerName: "Message", flex: 1, minWidth: 300 },
    { field: "employeeName", headerName: "Employee", width: 160, renderCell: (p) => p.value ?? "-" },
    { field: "createdAt", headerName: "Raised At", width: 180, renderCell: (p) => new Date(p.value as string).toLocaleString() },
    {
      field: "actions", headerName: "", width: 140, sortable: false,
      renderCell: (p) => !p.row.isAcknowledged && (
        <Button size="small" onClick={() => acknowledge(p.row.id)}>Acknowledge</Button>
      )
    }
  ];

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h5">Alerts</Typography>
        <FormControlLabel
          control={<Switch checked={unacknowledgedOnly} onChange={(e) => setUnacknowledgedOnly(e.target.checked)} />}
          label="Unacknowledged only"
        />
      </Stack>

      {loadError ? (
        <Box sx={{ textAlign: "center", mt: 4 }}>
          <Typography color="error" sx={{ mb: 2 }}>Failed to load alerts.</Typography>
          <Button variant="contained" onClick={loadAlerts}>Retry</Button>
        </Box>
      ) : (
        <Box sx={{ height: 600, bgcolor: "background.paper" }}>
          <DataGrid rows={rows} columns={columns} loading={loading} getRowId={(r) => r.id} />
        </Box>
      )}
    </Box>
  );
}
