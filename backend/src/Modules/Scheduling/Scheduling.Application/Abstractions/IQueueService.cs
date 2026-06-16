using Scheduling.Application.DTOs;
using SharedKernel.Domain;

namespace Scheduling.Application.Abstractions;

public interface IQueueService
{
    Task<IReadOnlyList<QueueEntryDto>> GetTodayQueueAsync(CancellationToken cancellationToken = default);

    Task<QueueEntryDto?> GetPatientQueueEntryAsync(Guid patientId, CancellationToken cancellationToken = default);

    Task<Result<QueueEntryDto>> UpdateStatusAsync(
        Guid appointmentId,
        string newStatus,
        byte[] rowVersion,
        Guid staffActorId,
        string staffActorName,
        CancellationToken cancellationToken = default);

    Task<Result<QueueEntryDto>> MarkArrivedAsync(
        Guid appointmentId,
        Guid staffActorId,
        string staffActorName,
        CancellationToken cancellationToken = default);
}
