using Clinical.Application.Abstractions;
using Clinical.Application.DTOs;
using Clinical.Application.Validators;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;

namespace Clinical.Infrastructure.Services;

/// <summary>
/// Manual intake service handling autosave, submit, field edit, and retrieval (AC-1 through AC-4).
/// </summary>
public sealed class ManualIntakeService : IManualIntakeService
{
    private static readonly HashSet<string> AllowedFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "MedicalHistory",
        "Symptoms",
        "Allergies",
        "CurrentMedications"
    };

    private readonly IManualIntakePersistence _persistence;
    private readonly ManualIntakeValidator _validator;
    private readonly ILogger<ManualIntakeService> _logger;

    public ManualIntakeService(
        IManualIntakePersistence persistence,
        ManualIntakeValidator validator,
        ILogger<ManualIntakeService> logger)
    {
        _persistence = persistence;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<ManualIntakeResponse>> GetAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var record = await _persistence.GetByAppointmentIdAsync(appointmentId, cancellationToken);
        if (record is null)
        {
            return Result<ManualIntakeResponse>.Failure("Intake record not found for the specified appointment.");
        }

        return Result<ManualIntakeResponse>.Success(record);
    }

    public async Task<Result<ManualIntakeResponse>> AutosaveAsync(
        Guid appointmentId,
        ManualIntakeRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _persistence.UpsertDraftAsync(appointmentId, request, cancellationToken);
        _logger.LogInformation("Autosaved intake draft for appointment {AppointmentId}.", appointmentId);
        return Result<ManualIntakeResponse>.Success(response);
    }

    public async Task<Result<ManualIntakeResponse>> SubmitAsync(
        Guid appointmentId,
        ManualIntakeRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<ManualIntakeResponse>.Failure(errors);
        }

        var response = await _persistence.SubmitAsync(appointmentId, request, cancellationToken);
        _logger.LogInformation("Submitted intake for appointment {AppointmentId}.", appointmentId);
        return Result<ManualIntakeResponse>.Success(response);
    }

    public async Task<Result<ManualIntakeResponse>> UpdateFieldsAsync(
        Guid appointmentId,
        IntakeFieldUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Fields is null || request.Fields.Count == 0)
        {
            return Result<ManualIntakeResponse>.Failure("At least one field update is required.");
        }

        var invalidFields = request.Fields
            .Where(f => !AllowedFieldNames.Contains(f.FieldName))
            .Select(f => f.FieldName)
            .ToList();

        if (invalidFields.Count > 0)
        {
            return Result<ManualIntakeResponse>.Failure(
                $"Invalid field names: {string.Join(", ", invalidFields)}. " +
                $"Allowed fields: {string.Join(", ", AllowedFieldNames)}.");
        }

        ManualIntakeResponse? response = null;
        foreach (var field in request.Fields)
        {
            response = await _persistence.UpdateFieldAsync(
                appointmentId, field.FieldName, field.Value, cancellationToken);

            if (response is null)
            {
                return Result<ManualIntakeResponse>.Failure(
                    "Intake record not found for the specified appointment.");
            }
        }

        _logger.LogInformation(
            "Updated {FieldCount} fields for appointment {AppointmentId}.",
            request.Fields.Count, appointmentId);

        return Result<ManualIntakeResponse>.Success(response!);
    }
}
