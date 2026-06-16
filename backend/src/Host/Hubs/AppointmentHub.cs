using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Host.Hubs;

/// <summary>
/// SignalR hub for real-time appointment slot availability and queue status updates (AD-008, NFR-002).
/// Clients join provider-specific or queue groups to receive targeted broadcasts within 500ms.
/// </summary>
public sealed class AppointmentHub : Hub
{
    private readonly ILogger<AppointmentHub> _logger;

    private static long _connectionCount;

    public AppointmentHub(ILogger<AppointmentHub> logger)
    {
        _logger = logger;
    }

    /// <summary>Current number of active SignalR connections for monitoring (AC-5).</summary>
    public static long ConnectionCount => Interlocked.Read(ref _connectionCount);

    public override async Task OnConnectedAsync()
    {
        Interlocked.Increment(ref _connectionCount);
        _logger.LogInformation(
            "Client connected: {ConnectionId}. Active connections: {Count}",
            Context.ConnectionId,
            ConnectionCount);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Interlocked.Decrement(ref _connectionCount);
        _logger.LogInformation(
            "Client disconnected: {ConnectionId}. Active connections: {Count}",
            Context.ConnectionId,
            ConnectionCount);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribes the caller to slot updates for a specific provider.
    /// </summary>
    public async Task JoinProviderGroup(string providerId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"provider:{providerId}");
        _logger.LogDebug("Client {ConnectionId} joined provider group {ProviderId}.", Context.ConnectionId, providerId);
    }

    /// <summary>
    /// Unsubscribes the caller from a provider's slot updates.
    /// </summary>
    public async Task LeaveProviderGroup(string providerId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"provider:{providerId}");
        _logger.LogDebug("Client {ConnectionId} left provider group {ProviderId}.", Context.ConnectionId, providerId);
    }

    /// <summary>
    /// Subscribes the caller to same-day queue status updates.
    /// </summary>
    public async Task JoinQueueGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "queue:status");
        _logger.LogDebug("Client {ConnectionId} joined queue status group.", Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribes the caller from queue status updates.
    /// </summary>
    public async Task LeaveQueueGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "queue:status");
        _logger.LogDebug("Client {ConnectionId} left queue status group.", Context.ConnectionId);
    }

    /// <summary>
    /// Subscribes the caller to staff-only queue management updates (queue-status-changed events).
    /// </summary>
    public async Task JoinStaffQueueGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "queue:staff");
        _logger.LogDebug("Client {ConnectionId} joined staff queue group.", Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribes the caller from staff queue management updates.
    /// </summary>
    public async Task LeaveStaffQueueGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "queue:staff");
        _logger.LogDebug("Client {ConnectionId} left staff queue group.", Context.ConnectionId);
    }
}
