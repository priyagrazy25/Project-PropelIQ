using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions;
using Notification.Domain.Entities;
using Notification.Infrastructure.Data;

namespace Notification.Infrastructure.Repositories;

public sealed class PdfConfirmationRepository : IPdfConfirmationRepository
{
    private readonly NotificationDbContext _dbContext;

    public PdfConfirmationRepository(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PdfConfirmationRecord?> GetByAppointmentIdAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PdfConfirmationRecords
            .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId && !r.IsDeleted,
                cancellationToken);
    }

    public async Task AddAsync(PdfConfirmationRecord record, CancellationToken cancellationToken = default)
    {
        _dbContext.PdfConfirmationRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PdfConfirmationRecord record, CancellationToken cancellationToken = default)
    {
        _dbContext.PdfConfirmationRecords.Update(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
