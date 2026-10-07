using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Common.Interfaces;

public interface ICatalogDbContext
{
    DbSet<Tenant> Tenants { get; }

    DbSet<User> Users { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<UserToken> UserTokens { get; }

    DbSet<RecoveryCode> RecoveryCodes { get; }

    DbSet<LoginAttempt> LoginAttempts { get; }

    DbSet<Role> Roles { get; }

    DbSet<RolePermission> RolePermissions { get; }

    DbSet<UserRole> UserRoles { get; }

    DbSet<UserPermission> UserPermissions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
