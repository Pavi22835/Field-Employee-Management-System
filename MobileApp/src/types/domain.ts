export interface LoginResponseDto {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  userId: string;
  username: string;
  roles: string[];
  employeeId?: string;
  deviceVerified: boolean;
}

export interface DeviceStatusResponse {
  deviceId: string;
  status: "Pending" | "Active" | "Suspended" | "Revoked" | "Lost";
  assignedEmployeeId?: string;
  registeredAt: string;
  isCompliant: boolean;
}

export interface FieldAssignmentResponse {
  id: string;
  employeeId: string;
  employeeName: string;
  fieldAreaId: string;
  fieldAreaName: string;
  fieldAreaLatitude: number;
  fieldAreaLongitude: number;
  fieldAreaRadiusMeters: number;
  visitDate: string;
  startTime: string;
  expectedEndTime: string;
  priority: number;
  instructions?: string;
  dynamicFormId?: string;
  status: "Assigned" | "Accepted" | "Started" | "InProgress" | "Completed" | "Cancelled" | "Missed";
}

export interface FieldVisitResponse {
  id: string;
  fieldAssignmentId: string;
  employeeId: string;
  status: string;
  checkInAt?: string;
  checkInDistanceMeters?: number;
  geofenceSatisfied: boolean;
  geofenceMessage: string;
  checkOutAt?: string;
  remarks?: string;
}

export interface FormFieldDto {
  id?: string;
  label: string;
  fieldType: "Text" | "Number" | "Dropdown" | "Checkbox" | "RadioButton" | "Date" | "Photo" | "Video" | "DocumentAttachment" | "GpsCoordinates" | "Signature";
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
  fields: FormFieldDto[];
}
