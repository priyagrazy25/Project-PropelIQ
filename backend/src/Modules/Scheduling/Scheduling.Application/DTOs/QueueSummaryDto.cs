namespace Scheduling.Application.DTOs;

/// <summary>
/// Summary of today's queue with statistics.
/// </summary>
public sealed class QueueSummaryDto
{
    public required IReadOnlyList<QueueEntryDto> Entries { get; init; }
    public required int WaitingCount { get; init; }
    public required int InProgressCount { get; init; }
    public required int CompletedCount { get; init; }
    public required int AverageWaitMinutes { get; init; }
}
