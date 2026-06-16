using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.Application.Services;

namespace Notification.Application.Workers;

public sealed class ReminderSchedulerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderSchedulerWorker> _logger;
    private readonly TimeSpan _evaluationInterval;

    public ReminderSchedulerWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ReminderSchedulerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _evaluationInterval = TimeSpan.FromMinutes(15);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ReminderSchedulerWorker started — evaluating every {Interval} minutes",
            _evaluationInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();

                await reminderService.EvaluateAndSendRemindersAsync(stoppingToken);

                _logger.LogDebug("Reminder evaluation cycle completed");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during reminder evaluation cycle");
            }

            try
            {
                await Task.Delay(_evaluationInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("ReminderSchedulerWorker stopped");
    }
}
