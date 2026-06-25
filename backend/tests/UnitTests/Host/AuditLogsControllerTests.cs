using Host.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SharedKernel.Audit;

namespace UnitTests.Host;

public sealed class AuditLogsControllerTests
{
    [Fact]
    public async Task GetAuditLogs_ForwardsFilterAndPagingToService()
    {
        var serviceMock = new Mock<IAuditService>();
        AuditLogFilter? capturedFilter = null;
        var response = new PaginatedAuditResponse
        {
            Items =
            [
                new AuditEntry
                {
                    ActorName = "Admin User",
                    Action = "View",
                    Resource = "Patient",
                    Timestamp = DateTime.UtcNow
                }
            ],
            TotalCount = 1,
            Page = 2,
            PageSize = 25
        };

        serviceMock
            .Setup(s => s.GetAuditLogsAsync(It.IsAny<AuditLogFilter>(), 2, 25, It.IsAny<CancellationToken>()))
            .Callback<AuditLogFilter, int, int, CancellationToken>((filter, _, _, _) => capturedFilter = filter)
            .ReturnsAsync(response);

        var controller = new AuditLogsController(serviceMock.Object);

        var result = await controller.GetAuditLogs(
            startDate: new DateTime(2026, 1, 1),
            endDate: new DateTime(2026, 1, 31),
            actorName: "Admin",
            action: "View",
            resource: "Patient",
            resourceId: "abc-1",
            ipAddress: "127.0.0.1",
            page: 2,
            pageSize: 25);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PaginatedAuditResponse>(ok.Value);
        Assert.Single(payload.Items);

        Assert.NotNull(capturedFilter);
        Assert.Equal("Admin", capturedFilter!.ActorName);
        Assert.Equal("View", capturedFilter.Action);
        Assert.Equal("Patient", capturedFilter.Resource);
        Assert.Equal("abc-1", capturedFilter.ResourceId);
        Assert.Equal("127.0.0.1", capturedFilter.IpAddress);
    }

    [Fact]
    public async Task ExportAuditLogs_ReturnsCsvFile()
    {
        var serviceMock = new Mock<IAuditService>();
        serviceMock
            .Setup(s => s.GetAuditLogsAsync(It.IsAny<AuditLogFilter>(), 1, 10000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedAuditResponse
            {
                Items =
                [
                    new AuditEntry
                    {
                        ActorId = Guid.NewGuid(),
                        ActorName = "System Admin",
                        Action = "Delete",
                        Resource = "User",
                        ResourceId = "user-1",
                        IpAddress = "10.0.0.1",
                        CorrelationId = "corr-123",
                        Timestamp = DateTime.UtcNow
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10000
            });

        var controller = new AuditLogsController(serviceMock.Object);

        var result = await controller.ExportAuditLogs(null, null, "System Admin", "Delete", "User");

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", file.ContentType);
        Assert.StartsWith("audit-logs-", file.FileDownloadName);
        Assert.EndsWith(".csv", file.FileDownloadName);

        var csv = System.Text.Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("Timestamp,ActorId,ActorName,Action,Resource,ResourceId,IpAddress,CorrelationId", csv);
        Assert.Contains("System Admin", csv);
        Assert.Contains("Delete", csv);
    }

    [Fact]
    public async Task GetAuditStats_ReturnsExpectedBreakdowns()
    {
        var now = DateTime.UtcNow;
        var items = new[]
        {
            new AuditEntry { ActorName = "A1", Action = "Create", Resource = "Patient", Timestamp = now },
            new AuditEntry { ActorName = "A2", Action = "Create", Resource = "Patient", Timestamp = now },
            new AuditEntry { ActorName = "A1", Action = "Delete", Resource = "User", Timestamp = now }
        };

        var serviceMock = new Mock<IAuditService>();
        serviceMock
            .Setup(s => s.GetAuditLogsAsync(It.IsAny<AuditLogFilter>(), 1, 10000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedAuditResponse
            {
                Items = items,
                TotalCount = items.Length,
                Page = 1,
                PageSize = 10000
            });

        var controller = new AuditLogsController(serviceMock.Object);

        var result = await controller.GetAuditStats(now.AddDays(-7), now);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<AuditStatsResponse>(ok.Value);

        Assert.Equal(3, payload.TotalRecords);
        Assert.Equal(2, payload.UniqueActors);
        Assert.Equal(2, payload.ActionBreakdown["Create"]);
        Assert.Equal(1, payload.ActionBreakdown["Delete"]);
        Assert.Equal(2, payload.ResourceBreakdown["Patient"]);
        Assert.Equal(1, payload.ResourceBreakdown["User"]);
    }
}
