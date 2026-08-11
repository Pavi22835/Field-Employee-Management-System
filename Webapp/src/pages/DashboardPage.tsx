import { useEffect, useState } from "react";
import { Card, CardContent, Grid, Typography, CircularProgress, Box } from "@mui/material";
import { apiClient } from "@/api/client";
import type { ApiResponse } from "@/types/api";
import type { DashboardResponse } from "@/types/domain";

function StatCard({ label, value }: { label: string; value: number }) {
  return (
    <Grid item xs={6} sm={4} md={3} lg={2}>
      <Card>
        <CardContent>
          <Typography variant="h4">{value}</Typography>
          <Typography variant="body2" color="text.secondary">{label}</Typography>
        </CardContent>
      </Card>
    </Grid>
  );
}

export function DashboardPage() {
  const [data, setData] = useState<DashboardResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    apiClient.get<ApiResponse<DashboardResponse>>("/admin/dashboard")
      .then((res) => setData(res.data.data ?? null))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <Box sx={{ display: "flex", justifyContent: "center", mt: 4 }}><CircularProgress /></Box>;
  if (!data) return <Typography>Unable to load dashboard.</Typography>;

  return (
    <Box>
      <Typography variant="h5" gutterBottom>Employees (section 13.1)</Typography>
      <Grid container spacing={2} sx={{ mb: 4 }}>
        <StatCard label="Total" value={data.employees.totalEmployees} />
        <StatCard label="Active" value={data.employees.activeEmployees} />
        <StatCard label="Logged In" value={data.employees.loggedInEmployees} />
        <StatCard label="Logged Out" value={data.employees.loggedOutEmployees} />
        <StatCard label="On Field Visit" value={data.employees.onFieldVisit} />
        <StatCard label="Offline" value={data.employees.offline} />
      </Grid>

      <Typography variant="h5" gutterBottom>Today's Visits (section 13.2)</Typography>
      <Grid container spacing={2} sx={{ mb: 4 }}>
        <StatCard label="Total Today" value={data.visits.todaysVisits} />
        <StatCard label="Completed" value={data.visits.completed} />
        <StatCard label="In Progress" value={data.visits.inProgress} />
        <StatCard label="Pending" value={data.visits.pending} />
        <StatCard label="Missed" value={data.visits.missed} />
        <StatCard label="Cancelled" value={data.visits.cancelled} />
      </Grid>

      <Typography variant="h5" gutterBottom>Devices (section 13.3)</Typography>
      <Grid container spacing={2}>
        <StatCard label="Total" value={data.devices.totalDevices} />
        <StatCard label="Active" value={data.devices.activeDevices} />
        <StatCard label="Offline/Pending" value={data.devices.offlineDevices} />
        <StatCard label="Non-compliant" value={data.devices.nonCompliantDevices} />
        <StatCard label="Unknown Attempts" value={data.devices.unknownDeviceAttempts} />
      </Grid>
    </Box>
  );
}
