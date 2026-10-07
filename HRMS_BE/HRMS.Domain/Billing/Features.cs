namespace HRMS.Domain.Billing;

public static class Features
{
    public const string Attendance = "attendance";
    public const string Payroll = "payroll";
    public const string Reports = "reports";
    public const string PublicApi = "public-api";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Attendance,
        Payroll,
        Reports,
        PublicApi
    };

    public static bool IsKnown(string feature) => All.Contains(feature);
}

public static class PlanCodes
{
    public const string Free = "free";
    public const string Basic = "basic";
    public const string Pro = "pro";
    public const string Enterprise = "enterprise";
}
