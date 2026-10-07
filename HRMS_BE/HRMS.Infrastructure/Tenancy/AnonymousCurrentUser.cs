using HRMS.Application.Common.Interfaces;

namespace HRMS.Infrastructure.Tenancy;

public sealed class AnonymousCurrentUser : ICurrentUser
{
    public string? Id => null;
}
