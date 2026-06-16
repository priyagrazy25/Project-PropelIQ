using Scheduling.Application.Queries.SearchProviders;

namespace Scheduling.Application.Queries.GetProviderSlots;

public sealed record GetProviderSlotsResult(
    Guid ProviderId,
    List<SlotDto> Slots);
