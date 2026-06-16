using Identity.Application.Abstractions;
using Identity.Application.Commands.CreateUser;
using Identity.Application.Commands.DeactivateUser;
using Identity.Application.Commands.ExtendSession;
using Identity.Application.Commands.Login;
using Identity.Application.Commands.Logout;
using Identity.Application.Commands.ReactivateUser;
using Identity.Application.Commands.RefreshToken;
using Identity.Application.Commands.Register;
using Identity.Application.Commands.UpdateUser;
using Identity.Application.Queries.GetUsers;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Identity.Infrastructure;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("IdentityDb"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                }));

        // Identity application services
        services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<IdentityDbContext>());
        services.AddSingleton<IPasswordHasher, Argon2IdPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ISessionTrackingService, SessionTrackingService>();
        services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
        services.AddScoped<RegisterCommandValidator>();
        services.AddScoped<RegisterCommandHandler>();
        services.AddScoped<LoginCommandValidator>();
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<RefreshTokenCommandHandler>();
        services.AddScoped<ExtendSessionCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();

        // Admin user management
        services.AddScoped<GetUsersQueryHandler>();
        services.AddScoped<CreateUserCommandValidator>();
        services.AddScoped<CreateUserCommandHandler>();
        services.AddScoped<UpdateUserCommandValidator>();
        services.AddScoped<UpdateUserCommandHandler>();
        services.AddScoped<DeactivateUserCommandHandler>();
        services.AddScoped<ReactivateUserCommandHandler>();

        services.AddHealthChecks()
            .AddCheck<IdentityHealthCheck>("identity_module", tags: ["module"]);

        return services;
    }
}

public sealed class IdentityHealthCheck : IHealthCheck
{
    private readonly IdentityDbContext _dbContext;

    public IdentityHealthCheck(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.Database.CanConnectAsync(cancellationToken);
            return HealthCheckResult.Healthy("Identity module is operational.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Identity module database unavailable.", ex);
        }
    }
}
