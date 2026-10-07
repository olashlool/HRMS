using HRMS.Domain.Entities;

namespace HRMS.Application.Common.Interfaces;

public interface IAccessTokenGenerator
{
    AccessToken Generate(User user, Tenant tenant, IReadOnlySet<string> permissions);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);
