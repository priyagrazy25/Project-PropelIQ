namespace Scheduling.Application.Queries.GetProviderSlots;

public sealed record GetProviderSlotsQuery(
    Guid ProviderId,
    DateTime? Date);
