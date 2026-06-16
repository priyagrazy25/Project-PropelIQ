using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Host.Hubs;

/// <summary>
/// SignalR hub for real-time document processing status updates (SCR-014).
/// Clients receive notifications when document OCR/NER processing completes.
/// </summary>
[Authorize]
public sealed class DocumentHub : Hub
{
    private readonly ILogger<DocumentHub> _logger;

    private static long _connectionCount;

    public DocumentHub(ILogger<DocumentHub> logger)
    {
        _logger = logger;
    }

    /// <summary>Current number of active SignalR connections for monitoring.</summary>
    public static long ConnectionCount => Interlocked.Read(ref _connectionCount);

    public override async Task OnConnectedAsync()
    {
        Interlocked.Increment(ref _connectionCount);
        
        // Add user to their personal group for targeted updates
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }
        
        _logger.LogInformation(
            "Document hub client connected: {ConnectionId}. Active connections: {Count}",
            Context.ConnectionId,
            ConnectionCount);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Interlocked.Decrement(ref _connectionCount);
        
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
        }
        
        _logger.LogInformation(
            "Document hub client disconnected: {ConnectionId}. Active connections: {Count}",
            Context.ConnectionId,
            ConnectionCount);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribes the caller to updates for a specific document.
    /// </summary>
    public async Task JoinDocumentGroup(string documentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"document:{documentId}");
        _logger.LogDebug("Client {ConnectionId} joined document group {DocumentId}.", Context.ConnectionId, documentId);
    }

    /// <summary>
    /// Unsubscribes the caller from updates for a specific document.
    /// </summary>
    public async Task LeaveDocumentGroup(string documentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"document:{documentId}");
        _logger.LogDebug("Client {ConnectionId} left document group {DocumentId}.", Context.ConnectionId, documentId);
    }
}
