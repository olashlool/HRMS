namespace HRMS.Domain.Authorization;

public static class Permissions
{
    public static class Employees
    {
        public const string Read = "employees.read";
        public const string Create = "employees.create";
        public const string Update = "employees.update";
        public const string Delete = "employees.delete";
    }

    public static class Users
    {
        public const string Read = "users.read";
        public const string Manage = "users.manage";
    }

    public static class Roles
    {
        public const string Read = "roles.read";
        public const string Manage = "roles.manage";
    }

    public static class Leave
    {
        public const string Read = "leave.read";
        public const string Request = "leave.request";
        public const string ApproveTeam = "leave.approve-team";
        public const string ApproveAny = "leave.approve-any";
        public const string Manage = "leave.manage";
    }

    public static class Attendance
    {
        public const string Read = "attendance.read";
        public const string Record = "attendance.record";
        public const string ManageAny = "attendance.manage-any";
    }

    public static class Payroll
    {
        public const string Read = "payroll.read";
        public const string Manage = "payroll.manage";
    }

    public static class Tenants
    {
        public const string Manage = "tenants.manage";
    }

    public static readonly IReadOnlySet<string> TenantScoped = new HashSet<string>(StringComparer.Ordinal)
    {
        Employees.Read,
        Employees.Create,
        Employees.Update,
        Employees.Delete,
        Users.Read,
        Users.Manage,
        Roles.Read,
        Roles.Manage,
        Payroll.Read,
        Payroll.Manage,
        Leave.Read,
        Leave.Request,
        Leave.ApproveTeam,
        Leave.ApproveAny,
        Leave.Manage,
        Attendance.Read,
        Attendance.Record,
        Attendance.ManageAny
    };

    public static readonly IReadOnlySet<string> SystemScoped = new HashSet<string>(StringComparer.Ordinal)
    {
        Tenants.Manage
    };

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(TenantScoped.Concat(SystemScoped), StringComparer.Ordinal);

    public static bool IsKnown(string permission) => All.Contains(permission);
}

public static class SystemRoles
{
    public const string TenantAdmin = "Tenant Administrator";
    public const string Employee = "Employee";

    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Defaults =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [TenantAdmin] = Permissions.TenantScoped,
            [Employee] = new HashSet<string>(StringComparer.Ordinal)
            {
                Permissions.Employees.Read,
                Permissions.Leave.Read,
                Permissions.Leave.Request,
                Permissions.Attendance.Read,
                Permissions.Attendance.Record
            }
        };
}
