using HRMS.Domain.Entities;

namespace HRMS.Application.Common.Interfaces;

public interface IAccessTokenGenerator
{
    AccessToken Generate(User user, Tenant tenant);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);
