namespace HRMS.Application.Tenants.Dtos;

public sealed record TenantResponse(Guid Id, string Name, string Slug, string Status, DateTimeOffset CreatedAtUtc);
