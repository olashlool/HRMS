using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace HRMS.Infrastructure.Tenancy;

public sealed partial class TenantConnectionStringFactory
{
    private const string DatabaseNamePlaceholder = "{DatabaseName}";

    private readonly string _template;

    public TenantConnectionStringFactory(IConfiguration configuration)
    {
        var template = configuration.GetConnectionString("TenantTemplate");

        if (string.IsNullOrWhiteSpace(template))
        {
            throw new InvalidOperationException(
                "Connection string 'TenantTemplate' is not configured. " +
                "In development set it with: " +
                "dotnet user-secrets set \"ConnectionStrings:TenantTemplate\" \"<value>\"");
        }

        if (!template.Contains(DatabaseNamePlaceholder, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Connection string 'TenantTemplate' must contain the {DatabaseNamePlaceholder} placeholder.");
        }

        _template = template;
    }

    public string Create(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName) || !SafeDatabaseName().IsMatch(databaseName))
        {
            throw new InvalidOperationException(
                $"Refusing to build a connection string for an unsafe database name: '{databaseName}'.");
        }

        return _template.Replace(DatabaseNamePlaceholder, databaseName, StringComparison.Ordinal);
    }

    [GeneratedRegex("^[A-Za-z0-9_]{1,128}$")]
    private static partial Regex SafeDatabaseName();
}
