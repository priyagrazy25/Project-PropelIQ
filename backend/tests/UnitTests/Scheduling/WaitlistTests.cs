using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Services;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using SharedKernel.Caching;

namespace UnitTests.Scheduling;

#region WaitlistService Enrollment Tests

public class WaitlistEnrollmentTests : IDisposable
{
    private readonly TestSchedulingDbContext _dbContext;
    private readonly Mock<ISlotNotificationService> _notificationMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<WaitlistService>> _loggerMock;
    private readonly WaitlistService _service;

    public WaitlistEnrollmentTests()
    {
        var options = new DbContextOptionsBuilder<TestSchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestSchedulingDbContext(options);
        _notificationMock = new Mock<ISlotNotificationService>();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<WaitlistService>>();
        _service = new WaitlistService(_dbContext, _notificationMock.Object, _cacheMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<Provider> SeedProviderAsync()
    {
        var provider = new Provider
        {
            Name = "Dr. Sarah Mitchell",
            Specialty = "Primary Care",
            Location = "Downtown Clinic",
            IsActive = true,
        };
        _dbContext.Providers.Add(provider);
        await _dbContext.SaveChangesAsync();
        return provider;
    }

    [Fact]
    public async Task EnrollAsync_ValidRequest_CreatesWaitlistEntry()
    {
        var provider = await SeedProviderAsync();
        var patientId = Guid.NewGuid();
        var start = DateTime.UtcNow.Date.AddDays(1);
        var end = DateTime.UtcNow.Date.AddDays(14);

        var result = await _service.EnrollAsync(patientId, provider.Id, start, end);

        Assert.True(result.IsSuccess);
        Assert.Equal(provider.Id, result.Value!.ProviderId);
        Assert.Equal("Dr. Sarah Mitchell", result.Value.ProviderName);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal(1, result.Value.Position);
    }

    [Fact]
    public async Task EnrollAsync_AssignsFifoPosition()
    {
        var provider = await SeedProviderAsync();
        var start = DateTime.UtcNow.Date.AddDays(1);
        var end = DateTime.UtcNow.Date.AddDays(14);

        var result1 = await _service.EnrollAsync(Guid.NewGuid(), provider.Id, start, end);
        var result2 = await _service.EnrollAsync(Guid.NewGuid(), provider.Id, start, end);

        Assert.Equal(1, result1.Value!.Position);
        Assert.Equal(2, result2.Value!.Position);
    }

    [Fact]
    public async Task EnrollAsync_ProviderNotFound_ReturnsFailure()
    {
        var result = await _service.EnrollAsync(Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.Date.AddDays(1), DateTime.UtcNow.Date.AddDays(14));

        Assert.False(result.IsSuccess);
        Assert.Contains("Provider not found", result.Error!);
    }

    [Fact]
    public async Task EnrollAsync_EndBeforeStart_ReturnsFailure()
    {
        var provider = await SeedProviderAsync();

        var result = await _service.EnrollAsync(Guid.NewGuid(), provider.Id,
            DateTime.UtcNow.Date.AddDays(14), DateTime.UtcNow.Date.AddDays(1));

        Assert.False(result.IsSuccess);
        Assert.Contains("End date must be after start date", result.Error!);
    }

    [Fact]
    public async Task EnrollAsync_PastDateRange_ReturnsFailure()
    {
        var provider = await SeedProviderAsync();

        var result = await _service.EnrollAsync(Guid.NewGuid(), provider.Id,
            DateTime.UtcNow.Date.AddDays(-14), DateTime.UtcNow.Date.AddDays(-1));

        Assert.False(result.IsSuccess);
        Assert.Contains("cannot be in the past", result.Error!);
    }

    [Fact]
    public async Task EnrollAsync_DuplicateActiveEntry_ReturnsFailure()
    {
        var provider = await SeedProviderAsync();
        var patientId = Guid.NewGuid();
        var start = DateTime.UtcNow.Date.AddDays(1);
        var end = DateTime.UtcNow.Date.AddDays(14);

        await _service.EnrollAsync(patientId, provider.Id, start, end);
        var result = await _service.EnrollAsync(patientId, provider.Id, start, end);

        Assert.False(result.IsSuccess);
        Assert.Contains("already on the waitlist", result.Error!);
    }
}

#endregion

#region WaitlistService GetEntries and Remove Tests

public class WaitlistManagementTests : IDisposable
{
    private readonly TestSchedulingDbContext _dbContext;
    private readonly Mock<ISlotNotificationService> _notificationMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<WaitlistService>> _loggerMock;
    private readonly WaitlistService _service;

    public WaitlistManagementTests()
    {
        var options = new DbContextOptionsBuilder<TestSchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestSchedulingDbContext(options);
        _notificationMock = new Mock<ISlotNotificationService>();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<WaitlistService>>();
        _service = new WaitlistService(_dbContext, _notificationMock.Object, _cacheMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<(Provider provider, Guid patientId, Waitlist entry)> SeedWaitlistEntryAsync(
        WaitlistStatus status = WaitlistStatus.Active)
    {
        var provider = new Provider
        {
            Name = "Dr. Sarah Mitchell",
            Specialty = "Primary Care",
            Location = "Downtown Clinic",
            IsActive = true,
        };
        _dbContext.Providers.Add(provider);

        var patientId = Guid.NewGuid();
        var entry = new Waitlist
        {
            PatientId = patientId,
            ProviderId = provider.Id,
            PreferredDateStart = DateTime.UtcNow.Date.AddDays(1),
            PreferredDateEnd = DateTime.UtcNow.Date.AddDays(14),
            Status = status,
            Position = 1,
        };
        _dbContext.Waitlists.Add(entry);
        await _dbContext.SaveChangesAsync();

        return (provider, patientId, entry);
    }

    [Fact]
    public async Task GetPatientEntriesAsync_ReturnsActiveEntries()
    {
        var (_, patientId, _) = await SeedWaitlistEntryAsync();

        var entries = await _service.GetPatientEntriesAsync(patientId);

        Assert.Single(entries);
        Assert.Equal("Dr. Sarah Mitchell", entries[0].ProviderName);
        Assert.Equal("Active", entries[0].Status);
    }

    [Fact]
    public async Task RemoveAsync_ActiveEntry_SetsCancelled()
    {
        var (_, patientId, entry) = await SeedWaitlistEntryAsync();

        var result = await _service.RemoveAsync(patientId, entry.Id);

        Assert.True(result.IsSuccess);
        var updated = await _dbContext.Waitlists.FindAsync(entry.Id);
        Assert.Equal(WaitlistStatus.Cancelled, updated!.Status);
    }

    [Fact]
    public async Task RemoveAsync_NotifiedEntry_SetsCancelled()
    {
        var (_, patientId, entry) = await SeedWaitlistEntryAsync(WaitlistStatus.Notified);

        var result = await _service.RemoveAsync(patientId, entry.Id);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RemoveAsync_ExpiredEntry_ReturnsFailure()
    {
        var (_, patientId, entry) = await SeedWaitlistEntryAsync(WaitlistStatus.Expired);

        var result = await _service.RemoveAsync(patientId, entry.Id);

        Assert.False(result.IsSuccess);
        Assert.Contains("Only active or notified", result.Error!);
    }

    [Fact]
    public async Task RemoveAsync_NotFound_ReturnsFailure()
    {
        var result = await _service.RemoveAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error!);
    }

    [Fact]
    public async Task RemoveAsync_WrongPatient_ReturnsFailure()
    {
        var (_, _, entry) = await SeedWaitlistEntryAsync();

        var result = await _service.RemoveAsync(Guid.NewGuid(), entry.Id);

        Assert.False(result.IsSuccess);
    }
}

#endregion

#region WaitlistService Slot Release Processing Tests

public class WaitlistSlotReleaseTests : IDisposable
{
    private readonly TestSchedulingDbContext _dbContext;
    private readonly Mock<ISlotNotificationService> _notificationMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<WaitlistService>> _loggerMock;
    private readonly WaitlistService _service;

    public WaitlistSlotReleaseTests()
    {
        var options = new DbContextOptionsBuilder<TestSchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestSchedulingDbContext(options);
        _notificationMock = new Mock<ISlotNotificationService>();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<WaitlistService>>();
        _service = new WaitlistService(_dbContext, _notificationMock.Object, _cacheMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<(Provider provider, AppointmentSlot slot, Waitlist entry)> SeedSlotReleaseDataAsync(
        DateTime? slotTime = null,
        DateTime? preferredStart = null,
        DateTime? preferredEnd = null)
    {
        var provider = new Provider
        {
            Name = "Dr. Sarah Mitchell",
            Specialty = "Primary Care",
            Location = "Downtown Clinic",
            IsActive = true,
        };
        _dbContext.Providers.Add(provider);

        var slot = new AppointmentSlot
        {
            ProviderId = provider.Id,
            StartTime = slotTime ?? DateTime.UtcNow.Date.AddDays(3).AddHours(10),
            DurationMinutes = 30,
            Status = SlotStatus.Available,
        };
        _dbContext.AppointmentSlots.Add(slot);

        var entry = new Waitlist
        {
            PatientId = Guid.NewGuid(),
            ProviderId = provider.Id,
            PreferredDateStart = preferredStart ?? DateTime.UtcNow.Date.AddDays(1),
            PreferredDateEnd = preferredEnd ?? DateTime.UtcNow.Date.AddDays(14),
            Status = WaitlistStatus.Active,
            Position = 1,
        };
        _dbContext.Waitlists.Add(entry);
        await _dbContext.SaveChangesAsync();

        return (provider, slot, entry);
    }

    [Fact]
    public async Task ProcessSlotRelease_MatchingEntry_NotifiesPatientViaSignalR()
    {
        var (provider, slot, entry) = await SeedSlotReleaseDataAsync();

        await _service.ProcessSlotReleaseForWaitlistAsync(slot.Id, provider.Id);

        _notificationMock.Verify(n => n.NotifyWaitlistAvailableAsync(
            entry.PatientId,
            entry.Id,
            provider.Id,
            "Dr. Sarah Mitchell",
            slot.Id,
            slot.StartTime,
            slot.StartTime.AddMinutes(30),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessSlotRelease_MatchingEntry_SetsStatusToNotified()
    {
        var (provider, slot, entry) = await SeedSlotReleaseDataAsync();

        await _service.ProcessSlotReleaseForWaitlistAsync(slot.Id, provider.Id);

        var updated = await _dbContext.Waitlists.FindAsync(entry.Id);
        Assert.Equal(WaitlistStatus.Notified, updated!.Status);
        Assert.NotNull(updated.NotifiedAt);
    }

    [Fact]
    public async Task ProcessSlotRelease_MultipleMatches_NotifiesAllInFifoOrder()
    {
        var provider = new Provider
        {
            Name = "Dr. Sarah Mitchell",
            Specialty = "Primary Care",
            Location = "Downtown Clinic",
            IsActive = true,
        };
        _dbContext.Providers.Add(provider);

        var slot = new AppointmentSlot
        {
            ProviderId = provider.Id,
            StartTime = DateTime.UtcNow.Date.AddDays(3).AddHours(10),
            DurationMinutes = 30,
            Status = SlotStatus.Available,
        };
        _dbContext.AppointmentSlots.Add(slot);

        var entry1 = new Waitlist
        {
            PatientId = Guid.NewGuid(),
            ProviderId = provider.Id,
            PreferredDateStart = DateTime.UtcNow.Date.AddDays(1),
            PreferredDateEnd = DateTime.UtcNow.Date.AddDays(14),
            Status = WaitlistStatus.Active,
            Position = 1,
        };
        var entry2 = new Waitlist
        {
            PatientId = Guid.NewGuid(),
            ProviderId = provider.Id,
            PreferredDateStart = DateTime.UtcNow.Date.AddDays(1),
            PreferredDateEnd = DateTime.UtcNow.Date.AddDays(14),
            Status = WaitlistStatus.Active,
            Position = 2,
        };
        _dbContext.Waitlists.AddRange(entry1, entry2);
        await _dbContext.SaveChangesAsync();

        await _service.ProcessSlotReleaseForWaitlistAsync(slot.Id, provider.Id);

        _notificationMock.Verify(n => n.NotifyWaitlistAvailableAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessSlotRelease_NoMatchingDateRange_DoesNotNotify()
    {
        // Slot is on day 3, but waitlist prefers days 20-30
        var (provider, slot, _) = await SeedSlotReleaseDataAsync(
            preferredStart: DateTime.UtcNow.Date.AddDays(20),
            preferredEnd: DateTime.UtcNow.Date.AddDays(30));

        await _service.ProcessSlotReleaseForWaitlistAsync(slot.Id, provider.Id);

        _notificationMock.Verify(n => n.NotifyWaitlistAvailableAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessSlotRelease_DifferentProvider_DoesNotNotify()
    {
        var (_, slot, _) = await SeedSlotReleaseDataAsync();

        // Process with wrong provider ID
        await _service.ProcessSlotReleaseForWaitlistAsync(slot.Id, Guid.NewGuid());

        _notificationMock.Verify(n => n.NotifyWaitlistAvailableAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessSlotRelease_SlotNotAvailable_DoesNotNotify()
    {
        var (provider, slot, _) = await SeedSlotReleaseDataAsync();
        slot.Status = SlotStatus.Booked;
        await _dbContext.SaveChangesAsync();

        await _service.ProcessSlotReleaseForWaitlistAsync(slot.Id, provider.Id);

        _notificationMock.Verify(n => n.NotifyWaitlistAvailableAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<DateTime>(), It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion

#region WaitlistService Stale Entry Expiry Tests

public class WaitlistExpiryTests : IDisposable
{
    private readonly TestSchedulingDbContext _dbContext;
    private readonly Mock<ISlotNotificationService> _notificationMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<WaitlistService>> _loggerMock;
    private readonly WaitlistService _service;

    public WaitlistExpiryTests()
    {
        var options = new DbContextOptionsBuilder<TestSchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestSchedulingDbContext(options);
        _notificationMock = new Mock<ISlotNotificationService>();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<WaitlistService>>();
        _service = new WaitlistService(_dbContext, _notificationMock.Object, _cacheMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ExpireStaleEntries_PastDateRange_SetsExpired()
    {
        var provider = new Provider
        {
            Name = "Dr. Sarah Mitchell",
            Specialty = "Primary Care",
            Location = "Downtown Clinic",
            IsActive = true,
        };
        _dbContext.Providers.Add(provider);

        var staleEntry = new Waitlist
        {
            PatientId = Guid.NewGuid(),
            ProviderId = provider.Id,
            PreferredDateStart = DateTime.UtcNow.Date.AddDays(-14),
            PreferredDateEnd = DateTime.UtcNow.Date.AddDays(-1),
            Status = WaitlistStatus.Active,
            Position = 1,
        };
        _dbContext.Waitlists.Add(staleEntry);
        await _dbContext.SaveChangesAsync();

        await _service.ExpireStaleEntriesAsync();

        var updated = await _dbContext.Waitlists.FindAsync(staleEntry.Id);
        Assert.Equal(WaitlistStatus.Expired, updated!.Status);
    }

    [Fact]
    public async Task ExpireStaleEntries_FutureDateRange_RemainsActive()
    {
        var provider = new Provider
        {
            Name = "Dr. Sarah Mitchell",
            Specialty = "Primary Care",
            Location = "Downtown Clinic",
            IsActive = true,
        };
        _dbContext.Providers.Add(provider);

        var activeEntry = new Waitlist
        {
            PatientId = Guid.NewGuid(),
            ProviderId = provider.Id,
            PreferredDateStart = DateTime.UtcNow.Date.AddDays(1),
            PreferredDateEnd = DateTime.UtcNow.Date.AddDays(14),
            Status = WaitlistStatus.Active,
            Position = 1,
        };
        _dbContext.Waitlists.Add(activeEntry);
        await _dbContext.SaveChangesAsync();

        await _service.ExpireStaleEntriesAsync();

        var updated = await _dbContext.Waitlists.FindAsync(activeEntry.Id);
        Assert.Equal(WaitlistStatus.Active, updated!.Status);
    }

    [Fact]
    public async Task ExpireStaleEntries_NotifiedWithPastDate_AlsoExpires()
    {
        var provider = new Provider
        {
            Name = "Dr. Sarah Mitchell",
            Specialty = "Primary Care",
            Location = "Downtown Clinic",
            IsActive = true,
        };
        _dbContext.Providers.Add(provider);

        var notifiedEntry = new Waitlist
        {
            PatientId = Guid.NewGuid(),
            ProviderId = provider.Id,
            PreferredDateStart = DateTime.UtcNow.Date.AddDays(-14),
            PreferredDateEnd = DateTime.UtcNow.Date.AddDays(-1),
            Status = WaitlistStatus.Notified,
            NotifiedAt = DateTime.UtcNow.AddDays(-2),
            Position = 1,
        };
        _dbContext.Waitlists.Add(notifiedEntry);
        await _dbContext.SaveChangesAsync();

        await _service.ExpireStaleEntriesAsync();

        var updated = await _dbContext.Waitlists.FindAsync(notifiedEntry.Id);
        Assert.Equal(WaitlistStatus.Expired, updated!.Status);
    }

    [Fact]
    public async Task ExpireStaleEntries_AlreadyExpired_NoDoubleProcess()
    {
        var provider = new Provider
        {
            Name = "Dr. Sarah Mitchell",
            Specialty = "Primary Care",
            Location = "Downtown Clinic",
            IsActive = true,
        };
        _dbContext.Providers.Add(provider);

        var expiredEntry = new Waitlist
        {
            PatientId = Guid.NewGuid(),
            ProviderId = provider.Id,
            PreferredDateStart = DateTime.UtcNow.Date.AddDays(-14),
            PreferredDateEnd = DateTime.UtcNow.Date.AddDays(-1),
            Status = WaitlistStatus.Expired,
            Position = 1,
        };
        _dbContext.Waitlists.Add(expiredEntry);
        await _dbContext.SaveChangesAsync();

        await _service.ExpireStaleEntriesAsync();

        // Should not throw or re-process
        var updated = await _dbContext.Waitlists.FindAsync(expiredEntry.Id);
        Assert.Equal(WaitlistStatus.Expired, updated!.Status);
    }
}

#endregion
