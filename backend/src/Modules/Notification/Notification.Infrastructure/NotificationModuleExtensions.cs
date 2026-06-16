using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Notification.Application.Abstractions;
using Notification.Application.Channels;
using Notification.Application.Providers;
using Notification.Application.Services;
using Notification.Application.Workers;
using Notification.Infrastructure.Channels;
using Notification.Infrastructure.Data;
using Notification.Infrastructure.Providers;
using Notification.Infrastructure.Repositories;
using QuestPDF.Infrastructure;
using System.Net;
using System.Security.Authentication;

namespace Notification.Infrastructure;

public static class NotificationModuleExtensions
{
    public static IServiceCollection AddNotificationModule(this IServiceCollection services, IConfiguration configuration)
    {
        // QuestPDF Community License
        QuestPDF.Settings.License = LicenseType.Community;

        // NotificationDbContext for delivery logs + calendar sync
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("SchedulingDb"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "notification");
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                }));

        // Channel implementations
        services.AddSingleton<ISmsChannel, TwilioSmsChannel>();
        services.AddSingleton<IEmailChannel, SendGridEmailChannel>();

        // Repositories
        services.AddScoped<IReminderDeliveryLogRepository, ReminderDeliveryLogRepository>();
        services.AddScoped<ICalendarSyncRecordRepository, CalendarSyncRecordRepository>();
        services.AddScoped<ICalendarOAuthTokenRepository, CalendarOAuthTokenRepository>();
        services.AddScoped<IPdfConfirmationRepository, PdfConfirmationRepository>();

        // Calendar providers (strategy pattern)
        services.AddSingleton<ICalendarProvider, GoogleCalendarProvider>();
        services.AddSingleton<ICalendarProvider, OutlookCalendarProvider>();

        // Named HTTP clients for calendar APIs with TLS 1.2+ enforcement (NFR-006)
        services.AddHttpClient("GoogleCalendar")
            .ConfigurePrimaryHttpMessageHandler(() => CreateTls12Handler());
        services.AddHttpClient("OutlookCalendar")
            .ConfigurePrimaryHttpMessageHandler(() => CreateTls12Handler());

        // Service
        services.AddScoped<IReminderService, ReminderService>();
        services.AddScoped<IPdfConfirmationService, PdfConfirmationService>();

        // Background worker
        services.AddHostedService<ReminderSchedulerWorker>();

        // Health check
        services.AddHealthChecks()
            .AddCheck<NotificationHealthCheck>("notification_module", tags: ["module"]);

        return services;
    }

    /// <summary>
    /// Creates an HttpMessageHandler that enforces TLS 1.2+ for outbound connections (NFR-006).
    /// </summary>
    private static HttpMessageHandler CreateTls12Handler()
    {
        return new SocketsHttpHandler
        {
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }
        };
    }
}

public sealed class NotificationHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("Notification module is operational."));
    }
}
