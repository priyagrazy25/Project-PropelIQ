using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Commands.WalkInBooking;
using Scheduling.API.Controllers;

namespace UnitTests.Scheduling;

public class WalkInPatientSearchTests
{
    private readonly Mock<IPatientLookupService> _lookupMock = new();
    private readonly Mock<IWalkInService> _walkInServiceMock = new();
    private readonly Mock<ILogger<WalkInController>> _loggerMock = new();

    private WalkInController CreateController()
    {
        var controller = new WalkInController(
            _walkInServiceMock.Object,
            _lookupMock.Object,
            _loggerMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };

        return controller;
    }

    [Fact]
    public async Task SearchPatients_EmptyQuery_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.SearchPatients("   ");

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task SearchPatients_ValidQuery_ReturnsMatchingPatients()
    {
        var patientId = Guid.NewGuid();
        var expected = new List<PatientSearchResult>
        {
            new(patientId, Guid.NewGuid(), "Alice Nguyen", "alice@example.com",
                "555-0100", new DateOnly(1990, 5, 14),
                $"MRN-{patientId.ToString()[..8].ToUpperInvariant()}"),
        };

        _lookupMock
            .Setup(s => s.SearchAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = CreateController();

        var result = await controller.SearchPatients("alice");

        var ok = Assert.IsType<OkObjectResult>(result);
        var patients = Assert.IsAssignableFrom<IReadOnlyList<PatientSearchResult>>(ok.Value);
        Assert.Single(patients);
        Assert.Equal("Alice Nguyen", patients[0].FullName);
    }

    [Fact]
    public async Task SearchPatients_ResultIncludesMrn_NotEmpty()
    {
        var patientId = Guid.NewGuid();
        var expectedMrn = $"MRN-{patientId.ToString()[..8].ToUpperInvariant()}";

        _lookupMock
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PatientSearchResult>
            {
                new(patientId, Guid.NewGuid(), "Bob Smith", "bob@example.com",
                    null, null, expectedMrn),
            });

        var controller = CreateController();
        var result = await controller.SearchPatients("bob");

        var ok = Assert.IsType<OkObjectResult>(result);
        var patients = Assert.IsAssignableFrom<IReadOnlyList<PatientSearchResult>>(ok.Value);
        Assert.NotEmpty(patients[0].Mrn);
        Assert.Equal(expectedMrn, patients[0].Mrn);
    }

    [Fact]
    public async Task SearchPatients_EmptyResults_ReturnsOkWithEmptyList()
    {
        _lookupMock
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PatientSearchResult>());

        var controller = CreateController();
        var result = await controller.SearchPatients("zzz");

        var ok = Assert.IsType<OkObjectResult>(result);
        var patients = Assert.IsAssignableFrom<IReadOnlyList<PatientSearchResult>>(ok.Value);
        Assert.Empty(patients);
    }
}
