namespace FEMS.Api.Common;

public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Supervisor = "Supervisor";
    public const string Employee = "Employee";
}

public static class PolicyNames
{
    public const string AdminOnly = "AdminOnly";
    public const string ManagementOnly = "ManagementOnly"; // SuperAdmin, Admin, Supervisor
    public const string EmployeeOnly = "EmployeeOnly";
}
