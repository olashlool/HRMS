using HRMS.Application.Authentication;
using HRMS.Application.Employees;
using HRMS.Application.Tenants;
using Microsoft.Extensions.DependencyInjection;

namespace HRMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ITenantLookup, TenantLookup>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAccountService, AccountService>();

        return services;
    }
}
