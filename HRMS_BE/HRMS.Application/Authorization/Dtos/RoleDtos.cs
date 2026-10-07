namespace HRMS.Application.Authorization.Dtos;

public sealed record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    IReadOnlyList<string> Permissions);

public sealed record CreateRoleRequest(string Name, string? Description, IReadOnlyList<string> Permissions);

public sealed record SetRolePermissionsRequest(IReadOnlyList<string> Permissions);

public sealed record AssignRoleRequest(Guid RoleId);

public sealed record SetUserPermissionRequest(string Permission, bool IsGranted);

public sealed record EffectivePermissionsResponse(Guid UserId, IReadOnlyList<string> Permissions);
