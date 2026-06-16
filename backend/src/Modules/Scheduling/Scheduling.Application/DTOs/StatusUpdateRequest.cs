namespace Scheduling.Application.DTOs;

public sealed record StatusUpdateRequest(
    string NewStatus,
    byte[] RowVersion);
