using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Commands.BookAppointment;
using Scheduling.Application.Commands.CancelAppointment;
using Scheduling.Application.Commands.RescheduleAppointment;
using Scheduling.Application.Services;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using SharedKernel.Caching;

namespace UnitTests.Scheduling;

#region Cancel Idempotency & Walk-In Guard Tests

public class CancelIdempotencyTests : IDisposable
{
    private readonly TestSchedulingDbContext _dbContext;
    private readonly Mock<ISwapEngineService> _swapEngineMock;
    private readonly Mock<ISlotNotificationService> _notificationMock;
    private readonly Mock<ICalendarSyncService> _calendarSyncMock;
    private readonly SlotReleasedChannel _channel;
    private readonly WaitlistSlotReleasedChannel _waitlistChannel;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<CancelAppointmentCommandHandler>> _loggerMock;
    private readonly CancelAppointmentCommandHandler _handler;

    public CancelIdempotencyTests()
    {
        var options = new DbContextOptionsBuilder<TestSchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestSchedulingDbContext(options);
        _swapEngineMock = new Mock<ISwapEngineService>();
        _notificationMock = new Mock<ISlotNotificationService>();
        _calendarSyncMock = new Mock<ICalendarSyncService>();
        _channel = new SlotReleasedChannel();
        _waitlistChannel = new WaitlistSlotReleasedChannel();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<CancelAppointmentCommandHandler>>();
        _handler = new CancelAppointmentCommandHandler(
            _dbContext, _swapEngineMock.Object, _notificationMock.Object,
            _calendarSyncMock.Object, _channel, _waitlistChannel,
            _cacheMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<(Provider provider, AppointmentSlot slot, Appointment appointment)> SeedAsync(
        AppointmentType type = AppointmentType.InPerson,
        AppointmentStatus status = AppointmentStatus.Confirmed)
    {
        var provider = new Provider
        {
            Name = "Dr. Test Provider",
            Specialty = "General",
            Location = "Clinic A",
            IsActive = true,
        };

        var slot = new AppointmentSlot
        {
            ProviderId = provider.Id,
            StartTime = DateTime.UtcNow.Date.AddDays(1).AddHours(10),
            DurationMinutes = 30,
            Status = SlotStatus.Booked,
        };

        var appointment = new Appointment
        {
            PatientId = Guid.NewGuid(),
            ProviderId = provider.Id,
            SlotId = slot.Id,
            AppointmentDateTime = slot.StartTime,
            DurationMinutes = 30,
            Status = status,
            Type = type,
        };

        _dbContext.Providers.Add(provider);
        _dbContext.AppointmentSlots.Add(slot);
        _dbContext.Appointments.Add(appointment);
        await _dbContext.SaveChangesAsync();

        return (provider, slot, appointment);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_ReturnsSuccess_Idempotent()
    {
        var (_, _, appointment) = await SeedAsync(status: AppointmentStatus.Cancelled);

        var command = new CancelAppointmentCommand(
            appointment.PatientId, appointment.Id, null);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_DoesNotReleaseSlot()
    {
        var (_, slot, appointment) = await SeedAsync(status: AppointmentStatus.Cancelled);

        var command = new CancelAppointmentCommand(
            appointment.PatientId, appointment.Id, null);

        await _handler.HandleAsync(command);

        // Slot should remain booked — no re-release
        var updatedSlot = await _dbContext.AppointmentSlots.FindAsync(slot.Id);
        Assert.Equal(SlotStatus.Booked, updatedSlot!.Status);
    }

    [Fact]
    public async Task Cancel_CalledTwice_BothReturnSuccess()
    {
        var (_, _, appointment) = await SeedAsync();

        var command = new CancelAppointmentCommand(
            appointment.PatientId, appointment.Id, "First call");

        var result1 = await _handler.HandleAsync(command);
        Assert.True(result1.IsSuccess);

        var result2 = await _handler.HandleAsync(command);
        Assert.True(result2.IsSuccess);
    }

    [Fact]
    public async Task Cancel_InvokesCalendarSync()
    {
        var (_, _, appointment) = await SeedAsync();

        var command = new CancelAppointmentCommand(
            appointment.PatientId, appointment.Id, null);

        await _handler.HandleAsync(command);

        _calendarSyncMock.Verify(
            c => c.SyncAppointmentCancelledAsync(appointment.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Cancel_WalkIn_ByPatient_ReturnsFailure()
    {
        var (_, _, appointment) = await SeedAsync(type: AppointmentType.WalkIn);

        var command = new CancelAppointmentCommand(
            appointment.PatientId, appointment.Id, null, IsStaffAction: false);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("staff", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cancel_WalkIn_ByStaff_Succeeds()
    {
        var (_, _, appointment) = await SeedAsync(type: AppointmentType.WalkIn);

        var command = new CancelAppointmentCommand(
            appointment.PatientId, appointment.Id, null, IsStaffAction: true);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        var updated = await _dbContext.Appointments.FindAsync(appointment.Id);
        Assert.Equal(AppointmentStatus.Cancelled, updated!.Status);
    }

    [Fact]
    public async Task Cancel_Staff_CanCancelAnyPatientAppointment()
    {
        var (_, _, appointment) = await SeedAsync();
        var differentUserId = Guid.NewGuid(); // not the appointment's patient

        var command = new CancelAppointmentCommand(
            differentUserId, appointment.Id, null, IsStaffAction: true);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Cancel_Patient_CannotCancelOthersAppointment()
    {
        var (_, _, appointment) = await SeedAsync();
        var differentPatientId = Guid.NewGuid();

        var command = new CancelAppointmentCommand(
            differentPatientId, appointment.Id, null, IsStaffAction: false);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("does not belong", result.Error!);
    }
}

#endregion

#region Reschedule Tests

public class RescheduleAppointmentCommandHandlerTests : IDisposable
{
    private readonly TestSchedulingDbContext _dbContext;
    private readonly Mock<ISwapEngineService> _swapEngineMock;
    private readonly Mock<ISlotNotificationService> _notificationMock;
    private readonly Mock<ICalendarSyncService> _calendarSyncMock;
    private readonly SlotReleasedChannel _channel;
    private readonly WaitlistSlotReleasedChannel _waitlistChannel;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<RescheduleAppointmentCommandHandler>> _loggerMock;
    private readonly RescheduleAppointmentCommandHandler _handler;

    public RescheduleAppointmentCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestSchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestSchedulingDbContext(options);
        _swapEngineMock = new Mock<ISwapEngineService>();
        _notificationMock = new Mock<ISlotNotificationService>();
        _calendarSyncMock = new Mock<ICalendarSyncService>();
        _channel = new SlotReleasedChannel();
        _waitlistChannel = new WaitlistSlotReleasedChannel();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<RescheduleAppointmentCommandHandler>>();
        _handler = new RescheduleAppointmentCommandHandler(
            _dbContext, _swapEngineMock.Object, _notificationMock.Object,
            _calendarSyncMock.Object, _channel, _waitlistChannel,
            _cacheMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<(Provider provider, AppointmentSlot oldSlot, AppointmentSlot newSlot, Appointment appointment)> SeedAsync()
    {
        var provider = new Provider
        {
            Name = "Dr. Sarah Mitchell",
            Specialty = "Primary Care",
            Location = "Downtown Clinic",
            IsActive = true,
        };

        var oldSlot = new AppointmentSlot
        {
            ProviderId = provider.Id,
            StartTime = DateTime.UtcNow.Date.AddDays(1).AddHours(10),
            DurationMinutes = 30,
            Status = SlotStatus.Booked,
        };

        var newSlot = new AppointmentSlot
        {
            ProviderId = provider.Id,
            StartTime = DateTime.UtcNow.Date.AddDays(2).AddHours(14),
            DurationMinutes = 30,
            Status = SlotStatus.Available,
        };

        var appointment = new Appointment
        {
            PatientId = Guid.NewGuid(),
            ProviderId = provider.Id,
            SlotId = oldSlot.Id,
            AppointmentDateTime = oldSlot.StartTime,
            DurationMinutes = 30,
            Status = AppointmentStatus.Confirmed,
        };

        _dbContext.Providers.Add(provider);
        _dbContext.AppointmentSlots.AddRange(oldSlot, newSlot);
        _dbContext.Appointments.Add(appointment);
        await _dbContext.SaveChangesAsync();

        return (provider, oldSlot, newSlot, appointment);
    }

    [Fact]
    public async Task Reschedule_CreatesNewAppointment()
    {
        var (provider, _, newSlot, appointment) = await SeedAsync();

        var command = new RescheduleAppointmentCommand(
            appointment.PatientId, appointment.Id, newSlot.Id, Guid.NewGuid().ToString());

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(provider.Name, result.Value.ProviderName);
        Assert.Equal(newSlot.StartTime, result.Value.SlotStartTime);
        Assert.Equal("Confirmed", result.Value.Status);
    }

    [Fact]
    public async Task Reschedule_SetsOldAppointmentToRescheduled()
    {
        var (_, _, newSlot, appointment) = await SeedAsync();

        var command = new RescheduleAppointmentCommand(
            appointment.PatientId, appointment.Id, newSlot.Id, Guid.NewGuid().ToString());

        await _handler.HandleAsync(command);

        var updated = await _dbContext.Appointments.FindAsync(appointment.Id);
        Assert.Equal(AppointmentStatus.Rescheduled, updated!.Status);
    }

    [Fact]
    public async Task Reschedule_ReleasesOldSlot()
    {
        var (_, oldSlot, newSlot, appointment) = await SeedAsync();

        var command = new RescheduleAppointmentCommand(
            appointment.PatientId, appointment.Id, newSlot.Id, Guid.NewGuid().ToString());

        await _handler.HandleAsync(command);

        var updatedOldSlot = await _dbContext.AppointmentSlots.FindAsync(oldSlot.Id);
        Assert.Equal(SlotStatus.Available, updatedOldSlot!.Status);
    }

    [Fact]
    public async Task Reschedule_BooksNewSlot()
    {
        var (_, _, newSlot, appointment) = await SeedAsync();

        var command = new RescheduleAppointmentCommand(
            appointment.PatientId, appointment.Id, newSlot.Id, Guid.NewGuid().ToString());

        await _handler.HandleAsync(command);

        var updatedNewSlot = await _dbContext.AppointmentSlots.FindAsync(newSlot.Id);
        Assert.Equal(SlotStatus.Booked, updatedNewSlot!.Status);
    }

    [Fact]
    public async Task Reschedule_PublishesSlotReleasedToSwapChannel()
    {
        var (provider, oldSlot, newSlot, appointment) = await SeedAsync();

        var command = new RescheduleAppointmentCommand(
            appointment.PatientId, appointment.Id, newSlot.Id, Guid.NewGuid().ToString());

        await _handler.HandleAsync(command);

        Assert.True(_channel.Reader.TryRead(out var slotEvent));
        Assert.Equal(oldSlot.Id, slotEvent.SlotId);
        Assert.Equal(provider.Id, slotEvent.ProviderId);
    }

    [Fact]
    public async Task Reschedule_PublishesSlotReleasedToWaitlistChannel()
    {
        var (provider, oldSlot, newSlot, appointment) = await SeedAsync();

        var command = new RescheduleAppointmentCommand(
            appointment.PatientId, appointment.Id, newSlot.Id, Guid.NewGuid().ToString());

        await _handler.HandleAsync(command);

        Assert.True(_waitlistChannel.Reader.TryRead(out var slotEvent));
        Assert.Equal(oldSlot.Id, slotEvent.SlotId);
        Assert.Equal(provider.Id, slotEvent.ProviderId);
    }

    [Fact]
    public async Task Reschedule_ExpiresSwapPreferencesForOldAppointment()
    {
        var (_, _, newSlot, appointment) = await SeedAsync();

        var command = new RescheduleAppointmentCommand(
            appointment.PatientId, appointment.Id, newSlot.Id, Guid.NewGuid().ToString());

        await _handler.HandleAsync(command);

        _swapEngineMock.Verify(
            s => s.ExpireSwapsForAppointmentAsync(appointment.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Reschedule_InvokesCalendarSync()
    {
        var (_, _, newSlot, appointment) = await SeedAsync();

        var command = new RescheduleAppointmentCommand(
            appointment.PatientId, appointment.Id, newSlot.Id, Guid.NewGuid().ToString());

        var result = await _handler.HandleAsync(command);

        _calendarSyncMock.Verify(
            c => c.SyncAppointmentRescheduledAsync(
                appointment.Id, result.Value!.AppointmentId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Reschedule_NewSlotUnavailable_ReturnsConflict()
    {
        var (_, _, newSlot, appointment) = await SeedAsync();
        newSlot.Status = SlotStatus.Booked;
        await _dbContext.SaveChangesAsync();

        var command = new RescheduleAppointmentCommand(
            appointment.PatientId, appointment.Id, newSlot.Id, Guid.NewGuid().ToString());

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal("CONFLICT", result.Error);
    }

    [Fact]
    public async Task Reschedule_OldAppointmentNotFound_ReturnsFailure()
    {
        var command = new RescheduleAppointmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid().ToString());

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error!);
    }

    [Fact]
    public async Task Reschedule_OldAppointmentAlreadyCancelled_ReturnsFailure()
    {
        var (_, _, newSlot, appointment) = await SeedAsync();
        appointment.Status = AppointmentStatus.Cancelled;
        await _dbContext.SaveChangesAsync();

        var command = new RescheduleAppointmentCommand(
            appointment.PatientId, appointment.Id, newSlot.Id, Guid.NewGuid().ToString());

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("Cancelled", result.Error!);
    }

    [Fact]
    public async Task Reschedule_NewSlotNotFound_ReturnsFailure()
    {
        var (_, _, _, appointment) = await SeedAsync();

        var command = new RescheduleAppointmentCommand(
            appointment.PatientId, appointment.Id, Guid.NewGuid(), Guid.NewGuid().ToString());

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error!);
    }

    [Fact]
    public async Task Reschedule_DifferentPatient_ReturnsFailure()
    {
        var (_, _, newSlot, appointment) = await SeedAsync();

        var command = new RescheduleAppointmentCommand(
            Guid.NewGuid(), appointment.Id, newSlot.Id, Guid.NewGuid().ToString());

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("does not belong", result.Error!);
    }

    [Fact]
    public async Task Reschedule_Idempotent_ReturnsCachedResult()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var cachedResult = new BookAppointmentResult(
            Guid.NewGuid(), Guid.NewGuid(), "Dr. Cached", "General",
            "Clinic", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30), "Confirmed");

        _cacheMock
            .Setup(c => c.GetAsync<BookAppointmentResult>(
                $"reschedule-idempotency:{idempotencyKey}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        var command = new RescheduleAppointmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), idempotencyKey);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(cachedResult.AppointmentId, result.Value!.AppointmentId);
    }

    [Fact]
    public async Task Reschedule_InvalidatesCaches()
    {
        var (_, _, newSlot, appointment) = await SeedAsync();

        var command = new RescheduleAppointmentCommand(
            appointment.PatientId, appointment.Id, newSlot.Id, Guid.NewGuid().ToString());

        await _handler.HandleAsync(command);

        _cacheMock.Verify(
            c => c.RemoveByPrefixAsync("provider-search:", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Reschedule_NotifiesSlotBookedForNewSlot()
    {
        var (provider, _, newSlot, appointment) = await SeedAsync();

        var command = new RescheduleAppointmentCommand(
            appointment.PatientId, appointment.Id, newSlot.Id, Guid.NewGuid().ToString());

        await _handler.HandleAsync(command);

        _notificationMock.Verify(
            n => n.NotifySlotBookedAsync(provider.Id, newSlot.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Reschedule_NotifiesSlotReleasedForOldSlot()
    {
        var (provider, oldSlot, newSlot, appointment) = await SeedAsync();

        var command = new RescheduleAppointmentCommand(
            appointment.PatientId, appointment.Id, newSlot.Id, Guid.NewGuid().ToString());

        await _handler.HandleAsync(command);

        _notificationMock.Verify(
            n => n.NotifySlotReleasedAsync(provider.Id, oldSlot.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Reschedule_SetsCancellationReasonOnOld()
    {
        var (_, _, newSlot, appointment) = await SeedAsync();

        var command = new RescheduleAppointmentCommand(
            appointment.PatientId, appointment.Id, newSlot.Id,
            Guid.NewGuid().ToString(), "Changed my mind");

        await _handler.HandleAsync(command);

        var updated = await _dbContext.Appointments.FindAsync(appointment.Id);
        Assert.Equal("Changed my mind", updated!.CancellationReason);
    }
}

#endregion
