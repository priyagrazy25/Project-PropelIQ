using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Scheduling.Application.Services;

/// <summary>
/// Background worker that monitors the SlotReleasedChannel and triggers swap engine processing (UC-004).
/// </summary>
public sealed class SwapBackgroundWorker : BackgroundService
{
    private readonly SlotReleasedChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SwapBackgroundWorker> _logger;

    public SwapBackgroundWorker(
        SlotReleasedChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<SwapBackgroundWorker> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SwapBackgroundWorker started — listening for slot-released events");

        await foreach (var slotEvent in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var swapEngine = scope.ServiceProvider.GetRequiredService<ISwapEngineService>();

                var result = await swapEngine.ProcessSlotReleaseAsync(slotEvent.SlotId, stoppingToken);

                if (result is not null)
                {
                    _logger.LogInformation(
                        "Swap executed for slot {SlotId}: patient {PatientId} moved to new slot {NewSlotId}",
                        slotEvent.SlotId, result.PatientId, result.NewSlotId);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing swap for released slot {SlotId}", slotEvent.SlotId);
            }
        }

        _logger.LogInformation("SwapBackgroundWorker stopped");
    }
}
