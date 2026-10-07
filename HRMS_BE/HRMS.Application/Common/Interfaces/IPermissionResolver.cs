namespace HRMS.Application.Common.Interfaces;

public interface IPermissionResolver
{
    Task<IReadOnlySet<string>> ResolveAsync(Guid userId, CancellationToken cancellationToken = default);
}
