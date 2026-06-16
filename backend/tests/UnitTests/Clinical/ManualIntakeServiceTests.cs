using Clinical.Application.Abstractions;
using Clinical.Application.DTOs;
using Clinical.Application.Validators;
using Clinical.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using SharedKernel.Domain;

namespace UnitTests.Clinical;

public sealed class ManualIntakeServiceTests
{
    private readonly Mock<IManualIntakePersistence> _persistence = new();
    private readonly ManualIntakeValidator _validator = new();
    private readonly Mock<ILogger<ManualIntakeService>> _logger = new();
    private readonly ManualIntakeService _sut;

    private static readonly Guid AppointmentId = Guid.NewGuid();
    private static readonly Guid PatientId = Guid.NewGuid();

    public ManualIntakeServiceTests()
    {
        _sut = new ManualIntakeService(_persistence.Object, _validator, _logger.Object);
    }

    private static ManualIntakeRequest ValidRequest() => new(
        PatientId: PatientId,
        MedicalHistory: "No significant history",
        Symptoms: "Headache and fever",
        Allergies: "Penicillin",
        CurrentMedications: "Ibuprofen 200mg");

    private static ManualIntakeResponse SampleResponse(bool isComplete = false) => new(
        Id: Guid.NewGuid(),
        PatientId: PatientId,
        AppointmentId: AppointmentId,
        MedicalHistory: "No significant history",
        Symptoms: "Headache and fever",
        Allergies: "Penicillin",
        CurrentMedications: "Ibuprofen 200mg",
        IsComplete: isComplete,
        CompletedAt: isComplete ? DateTime.UtcNow : null,
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: DateTime.UtcNow);

    // ── GET ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_RecordExists_ReturnsSuccess()
    {
        var response = SampleResponse();
        _persistence.Setup(p => p.GetByAppointmentIdAsync(AppointmentId, default))
            .ReturnsAsync(response);

        var result = await _sut.GetAsync(AppointmentId);

        Assert.True(result.IsSuccess);
        Assert.Equal(response.PatientId, result.Value!.PatientId);
    }

    [Fact]
    public async Task GetAsync_RecordNotFound_ReturnsFailure()
    {
        _persistence.Setup(p => p.GetByAppointmentIdAsync(AppointmentId, default))
            .ReturnsAsync((ManualIntakeResponse?)null);

        var result = await _sut.GetAsync(AppointmentId);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    // ── AUTOSAVE ─────────────────────────────────────────────────────

    [Fact]
    public async Task AutosaveAsync_PersistsDraft()
    {
        var request = ValidRequest();
        var response = SampleResponse();
        _persistence.Setup(p => p.UpsertDraftAsync(AppointmentId, request, default))
            .ReturnsAsync(response);

        var result = await _sut.AutosaveAsync(AppointmentId, request);

        Assert.True(result.IsSuccess);
        _persistence.Verify(p => p.UpsertDraftAsync(AppointmentId, request, default), Times.Once);
    }

    // ── SUBMIT ───────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitAsync_ValidRequest_ReturnsSuccess()
    {
        var request = ValidRequest();
        var response = SampleResponse(isComplete: true);
        _persistence.Setup(p => p.SubmitAsync(AppointmentId, request, default))
            .ReturnsAsync(response);

        var result = await _sut.SubmitAsync(AppointmentId, request);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsComplete);
    }

    [Fact]
    public async Task SubmitAsync_NullOptionalFields_Succeeds()
    {
        var request = ValidRequest() with { Symptoms = null };
        var response = SampleResponse(isComplete: true);
        _persistence.Setup(p => p.SubmitAsync(AppointmentId, request, default))
            .ReturnsAsync(response);

        var result = await _sut.SubmitAsync(AppointmentId, request);

        Assert.True(result.IsSuccess);
        _persistence.Verify(p => p.SubmitAsync(AppointmentId, request, default), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_AllFieldsEmpty_ReturnsMultipleErrors()
    {
        var request = new ManualIntakeRequest(Guid.Empty, null, null, null, null);

        var result = await _sut.SubmitAsync(AppointmentId, request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Patient ID is required", result.Error);
    }

    // ── UPDATE FIELDS ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateFieldsAsync_ValidFields_ReturnsSuccess()
    {
        var request = new IntakeFieldUpdateRequest(
            [new IntakeFieldEdit("Symptoms", "Updated headache")]);
        var response = SampleResponse(isComplete: true);
        _persistence.Setup(p => p.UpdateFieldAsync(AppointmentId, "Symptoms", "Updated headache", default))
            .ReturnsAsync(response);

        var result = await _sut.UpdateFieldsAsync(AppointmentId, request);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateFieldsAsync_EmptyFields_ReturnsFailure()
    {
        var request = new IntakeFieldUpdateRequest([]);

        var result = await _sut.UpdateFieldsAsync(AppointmentId, request);

        Assert.False(result.IsSuccess);
        Assert.Contains("At least one field update is required", result.Error);
    }

    [Fact]
    public async Task UpdateFieldsAsync_InvalidFieldName_ReturnsFailure()
    {
        var request = new IntakeFieldUpdateRequest(
            [new IntakeFieldEdit("InvalidField", "value")]);

        var result = await _sut.UpdateFieldsAsync(AppointmentId, request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid field names", result.Error);
    }

    [Fact]
    public async Task UpdateFieldsAsync_RecordNotFound_ReturnsFailure()
    {
        var request = new IntakeFieldUpdateRequest(
            [new IntakeFieldEdit("Allergies", "None")]);
        _persistence.Setup(p => p.UpdateFieldAsync(AppointmentId, "Allergies", "None", default))
            .ReturnsAsync((ManualIntakeResponse?)null);

        var result = await _sut.UpdateFieldsAsync(AppointmentId, request);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }
}
