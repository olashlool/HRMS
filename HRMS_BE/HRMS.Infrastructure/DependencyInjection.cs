using HRMS.Application.Common.Interfaces;
using HRMS.Infrastructure.Authentication;
using HRMS.Infrastructure.Persistence;
using HRMS.Infrastructure.Persistence.Catalog;
using HRMS.Infrastructure.Persistence.Tenants;
using HRMS.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HRMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Catalog");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Catalog' is not configured. " +
                "In development set it with: " +
                "dotnet user-secrets set \"ConnectionStrings:Catalog\" \"<value>\"");
        }

        services.AddSingleton(TimeProvider.System);

        services.AddScoped<ICurrentUser, AnonymousCurrentUser>();
        services.AddScoped<AuditingInterceptor>();

        services.AddDbContext<CatalogDbContext>((serviceProvider, options) =>
            options
                .UseSqlServer(connectionString)
                .AddInterceptors(serviceProvider.GetRequiredService<AuditingInterceptor>()));

        services.AddScoped<ICatalogDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());

        services.AddScoped<CurrentTenant>();
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<CurrentTenant>());
        services.AddScoped<ICurrentTenantSetter>(sp => sp.GetRequiredService<CurrentTenant>());

        services.AddSingleton<TenantConnectionStringFactory>();
        services.AddScoped<ITenantProvisioner, TenantProvisioner>();

        services.AddDbContext<TenantDbContext>((serviceProvider, options) =>
        {
            var currentTenant = serviceProvider.GetRequiredService<ICurrentTenant>();
            var connectionStringFactory = serviceProvider.GetRequiredService<TenantConnectionStringFactory>();

            options
                .UseSqlServer(connectionStringFactory.Create(currentTenant.DatabaseName))
                .AddInterceptors(serviceProvider.GetRequiredService<AuditingInterceptor>());
        });

        services.AddScoped<ITenantDbContext>(sp => sp.GetRequiredService<TenantDbContext>());

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey), "Jwt:SigningKey is required.")
            .Validate(o => o.SigningKey.Length >= 32, "Jwt:SigningKey must be at least 32 characters.")
            .Validate(o => o.AccessTokenMinutes is > 0 and <= 60, "Jwt:AccessTokenMinutes must be between 1 and 60.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddSingleton<IAccessTokenGenerator, AccessTokenGenerator>();
        services.AddSingleton<ITotpGenerator, TotpGenerator>();
        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddDataProtection();

        return services;
    }
}
