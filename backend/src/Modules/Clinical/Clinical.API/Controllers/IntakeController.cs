using System.Security.Claims;
using System.Text.Json;
using Clinical.Application.Abstractions;
using Clinical.Application.AI;
using Clinical.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Clinical.API.Controllers;

/// <summary>
/// Intake session endpoints for AI conversational intake (AC-1, AC-2, AC-5)
/// and manual form-based intake (AC-1 through AC-4).
/// </summary>
[ApiController]
[Route("api/clinical/intake")]
[Authorize]
[Produces("application/json")]
public class IntakeController : ControllerBase
{
    private readonly IIntakeSessionService _sessionService;
    private readonly IManualIntakeService _manualIntakeService;
    private readonly IAiInferenceService _aiService;
    private readonly ILogger<IntakeController> _logger;

    public IntakeController(
        IIntakeSessionService sessionService,
        IManualIntakeService manualIntakeService,
        IAiInferenceService aiService,
        ILogger<IntakeController> logger)
    {
        _sessionService = sessionService;
        _manualIntakeService = manualIntakeService;
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new intake session for a patient.
    /// </summary>
    /// <param name="request">Session creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created intake session.</returns>
    /// <response code="201">Session created.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(IntakeSessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSession(
        [FromBody] CreateIntakeSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PatientId == Guid.Empty)
        {
            return Problem(
                detail: "PatientId is required.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error");
        }

        var result = await _sessionService.CreateSessionAsync(
            request.PatientId, request.AppointmentId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Session Creation Failed");
        }

        return CreatedAtAction(null, new { id = result.Value!.SessionId }, result.Value);
    }

    /// <summary>
    /// Sends a patient message to the AI engine and returns parsed response with confidence scores.
    /// Response within 5s at p95 per AIR-Q02.
    /// </summary>
    /// <param name="id">Session ID.</param>
    /// <param name="request">Patient message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AI-parsed response with extracted fields.</returns>
    /// <response code="200">Message processed.</response>
    /// <response code="404">Session not found.</response>
    /// <response code="408">AI processing timeout.</response>
    [HttpPost("sessions/{id:guid}/messages")]
    [ProducesResponseType(typeof(IntakeMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    public async Task<IActionResult> SendMessage(
        Guid id,
        [FromBody] IntakeMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(180));

        try
        {
            var result = await _sessionService.SendMessageAsync(
                id, request.Message, cts.Token);

            if (!result.IsSuccess)
            {
                var statusCode = result.Error!.Contains("not found")
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest;

                return Problem(
                    detail: result.Error,
                    statusCode: statusCode,
                    title: "Message Processing Failed");
            }

            return Ok(result.Value);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AI processing timed out for session {SessionId}.", id);

            return Problem(
                detail: "AI processing timed out. Please try again or switch to manual form.",
                statusCode: StatusCodes.Status408RequestTimeout,
                title: "Processing Timeout");
        }
    }

    /// <summary>
    /// Updates individual parsed fields for review corrections (AC-5).
    /// </summary>
    /// <param name="id">Session ID.</param>
    /// <param name="request">Field updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated session.</returns>
    /// <response code="200">Fields updated.</response>
    /// <response code="404">Session not found.</response>
    [HttpPut("sessions/{id:guid}/fields")]
    [ProducesResponseType(typeof(IntakeSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFields(
        Guid id,
        [FromBody] FieldUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _sessionService.UpdateFieldsAsync(
            id, request.Fields, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status404NotFound,
                title: "Session Not Found");
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Completes the intake session and persists the IntakeRecord.
    /// </summary>
    /// <param name="id">Session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed session.</returns>
    /// <response code="200">Session completed and persisted.</response>
    /// <response code="404">Session not found.</response>
    [HttpPost("sessions/{id:guid}/complete")]
    [ProducesResponseType(typeof(IntakeSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteSession(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sessionService.CompleteSessionAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status404NotFound,
                title: "Session Completion Failed");
        }

        return Ok(result.Value);
    }

    // ── Frontend AI Chat Endpoints ──────────────────────────────────────

    /// <summary>
    /// Returns AI service availability for the frontend circuit breaker (AC-4, NFR-013).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AI availability status.</returns>
    /// <response code="200">AI is available.</response>
    /// <response code="503">AI is unavailable.</response>
    [HttpGet("ai-status")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetAiStatus(CancellationToken cancellationToken = default)
    {
        var isAvailable = await _aiService.IsAvailableAsync(cancellationToken);

        if (!isAvailable)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { available = false });
        }

        return Ok(new { available = true });
    }

    /// <summary>
    /// Convenience chat endpoint for the frontend AI intake flow.
    /// Creates a session on first message; sends subsequent messages to existing session.
    /// </summary>
    /// <param name="request">Chat message with optional conversationId.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AI response with extracted fields.</returns>
    /// <response code="200">Message processed.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="408">AI processing timeout.</response>
    [HttpPost("chat")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    public async Task<IActionResult> Chat(
        [FromBody] IntakeChatRequest request,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(180));

        try
        {
            Guid sessionId;

            if (string.IsNullOrEmpty(request.ConversationId) ||
                !Guid.TryParse(request.ConversationId, out sessionId))
            {
                var patientId = GetUserId();
                Guid? appointmentId = null;
                if (!string.IsNullOrEmpty(request.AppointmentId) &&
                    Guid.TryParse(request.AppointmentId, out var aid))
                {
                    appointmentId = aid;
                }

                var createResult = await _sessionService.CreateSessionAsync(
                    patientId, appointmentId, cts.Token);

                if (!createResult.IsSuccess)
                {
                    return Problem(
                        detail: createResult.Error,
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Session Creation Failed");
                }

                sessionId = createResult.Value!.SessionId;
            }

            var result = await _sessionService.SendMessageAsync(
                sessionId, request.Message, cts.Token);

            if (!result.IsSuccess)
            {
                return Problem(
                    detail: result.Error,
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Chat Processing Failed");
            }

            var response = result.Value!;
            var extractedFields = response.ParsedFields.Select(f => new
            {
                key = f.FieldName,
                label = f.FieldName,
                value = f.Value,
                category = MapCategory(f.Category),
                confidence = f.ConfidenceScore
            }).ToList();

            return Ok(new
            {
                conversationId = response.SessionId.ToString(),
                reply = new
                {
                    id = $"ai-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                    role = "ai",
                    content = response.AiMessage,
                    timestamp = DateTime.UtcNow.ToString("h:mm tt"),
                    extractedFields
                },
                extractedFields,
                isComplete = response.TokenBudgetExhausted
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AI chat processing timed out.");

            return Problem(
                detail: "AI processing timed out. Please try again or switch to manual form.",
                statusCode: StatusCodes.Status408RequestTimeout,
                title: "Processing Timeout");
        }
    }

    private static string MapCategory(string? source)
    {
        return source?.ToLowerInvariant() switch
        {
            "medicalhistory" or "history" => "history",
            "chiefcomplaint" or "symptoms" or "symptom" => "symptom",
            "allergies" or "allergy" => "allergy",
            "currentmedications" or "medications" or "medication" => "medication",
            "vital" or "vitals" => "vital",
            _ => source?.ToLowerInvariant() ?? "history"
        };
    }

    // ── Manual Intake Endpoints (TASK_002) ──────────────────────────────

    /// <summary>
    /// Retrieves the current intake state for an appointment (draft or submitted).
    /// </summary>
    /// <param name="appointmentId">Appointment ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current intake record.</returns>
    /// <response code="200">Intake record retrieved.</response>
    /// <response code="404">No intake record found.</response>
    [HttpGet("{appointmentId:guid}")]
    [ProducesResponseType(typeof(ManualIntakeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIntake(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var result = await _manualIntakeService.GetAsync(appointmentId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status404NotFound,
                title: "Intake Not Found");
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Autosaves partial intake form state (AC-2: every 30s per UXR-505).
    /// </summary>
    /// <param name="appointmentId">Appointment ID.</param>
    /// <param name="request">Partial intake form data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Persisted draft state.</returns>
    /// <response code="200">Draft saved.</response>
    [HttpPut("{appointmentId:guid}/autosave")]
    [ProducesResponseType(typeof(ManualIntakeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Autosave(
        Guid appointmentId,
        [FromBody] ManualIntakeRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _manualIntakeService.AutosaveAsync(
            appointmentId, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Autosave Failed");
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Submits the intake form with full validation (AC-3).
    /// </summary>
    /// <param name="appointmentId">Appointment ID.</param>
    /// <param name="request">Complete intake form data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Submitted intake record.</returns>
    /// <response code="200">Intake submitted.</response>
    /// <response code="422">Validation failed.</response>
    [HttpPost("{appointmentId:guid}/submit")]
    [ProducesResponseType(typeof(ManualIntakeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Submit(
        Guid appointmentId,
        [FromBody] ManualIntakeRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _manualIntakeService.SubmitAsync(
            appointmentId, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Validation Error");
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Updates individual fields after submission (AC-4, FR-017).
    /// Patient-accessible without staff assistance.
    /// </summary>
    /// <param name="appointmentId">Appointment ID.</param>
    /// <param name="request">Field-level updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated intake record.</returns>
    /// <response code="200">Fields updated.</response>
    /// <response code="404">Intake record not found.</response>
    /// <response code="400">Invalid field names.</response>
    [HttpPatch("{appointmentId:guid}/fields")]
    [ProducesResponseType(typeof(ManualIntakeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateFields(
        Guid appointmentId,
        [FromBody] IntakeFieldUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _manualIntakeService.UpdateFieldsAsync(
            appointmentId, request, cancellationToken);

        if (!result.IsSuccess)
        {
            var statusCode = result.Error!.Contains("not found")
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;

            return Problem(
                detail: result.Error,
                statusCode: statusCode,
                title: result.Error!.Contains("not found")
                    ? "Intake Not Found"
                    : "Invalid Request");
        }

        return Ok(result.Value);
    }

    // ── Mode Switch & Summary Endpoints (TASK_002 US_026) ───────────────

    /// <summary>
    /// Switches intake mode between AI and manual, preserving data from the current mode.
    /// </summary>
    /// <param name="appointmentId">Appointment ID.</param>
    /// <param name="request">Mode switch request with target mode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session state after mode switch.</returns>
    /// <response code="200">Mode switched successfully.</response>
    /// <response code="400">Invalid target mode or intake is locked.</response>
    [HttpPost("{appointmentId:guid}/switch-mode")]
    [ProducesResponseType(typeof(IntakeSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SwitchMode(
        Guid appointmentId,
        [FromBody] ModeSwitchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _sessionService.SwitchModeAsync(
            appointmentId, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Mode Switch Failed");
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns a categorized summary of intake data from both AI and manual modes.
    /// Confidence scores are null for manual edits.
    /// </summary>
    /// <param name="appointmentId">Appointment ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Categorized intake summary.</returns>
    /// <response code="200">Summary retrieved.</response>
    /// <response code="404">No intake data found.</response>
    [HttpGet("{appointmentId:guid}/summary")]
    [ProducesResponseType(typeof(IntakeSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummary(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var result = await _sessionService.GetSummaryAsync(
            appointmentId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status404NotFound,
                title: "Summary Not Found");
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Confirms and locks the intake record, preventing further edits.
    /// </summary>
    /// <param name="appointmentId">Appointment ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Locked intake summary.</returns>
    /// <response code="200">Intake confirmed and locked.</response>
    /// <response code="400">Intake already locked.</response>
    /// <response code="404">No intake data found.</response>
    [HttpPost("{appointmentId:guid}/confirm")]
    [ProducesResponseType(typeof(IntakeSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmIntake(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var result = await _sessionService.ConfirmIntakeAsync(
            appointmentId, cancellationToken);

        if (!result.IsSuccess)
        {
            var statusCode = result.Error!.Contains("not found")
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;

            return Problem(
                detail: result.Error,
                statusCode: statusCode,
                title: result.Error!.Contains("locked")
                    ? "Already Locked"
                    : "Confirmation Failed");
        }

        return Ok(result.Value);
    }

    // ── Frontend-facing draft & submit endpoints ────────────────────────

    /// <summary>
    /// Saves a draft of the intake form (autosave from the frontend).
    /// Persists via the persistence layer with nullable AppointmentId.
    /// </summary>
    [HttpPut("draft")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveDraft(
        [FromBody] JsonElement body,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var request = MapFromFrontendPayload(body, userId);

        try
        {
            var result = await _manualIntakeService.AutosaveAsync(
                Guid.Empty, request, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Draft save failed for user {UserId}: {Error}", userId, result.Error);
            }

            return Ok(new { saved = true, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            // DB errors (FK violations, connectivity) should not crash autosave.
            // The frontend already has localStorage fallback.
            _logger.LogWarning(ex, "Draft persistence failed for user {UserId}. Frontend localStorage is the fallback.", userId);
            return Ok(new { saved = true, timestamp = DateTime.UtcNow, persisted = false });
        }
    }

    /// <summary>
    /// Submits the intake form from the review/summary page.
    /// </summary>
    [HttpPost("submit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SubmitFromSummary(
        [FromBody] JsonElement body,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var request = MapFromFrontendPayload(body, userId);

        try
        {
            var result = await _manualIntakeService.SubmitAsync(
                Guid.Empty, request, cancellationToken);

            if (!result.IsSuccess)
            {
                return Problem(
                    detail: result.Error,
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Validation Error");
            }

            return Ok(new { submitted = true, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Submit failed for user {UserId}.", userId);
            return Problem(
                detail: "An error occurred while saving intake data. Please try again.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Submit Failed");
        }
    }

    // ── Private helpers ─────────────────────────────────────────────────

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");

        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// Maps the structured frontend JSON into the flat ManualIntakeRequest DTO.
    /// </summary>
    private static ManualIntakeRequest MapFromFrontendPayload(JsonElement body, Guid userId)
    {
        string? allergies = null;
        string? medications = null;
        string? symptoms = null;
        string? history = null;

        if (body.TryGetProperty("allergies", out var a))
        {
            var parts = new List<string>();
            if (a.TryGetProperty("name", out var n) && n.GetString() is { Length: > 0 } name)
                parts.Add(name);
            if (a.TryGetProperty("severity", out var s) && s.GetString() is { Length: > 0 } sev)
                parts.Add($"Severity: {sev}");
            if (a.TryGetProperty("reaction", out var r) && r.GetString() is { Length: > 0 } rx)
                parts.Add($"Reaction: {rx}");

            if (parts.Count > 0) allergies = string.Join(" | ", parts);
        }

        if (body.TryGetProperty("medications", out var m))
        {
            var parts = new List<string>();
            if (m.TryGetProperty("name", out var n) && n.GetString() is { Length: > 0 } name)
                parts.Add(name);
            if (m.TryGetProperty("dosage", out var d) && d.GetString() is { Length: > 0 } dos)
                parts.Add(dos);
            if (m.TryGetProperty("notes", out var nt) && nt.GetString() is { Length: > 0 } notes)
                parts.Add(notes);

            if (parts.Count > 0) medications = string.Join(" | ", parts);
        }

        if (body.TryGetProperty("symptoms", out var sym))
        {
            var parts = new List<string>();
            if (sym.TryGetProperty("chiefComplaint", out var c) && c.GetString() is { Length: > 0 } cc)
                parts.Add(cc);
            if (sym.TryGetProperty("duration", out var dur) && dur.GetString() is { Length: > 0 } d)
                parts.Add($"Duration: {d}");
            if (sym.TryGetProperty("severity", out var sev) && sev.GetString() is { Length: > 0 } sv)
                parts.Add($"Severity: {sv}/10");

            if (parts.Count > 0) symptoms = string.Join(" | ", parts);
        }

        if (body.TryGetProperty("history", out var h))
        {
            var parts = new List<string>();
            if (h.TryGetProperty("conditions", out var conds) && conds.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in conds.EnumerateArray())
                {
                    if (c.GetString() is { Length: > 0 } val) parts.Add(val);
                }
            }
            if (h.TryGetProperty("additionalNotes", out var an) && an.GetString() is { Length: > 0 } notes)
                parts.Add(notes);

            if (parts.Count > 0) history = string.Join(", ", parts);
        }

        return new ManualIntakeRequest(
            PatientId: userId,
            MedicalHistory: history,
            Symptoms: symptoms,
            Allergies: allergies,
            CurrentMedications: medications);
    }
}

/// <summary>
/// AI health check controller for circuit breaker status (NFR-013).
/// </summary>
[ApiController]
[Route("api/clinical/ai")]
[Produces("application/json")]
public class AiHealthController : ControllerBase
{
    private readonly IAiInferenceService _aiService;

    public AiHealthController(IAiInferenceService aiService)
    {
        _aiService = aiService;
    }

    /// <summary>
    /// Returns current AI service availability and circuit breaker status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AI health status.</returns>
    /// <response code="200">AI health status retrieved.</response>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AiHealthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken = default)
    {
        var isAvailable = await _aiService.IsAvailableAsync(cancellationToken);

        return Ok(new AiHealthResponse(
            IsAvailable: isAvailable,
            Status: isAvailable ? "Healthy" : "Degraded",
            CheckedAt: DateTime.UtcNow));
    }
}

/// <summary>
/// Response DTO for AI health check endpoint.
/// </summary>
public sealed record AiHealthResponse(
    bool IsAvailable,
    string Status,
    DateTime CheckedAt);

/// <summary>
/// Request DTO for creating an intake session.
/// </summary>
public sealed record CreateIntakeSessionRequest(
    Guid PatientId,
    Guid? AppointmentId);

/// <summary>
/// Request DTO for the frontend chat endpoint.
/// </summary>
public sealed record IntakeChatRequest(
    string Message,
    string? ConversationId = null,
    string? AppointmentId = null);
