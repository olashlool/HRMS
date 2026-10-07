namespace HRMS.Application.Common.Interfaces;

public interface IPlanSeeder
{
    Task EnsurePlansAsync(CancellationToken cancellationToken = default);
}
