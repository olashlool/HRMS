using System.Text.RegularExpressions;
using HRMS.Domain.Common;
using HRMS.Domain.Entities.Enums;

namespace HRMS.Domain.Entities;

public sealed partial class Tenant : BaseEntity
{
    public const int NameMaxLength = 200;
    public const int SlugMaxLength = 63;
    public const int DatabaseNameMaxLength = 128;

    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string DatabaseName { get; private set; } = null!;
    public TenantStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Tenant()
    {
    }

    private Tenant(string name, string slug, string databaseName)
    {
        Id = Guid.CreateVersion7();
        Name = name;
        Slug = slug;
        DatabaseName = databaseName;
        Status = TenantStatus.Active;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Tenant Create(string name, string slug, string databaseName)
    {
        name = name?.Trim() ?? string.Empty;
        slug = slug?.Trim().ToLowerInvariant() ?? string.Empty;
        databaseName = databaseName?.Trim() ?? string.Empty;

        if (name.Length == 0)
            throw new DomainException("Tenant name is required.");

        if (name.Length > NameMaxLength)
            throw new DomainException($"Tenant name must not exceed {NameMaxLength} characters.");

        if (slug.Length == 0)
            throw new DomainException("Tenant slug is required.");

        if (slug.Length > SlugMaxLength)
            throw new DomainException($"Tenant slug must not exceed {SlugMaxLength} characters.");

        if (!SlugPattern().IsMatch(slug))
            throw new DomainException(
                "Tenant slug may contain lowercase letters, digits and single hyphens between them.");

        if (databaseName.Length == 0)
            throw new DomainException("Database name is required.");

        if (databaseName.Length > DatabaseNameMaxLength)
            throw new DomainException($"Database name must not exceed {DatabaseNameMaxLength} characters.");

        if (!DatabaseNamePattern().IsMatch(databaseName))
            throw new DomainException(
                "Database name may contain letters, digits and underscores only.");

        return new Tenant(name, slug, databaseName);
    }

    public void Rename(string name)
    {
        name = name?.Trim() ?? string.Empty;

        if (name.Length == 0)
            throw new DomainException("Tenant name is required.");

        if (name.Length > NameMaxLength)
            throw new DomainException($"Tenant name must not exceed {NameMaxLength} characters.");

        Name = name;
    }

    public void Suspend() => Status = TenantStatus.Suspended;

    public void Activate() => Status = TenantStatus.Active;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();

    [GeneratedRegex("^[A-Za-z0-9_]+$")]
    private static partial Regex DatabaseNamePattern();
}
