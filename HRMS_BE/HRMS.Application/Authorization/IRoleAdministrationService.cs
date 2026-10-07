using HRMS.Application.Authorization.Dtos;

namespace HRMS.Application.Authorization;

public interface IRoleAdministrationService
{
    Task<IReadOnlyList<RoleResponse>> ListAsync(CancellationToken cancellationToken = default);

    Task<RoleResponse> CreateAsync(Guid actorUserId, CreateRoleRequest request, CancellationToken cancellationToken = default);

    Task<RoleResponse> SetPermissionsAsync(Guid actorUserId, Guid roleId, SetRolePermissionsRequest request, CancellationToken cancellationToken = default);

    Task AssignRoleAsync(Guid actorUserId, Guid targetUserId, Guid roleId, CancellationToken cancellationToken = default);

    Task SetUserPermissionOverrideAsync(Guid actorUserId, Guid targetUserId, SetUserPermissionRequest request, CancellationToken cancellationToken = default);

    Task<EffectivePermissionsResponse> GetEffectivePermissionsAsync(Guid targetUserId, CancellationToken cancellationToken = default);
}
