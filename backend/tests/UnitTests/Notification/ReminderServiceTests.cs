using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Notification.Application.Abstractions;
using Notification.Application.Channels;
using Notification.Application.Services;
using Notification.Domain.Entities;

namespace UnitTests.Notification;

public sealed class ReminderServiceTests
{
    [Fact]
    public async Task EvaluateAndSendRemindersAsync_UsesEscalatedWindowsOnlyForHighRiskAppointments()
    {
        var appointmentQueryMock = new Mock<IReminderAppointmentQuery>();
        var deliveryLogRepoMock = new Mock<IReminderDeliveryLogRepository>();
        var smsChannelMock = new Mock<ISmsChannel>();
        var emailChannelMock = new Mock<IEmailChannel>();
        var loggerMock = new Mock<ILogger<ReminderService>>();

        var highRiskAppointment = new ReminderAppointmentInfo(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(24),
            "High Risk Patient",
            "high@example.com",
            "+15550000001",
            "Provider One",
            82);

        var mediumRiskAppointment = new ReminderAppointmentInfo(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(24),
            "Medium Risk Patient",
            "medium@example.com",
            "+15550000002",
            "Provider Two",
            55);

        appointmentQueryMock
            .Setup(q => q.GetUpcomingAppointmentsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReminderAppointmentInfo> { highRiskAppointment, mediumRiskAppointment });

        deliveryLogRepoMock
            .Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        deliveryLogRepoMock
            .Setup(r => r.GetTodayEmailCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        smsChannelMock
            .Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChannelResult(true));
        emailChannelMock
            .Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChannelResult(true));

        var capturedLogs = new List<ReminderDeliveryLog>();
        deliveryLogRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ReminderDeliveryLog>(), It.IsAny<CancellationToken>()))
            .Callback<ReminderDeliveryLog, CancellationToken>((log, _) => capturedLogs.Add(log))
            .Returns(Task.CompletedTask);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SendGrid:DailyLimit"] = "100"
            })
            .Build();

        var service = new ReminderService(
            appointmentQueryMock.Object,
            deliveryLogRepoMock.Object,
            smsChannelMock.Object,
            emailChannelMock.Object,
            configuration,
            loggerMock.Object);

        await service.EvaluateAndSendRemindersAsync();

        Assert.Contains(capturedLogs, log =>
            log.AppointmentId == highRiskAppointment.AppointmentId &&
            log.ReminderWindow == "48h");
        Assert.Contains(capturedLogs, log =>
            log.AppointmentId == highRiskAppointment.AppointmentId &&
            log.ReminderWindow == "12h");

        Assert.DoesNotContain(capturedLogs, log =>
            log.AppointmentId == mediumRiskAppointment.AppointmentId &&
            log.ReminderWindow == "48h");
        Assert.DoesNotContain(capturedLogs, log =>
            log.AppointmentId == mediumRiskAppointment.AppointmentId &&
            log.ReminderWindow == "12h");

        Assert.Contains(capturedLogs, log =>
            log.AppointmentId == mediumRiskAppointment.AppointmentId &&
            log.ReminderWindow == "72h");
        Assert.Contains(capturedLogs, log =>
            log.AppointmentId == mediumRiskAppointment.AppointmentId &&
            log.ReminderWindow == "24h");
        Assert.Contains(capturedLogs, log =>
            log.AppointmentId == mediumRiskAppointment.AppointmentId &&
            log.ReminderWindow == "2h");
    }
}
