using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Commands.BookAppointment;
using Scheduling.Application.Commands.CancelAppointment;
using Scheduling.Application.Commands.RescheduleAppointment;
using Scheduling.Application.Commands.RegisterSwapPreference;
using Scheduling.Application.Queries.GetProviderSlots;
using Scheduling.Application.Queries.GetMyAppointments;
using Scheduling.Application.Queries.SearchProviders;
using Scheduling.Application.Services;
using Scheduling.Infrastructure.Data;
using Scheduling.Infrastructure.ML;
using Scheduling.Infrastructure.Services;

namespace Scheduling.Infrastructure;

public static class SchedulingModuleExtensions
{
    public static IServiceCollection AddSchedulingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<SlotLockingInterceptor>();

        services.AddDbContext<SchedulingDbContext>((sp, options) =>
            options
                .UseSqlServer(
                    configuration.GetConnectionString("SchedulingDb"),
                    sqlOptions =>
                    {
                        sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "scheduling");
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null);
                    })
                .AddInterceptors(sp.GetRequiredService<SlotLockingInterceptor>()));

        // Register DbContext abstraction
        services.AddScoped<ISchedulingDbContext>(sp => sp.GetRequiredService<SchedulingDbContext>());

        // Register query handlers
        services.AddScoped<SearchProvidersQueryHandler>();
        services.AddScoped<GetProviderSlotsQueryHandler>();
        services.AddScoped<GetMyAppointmentsQueryHandler>();

        // Register command handlers
        services.AddScoped<BookAppointmentCommandHandler>();
        services.AddScoped<CancelAppointmentCommandHandler>();
        services.AddScoped<RescheduleAppointmentCommandHandler>();
        services.AddScoped<RegisterSwapPreferenceCommandHandler>();

        // Register swap engine
        services.AddSingleton<SlotReleasedChannel>();
        services.AddScoped<ISwapEngineService, SwapEngineService>();
        services.AddHostedService<SwapBackgroundWorker>();

        // Register waitlist
        services.AddSingleton<WaitlistSlotReleasedChannel>();
        services.AddScoped<IWaitlistService, WaitlistService>();
        services.AddHostedService<WaitlistNotificationWorker>();

        // Register walk-in service
        services.AddScoped<IWalkInService, WalkInService>();

        // Register queue service
        services.AddScoped<IQueueService, QueueService>();

        // Register calendar sync (no-op stub until credentials configured)
        services.AddScoped<ICalendarSyncService, NoOpCalendarSyncService>();

        // Register ML no-show risk prediction services (AIR-007, AIR-O03)
        services.AddSingleton(sp =>
        {
            var modelDirectory = Path.Combine(
                AppContext.BaseDirectory, "ml-models", "noshow");
            return new ModelVersionManager(
                modelDirectory,
                sp.GetRequiredService<ILogger<ModelVersionManager>>());
        });
        services.AddSingleton<NoShowModelTrainer>();
        services.AddSingleton<NoShowPredictionEngine>();
        services.AddScoped<INoShowRiskService, NoShowRiskService>();
        services.AddHostedService<NoShowModelInitializer>();

        services.AddHealthChecks()
            .AddCheck<SchedulingHealthCheck>("scheduling_module", tags: ["module"]);

        return services;
    }
}

public sealed class SchedulingHealthCheck : IHealthCheck
{
    private readonly SchedulingDbContext _dbContext;

    public SchedulingHealthCheck(SchedulingDbContext dbContext)
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
            return HealthCheckResult.Healthy("Scheduling module is operational.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Scheduling module database unavailable.", ex);
        }
    }
}

/// <summary>
/// Hosted service to initialize the ML prediction engine on startup.
/// </summary>
public sealed class NoShowModelInitializer : IHostedService
{
    private readonly NoShowPredictionEngine _predictionEngine;
    private readonly ILogger<NoShowModelInitializer> _logger;

    public NoShowModelInitializer(
        NoShowPredictionEngine predictionEngine,
        ILogger<NoShowModelInitializer> logger)
    {
        _predictionEngine = predictionEngine;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing no-show risk prediction engine...");
        
        try
        {
            await _predictionEngine.InitializeAsync(cancellationToken);
            _logger.LogInformation(
                "No-show risk prediction engine initialized. Active model: {Version}",
                _predictionEngine.GetCurrentVersion());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize no-show risk prediction engine");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
