using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Scheduling.API.Controllers;
using Scheduling.Application.Abstractions;

namespace UnitTests.Scheduling;

public sealed class NoShowRiskControllerTests
{
    [Fact]
    public async Task GetRiskAssessments_ReturnsBadRequest_WhenStartDateAfterEndDate()
    {
        var controller = CreateController();

        var result = await controller.GetRiskAssessments(
            new DateTime(2026, 6, 21),
            new DateTime(2026, 6, 20));

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetRiskAssessments_ReturnsBadRequest_WhenDateRangeExceeds30Days()
    {
        var controller = CreateController();

        var result = await controller.GetRiskAssessments(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 2, 5));

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetRiskAssessments_ReturnsOk_WithAssessments()
    {
        var assessments = new List<AppointmentRiskAssessment>
        {
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Patient A",
                DateTime.UtcNow.AddHours(2),
                "Dr. Smith",
                84,
                "High",
                new[] { "prior no-show" })
        };

        var riskServiceMock = new Mock<INoShowRiskService>();
        riskServiceMock
            .Setup(s => s.GetRiskAssessmentsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assessments);

        var controller = CreateController(riskServiceMock: riskServiceMock);

        var result = await controller.GetRiskAssessments(
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(1));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<AppointmentRiskAssessment>>(okResult.Value);
        Assert.Single(payload);
        Assert.Equal("High", payload[0].RiskLevel);
    }

    [Fact]
    public async Task RollbackModel_ReturnsBadRequest_WhenTargetVersionEmpty()
    {
        var controller = CreateController();

        var result = await controller.RollbackModel(new RollbackRequest(string.Empty));

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    private static NoShowRiskController CreateController(
        Mock<INoShowRiskService>? riskServiceMock = null)
    {
        riskServiceMock ??= new Mock<INoShowRiskService>();
        var dbContextMock = new Mock<ISchedulingDbContext>();
        var loggerMock = new Mock<ILogger<NoShowRiskController>>();

        return new NoShowRiskController(
            riskServiceMock.Object,
            dbContextMock.Object,
            loggerMock.Object);
    }
}
