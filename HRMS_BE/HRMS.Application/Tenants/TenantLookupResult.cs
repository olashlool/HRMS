namespace HRMS.Application.Tenants;

public sealed record TenantLookupResult(Guid Id, string Slug, string DatabaseName);
