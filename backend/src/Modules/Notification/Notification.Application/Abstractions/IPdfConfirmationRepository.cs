using Notification.Domain.Entities;

namespace Notification.Application.Abstractions;

public interface IPdfConfirmationRepository
{
    Task<PdfConfirmationRecord?> GetByAppointmentIdAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PdfConfirmationRecord record, CancellationToken cancellationToken = default);

    Task UpdateAsync(PdfConfirmationRecord record, CancellationToken cancellationToken = default);
}
