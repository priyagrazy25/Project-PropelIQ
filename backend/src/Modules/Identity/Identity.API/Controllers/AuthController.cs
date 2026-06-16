using System.Security.Claims;
using Identity.Application.Commands.ExtendSession;
using Identity.Application.Commands.Login;
using Identity.Application.Commands.Logout;
using Identity.Application.Commands.RefreshToken;
using Identity.Application.Commands.Register;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedKernel.Audit;

namespace Identity.API.Controllers;

/// <summary>
/// Authentication endpoints for patient registration, login, and token management.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly RegisterCommandHandler _registerHandler;
    private readonly LoginCommandHandler _loginHandler;
    private readonly RefreshTokenCommandHandler _refreshHandler;
    private readonly ExtendSessionCommandHandler _extendSessionHandler;
    private readonly LogoutCommandHandler _logoutHandler;
    private readonly IAuditService? _auditService;
    private readonly ILogger<AuthController> _logger;
    private readonly IHostEnvironment _environment;

    public AuthController(
        RegisterCommandHandler registerHandler,
        LoginCommandHandler loginHandler,
        RefreshTokenCommandHandler refreshHandler,
        ExtendSessionCommandHandler extendSessionHandler,
        LogoutCommandHandler logoutHandler,
        ILogger<AuthController> logger,
        IHostEnvironment environment,
        IAuditService? auditService = null)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _refreshHandler = refreshHandler;
        _extendSessionHandler = extendSessionHandler;
        _logoutHandler = logoutHandler;
        _logger = logger;
        _environment = environment;
        _auditService = auditService;
    }

    /// <summary>
    /// Registers a new patient user account.
    /// </summary>
    /// <param name="request">Registration details including name, email, DOB, phone, and password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with user ID on success.</returns>
    /// <response code="201">User account created successfully.</response>
    /// <response code="400">Validation failed — malformed or missing fields.</response>
    /// <response code="409">Email address already registered.</response>
    /// <response code="503">Service temporarily unavailable — retry later.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new ErrorResponse("Request body is required."));
        }

        var command = new RegisterCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.DateOfBirth,
            request.Gender,
            request.Password);

        try
        {
            var result = await _registerHandler.HandleAsync(command, cancellationToken);

            if (!result.IsSuccess)
            {
                if (result.Error == "DUPLICATE_EMAIL")
                {
                    return Conflict(new ErrorResponse("An account with this email address already exists."));
                }

                return BadRequest(new ErrorResponse(result.Error ?? "Validation failed."));
            }

            var response = new RegisterResponse(result.Value!.UserId);
            return CreatedAtAction(nameof(Register), new { id = response.UserId }, response);
        }
        catch (Exception ex) when (ex is DbUpdateException or TimeoutException)
        {
            _logger.LogError(ex, "Database error during patient registration");
            Response.Headers.Append("Retry-After", "30");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse("Service temporarily unavailable. Please try again later."));
        }
    }

    /// <summary>
    /// Authenticates a user and returns JWT access token + refresh token.
    /// </summary>
    /// <param name="request">Email and password credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JWT access token and user info on success.</returns>
    /// <response code="200">Authentication successful.</response>
    /// <response code="401">Invalid credentials.</response>
    /// <response code="423">Account locked.</response>
    /// <response code="503">Service temporarily unavailable.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthRateLimit")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status423Locked)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new ErrorResponse("Request body is required."));
        }

        var deviceId = Request.Headers["X-Device-Id"].FirstOrDefault();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            var command = new LoginCommand(request.Email, request.Password, deviceId);
            var result = await _loginHandler.HandleAsync(command, cancellationToken);

            if (!result.IsSuccess)
            {
                // Log failed login attempt
                if (_auditService != null)
                {
                    await _auditService.LogActionAsync(new AuditEntry
                    {
                        ActorName = request.Email,
                        Action = "LoginFailed",
                        Resource = "Authentication",
                        IpAddress = ipAddress,
                        CorrelationId = HttpContext.TraceIdentifier,
                        AfterState = new { reason = result.Error }
                    }, cancellationToken);
                }

                if (result.Error == "ACCOUNT_INACTIVE")
                {
                    return StatusCode(StatusCodes.Status423Locked,
                        new ErrorResponse("Your account is not active. Please contact support."));
                }

                return Unauthorized(new LoginErrorResponse("Invalid email or password."));
            }

            // Log successful login
            if (_auditService != null)
            {
                await _auditService.LogActionAsync(new AuditEntry
                {
                    ActorId = result.Value!.UserId,
                    ActorName = result.Value.FullName,
                    Action = "Login",
                    Resource = "Authentication",
                    ResourceId = result.Value.UserId.ToString(),
                    IpAddress = ipAddress,
                    CorrelationId = HttpContext.TraceIdentifier,
                    AfterState = new { role = result.Value.Role, deviceId }
                }, cancellationToken);
            }

            SetRefreshTokenCookie(result.Value!.RefreshToken);

            return Ok(new LoginResponse(
                result.Value.AccessToken,
                result.Value.UserId,
                result.Value.Role,
                result.Value.FullName));
        }
        catch (Exception ex) when (ex is DbUpdateException or TimeoutException)
        {
            _logger.LogError(ex, "Database error during login");
            Response.Headers.Append("Retry-After", "30");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse("Service temporarily unavailable. Please try again later."));
        }
    }

    /// <summary>
    /// Refreshes an access token using a valid refresh token from httpOnly cookie.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New JWT access token and rotated refresh token.</returns>
    /// <response code="200">Token refreshed successfully.</response>
    /// <response code="401">Invalid or expired refresh token.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(new ErrorResponse("Refresh token is required."));
        }

        var deviceId = Request.Headers["X-Device-Id"].FirstOrDefault();

        try
        {
            var command = new RefreshTokenCommand(refreshToken, deviceId);
            var result = await _refreshHandler.HandleAsync(command, cancellationToken);

            if (!result.IsSuccess)
            {
                if (result.Error == "TOKEN_REUSE_DETECTED")
                {
                    ClearRefreshTokenCookie();
                    _logger.LogWarning("Refresh token reuse detected — all user tokens revoked");
                    return Unauthorized(new ErrorResponse("Security violation detected. Please log in again."));
                }

                ClearRefreshTokenCookie();
                return Unauthorized(new ErrorResponse("Session expired. Please log in again."));
            }

            SetRefreshTokenCookie(result.Value!.RefreshToken);

            return Ok(new RefreshResponse(result.Value.AccessToken));
        }
        catch (Exception ex) when (ex is DbUpdateException or TimeoutException)
        {
            _logger.LogError(ex, "Database error during token refresh");
            Response.Headers.Append("Retry-After", "30");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse("Service temporarily unavailable. Please try again later."));
        }
    }

    /// <summary>
    /// Extends the current session by validating the refresh token and issuing new tokens.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New JWT access token and rotated refresh token.</returns>
    /// <response code="200">Session extended successfully.</response>
    /// <response code="401">Invalid, expired, or reused refresh token.</response>
    /// <response code="503">Service temporarily unavailable.</response>
    [HttpPost("extend-session")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ExtendSession(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(new ErrorResponse("Refresh token is required."));
        }

        var deviceId = Request.Headers["X-Device-Id"].FirstOrDefault();

        try
        {
            var command = new ExtendSessionCommand(refreshToken, deviceId);
            var result = await _extendSessionHandler.HandleAsync(command, cancellationToken);

            if (!result.IsSuccess)
            {
                if (result.Error == "TOKEN_REUSE_DETECTED")
                {
                    ClearRefreshTokenCookie();
                    _logger.LogWarning("Token reuse detected during session extension — all user tokens revoked");
                    return Unauthorized(new ErrorResponse("Security violation detected. Please log in again."));
                }

                ClearRefreshTokenCookie();
                return Unauthorized(new ErrorResponse("Session expired. Please log in again."));
            }

            SetRefreshTokenCookie(result.Value!.RefreshToken);

            return Ok(new RefreshResponse(result.Value.AccessToken));
        }
        catch (Exception ex) when (ex is DbUpdateException or TimeoutException)
        {
            _logger.LogError(ex, "Database error during session extension");
            Response.Headers.Append("Retry-After", "30");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse("Service temporarily unavailable. Please try again later."));
        }
    }

    /// <summary>
    /// Logs out the current user, invalidating all tokens and sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    /// <response code="204">Logout successful.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="503">Service temporarily unavailable.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst("sub")
            ?? User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new ErrorResponse("Invalid token."));
        }

        var userName = User.FindFirst(ClaimTypes.Name)?.Value
                       ?? User.FindFirst("name")?.Value
                       ?? User.Identity?.Name
                       ?? "Unknown";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var jti = User.FindFirst("jti")?.Value;
        var expClaim = User.FindFirst("exp")?.Value;
        TimeSpan? remainingLifetime = null;

        if (expClaim is not null && long.TryParse(expClaim, out var expUnix))
        {
            var expiry = DateTimeOffset.FromUnixTimeSeconds(expUnix);
            var remaining = expiry - DateTimeOffset.UtcNow;
            if (remaining > TimeSpan.Zero)
            {
                remainingLifetime = remaining;
            }
        }

        try
        {
            var command = new LogoutCommand(userId, jti, remainingLifetime);
            await _logoutHandler.HandleAsync(command, cancellationToken);

            // Log logout action
            if (_auditService != null)
            {
                await _auditService.LogActionAsync(new AuditEntry
                {
                    ActorId = userId,
                    ActorName = userName,
                    Action = "Logout",
                    Resource = "Authentication",
                    ResourceId = userId.ToString(),
                    IpAddress = ipAddress,
                    CorrelationId = HttpContext.TraceIdentifier
                }, cancellationToken);
            }

            ClearRefreshTokenCookie();
            return NoContent();
        }
        catch (Exception ex) when (ex is DbUpdateException or TimeoutException)
        {
            _logger.LogError(ex, "Database error during logout for user {UserId}", userId);
            Response.Headers.Append("Retry-After", "30");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse("Service temporarily unavailable. Please try again later."));
        }
    }

    private void SetRefreshTokenCookie(string token)
    {
        var isProduction = _environment.IsProduction();
        Response.Cookies.Append("refreshToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/api/auth",
        });
    }

    private void ClearRefreshTokenCookie()
    {
        var isProduction = _environment.IsProduction();
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax,
            Path = "/api/auth",
        });
    }
}

/// <summary>Registration request DTO.</summary>
public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string DateOfBirth,
    string? Gender,
    string Password);

/// <summary>Registration success response.</summary>
public sealed record RegisterResponse(Guid UserId);

/// <summary>Login request DTO.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Login success response (access token in body, refresh token in httpOnly cookie).</summary>
public sealed record LoginResponse(string AccessToken, Guid UserId, string Role, string FullName);

/// <summary>Login error response with optional remaining attempts info.</summary>
public sealed record LoginErrorResponse(string Message, int? RemainingAttempts = null);

/// <summary>Refresh token response.</summary>
public sealed record RefreshResponse(string AccessToken);

/// <summary>Error response envelope.</summary>
public sealed record ErrorResponse(string Message);
