using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Scheduling.Application.Services;

/// <summary>
/// Background worker that monitors slot-released events for waitlist notifications
/// and periodically expires stale waitlist entries (AC-2, FR-009).
/// </summary>
public sealed class WaitlistNotificationWorker : BackgroundService
{
    private readonly WaitlistSlotReleasedChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WaitlistNotificationWorker> _logger;
    private static readonly TimeSpan ExpiryCheckInterval = TimeSpan.FromHours(1);

    public WaitlistNotificationWorker(
        WaitlistSlotReleasedChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<WaitlistNotificationWorker> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WaitlistNotificationWorker started — listening for slot-released events");

        // Run expiry check and channel processing concurrently
        var expiryTask = RunExpiryLoopAsync(stoppingToken);
        var channelTask = RunChannelProcessingAsync(stoppingToken);

        await Task.WhenAll(expiryTask, channelTask);

        _logger.LogInformation("WaitlistNotificationWorker stopped");
    }

    private async Task RunChannelProcessingAsync(CancellationToken stoppingToken)
    {
        await foreach (var slotEvent in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var waitlistService = scope.ServiceProvider.GetRequiredService<IWaitlistService>();

                await waitlistService.ProcessSlotReleaseForWaitlistAsync(
                    slotEvent.SlotId, slotEvent.ProviderId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing waitlist notification for released slot {SlotId}", slotEvent.SlotId);
            }
        }
    }

    private async Task RunExpiryLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(ExpiryCheckInterval, stoppingToken);

                await using var scope = _scopeFactory.CreateAsyncScope();
                var waitlistService = scope.ServiceProvider.GetRequiredService<IWaitlistService>();

                await waitlistService.ExpireStaleEntriesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during waitlist stale entry expiry check");
            }
        }
    }
}
