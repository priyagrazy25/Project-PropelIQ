using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Commands.BookAppointment;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using SharedKernel.Caching;

namespace UnitTests.Scheduling;

public class BookAppointmentCommandHandlerTests : IDisposable
{
    private readonly TestSchedulingDbContext _dbContext;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ISlotNotificationService> _notificationMock;
    private readonly Mock<ICalendarSyncService> _calendarSyncMock;
    private readonly Mock<IBookingConfirmationNotifier> _confirmationNotifierMock;
    private readonly Mock<INoShowRiskService> _riskServiceMock;
    private readonly Mock<ILogger<BookAppointmentCommandHandler>> _loggerMock;
    private readonly BookAppointmentCommandHandler _handler;

    public BookAppointmentCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestSchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestSchedulingDbContext(options);
        _cacheMock = new Mock<ICacheService>();
        _notificationMock = new Mock<ISlotNotificationService>();
        _calendarSyncMock = new Mock<ICalendarSyncService>();
        _confirmationNotifierMock = new Mock<IBookingConfirmationNotifier>();
        _riskServiceMock = new Mock<INoShowRiskService>();
        _riskServiceMock.Setup(r => r.CalculateAndStoreRiskAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(50); // Default medium risk
        _loggerMock = new Mock<ILogger<BookAppointmentCommandHandler>>();
        _handler = new BookAppointmentCommandHandler(
            _dbContext, _cacheMock.Object, _notificationMock.Object, _calendarSyncMock.Object, 
            _confirmationNotifierMock.Object, _riskServiceMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<(Provider provider, AppointmentSlot slot)> SeedProviderWithSlotAsync()
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
            Status = SlotStatus.Available,
        };

        _dbContext.Providers.Add(provider);
        _dbContext.AppointmentSlots.Add(slot);
        await _dbContext.SaveChangesAsync();

        return (provider, slot);
    }

    [Fact]
    public async Task HandleAsync_BooksAppointment_WhenSlotAvailable()
    {
        // Arrange
        var (provider, slot) = await SeedProviderWithSlotAsync();
        var command = new BookAppointmentCommand(
            Guid.NewGuid(), provider.Id, slot.Id, Guid.NewGuid().ToString());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(provider.Id, result.Value.ProviderId);
        Assert.Equal(provider.Name, result.Value.ProviderName);
        Assert.Equal(provider.Specialty, result.Value.Specialty);
        Assert.Equal("Confirmed", result.Value.Status);
        Assert.Equal(slot.StartTime, result.Value.SlotStartTime);
    }

    [Fact]
    public async Task HandleAsync_SetsSlotToBooked_WhenSuccessful()
    {
        // Arrange
        var (provider, slot) = await SeedProviderWithSlotAsync();
        var command = new BookAppointmentCommand(
            Guid.NewGuid(), provider.Id, slot.Id, Guid.NewGuid().ToString());

        // Act
        await _handler.HandleAsync(command);

        // Assert
        var updatedSlot = await _dbContext.AppointmentSlots.FindAsync(slot.Id);
        Assert.NotNull(updatedSlot);
        Assert.Equal(SlotStatus.Booked, updatedSlot.Status);
    }

    [Fact]
    public async Task HandleAsync_CreatesAppointmentInDb_WhenSuccessful()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var (provider, slot) = await SeedProviderWithSlotAsync();
        var command = new BookAppointmentCommand(
            patientId, provider.Id, slot.Id, Guid.NewGuid().ToString());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        var appointment = await _dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == result.Value!.AppointmentId);
        Assert.NotNull(appointment);
        Assert.Equal(patientId, appointment.PatientId);
        Assert.Equal(provider.Id, appointment.ProviderId);
        Assert.Equal(slot.Id, appointment.SlotId);
        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenSlotAlreadyBooked()
    {
        // Arrange
        var (provider, slot) = await SeedProviderWithSlotAsync();
        slot.Status = SlotStatus.Booked;
        await _dbContext.SaveChangesAsync();

        var command = new BookAppointmentCommand(
            Guid.NewGuid(), provider.Id, slot.Id, Guid.NewGuid().ToString());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CONFLICT", result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_WhenProviderNotFound()
    {
        // Arrange
        var command = new BookAppointmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid().ToString());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Provider not found or inactive.", result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_WhenProviderInactive()
    {
        // Arrange
        var provider = new Provider
        {
            Name = "Dr. Inactive",
            Specialty = "Test",
            IsActive = false,
        };
        _dbContext.Providers.Add(provider);
        await _dbContext.SaveChangesAsync();

        var command = new BookAppointmentCommand(
            Guid.NewGuid(), provider.Id, Guid.NewGuid(), Guid.NewGuid().ToString());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Provider not found or inactive.", result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_WhenSlotNotFound()
    {
        // Arrange
        var provider = new Provider
        {
            Name = "Dr. Valid",
            Specialty = "Test",
            IsActive = true,
        };
        _dbContext.Providers.Add(provider);
        await _dbContext.SaveChangesAsync();

        var command = new BookAppointmentCommand(
            Guid.NewGuid(), provider.Id, Guid.NewGuid(), Guid.NewGuid().ToString());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Slot not found.", result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCachedResult_WhenIdempotencyKeyMatches()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var cachedResult = new BookAppointmentResult(
            Guid.NewGuid(), Guid.NewGuid(), "Dr. Cached", "Test", "Clinic",
            DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30), "Confirmed");

        _cacheMock.Setup(c => c.GetAsync<BookAppointmentResult>(
                $"booking-idempotency:{idempotencyKey}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        var command = new BookAppointmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), idempotencyKey);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(cachedResult.AppointmentId, result.Value!.AppointmentId);
        Assert.Equal("Dr. Cached", result.Value.ProviderName);
    }

    [Fact]
    public async Task HandleAsync_CachesResult_OnSuccessfulBooking()
    {
        // Arrange
        var (provider, slot) = await SeedProviderWithSlotAsync();
        var idempotencyKey = Guid.NewGuid().ToString();
        var command = new BookAppointmentCommand(
            Guid.NewGuid(), provider.Id, slot.Id, idempotencyKey);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _cacheMock.Verify(c => c.SetAsync(
            $"booking-idempotency:{idempotencyKey}",
            It.IsAny<BookAppointmentResult>(),
            CacheTier.L2,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_InvalidatesSearchCache_OnSuccessfulBooking()
    {
        // Arrange
        var (provider, slot) = await SeedProviderWithSlotAsync();
        var command = new BookAppointmentCommand(
            Guid.NewGuid(), provider.Id, slot.Id, Guid.NewGuid().ToString());

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _cacheMock.Verify(c => c.RemoveByPrefixAsync(
            "provider-search:", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_BroadcastsSlotBooked_OnSuccessfulBooking()
    {
        // Arrange
        var (provider, slot) = await SeedProviderWithSlotAsync();
        var command = new BookAppointmentCommand(
            Guid.NewGuid(), provider.Id, slot.Id, Guid.NewGuid().ToString());

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _notificationMock.Verify(n => n.NotifySlotBookedAsync(
            provider.Id, slot.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DoesNotBroadcast_WhenSlotConflict()
    {
        // Arrange
        var (provider, slot) = await SeedProviderWithSlotAsync();
        slot.Status = SlotStatus.Booked;
        await _dbContext.SaveChangesAsync();

        var command = new BookAppointmentCommand(
            Guid.NewGuid(), provider.Id, slot.Id, Guid.NewGuid().ToString());

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _notificationMock.Verify(n => n.NotifySlotBookedAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DoesNotInvalidateCache_WhenSlotConflict()
    {
        // Arrange
        var (provider, slot) = await SeedProviderWithSlotAsync();
        slot.Status = SlotStatus.Booked;
        await _dbContext.SaveChangesAsync();

        var command = new BookAppointmentCommand(
            Guid.NewGuid(), provider.Id, slot.Id, Guid.NewGuid().ToString());

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _cacheMock.Verify(c => c.RemoveByPrefixAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
