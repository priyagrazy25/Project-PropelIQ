using Scheduling.Application.Commands.WalkInBooking;
using SharedKernel.Domain;

namespace Scheduling.Application.Abstractions;

/// <summary>
/// Walk-in booking service for staff-initiated same-day appointments (AC-4).
/// </summary>
public interface IWalkInService
{
    Task<Result<WalkInBookingResult>> BookWalkInAsync(
        WalkInBookingCommand command,
        CancellationToken cancellationToken = default);
}
