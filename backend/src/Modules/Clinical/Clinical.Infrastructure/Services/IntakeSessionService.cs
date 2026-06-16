using Clinical.Application.Abstractions;
using Clinical.Application.AI;
using Clinical.Application.DTOs;
using Clinical.Application.Models;
using Microsoft.Extensions.Logging;
using SharedKernel.Caching;
using SharedKernel.Domain;

namespace Clinical.Infrastructure.Services;

/// <summary>
/// Intake session service managing conversation state in Redis and coordinating with AI engine.
/// Implements AC-1 (guided flow), AC-2 (structured data with confidence scores), AC-5 (field editing).
/// Mode switch (US_026): snapshot, merge, and summarize across AI and manual modes.
/// </summary>
public sealed class IntakeSessionService : IIntakeSessionService
{
    private readonly ICacheService _cache;
    private readonly IConversationalIntakeEngine _intakeEngine;
    private readonly IIntakeRecordPersistence _persistence;
    private readonly IManualIntakePersistence _manualPersistence;
    private readonly ILogger<IntakeSessionService> _logger;

    private const string SessionKeyPrefix = "intake:session:";
    private const string ModeSwitchKeyPrefix = "intake:modeswitch:";

    public IntakeSessionService(
        ICacheService cache,
        IConversationalIntakeEngine intakeEngine,
        IIntakeRecordPersistence persistence,
        IManualIntakePersistence manualPersistence,
        ILogger<IntakeSessionService> logger)
    {
        _cache = cache;
        _intakeEngine = intakeEngine;
        _persistence = persistence;
        _manualPersistence = manualPersistence;
        _logger = logger;
    }

    public async Task<Result<IntakeSessionDto>> CreateSessionAsync(
        Guid patientId,
        Guid? appointmentId,
        CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid();

        var state = new IntakeSessionState
        {
            SessionId = sessionId,
            PatientId = patientId,
            AppointmentId = appointmentId,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        // Add initial system prompt to conversation history
        state.ConversationHistory.Add(new ConversationTurn
        {
            Role = "assistant",
            Content = "Hello! I'll help you complete your pre-visit intake. Let's start with your medical history. Do you have any pre-existing conditions or past surgeries?"
        });

        await SetSessionStateAsync(sessionId, state, cancellationToken);

        _logger.LogInformation(
            "Intake session {SessionId} created for patient {PatientId}.",
            sessionId, patientId);

        return Result<IntakeSessionDto>.Success(MapToDto(state));
    }

    public async Task<Result<IntakeMessageResponse>> SendMessageAsync(
        Guid sessionId,
        string message,
        CancellationToken cancellationToken = default)
    {
        var state = await GetSessionStateAsync(sessionId, cancellationToken);
        if (state is null)
        {
            return Result<IntakeMessageResponse>.Failure("Session not found or expired.");
        }

        if (state.Status != "Active")
        {
            return Result<IntakeMessageResponse>.Failure("Session is no longer active.");
        }

        // Record patient message
        state.ConversationHistory.Add(new ConversationTurn
        {
            Role = "user",
            Content = message
        });

        // Delegate to conversational intake engine (AIR-003, AIR-Q02, AIR-008)
        var engineResult = await _intakeEngine.ProcessMessageAsync(
            state, message, cancellationToken);

        // Handle token budget exhaustion
        if (engineResult.TokenBudgetExhausted)
        {
            state.Status = "TokenBudgetExhausted";
            await SetSessionStateAsync(sessionId, state, cancellationToken);

            return Result<IntakeMessageResponse>.Success(new IntakeMessageResponse(
                sessionId,
                engineResult.AiMessage,
                state.ParsedFields,
                SuggestManualFallback: false,
                TokenBudgetExhausted: true));
        }

        if (!engineResult.Success)
        {
            _logger.LogWarning(
                "AI inference failed for session {SessionId}: {Error}",
                sessionId, engineResult.Error);

            return Result<IntakeMessageResponse>.Failure(
                "AI service is temporarily unavailable. Please try again or switch to manual form.");
        }

        state.TotalTokensUsed += engineResult.TokensUsed;

        // Merge new fields into session state
        foreach (var field in engineResult.ParsedFields)
        {
            var existingIndex = state.ParsedFields
                .FindIndex(f => f.FieldName == field.FieldName);

            if (existingIndex >= 0)
            {
                state.ParsedFields[existingIndex] = field;
            }
            else
            {
                state.ParsedFields.Add(field);
            }
        }

        // Record AI response
        state.ConversationHistory.Add(new ConversationTurn
        {
            Role = "assistant",
            Content = engineResult.AiMessage
        });

        await SetSessionStateAsync(sessionId, state, cancellationToken);

        return Result<IntakeMessageResponse>.Success(new IntakeMessageResponse(
            sessionId,
            engineResult.AiMessage,
            state.ParsedFields,
            engineResult.SuggestManualFallback,
            TokenBudgetExhausted: false));
    }

    public async Task<Result<IntakeSessionDto>> UpdateFieldsAsync(
        Guid sessionId,
        IReadOnlyList<FieldEdit> fields,
        CancellationToken cancellationToken = default)
    {
        var state = await GetSessionStateAsync(sessionId, cancellationToken);
        if (state is null)
        {
            return Result<IntakeSessionDto>.Failure("Session not found or expired.");
        }

        foreach (var edit in fields)
        {
            var existingIndex = state.ParsedFields
                .FindIndex(f => f.FieldName == edit.FieldName);

            if (existingIndex >= 0)
            {
                var existing = state.ParsedFields[existingIndex];
                state.ParsedFields[existingIndex] = existing with
                {
                    Value = edit.Value,
                    ConfidenceScore = 1.0 // User-edited fields have full confidence
                };
            }
            else
            {
                state.ParsedFields.Add(new ParsedFieldDto(
                    edit.FieldName, edit.Value, 1.0, "UserProvided"));
            }
        }

        await SetSessionStateAsync(sessionId, state, cancellationToken);

        _logger.LogInformation(
            "Updated {Count} fields in session {SessionId}.",
            fields.Count, sessionId);

        return Result<IntakeSessionDto>.Success(MapToDto(state));
    }

    public async Task<Result<IntakeSessionDto>> CompleteSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var state = await GetSessionStateAsync(sessionId, cancellationToken);
        if (state is null)
        {
            return Result<IntakeSessionDto>.Failure("Session not found or expired.");
        }

        if (state.Status == "Completed")
        {
            return Result<IntakeSessionDto>.Failure("Session is already completed.");
        }

        // Aggregate parsed fields into intake record categories
        var chiefComplaint = GetFieldValue(state, "ChiefComplaint", "Symptoms");
        var medications = GetFieldValue(state, "CurrentMedications", "Medications");
        var allergies = GetFieldValue(state, "Allergies");
        var medicalHistory = GetFieldValue(state, "MedicalHistory", "History");

        await _persistence.PersistAsync(
            state.PatientId,
            state.AppointmentId,
            chiefComplaint,
            medications,
            allergies,
            medicalHistory,
            cancellationToken);

        state.Status = "Completed";
        state.CompletedAt = DateTime.UtcNow;
        await SetSessionStateAsync(sessionId, state, cancellationToken);

        _logger.LogInformation(
            "Intake session {SessionId} completed for patient {PatientId}.",
            sessionId, state.PatientId);

        return Result<IntakeSessionDto>.Success(MapToDto(state));
    }

    public async Task<Result<IntakeSessionDto>> SwitchModeAsync(
        Guid appointmentId,
        ModeSwitchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.TargetMode is not ("AI" or "Manual"))
        {
            return Result<IntakeSessionDto>.Failure("TargetMode must be 'AI' or 'Manual'.");
        }

        var switchState = await GetModeSwitchStateAsync(appointmentId, cancellationToken)
            ?? new ModeSwitchState { AppointmentId = appointmentId };

        if (switchState.IsLocked)
        {
            return Result<IntakeSessionDto>.Failure("Intake is locked and cannot be switched.");
        }

        // Snapshot current AI session data if switching away from AI
        if (request.TargetMode == "Manual" && request.SessionId.HasValue)
        {
            var aiState = await GetSessionStateAsync(request.SessionId.Value, cancellationToken);
            if (aiState is not null)
            {
                switchState.AiParsedFields = aiState.ParsedFields;
                switchState.AiSessionId = aiState.SessionId;
            }
        }

        // Snapshot current manual data if switching away from Manual
        if (request.TargetMode == "AI")
        {
            var manualData = await _manualPersistence.GetByAppointmentIdAsync(appointmentId, cancellationToken);
            if (manualData is not null)
            {
                switchState.ManualSnapshot = manualData;
            }
        }

        switchState.CurrentMode = request.TargetMode;
        switchState.LastSwitchedAt = DateTime.UtcNow;
        await SetModeSwitchStateAsync(appointmentId, switchState, cancellationToken);

        _logger.LogInformation(
            "Intake mode switched to {TargetMode} for appointment {AppointmentId}.",
            request.TargetMode, appointmentId);

        // If switching to AI, create a new session pre-seeded with manual data
        if (request.TargetMode == "AI" && switchState.ManualSnapshot is not null)
        {
            var manual = switchState.ManualSnapshot;
            var sessionResult = await CreateSessionAsync(
                manual.PatientId, appointmentId, cancellationToken);

            if (sessionResult.IsSuccess)
            {
                var seedFields = BuildFieldsFromManualData(manual);
                if (seedFields.Count > 0)
                {
                    await UpdateFieldsAsync(
                        sessionResult.Value!.SessionId,
                        seedFields.Select(f => new FieldEdit(f.FieldName, f.Value)).ToList(),
                        cancellationToken);
                }

                switchState.AiSessionId = sessionResult.Value!.SessionId;
                await SetModeSwitchStateAsync(appointmentId, switchState, cancellationToken);
            }

            return sessionResult;
        }

        // If switching to Manual, return a synthetic session DTO representing the switch
        var dto = new IntakeSessionDto(
            switchState.AiSessionId ?? Guid.Empty,
            switchState.ManualSnapshot?.PatientId ?? Guid.Empty,
            appointmentId,
            $"SwitchedToManual",
            switchState.AiParsedFields ?? [],
            DateTime.UtcNow,
            null);

        return Result<IntakeSessionDto>.Success(dto);
    }

    public async Task<Result<IntakeSummaryDto>> GetSummaryAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var switchState = await GetModeSwitchStateAsync(appointmentId, cancellationToken);

        var aiFields = new List<ParsedFieldDto>();
        var manualData = await _manualPersistence.GetByAppointmentIdAsync(appointmentId, cancellationToken);

        // Load AI fields from switch state or active session
        if (switchState?.AiSessionId is not null)
        {
            var aiState = await GetSessionStateAsync(switchState.AiSessionId.Value, cancellationToken);
            if (aiState is not null)
            {
                aiFields = aiState.ParsedFields;
            }
        }

        if (switchState?.AiParsedFields is not null && switchState.AiParsedFields.Count > 0)
        {
            // Merge snapshot fields (older) with live fields (newer wins)
            foreach (var field in switchState.AiParsedFields)
            {
                if (!aiFields.Any(f => f.FieldName.Equals(field.FieldName, StringComparison.OrdinalIgnoreCase)))
                {
                    aiFields.Add(field);
                }
            }
        }

        if (aiFields.Count == 0 && manualData is null)
        {
            return Result<IntakeSummaryDto>.Failure("No intake data found for the specified appointment.");
        }

        var categories = BuildSummaryCategories(aiFields, manualData);
        var currentMode = switchState?.CurrentMode ?? (aiFields.Count > 0 ? "AI" : "Manual");

        var summary = new IntakeSummaryDto(
            appointmentId,
            currentMode,
            switchState?.IsLocked ?? false,
            categories,
            DateTime.UtcNow);

        return Result<IntakeSummaryDto>.Success(summary);
    }

    public async Task<Result<IntakeSummaryDto>> ConfirmIntakeAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var summaryResult = await GetSummaryAsync(appointmentId, cancellationToken);
        if (!summaryResult.IsSuccess)
        {
            return summaryResult;
        }

        var switchState = await GetModeSwitchStateAsync(appointmentId, cancellationToken)
            ?? new ModeSwitchState { AppointmentId = appointmentId };

        if (switchState.IsLocked)
        {
            return Result<IntakeSummaryDto>.Failure("Intake is already locked.");
        }

        switchState.IsLocked = true;
        switchState.LockedAt = DateTime.UtcNow;
        await SetModeSwitchStateAsync(appointmentId, switchState, cancellationToken);

        _logger.LogInformation("Intake locked for appointment {AppointmentId}.", appointmentId);

        return Result<IntakeSummaryDto>.Success(summaryResult.Value! with { IsLocked = true });
    }

    private static List<IntakeSummaryCategory> BuildSummaryCategories(
        List<ParsedFieldDto> aiFields,
        ManualIntakeResponse? manualData)
    {
        var mergedFields = new Dictionary<string, IntakeSummaryField>(StringComparer.OrdinalIgnoreCase);

        // Add AI fields first
        foreach (var field in aiFields)
        {
            var category = NormalizeCategoryName(field.Category);
            var key = $"{category}:{field.FieldName}";
            mergedFields[key] = new IntakeSummaryField(
                field.FieldName, field.Value, field.ConfidenceScore, "AI");
        }

        // Layer manual data on top — manual edits are latest and take precedence
        if (manualData is not null)
        {
            AddManualField(mergedFields, "MedicalHistory", "MedicalHistory", manualData.MedicalHistory);
            AddManualField(mergedFields, "Symptoms", "Symptoms", manualData.Symptoms);
            AddManualField(mergedFields, "Allergies", "Allergies", manualData.Allergies);
            AddManualField(mergedFields, "Medications", "CurrentMedications", manualData.CurrentMedications);
        }

        // Group into categories
        var grouped = mergedFields
            .GroupBy(kvp => kvp.Key.Split(':')[0])
            .Select(g => new IntakeSummaryCategory(
                g.Key,
                g.Select(kvp => kvp.Value).ToList()))
            .ToList();

        return grouped;
    }

    private static void AddManualField(
        Dictionary<string, IntakeSummaryField> fields,
        string category,
        string fieldName,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var key = $"{category}:{fieldName}";
        fields[key] = new IntakeSummaryField(fieldName, value, null, "Manual");
    }

    private static string NormalizeCategoryName(string category) => category switch
    {
        "MedicalHistory" or "History" => "MedicalHistory",
        "Symptoms" or "ChiefComplaint" => "Symptoms",
        "Medications" or "CurrentMedications" => "Medications",
        "Allergies" => "Allergies",
        _ => category
    };

    private static List<ParsedFieldDto> BuildFieldsFromManualData(ManualIntakeResponse manual)
    {
        var fields = new List<ParsedFieldDto>();

        if (!string.IsNullOrWhiteSpace(manual.MedicalHistory))
            fields.Add(new ParsedFieldDto("MedicalHistory", manual.MedicalHistory, 1.0, "MedicalHistory"));

        if (!string.IsNullOrWhiteSpace(manual.Symptoms))
            fields.Add(new ParsedFieldDto("ChiefComplaint", manual.Symptoms, 1.0, "Symptoms"));

        if (!string.IsNullOrWhiteSpace(manual.Allergies))
            fields.Add(new ParsedFieldDto("Allergies", manual.Allergies, 1.0, "Allergies"));

        if (!string.IsNullOrWhiteSpace(manual.CurrentMedications))
            fields.Add(new ParsedFieldDto("CurrentMedications", manual.CurrentMedications, 1.0, "Medications"));

        return fields;
    }

    private static string? GetFieldValue(IntakeSessionState state, params string[] fieldNames)
    {
        foreach (var name in fieldNames)
        {
            var field = state.ParsedFields
                .FirstOrDefault(f => f.FieldName.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (field is not null)
            {
                return field.Value;
            }
        }

        return null;
    }

    private static IntakeSessionDto MapToDto(IntakeSessionState state) =>
        new(state.SessionId,
            state.PatientId,
            state.AppointmentId,
            state.Status,
            state.ParsedFields,
            state.CreatedAt,
            state.CompletedAt);

    private async Task<IntakeSessionState?> GetSessionStateAsync(
        Guid sessionId, CancellationToken cancellationToken) =>
        await _cache.GetAsync<IntakeSessionState>(
            $"{SessionKeyPrefix}{sessionId}", cancellationToken);

    private async Task SetSessionStateAsync(
        Guid sessionId, IntakeSessionState state, CancellationToken cancellationToken) =>
        await _cache.SetAsync(
            $"{SessionKeyPrefix}{sessionId}", state, CacheTier.L2, cancellationToken);

    private async Task<ModeSwitchState?> GetModeSwitchStateAsync(
        Guid appointmentId, CancellationToken cancellationToken) =>
        await _cache.GetAsync<ModeSwitchState>(
            $"{ModeSwitchKeyPrefix}{appointmentId}", cancellationToken);

    private async Task SetModeSwitchStateAsync(
        Guid appointmentId, ModeSwitchState state, CancellationToken cancellationToken) =>
        await _cache.SetAsync(
            $"{ModeSwitchKeyPrefix}{appointmentId}", state, CacheTier.L2, cancellationToken);
}
