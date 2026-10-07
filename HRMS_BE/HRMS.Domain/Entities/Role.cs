using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public sealed class Role : AuditableEntity
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 400;

    public Guid? TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; }

    private Role()
    {
    }

    private Role(Guid? tenantId, string name, string? description, bool isSystemRole)
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Description = description;
        IsSystemRole = isSystemRole;
    }

    public static Role CreateForTenant(Guid tenantId, string name, string? description, bool isSystemRole = false)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("Tenant id is required.");
        }

        return new Role(tenantId, RequireName(name), Trim(description, DescriptionMaxLength), isSystemRole);
    }

    public void Rename(string name)
    {
        if (IsSystemRole)
        {
            throw new DomainException("A built-in role cannot be renamed.");
        }

        Name = RequireName(name);
        NormalizedName = Name.ToUpperInvariant();
    }

    public void Describe(string? description) => Description = Trim(description, DescriptionMaxLength);

    private static string RequireName(string? name)
    {
        name = name?.Trim() ?? string.Empty;

        if (name.Length == 0)
        {
            throw new DomainException("Role name is required.");
        }

        if (name.Length > NameMaxLength)
        {
            throw new DomainException($"Role name must not exceed {NameMaxLength} characters.");
        }

        return name;
    }

    private static string? Trim(string? value, int maxLength)
    {
        if (value is null)
        {
            return null;
        }

        value = value.Trim();

        return value.Length > maxLength ? value[..maxLength] : value;
    }
}
