using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Commands.WalkInBooking;
using Scheduling.Application.DTOs;
using Scheduling.Application.Services;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using SharedKernel.Domain;

namespace UnitTests.Scheduling;

public class ArrivalMarkingTests : IDisposable
{
    private readonly TestSchedulingDbContext _dbContext;
    private readonly Mock<IPatientLookupService> _patientLookupMock;
    private readonly Mock<ISlotNotificationService> _notificationMock;
    private readonly Mock<ILogger<QueueService>> _loggerMock;
    private readonly QueueService _service;

    private readonly Guid _staffId = Guid.NewGuid();
    private const string StaffName = "Nurse Johnson";

    public ArrivalMarkingTests()
    {
        var options = new DbContextOptionsBuilder<TestSchedulingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestSchedulingDbContext(options);
        _patientLookupMock = new Mock<IPatientLookupService>();
        _notificationMock = new Mock<ISlotNotificationService>();
        _loggerMock = new Mock<ILogger<QueueService>>();

        _service = new QueueService(
            _dbContext,
            _patientLookupMock.Object,
            _notificationMock.Object,
            _loggerMock.Object);

        _patientLookupMock
            .Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PatientSearchResult(Guid.NewGuid(), Guid.NewGuid(), "Jane Doe", "jane@example.com", null, null, "MRN-TEST1234"));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<Appointment> SeedAppointmentAsync(
        AppointmentStatus status = AppointmentStatus.Confirmed,
        DateTime? appointmentDateTime = null)
    {
        var provider = new Provider
        {
            Name = "Dr. Smith",
            Specialty = "General",
            Location = "Main Clinic",
            IsActive = true,
        };
        _dbContext.Providers.Add(provider);

        var appointment = new Appointment
        {
            PatientId = Guid.NewGuid(),
            ProviderId = provider.Id,
            AppointmentDateTime = appointmentDateTime ?? DateTime.UtcNow.Date.AddHours(10),
            DurationMinutes = 30,
            Status = status,
        };
        _dbContext.Appointments.Add(appointment);
        await _dbContext.SaveChangesAsync();

        return appointment;
    }

    [Fact]
    public async Task MarkArrivedAsync_SetsStatusToArrived_WhenAppointmentIsValidSameDay()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync(AppointmentStatus.Confirmed);

        // Act
        var result = await _service.MarkArrivedAsync(appointment.Id, _staffId, StaffName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Waiting", result.Value!.Status); // Arrived maps to "Waiting" queue status
        Assert.Equal(appointment.Id, result.Value.Id);

        var updated = await _dbContext.Appointments.FindAsync(appointment.Id);
        Assert.Equal(AppointmentStatus.Arrived, updated!.Status);
        Assert.NotNull(updated.ArrivedAt);
    }

    [Fact]
    public async Task MarkArrivedAsync_CreatesAuditLog_WithStaffActorAndTimestamp()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync(AppointmentStatus.Scheduled);

        // Act
        var result = await _service.MarkArrivedAsync(appointment.Id, _staffId, StaffName);

        // Assert
        Assert.True(result.IsSuccess);
        var auditLog = await _dbContext.AuditLogs
            .FirstOrDefaultAsync(a => a.ResourceId == appointment.Id.ToString());

        Assert.NotNull(auditLog);
        Assert.Equal(_staffId, auditLog!.ActorId);
        Assert.Equal(StaffName, auditLog.ActorName);
        Assert.Equal("PatientArrived", auditLog.Action);
        Assert.Equal("Appointment", auditLog.Resource);
    }

    [Fact]
    public async Task MarkArrivedAsync_BroadcastsViaSignalR()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync(AppointmentStatus.Confirmed);

        // Act
        await _service.MarkArrivedAsync(appointment.Id, _staffId, StaffName);

        // Assert
        _notificationMock.Verify(
            n => n.NotifyPatientArrivedAsync(appointment.Id, appointment.PatientId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkArrivedAsync_ReturnsBadRequest_WhenAppointmentIsCancelled()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync(AppointmentStatus.Cancelled);

        // Act
        var result = await _service.MarkArrivedAsync(appointment.Id, _staffId, StaffName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("cancelled", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MarkArrivedAsync_ReturnsBadRequest_WhenAlreadyArrived()
    {
        // Arrange
        var appointment = await SeedAppointmentAsync(AppointmentStatus.Arrived);

        // Act
        var result = await _service.MarkArrivedAsync(appointment.Id, _staffId, StaffName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("already arrived", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MarkArrivedAsync_ReturnsBadRequest_WhenAppointmentIsFutureDate()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var appointment = await SeedAppointmentAsync(AppointmentStatus.Confirmed, futureDate);

        // Act
        var result = await _service.MarkArrivedAsync(appointment.Id, _staffId, StaffName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("same-day", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MarkArrivedAsync_ReturnsNotFound_WhenAppointmentDoesNotExist()
    {
        // Act
        var result = await _service.MarkArrivedAsync(Guid.NewGuid(), _staffId, StaffName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MarkArrivedAsync_ReturnsBadRequest_WhenAppointmentIsPastDate()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.Date.AddDays(-1).AddHours(10);
        var appointment = await SeedAppointmentAsync(AppointmentStatus.Confirmed, pastDate);

        // Act
        var result = await _service.MarkArrivedAsync(appointment.Id, _staffId, StaffName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("same-day", result.Error!, StringComparison.OrdinalIgnoreCase);
    }
}
