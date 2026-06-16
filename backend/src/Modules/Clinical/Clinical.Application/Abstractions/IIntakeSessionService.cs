using Clinical.Application.DTOs;
using SharedKernel.Domain;

namespace Clinical.Application.Abstractions;

/// <summary>
/// Intake session service managing AI conversational intake flow (AC-1, AC-2, AC-5).
/// </summary>
public interface IIntakeSessionService
{
    Task<Result<IntakeSessionDto>> CreateSessionAsync(
        Guid patientId,
        Guid? appointmentId,
        CancellationToken cancellationToken = default);

    Task<Result<IntakeMessageResponse>> SendMessageAsync(
        Guid sessionId,
        string message,
        CancellationToken cancellationToken = default);

    Task<Result<IntakeSessionDto>> UpdateFieldsAsync(
        Guid sessionId,
        IReadOnlyList<FieldEdit> fields,
        CancellationToken cancellationToken = default);

    Task<Result<IntakeSessionDto>> CompleteSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<Result<IntakeSessionDto>> SwitchModeAsync(
        Guid appointmentId,
        ModeSwitchRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IntakeSummaryDto>> GetSummaryAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<Result<IntakeSummaryDto>> ConfirmIntakeAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);
}
