using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Commands.CancelAppointment;
using Scheduling.Application.Commands.RegisterSwapPreference;
using Scheduling.Application.Services;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using SharedKernel.Caching;

namespace UnitTests.Scheduling;

public class RegisterSwapPreferenceCommandHandlerTests : IDisposable
{
    private readonly TestSchedulingDbContext _dbContext;
    private readonly Mock<ILogger<RegisterSwapPreferenceCommandHandler>> _loggerMock;
    private readonly RegisterSwapPreferenceCommandHandler _handler;

    public RegisterSwapPreferenceCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestSchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestSchedulingDbContext(options);
        _loggerMock = new Mock<ILogger<RegisterSwapPreferenceCommandHandler>>();
        _handler = new RegisterSwapPreferenceCommandHandler(_dbContext, _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<(Provider provider, AppointmentSlot bookedSlot, AppointmentSlot desiredSlot, Appointment appointment)> SeedDataAsync()
    {
        var provider = new Provider
        {
            Name = "Dr. Sarah Mitchell",
            Specialty = "Primary Care",
            Location = "Downtown Clinic",
            IsActive = true,
        };

        var bookedSlot = new AppointmentSlot
        {
            ProviderId = provider.Id,
            StartTime = DateTime.UtcNow.Date.AddHours(10),
            DurationMinutes = 30,
            Status = SlotStatus.Booked,
        };

        var desiredSlot = new AppointmentSlot
        {
            ProviderId = provider.Id,
            StartTime = DateTime.UtcNow.Date.AddHours(9),
            DurationMinutes = 30,
            Status = SlotStatus.Booked,
        };

        var appointment = new Appointment
        {
            PatientId = Guid.NewGuid(),
            ProviderId = provider.Id,
            SlotId = bookedSlot.Id,
            AppointmentDateTime = bookedSlot.StartTime,
            DurationMinutes = 30,
            Status = AppointmentStatus.Confirmed,
        };

        _dbContext.Providers.Add(provider);
        _dbContext.AppointmentSlots.AddRange(bookedSlot, desiredSlot);
        _dbContext.Appointments.Add(appointment);
        await _dbContext.SaveChangesAsync();

        return (provider, bookedSlot, desiredSlot, appointment);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_RegistersSwapPreference()
    {
        var (provider, bookedSlot, desiredSlot, appointment) = await SeedDataAsync();

        var command = new RegisterSwapPreferenceCommand(
            appointment.PatientId, appointment.Id, desiredSlot.Id);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(appointment.Id, result.Value!.AppointmentId);
        Assert.Equal(desiredSlot.Id, result.Value.DesiredSlotId);
        Assert.Equal("Pending", result.Value.Status);
        Assert.Equal(1, result.Value.Priority);
    }

    [Fact]
    public async Task HandleAsync_FIFOPriority_IncrementsForSameSlot()
    {
        var (provider, bookedSlot, desiredSlot, appointment) = await SeedDataAsync();

        // Register first swap
        var command1 = new RegisterSwapPreferenceCommand(
            appointment.PatientId, appointment.Id, desiredSlot.Id);
        await _handler.HandleAsync(command1);

        // Create second patient + appointment
        var appointment2 = new Appointment
        {
            PatientId = Guid.NewGuid(),
            ProviderId = provider.Id,
            SlotId = null,
            AppointmentDateTime = DateTime.UtcNow.Date.AddHours(11),
            Status = AppointmentStatus.Confirmed,
        };
        _dbContext.Appointments.Add(appointment2);
        await _dbContext.SaveChangesAsync();

        var command2 = new RegisterSwapPreferenceCommand(
            appointment2.PatientId, appointment2.Id, desiredSlot.Id);
        var result = await _handler.HandleAsync(command2);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Priority);
    }

    [Fact]
    public async Task HandleAsync_AppointmentNotFound_ReturnsFailure()
    {
        var command = new RegisterSwapPreferenceCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("Appointment not found", result.Error!);
    }

    [Fact]
    public async Task HandleAsync_CancelledAppointment_ReturnsFailure()
    {
        var (_, _, desiredSlot, appointment) = await SeedDataAsync();
        appointment.Status = AppointmentStatus.Cancelled;
        await _dbContext.SaveChangesAsync();

        var command = new RegisterSwapPreferenceCommand(
            appointment.PatientId, appointment.Id, desiredSlot.Id);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("cancelled", result.Error!);
    }

    [Fact]
    public async Task HandleAsync_DesiredSlotAvailable_ReturnsFailure()
    {
        var (provider, bookedSlot, desiredSlot, appointment) = await SeedDataAsync();
        desiredSlot.Status = SlotStatus.Available;
        await _dbContext.SaveChangesAsync();

        var command = new RegisterSwapPreferenceCommand(
            appointment.PatientId, appointment.Id, desiredSlot.Id);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("already available", result.Error!);
    }

    [Fact]
    public async Task HandleAsync_DuplicatePending_ReturnsFailure()
    {
        var (_, _, desiredSlot, appointment) = await SeedDataAsync();

        var command = new RegisterSwapPreferenceCommand(
            appointment.PatientId, appointment.Id, desiredSlot.Id);

        await _handler.HandleAsync(command);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("already exists", result.Error!);
    }

    [Fact]
    public async Task HandleAsync_WrongPatient_ReturnsFailure()
    {
        var (_, _, desiredSlot, appointment) = await SeedDataAsync();

        var command = new RegisterSwapPreferenceCommand(
            Guid.NewGuid(), appointment.Id, desiredSlot.Id);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("does not belong", result.Error!);
    }

    [Fact]
    public async Task HandleAsync_SlotNotFoundForProvider_ReturnsFailure()
    {
        var (_, _, _, appointment) = await SeedDataAsync();

        var command = new RegisterSwapPreferenceCommand(
            appointment.PatientId, appointment.Id, Guid.NewGuid());

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("Desired slot not found", result.Error!);
    }
}

public class SwapEngineServiceTests : IDisposable
{
    private readonly TestSchedulingDbContext _dbContext;
    private readonly Mock<ISlotNotificationService> _notificationMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<SwapEngineService>> _loggerMock;
    private readonly WaitlistSlotReleasedChannel _waitlistChannel;
    private readonly SwapEngineService _engine;

    public SwapEngineServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestSchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestSchedulingDbContext(options);
        _notificationMock = new Mock<ISlotNotificationService>();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<SwapEngineService>>();
        _waitlistChannel = new WaitlistSlotReleasedChannel();
        _engine = new SwapEngineService(_dbContext, _notificationMock.Object, _waitlistChannel, _cacheMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<(Provider provider, AppointmentSlot originalSlot, AppointmentSlot desiredSlot, Appointment appointment, PreferredSlotSwap swap)> SeedSwapDataAsync()
    {
        var provider = new Provider
        {
            Name = "Dr. Sarah Mitchell",
            Specialty = "Primary Care",
            Location = "Downtown Clinic",
            IsActive = true,
        };

        var originalSlot = new AppointmentSlot
        {
            ProviderId = provider.Id,
            StartTime = DateTime.UtcNow.Date.AddHours(14),
            DurationMinutes = 30,
            Status = SlotStatus.Booked,
        };

        var desiredSlot = new AppointmentSlot
        {
            ProviderId = provider.Id,
            StartTime = DateTime.UtcNow.Date.AddHours(9),
            DurationMinutes = 30,
            Status = SlotStatus.Available, // Just released
        };

        var appointment = new Appointment
        {
            PatientId = Guid.NewGuid(),
            ProviderId = provider.Id,
            SlotId = originalSlot.Id,
            AppointmentDateTime = originalSlot.StartTime,
            DurationMinutes = 30,
            Status = AppointmentStatus.Confirmed,
        };

        var swap = new PreferredSlotSwap
        {
            RequestingPatientId = appointment.PatientId,
            OriginalAppointmentId = appointment.Id,
            DesiredSlotId = desiredSlot.Id,
            Status = SwapStatus.Pending,
            Priority = 1,
            RequestedAt = DateTime.UtcNow.AddMinutes(-10),
        };

        _dbContext.Providers.Add(provider);
        _dbContext.AppointmentSlots.AddRange(originalSlot, desiredSlot);
        _dbContext.Appointments.Add(appointment);
        _dbContext.PreferredSlotSwaps.Add(swap);
        await _dbContext.SaveChangesAsync();

        return (provider, originalSlot, desiredSlot, appointment, swap);
    }

    [Fact]
    public async Task ProcessSlotRelease_ExecutesSwap_MovesAppointmentToDesiredSlot()
    {
        var (provider, originalSlot, desiredSlot, appointment, swap) = await SeedSwapDataAsync();

        var result = await _engine.ProcessSlotReleaseAsync(desiredSlot.Id);

        Assert.NotNull(result);
        Assert.Equal(swap.Id, result.SwapId);
        Assert.Equal(appointment.PatientId, result.PatientId);

        var updatedAppointment = await _dbContext.Appointments.FindAsync(appointment.Id);
        Assert.Equal(desiredSlot.Id, updatedAppointment!.SlotId);
        Assert.Equal(desiredSlot.StartTime, updatedAppointment.AppointmentDateTime);
    }

    [Fact]
    public async Task ProcessSlotRelease_ReleasesOriginalSlot()
    {
        var (_, originalSlot, desiredSlot, _, _) = await SeedSwapDataAsync();

        await _engine.ProcessSlotReleaseAsync(desiredSlot.Id);

        var updatedOriginal = await _dbContext.AppointmentSlots.FindAsync(originalSlot.Id);
        Assert.Equal(SlotStatus.Available, updatedOriginal!.Status);
    }

    [Fact]
    public async Task ProcessSlotRelease_MarksDesiredSlotBooked()
    {
        var (_, _, desiredSlot, _, _) = await SeedSwapDataAsync();

        await _engine.ProcessSlotReleaseAsync(desiredSlot.Id);

        var updatedDesired = await _dbContext.AppointmentSlots.FindAsync(desiredSlot.Id);
        Assert.Equal(SlotStatus.Booked, updatedDesired!.Status);
    }

    [Fact]
    public async Task ProcessSlotRelease_MarksSwapExecuted()
    {
        var (_, _, desiredSlot, _, swap) = await SeedSwapDataAsync();

        await _engine.ProcessSlotReleaseAsync(desiredSlot.Id);

        var updatedSwap = await _dbContext.PreferredSlotSwaps.FindAsync(swap.Id);
        Assert.Equal(SwapStatus.Executed, updatedSwap!.Status);
        Assert.NotNull(updatedSwap.ProcessedAt);
    }

    [Fact]
    public async Task ProcessSlotRelease_NotifiesPatientViaSignalR()
    {
        var (provider, _, desiredSlot, _, swap) = await SeedSwapDataAsync();

        await _engine.ProcessSlotReleaseAsync(desiredSlot.Id);

        _notificationMock.Verify(
            n => n.NotifySwapExecutedAsync(
                swap.RequestingPatientId,
                provider.Id,
                provider.Name,
                desiredSlot.StartTime,
                desiredSlot.StartTime.AddMinutes(desiredSlot.DurationMinutes),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessSlotRelease_NotifiesOriginalSlotReleased()
    {
        var (provider, originalSlot, desiredSlot, _, _) = await SeedSwapDataAsync();

        await _engine.ProcessSlotReleaseAsync(desiredSlot.Id);

        _notificationMock.Verify(
            n => n.NotifySlotReleasedAsync(provider.Id, originalSlot.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessSlotRelease_InvalidatesCaches()
    {
        var (_, _, desiredSlot, _, _) = await SeedSwapDataAsync();

        await _engine.ProcessSlotReleaseAsync(desiredSlot.Id);

        _cacheMock.Verify(
            c => c.RemoveByPrefixAsync("provider-search:", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessSlotRelease_NoSwapRequest_ReturnsNull()
    {
        var result = await _engine.ProcessSlotReleaseAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task ProcessSlotRelease_FIFOOrder_FirstRegisteredWins()
    {
        var (provider, originalSlot, desiredSlot, appointment, swap1) = await SeedSwapDataAsync();

        // Add second swap request with higher priority (later)
        var appointment2 = new Appointment
        {
            PatientId = Guid.NewGuid(),
            ProviderId = provider.Id,
            SlotId = null,
            AppointmentDateTime = DateTime.UtcNow.Date.AddHours(15),
            Status = AppointmentStatus.Confirmed,
        };
        var swap2 = new PreferredSlotSwap
        {
            RequestingPatientId = appointment2.PatientId,
            OriginalAppointmentId = appointment2.Id,
            DesiredSlotId = desiredSlot.Id,
            Status = SwapStatus.Pending,
            Priority = 2,
            RequestedAt = DateTime.UtcNow,
        };
        _dbContext.Appointments.Add(appointment2);
        _dbContext.PreferredSlotSwaps.Add(swap2);
        await _dbContext.SaveChangesAsync();

        var result = await _engine.ProcessSlotReleaseAsync(desiredSlot.Id);

        // First-registered (priority 1) wins
        Assert.NotNull(result);
        Assert.Equal(swap1.Id, result.SwapId);
        Assert.Equal(appointment.PatientId, result.PatientId);

        // Second swap remains pending
        var remainingSwap = await _dbContext.PreferredSlotSwaps.FindAsync(swap2.Id);
        Assert.Equal(SwapStatus.Pending, remainingSwap!.Status);
    }

    [Fact]
    public async Task ProcessSlotRelease_CancelledAppointment_ExpiresSwap()
    {
        var (_, _, desiredSlot, appointment, swap) = await SeedSwapDataAsync();
        appointment.Status = AppointmentStatus.Cancelled;
        await _dbContext.SaveChangesAsync();

        var result = await _engine.ProcessSlotReleaseAsync(desiredSlot.Id);

        Assert.Null(result);
        var updatedSwap = await _dbContext.PreferredSlotSwaps.FindAsync(swap.Id);
        Assert.Equal(SwapStatus.Expired, updatedSwap!.Status);
    }

    [Fact]
    public async Task ExpireSwapsForAppointment_ExpiresPendingSwaps()
    {
        var (_, _, _, appointment, swap) = await SeedSwapDataAsync();

        await _engine.ExpireSwapsForAppointmentAsync(appointment.Id);

        var updatedSwap = await _dbContext.PreferredSlotSwaps.FindAsync(swap.Id);
        Assert.Equal(SwapStatus.Expired, updatedSwap!.Status);
        Assert.NotNull(updatedSwap.ProcessedAt);
    }

    [Fact]
    public async Task ExpireSwapsForAppointment_IgnoresExecutedSwaps()
    {
        var (_, _, _, appointment, swap) = await SeedSwapDataAsync();
        swap.Status = SwapStatus.Executed;
        await _dbContext.SaveChangesAsync();

        await _engine.ExpireSwapsForAppointmentAsync(appointment.Id);

        var updatedSwap = await _dbContext.PreferredSlotSwaps.FindAsync(swap.Id);
        Assert.Equal(SwapStatus.Executed, updatedSwap!.Status);
    }
}

public class CancelAppointmentCommandHandlerTests : IDisposable
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

    public CancelAppointmentCommandHandlerTests()
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

    private async Task<(Provider provider, AppointmentSlot slot, Appointment appointment)> SeedCancelDataAsync()
    {
        var provider = new Provider
        {
            Name = "Dr. Sarah Mitchell",
            Specialty = "Primary Care",
            Location = "Downtown Clinic",
            IsActive = true,
        };

        var slot = new AppointmentSlot
        {
            ProviderId = provider.Id,
            StartTime = DateTime.UtcNow.Date.AddHours(10),
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
            Status = AppointmentStatus.Confirmed,
        };

        _dbContext.Providers.Add(provider);
        _dbContext.AppointmentSlots.Add(slot);
        _dbContext.Appointments.Add(appointment);
        await _dbContext.SaveChangesAsync();

        return (provider, slot, appointment);
    }

    [Fact]
    public async Task HandleAsync_CancelsAppointment()
    {
        var (_, _, appointment) = await SeedCancelDataAsync();

        var command = new CancelAppointmentCommand(
            appointment.PatientId, appointment.Id, "No longer needed");

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        var updated = await _dbContext.Appointments.FindAsync(appointment.Id);
        Assert.Equal(AppointmentStatus.Cancelled, updated!.Status);
        Assert.Equal("No longer needed", updated.CancellationReason);
    }

    [Fact]
    public async Task HandleAsync_ReleasesSlot()
    {
        var (_, slot, appointment) = await SeedCancelDataAsync();

        var command = new CancelAppointmentCommand(
            appointment.PatientId, appointment.Id, null);

        await _handler.HandleAsync(command);

        var updatedSlot = await _dbContext.AppointmentSlots.FindAsync(slot.Id);
        Assert.Equal(SlotStatus.Available, updatedSlot!.Status);
    }

    [Fact]
    public async Task HandleAsync_ExpiresSwapPreferences()
    {
        var (_, _, appointment) = await SeedCancelDataAsync();

        var command = new CancelAppointmentCommand(
            appointment.PatientId, appointment.Id, null);

        await _handler.HandleAsync(command);

        _swapEngineMock.Verify(
            s => s.ExpireSwapsForAppointmentAsync(appointment.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NotifiesSlotReleased()
    {
        var (provider, slot, appointment) = await SeedCancelDataAsync();

        var command = new CancelAppointmentCommand(
            appointment.PatientId, appointment.Id, null);

        await _handler.HandleAsync(command);

        _notificationMock.Verify(
            n => n.NotifySlotReleasedAsync(provider.Id, slot.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PublishesSlotReleasedEvent()
    {
        var (provider, slot, appointment) = await SeedCancelDataAsync();

        var command = new CancelAppointmentCommand(
            appointment.PatientId, appointment.Id, null);

        await _handler.HandleAsync(command);

        Assert.True(_channel.Reader.TryRead(out var slotEvent));
        Assert.Equal(slot.Id, slotEvent.SlotId);
        Assert.Equal(provider.Id, slotEvent.ProviderId);
    }

    [Fact]
    public async Task HandleAsync_AppointmentNotFound_ReturnsFailure()
    {
        var command = new CancelAppointmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), null);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error!);
    }

    [Fact]
    public async Task HandleAsync_AlreadyCancelled_ReturnsSuccess_Idempotent()
    {
        var (_, _, appointment) = await SeedCancelDataAsync();
        appointment.Status = AppointmentStatus.Cancelled;
        await _dbContext.SaveChangesAsync();

        var command = new CancelAppointmentCommand(
            appointment.PatientId, appointment.Id, null);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_CompletedAppointment_ReturnsFailure()
    {
        var (_, _, appointment) = await SeedCancelDataAsync();
        appointment.Status = AppointmentStatus.Completed;
        await _dbContext.SaveChangesAsync();

        var command = new CancelAppointmentCommand(
            appointment.PatientId, appointment.Id, null);

        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("completed or in-progress", result.Error!);
    }

    [Fact]
    public async Task HandleAsync_InvalidatesCaches()
    {
        var (_, _, appointment) = await SeedCancelDataAsync();

        var command = new CancelAppointmentCommand(
            appointment.PatientId, appointment.Id, null);

        await _handler.HandleAsync(command);

        _cacheMock.Verify(
            c => c.RemoveByPrefixAsync("provider-search:", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
