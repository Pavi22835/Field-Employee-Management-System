import { useEffect, useState } from "react";
import {
  Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem,
  Stack, TextField, Typography, Divider, Alert
} from "@mui/material";
import { DataGrid, GridColDef } from "@mui/x-data-grid";
import { apiClient } from "@/api/client";
import type { ApiResponse, PagedResult } from "@/types/api";
import type {
  EmployeeResponse, FieldAreaResponse, FieldAssignmentResponse, DynamicFormResponse,
  FieldVisitSummaryResponse, FieldVisitDetailResponse
} from "@/types/domain";

// Section 8: field visit assignment and status workflow.
const statusColor: Record<string, "default" | "success" | "warning" | "error" | "info"> = {
  Assigned: "default", Accepted: "info", Started: "info", InProgress: "warning",
  Completed: "success", Cancelled: "error", Missed: "error"
};

const reviewColor: Record<string, "default" | "success" | "warning" | "error"> = {
  Pending: "warning", Approved: "success", Rejected: "error"
};

export function AssignmentsPage() {
  const [rows, setRows] = useState<FieldAssignmentResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [open, setOpen] = useState(false);
  const [employees, setEmployees] = useState<EmployeeResponse[]>([]);
  const [areas, setAreas] = useState<FieldAreaResponse[]>([]);
  const [forms, setForms] = useState<DynamicFormResponse[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({
    employeeId: "", fieldAreaId: "", visitDate: new Date().toISOString().slice(0, 10),
    startTime: "09:00", expectedEndTime: "17:00", priority: 1, instructions: "", requiredInformation: "",
    dynamicFormId: ""
  });

  const [visits, setVisits] = useState<FieldVisitSummaryResponse[]>([]);
  const [visitsLoading, setVisitsLoading] = useState(true);
  const [detail, setDetail] = useState<FieldVisitDetailResponse | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [photoUrls, setPhotoUrls] = useState<Record<string, string>>({});
  const [reviewComment, setReviewComment] = useState("");
  const [reviewingId, setReviewingId] = useState<string | null>(null);
  const [reviewError, setReviewError] = useState<string | null>(null);

  const loadAssignments = () => {
    setLoading(true);
    apiClient.get<ApiResponse<PagedResult<FieldAssignmentResponse>>>("/field-assignments", { params: { pageSize: 100 } })
      .then((res) => setRows(res.data.data?.items ?? []))
      .finally(() => setLoading(false));
  };

  const loadVisits = () => {
    setVisitsLoading(true);
    apiClient.get<ApiResponse<PagedResult<FieldVisitSummaryResponse>>>("/field-visits", { params: { pageSize: 100 } })
      .then((res) => setVisits(res.data.data?.items ?? []))
      .finally(() => setVisitsLoading(false));
  };

  useEffect(() => {
    loadAssignments();
    loadVisits();
    apiClient.get<ApiResponse<PagedResult<EmployeeResponse>>>("/employees", { params: { pageSize: 200 } })
      .then((res) => setEmployees(res.data.data?.items ?? []));
    apiClient.get<ApiResponse<PagedResult<FieldAreaResponse>>>("/field-areas", { params: { pageSize: 200 } })
      .then((res) => setAreas(res.data.data?.items ?? []));
    apiClient.get<ApiResponse<PagedResult<DynamicFormResponse>>>("/dynamic-forms", { params: { pageSize: 100 } })
      .then((res) => setForms(res.data.data?.items ?? []));
  }, []);

  const handleCreate = async () => {
    setSaving(true);
    setError(null);
    try {
      await apiClient.post("/field-assignments", { ...form, dynamicFormId: form.dynamicFormId || null });
      setOpen(false);
      loadAssignments();
    } catch (err: any) {
      setError(err?.response?.data?.message ?? "Failed to create assignment.");
    } finally {
      setSaving(false);
    }
  };

  const openDetail = async (visitId: string) => {
    setDetailLoading(true);
    setPhotoUrls({});
    setReviewError(null);
    try {
      const res = await apiClient.get<ApiResponse<FieldVisitDetailResponse>>(`/field-visits/${visitId}`);
      const data = res.data.data ?? null;
      setDetail(data);

      // Photo files require the bearer token, so fetch as a blob and hand the <img> an object URL.
      const photoFiles = (data?.submissions ?? []).flatMap((s) => s.files.filter((f) => f.contentType.startsWith("image/")));
      for (const file of photoFiles) {
        apiClient.get(`/field-visits/files/${file.id}`, { responseType: "blob" }).then((res) => {
          const url = URL.createObjectURL(res.data);
          setPhotoUrls((prev) => ({ ...prev, [file.id]: url }));
        }).catch(() => undefined);
      }
    } finally {
      setDetailLoading(false);
    }
  };

  const closeDetail = () => {
    Object.values(photoUrls).forEach((url) => URL.revokeObjectURL(url));
    setPhotoUrls({});
    setDetail(null);
  };

  const handleReview = async (submissionId: string, status: "Approved" | "Rejected") => {
    setReviewingId(submissionId);
    setReviewError(null);
    try {
      await apiClient.post(`/field-visits/submissions/${submissionId}/review`, { reviewStatus: status, comment: reviewComment || null });
      setReviewComment("");
      if (detail) await openDetail(detail.id);
      loadVisits();
    } catch (err: any) {
      setReviewError(err?.response?.data?.message ?? "Failed to record review.");
    } finally {
      setReviewingId(null);
    }
  };

  const columns: GridColDef<FieldAssignmentResponse>[] = [
    { field: "employeeName", headerName: "Employee", flex: 1, minWidth: 140 },
    { field: "fieldAreaName", headerName: "Area", flex: 1, minWidth: 140 },
    { field: "visitDate", headerName: "Visit Date", flex: 0.8, minWidth: 100 },
    { field: "startTime", headerName: "Start", flex: 0.6, minWidth: 80 },
    { field: "expectedEndTime", headerName: "Expected End", flex: 0.8, minWidth: 100 },
    { field: "priority", headerName: "Priority", flex: 0.5, minWidth: 70 },
    {
      field: "status", headerName: "Status", flex: 0.8, minWidth: 110,
      renderCell: (p) => <Chip label={p.value} color={statusColor[p.value as string] ?? "default"} size="small" />
    }
  ];

  const visitColumns: GridColDef<FieldVisitSummaryResponse>[] = [
    { field: "employeeName", headerName: "Employee", flex: 1, minWidth: 130 },
    { field: "fieldAreaName", headerName: "Area", flex: 1, minWidth: 130 },
    {
      field: "status", headerName: "Status", flex: 0.8, minWidth: 100,
      renderCell: (p) => <Chip label={p.value} color={statusColor[p.value as string] ?? "default"} size="small" />
    },
    { field: "checkInAt", headerName: "Check-In", flex: 1, minWidth: 150, valueFormatter: (v) => v ? new Date(v as string).toLocaleString() : "—" },
    { field: "checkOutAt", headerName: "Check-Out", flex: 1, minWidth: 150, valueFormatter: (v) => v ? new Date(v as string).toLocaleString() : "—" },
    { field: "submissionCount", headerName: "Submissions", flex: 0.6, minWidth: 100 },
    {
      field: "overallReviewStatus", headerName: "Review", flex: 0.7, minWidth: 100,
      renderCell: (p) => <Chip label={p.value} color={reviewColor[p.value as string] ?? "default"} size="small" />
    },
    {
      field: "actions", headerName: "Actions", flex: 0.6, minWidth: 90, sortable: false,
      renderCell: (p) => <Button size="small" onClick={() => openDetail(p.row.id)}>View</Button>
    }
  ];

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h5">Field Assignments</Typography>
        <Button variant="contained" onClick={() => setOpen(true)}>New Assignment</Button>
      </Stack>

      <Box
        sx={{
          height: 400, bgcolor: "background.paper", maxWidth: "100%", mb: 4,
          "& .MuiDataGrid-virtualScroller": { scrollbarWidth: "thin" }
        }}
      >
        <DataGrid rows={rows} columns={columns} loading={loading} getRowId={(r) => r.id} />
      </Box>

      <Typography variant="h5" gutterBottom>Field Visits (review &amp; approve submissions)</Typography>
      <Box
        sx={{
          height: 400, bgcolor: "background.paper", maxWidth: "100%",
          "& .MuiDataGrid-virtualScroller": { scrollbarWidth: "thin" }
        }}
      >
        <DataGrid rows={visits} columns={visitColumns} loading={visitsLoading} getRowId={(r) => r.id} />
      </Box>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>New Field Assignment</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Typography color="error">{error}</Typography>}
            <TextField select label="Employee" value={form.employeeId} onChange={(e) => setForm({ ...form, employeeId: e.target.value })} required>
              {employees.map((e) => (
                <MenuItem key={e.id} value={e.id}>{e.firstName} {e.lastName} ({e.employeeCode})</MenuItem>
              ))}
            </TextField>
            <TextField select label="Field Area" value={form.fieldAreaId} onChange={(e) => setForm({ ...form, fieldAreaId: e.target.value })} required>
              {areas.map((a) => (
                <MenuItem key={a.id} value={a.id}>{a.name}</MenuItem>
              ))}
            </TextField>
            <TextField
              select label="Dynamic Form (optional)" value={form.dynamicFormId}
              onChange={(e) => setForm({ ...form, dynamicFormId: e.target.value })}
              helperText="Attach a form template so the employee can submit field data for this visit."
            >
              <MenuItem value="">(none)</MenuItem>
              {forms.map((f) => (
                <MenuItem key={f.id} value={f.id}>{f.name}</MenuItem>
              ))}
            </TextField>
            <TextField label="Visit Date" type="date" value={form.visitDate} onChange={(e) => setForm({ ...form, visitDate: e.target.value })} InputLabelProps={{ shrink: true }} />
            <Stack direction="row" spacing={2}>
              <TextField label="Start Time" type="time" value={form.startTime} onChange={(e) => setForm({ ...form, startTime: e.target.value })} InputLabelProps={{ shrink: true }} fullWidth />
              <TextField label="Expected End" type="time" value={form.expectedEndTime} onChange={(e) => setForm({ ...form, expectedEndTime: e.target.value })} InputLabelProps={{ shrink: true }} fullWidth />
            </Stack>
            <TextField label="Priority (0-10)" type="number" value={form.priority} onChange={(e) => setForm({ ...form, priority: parseInt(e.target.value, 10) })} />
            <TextField label="Instructions" value={form.instructions} onChange={(e) => setForm({ ...form, instructions: e.target.value })} multiline rows={2} />
            <TextField label="Required Information" value={form.requiredInformation} onChange={(e) => setForm({ ...form, requiredInformation: e.target.value })} multiline rows={2} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={handleCreate} disabled={saving}>{saving ? "Saving..." : "Create"}</Button>
        </DialogActions>
      </Dialog>

      <Dialog open={!!detail || detailLoading} onClose={closeDetail} maxWidth="md" fullWidth>
        <DialogTitle>Visit Detail</DialogTitle>
        <DialogContent>
          {detailLoading && <Typography>Loading...</Typography>}
          {detail && (
            <Stack spacing={2} sx={{ mt: 1 }}>
              <Typography variant="subtitle1">{detail.employeeName} — {detail.fieldAreaName}</Typography>
              <Stack direction="row" spacing={3}>
                <Stack direction="row" spacing={1} alignItems="center">
                  <Typography variant="body2" component="span">Status:</Typography>
                  <Chip label={detail.status} size="small" color={statusColor[detail.status] ?? "default"} />
                </Stack>
                <Typography variant="body2">Check-in: {detail.checkInAt ? new Date(detail.checkInAt).toLocaleString() : "—"}</Typography>
                <Typography variant="body2">Check-out: {detail.checkOutAt ? new Date(detail.checkOutAt).toLocaleString() : "—"}</Typography>
              </Stack>
              {detail.checkInDistanceMeters != null && (
                <Typography variant="body2">Distance from area at check-in: {Math.round(detail.checkInDistanceMeters)}m</Typography>
              )}
              {detail.remarks && <Typography variant="body2">Remarks: {detail.remarks}</Typography>}

              <Divider />

              {detail.submissions.length === 0 && <Typography color="text.secondary">No submissions yet.</Typography>}
              {detail.submissions.map((s) => (
                <Box key={s.id} sx={{ border: "1px solid", borderColor: "divider", borderRadius: 1, p: 2 }}>
                  <Stack direction="row" justifyContent="space-between" alignItems="center">
                    <Typography variant="subtitle2">{s.dynamicFormName} — {new Date(s.submittedAt).toLocaleString()}</Typography>
                    <Chip label={s.reviewStatus} size="small" color={reviewColor[s.reviewStatus] ?? "default"} />
                  </Stack>

                  <Stack spacing={0.5} sx={{ mt: 1 }}>
                    {s.values.map((v) => (
                      <Typography key={v.formFieldId} variant="body2"><strong>{v.label}:</strong> {v.value}</Typography>
                    ))}
                  </Stack>

                  {s.files.length > 0 && (
                    <Stack direction="row" spacing={1} sx={{ mt: 1, flexWrap: "wrap" }}>
                      {s.files.map((f) => (
                        photoUrls[f.id]
                          ? <img key={f.id} src={photoUrls[f.id]} alt={f.fileName} style={{ width: 96, height: 96, objectFit: "cover", borderRadius: 4 }} />
                          : <Typography key={f.id} variant="caption">{f.fileName}</Typography>
                      ))}
                    </Stack>
                  )}

                  {s.reviewStatus === "Pending" ? (
                    <Stack spacing={1} sx={{ mt: 1.5 }}>
                      {reviewError && <Alert severity="error">{reviewError}</Alert>}
                      <TextField
                        size="small" label="Review comment (optional)" value={reviewComment}
                        onChange={(e) => setReviewComment(e.target.value)}
                      />
                      <Stack direction="row" spacing={1}>
                        <Button size="small" variant="contained" color="success" disabled={reviewingId === s.id}
                          onClick={() => handleReview(s.id, "Approved")}>
                          Approve
                        </Button>
                        <Button size="small" variant="outlined" color="error" disabled={reviewingId === s.id}
                          onClick={() => handleReview(s.id, "Rejected")}>
                          Reject
                        </Button>
                      </Stack>
                    </Stack>
                  ) : (
                    <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: "block" }}>
                      {s.reviewStatus} by {s.reviewedByUsername} on {s.reviewedAt ? new Date(s.reviewedAt).toLocaleString() : ""}
                      {s.reviewComment ? ` — "${s.reviewComment}"` : ""}
                    </Typography>
                  )}
                </Box>
              ))}
            </Stack>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDetail}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
