namespace FEMS.Application.Admin;

/// <summary>Section 13.1-13.3: aggregated admin dashboard statistics.</summary>
public record EmployeeStats(
    int TotalEmployees,
    int ActiveEmployees,
    int LoggedInEmployees,
    int LoggedOutEmployees,
    int OnFieldVisit,
    int Offline,
    int DeviceAlerts);

public record VisitStats(
    int TodaysVisits,
    int Completed,
    int InProgress,
    int Pending,
    int Missed,
    int Cancelled);

public record DeviceStats(
    int TotalDevices,
    int ActiveDevices,
    int OfflineDevices,
    int NonCompliantDevices,
    int UnknownDeviceAttempts,
    int SimNetworkAlertsReserved);

public record DashboardResponse(EmployeeStats Employees, VisitStats Visits, DeviceStats Devices);
