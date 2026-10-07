using HRMS.Application.Common.Interfaces;

namespace HRMS.Infrastructure.Tenancy;

public sealed class CurrentTenant : ICurrentTenant, ICurrentTenantSetter
{
    private Guid _tenantId;
    private string? _slug;
    private string? _databaseName;

    public bool IsResolved { get; private set; }

    public Guid TenantId => IsResolved ? _tenantId : throw NotResolved();

    public string Slug => IsResolved ? _slug! : throw NotResolved();

    public string DatabaseName => IsResolved ? _databaseName! : throw NotResolved();

    public void Set(Guid tenantId, string slug, string databaseName)
    {
        if (IsResolved)
        {
            throw new InvalidOperationException(
                "The tenant for this request has already been resolved and cannot be changed.");
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Tenant slug is required.", nameof(slug));
        }

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new ArgumentException("Database name is required.", nameof(databaseName));
        }

        _tenantId = tenantId;
        _slug = slug;
        _databaseName = databaseName;
        IsResolved = true;
    }

    private static InvalidOperationException NotResolved() =>
        new("No tenant has been resolved for the current request.");
}
