using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using SharedKernel.Caching;
using SharedKernel.Domain;

namespace Scheduling.Application.Services;

public sealed class WaitlistService : IWaitlistService
{
    private readonly ISchedulingDbContext _dbContext;
    private readonly ISlotNotificationService _notificationService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<WaitlistService> _logger;

    public WaitlistService(
        ISchedulingDbContext dbContext,
        ISlotNotificationService notificationService,
        ICacheService cacheService,
        ILogger<WaitlistService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<WaitlistEntryResult>> EnrollAsync(
        Guid patientId,
        Guid providerId,
        DateTime preferredDateStart,
        DateTime preferredDateEnd,
        CancellationToken cancellationToken = default)
    {
        // Validate provider exists
        var provider = await _dbContext.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == providerId && !p.IsDeleted, cancellationToken);

        if (provider is null)
        {
            return Result<WaitlistEntryResult>.Failure("Provider not found.");
        }

        // Validate date range
        if (preferredDateEnd < preferredDateStart)
        {
            return Result<WaitlistEntryResult>.Failure("End date must be after start date.");
        }

        if (preferredDateEnd < DateTime.UtcNow.Date)
        {
            return Result<WaitlistEntryResult>.Failure("Preferred date range cannot be in the past.");
        }

        // Check for duplicate active entry for same patient + provider
        var existingEntry = await _dbContext.Waitlists
            .AnyAsync(
                w => w.PatientId == patientId
                     && w.ProviderId == providerId
                     && w.Status == WaitlistStatus.Active
                     && !w.IsDeleted,
                cancellationToken);

        if (existingEntry)
        {
            return Result<WaitlistEntryResult>.Failure("You are already on the waitlist for this provider.");
        }

        // Assign FIFO position (max + 1 for provider)
        var maxPosition = await _dbContext.Waitlists
            .Where(w => w.ProviderId == providerId && w.Status == WaitlistStatus.Active && !w.IsDeleted)
            .MaxAsync(w => (int?)w.Position, cancellationToken) ?? 0;

        var entry = new Waitlist
        {
            PatientId = patientId,
            ProviderId = providerId,
            PreferredDateStart = preferredDateStart,
            PreferredDateEnd = preferredDateEnd,
            Status = WaitlistStatus.Active,
            Position = maxPosition + 1,
        };

        _dbContext.Waitlists.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Patient {PatientId} enrolled on waitlist for provider {ProviderId} at position {Position}",
            patientId, providerId, entry.Position);

        return Result<WaitlistEntryResult>.Success(new WaitlistEntryResult(
            entry.Id,
            provider.Id,
            provider.Name,
            provider.Specialty,
            entry.PreferredDateStart,
            entry.PreferredDateEnd,
            entry.Status.ToString(),
            entry.Position,
            entry.CreatedAt,
            entry.NotifiedAt));
    }

    public async Task<IReadOnlyList<WaitlistEntryResult>> GetPatientEntriesAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Waitlists
            .AsNoTracking()
            .Include(w => w.Provider)
            .Where(w => w.PatientId == patientId && !w.IsDeleted)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WaitlistEntryResult(
                w.Id,
                w.ProviderId,
                w.Provider.Name,
                w.Provider.Specialty,
                w.PreferredDateStart,
                w.PreferredDateEnd,
                w.Status.ToString(),
                w.Position,
                w.CreatedAt,
                w.NotifiedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<bool>> RemoveAsync(
        Guid patientId,
        Guid waitlistId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _dbContext.Waitlists
            .FirstOrDefaultAsync(
                w => w.Id == waitlistId
                     && w.PatientId == patientId
                     && !w.IsDeleted,
                cancellationToken);

        if (entry is null)
        {
            return Result<bool>.Failure("Waitlist entry not found.");
        }

        if (entry.Status != WaitlistStatus.Active && entry.Status != WaitlistStatus.Notified)
        {
            return Result<bool>.Failure("Only active or notified waitlist entries can be removed.");
        }

        entry.Status = WaitlistStatus.Cancelled;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Patient {PatientId} removed from waitlist entry {WaitlistId}",
            patientId, waitlistId);

        return Result<bool>.Success(true);
    }

    public async Task ProcessSlotReleaseForWaitlistAsync(
        Guid releasedSlotId,
        Guid providerId,
        CancellationToken cancellationToken = default)
    {
        // Get the released slot details
        var slot = await _dbContext.AppointmentSlots
            .AsNoTracking()
            .Include(s => s.Provider)
            .FirstOrDefaultAsync(s => s.Id == releasedSlotId && !s.IsDeleted, cancellationToken);

        if (slot is null || slot.Status != SlotStatus.Available)
        {
            _logger.LogDebug("Released slot {SlotId} not found or not available for waitlist processing", releasedSlotId);
            return;
        }

        var slotDate = slot.StartTime.Date;
        var slotEndTime = slot.StartTime.AddMinutes(slot.DurationMinutes);

        // Find active waitlist entries matching this provider whose preferred window includes the slot date
        var matchedEntries = await _dbContext.Waitlists
            .Where(w => w.ProviderId == providerId
                        && w.Status == WaitlistStatus.Active
                        && w.PreferredDateStart.Date <= slotDate
                        && w.PreferredDateEnd.Date >= slotDate
                        && !w.IsDeleted)
            .OrderBy(w => w.Position)
            .ToListAsync(cancellationToken);

        if (matchedEntries.Count == 0)
        {
            _logger.LogDebug("No waitlist matches for released slot {SlotId} on provider {ProviderId}", releasedSlotId, providerId);
            return;
        }

        // Notify all matched patients (first to book wins per AC-4/NFR-015)
        foreach (var entry in matchedEntries)
        {
            entry.Status = WaitlistStatus.Notified;
            entry.NotifiedAt = DateTime.UtcNow;

            await _notificationService.NotifyWaitlistAvailableAsync(
                entry.PatientId,
                entry.Id,
                providerId,
                slot.Provider.Name,
                releasedSlotId,
                slot.StartTime,
                slotEndTime,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notified {Count} waitlisted patients for released slot {SlotId} on provider {ProviderId}",
            matchedEntries.Count, releasedSlotId, providerId);
    }

    public async Task ExpireStaleEntriesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow.Date;

        var staleEntries = await _dbContext.Waitlists
            .Where(w => (w.Status == WaitlistStatus.Active || w.Status == WaitlistStatus.Notified)
                        && w.PreferredDateEnd.Date < now
                        && !w.IsDeleted)
            .ToListAsync(cancellationToken);

        if (staleEntries.Count == 0) return;

        foreach (var entry in staleEntries)
        {
            entry.Status = WaitlistStatus.Expired;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Expired {Count} stale waitlist entries", staleEntries.Count);
    }
}
