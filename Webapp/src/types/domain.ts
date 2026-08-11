export interface EmployeeResponse {
  id: string;
  employeeCode: string;
  firstName: string;
  lastName: string;
  email: string;
  username: string;
  phoneNumber?: string;
  designation?: string;
  department?: string;
  supervisorId?: string;
  isActive: boolean;
  dateOfJoining: string;
  roles: string[];
  hasActiveDevice: boolean;
}

export interface DeviceListItemResponse {
  id: string;
  employeeId?: string;
  employeeName?: string;
  model?: string;
  manufacturer?: string;
  osVersion?: string;
  status: string;
  isCompliant: boolean;
  lastHeartbeatAt?: string;
  appVersion?: string;
}

export interface FieldAreaResponse {
  id: string;
  name: string;
  description?: string;
  address?: string;
  latitude: number;
  longitude: number;
  radiusMeters: number;
  enforcementMode: string;
  isActive: boolean;
  assignedEmployeeCount: number;
}

export interface FieldAssignmentResponse {
  id: string;
  employeeId: string;
  employeeName: string;
  fieldAreaId: string;
  fieldAreaName: string;
  visitDate: string;
  startTime: string;
  expectedEndTime: string;
  priority: number;
  instructions?: string;
  status: string;
}

export interface DashboardResponse {
  employees: {
    totalEmployees: number;
    activeEmployees: number;
    loggedInEmployees: number;
    loggedOutEmployees: number;
    onFieldVisit: number;
    offline: number;
    deviceAlerts: number;
  };
  visits: {
    todaysVisits: number;
    completed: number;
    inProgress: number;
    pending: number;
    missed: number;
    cancelled: number;
  };
  devices: {
    totalDevices: number;
    activeDevices: number;
    offlineDevices: number;
    nonCompliantDevices: number;
    unknownDeviceAttempts: number;
    simNetworkAlertsReserved: number;
  };
}

export interface RoleResponse {
  id: string;
  name: string;
  description?: string;
}

export interface DynamicFormFieldDto {
  id?: string;
  label: string;
  fieldType: string;
  isRequired: boolean;
  displayOrder: number;
  optionsJson?: string;
  validationRulesJson?: string;
}

export interface DynamicFormResponse {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  version: number;
  fields: DynamicFormFieldDto[];
}

// Section 4/12: management (Admin/Supervisor) visibility into visit execution and submission review.
export interface FieldVisitSummaryResponse {
  id: string;
  fieldAssignmentId: string;
  employeeId: string;
  employeeName: string;
  fieldAreaId: string;
  fieldAreaName: string;
  status: string;
  checkInAt?: string;
  checkOutAt?: string;
  submissionCount: number;
  overallReviewStatus: string;
}

export interface SubmissionFileSummary {
  id: string;
  formFieldId?: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  capturedLatitude?: number;
  capturedLongitude?: number;
  capturedAt: string;
}

export interface FormFieldValueSummary {
  formFieldId: string;
  label: string;
  fieldType: string;
  value?: string;
}

export interface SubmissionDetail {
  id: string;
  dynamicFormId: string;
  dynamicFormName: string;
  submittedAt: string;
  values: FormFieldValueSummary[];
  files: SubmissionFileSummary[];
  reviewStatus: string;
  reviewedByUsername?: string;
  reviewedAt?: string;
  reviewComment?: string;
}

export interface FieldVisitDetailResponse {
  id: string;
  fieldAssignmentId: string;
  employeeId: string;
  employeeName: string;
  fieldAreaId: string;
  fieldAreaName: string;
  status: string;
  checkInAt?: string;
  checkInLatitude?: number;
  checkInLongitude?: number;
  checkInDistanceMeters?: number;
  checkOutAt?: string;
  remarks?: string;
  submissions: SubmissionDetail[];
}

export interface SecurityAlertResponse {
  id: string;
  alertType: string;
  severity: string;
  message: string;
  employeeId?: string;
  employeeName?: string;
  deviceId?: string;
  isAcknowledged: boolean;
  acknowledgedAt?: string;
  createdAt: string;
}

export interface SystemSettingsResponse {
  locationTrackingMode: string;
  periodicTrackingIntervalSeconds: number;
  defaultGeofenceRadiusMeters: number;
  heartbeatIntervalMinutes: number;
  sessionTimeoutMinutes: number;
  maxFailedLoginAttempts: number;
  lockoutMinutes: number;
  deviceReplacementRequiresApproval: boolean;
  minimumSupportedAppVersion: string;
  notifyAdminsOnUnregisteredDeviceAttempt: boolean;
  notifyAdminsOnDeviceNonCompliance: boolean;
  notifyEmployeeOnNewAssignment: boolean;
}
