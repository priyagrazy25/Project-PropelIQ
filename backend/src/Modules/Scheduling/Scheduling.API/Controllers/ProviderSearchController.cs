using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Queries.GetProviderSlots;
using Scheduling.Application.Queries.SearchProviders;
using System.Security.Cryptography;
using System.Text;

namespace Scheduling.API.Controllers;

/// <summary>
/// Provider search and slot availability endpoints for patient appointment booking.
/// </summary>
[ApiController]
[Route("api/scheduling/providers")]
[Authorize]
[Produces("application/json")]
public class ProviderSearchController : ControllerBase
{
    private readonly SearchProvidersQueryHandler _searchHandler;
    private readonly GetProviderSlotsQueryHandler _slotsHandler;
    private readonly ISchedulingDbContext _dbContext;
    private readonly ILogger<ProviderSearchController> _logger;

    public ProviderSearchController(
        SearchProvidersQueryHandler searchHandler,
        GetProviderSlotsQueryHandler slotsHandler,
        ISchedulingDbContext dbContext,
        ILogger<ProviderSearchController> logger)
    {
        _searchHandler = searchHandler;
        _slotsHandler = slotsHandler;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Returns all active providers for walk-in booking selection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available providers.</returns>
    /// <response code="200">Returns active providers.</response>
    [HttpGet("available")]
    [ProducesResponseType(typeof(IReadOnlyList<AvailableProviderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableProviders(CancellationToken cancellationToken = default)
    {
        var providers = await _dbContext.Providers
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .Select(p => new AvailableProviderDto(
                p.Id.ToString(),
                p.Name,
                p.Specialty ?? "General"))
            .ToListAsync(cancellationToken);

        return Ok(providers);
    }

    /// <summary>
    /// Searches providers by name, specialty, location, and date with pagination.
    /// </summary>
    /// <param name="name">Partial provider name filter.</param>
    /// <param name="specialty">Specialty filter.</param>
    /// <param name="location">Location filter.</param>
    /// <param name="date">Available date filter (defaults to today).</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Results per page (default 20, max 100).</param>
    /// <param name="sortBy">Sort order: earliest, name, rating (default earliest).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of providers with available slots.</returns>
    /// <response code="200">Returns matching providers.</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchProvidersResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? name,
        [FromQuery] string? specialty,
        [FromQuery] string? location,
        [FromQuery] DateTime? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchProvidersQuery(name, specialty, location, date, page, pageSize, sortBy);

        var result = await _searchHandler.HandleAsync(query, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Provider search failed: {Error}", result.Error);
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Search Error");
        }

        var payload = System.Text.Json.JsonSerializer.Serialize(result.Value);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        var etag = $"\"{Convert.ToHexString(hash)}\"";

        if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var ifNoneMatch) &&
            ifNoneMatch.Any(value => string.Equals(value, etag, StringComparison.Ordinal)))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers.ETag = etag;

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets available slots for a specific provider on a given date.
    /// </summary>
    /// <param name="id">Provider identifier.</param>
    /// <param name="date">Date to query slots for (defaults to today).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of time slots for the provider.</returns>
    /// <response code="200">Returns provider slots.</response>
    /// <response code="404">Provider not found.</response>
    [HttpGet("{id:guid}/slots")]
    [ProducesResponseType(typeof(GetProviderSlotsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSlots(
        Guid id,
        [FromQuery] DateTime? date,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProviderSlotsQuery(id, date);

        var result = await _slotsHandler.HandleAsync(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        return Ok(result.Value);
    }
}

/// <summary>
/// DTO for available provider listing.
/// </summary>
public sealed record AvailableProviderDto(string Id, string Name, string Specialty);
