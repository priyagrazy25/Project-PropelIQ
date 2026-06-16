using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Audit;

namespace Host.Controllers;

/// <summary>
/// API controller for SCR-025 Audit Log Viewer.
/// Provides read-only access to audit records for administrators.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SystemAdmin")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditLogsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// GET /api/auditlogs
    /// Returns paginated audit log records with optional filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedAuditResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedAuditResponse>> GetAuditLogs(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? actorName,
        [FromQuery] string? action,
        [FromQuery] string? resource,
        [FromQuery] string? resourceId,
        [FromQuery] string? ipAddress,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var filter = new AuditLogFilter
        {
            StartDate = startDate,
            EndDate = endDate,
            ActorName = actorName,
            Action = action,
            Resource = resource,
            ResourceId = resourceId,
            IpAddress = ipAddress
        };

        var result = await _auditService.GetAuditLogsAsync(filter, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/auditlogs/ai
    /// Returns paginated AI invocation audit records.
    /// </summary>
    [HttpGet("ai")]
    [ProducesResponseType(typeof(PaginatedAiAuditResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedAiAuditResponse>> GetAiAuditLogs(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? modelId,
        [FromQuery] string? functionName,
        [FromQuery] bool? successOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var filter = new AiAuditLogFilter
        {
            StartDate = startDate,
            EndDate = endDate,
            ModelId = modelId,
            FunctionName = functionName,
            SuccessOnly = successOnly
        };

        var result = await _auditService.GetAiAuditLogsAsync(filter, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/auditlogs/export
    /// Exports filtered audit logs as CSV for compliance reporting.
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? actorName,
        [FromQuery] string? action,
        [FromQuery] string? resource)
    {
        var filter = new AuditLogFilter
        {
            StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
            EndDate = endDate ?? DateTime.UtcNow,
            ActorName = actorName,
            Action = action,
            Resource = resource
        };

        // Get all records (up to 10,000 for export)
        var result = await _auditService.GetAuditLogsAsync(filter, 1, 10000);

        var csv = GenerateCsv(result.Items);
        var fileName = $"audit-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    /// <summary>
    /// GET /api/auditlogs/stats
    /// Returns audit statistics for dashboard display.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(AuditStatsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditStatsResponse>> GetAuditStats(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var filter = new AuditLogFilter
        {
            StartDate = startDate ?? DateTime.UtcNow.AddDays(-7),
            EndDate = endDate ?? DateTime.UtcNow
        };

        var result = await _auditService.GetAuditLogsAsync(filter, 1, 10000);

        var stats = new AuditStatsResponse
        {
            TotalRecords = result.TotalCount,
            UniqueActors = result.Items.Select(x => x.ActorName).Distinct().Count(),
            ActionBreakdown = result.Items
                .GroupBy(x => x.Action)
                .ToDictionary(g => g.Key, g => g.Count()),
            ResourceBreakdown = result.Items
                .GroupBy(x => x.Resource)
                .ToDictionary(g => g.Key, g => g.Count()),
            TimeRange = new TimeRangeInfo
            {
                Start = filter.StartDate!.Value,
                End = filter.EndDate!.Value
            }
        };

        return Ok(stats);
    }

    private static string GenerateCsv(IEnumerable<AuditEntry> entries)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Timestamp,ActorId,ActorName,Action,Resource,ResourceId,IpAddress,CorrelationId");

        foreach (var entry in entries)
        {
            sb.AppendLine($"\"{entry.Timestamp:O}\",\"{entry.ActorId}\",\"{EscapeCsv(entry.ActorName)}\",\"{entry.Action}\",\"{entry.Resource}\",\"{entry.ResourceId}\",\"{entry.IpAddress}\",\"{entry.CorrelationId}\"");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Replace("\"", "\"\"");
    }
}

/// <summary>
/// Filter for AI audit log queries.
/// </summary>
public record AiAuditLogFilter
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? ModelId { get; init; }
    public string? FunctionName { get; init; }
    public bool? SuccessOnly { get; init; }
}

/// <summary>
/// Response containing audit statistics.
/// </summary>
public record AuditStatsResponse
{
    public int TotalRecords { get; init; }
    public int UniqueActors { get; init; }
    public Dictionary<string, int> ActionBreakdown { get; init; } = new();
    public Dictionary<string, int> ResourceBreakdown { get; init; } = new();
    public TimeRangeInfo TimeRange { get; init; } = new();
}

/// <summary>
/// Time range information for stats.
/// </summary>
public record TimeRangeInfo
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
}
