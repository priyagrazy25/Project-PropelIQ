using System.Security.Claims;
using Identity.Application.Commands.CreateUser;
using Identity.Application.Commands.DeactivateUser;
using Identity.Application.Commands.ReactivateUser;
using Identity.Application.Commands.UpdateUser;
using Identity.Application.Queries.GetUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Identity.API.Controllers;

/// <summary>
/// Admin-only user management endpoints for CRUD operations and role assignment.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "AdminPolicy")]
[Produces("application/json")]
public class AdminUsersController : ControllerBase
{
    private readonly GetUsersQueryHandler _getUsersHandler;
    private readonly CreateUserCommandHandler _createUserHandler;
    private readonly UpdateUserCommandHandler _updateUserHandler;
    private readonly DeactivateUserCommandHandler _deactivateUserHandler;
    private readonly ReactivateUserCommandHandler _reactivateUserHandler;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        GetUsersQueryHandler getUsersHandler,
        CreateUserCommandHandler createUserHandler,
        UpdateUserCommandHandler updateUserHandler,
        DeactivateUserCommandHandler deactivateUserHandler,
        ReactivateUserCommandHandler reactivateUserHandler,
        ILogger<AdminUsersController> logger)
    {
        _getUsersHandler = getUsersHandler;
        _createUserHandler = createUserHandler;
        _updateUserHandler = updateUserHandler;
        _deactivateUserHandler = deactivateUserHandler;
        _reactivateUserHandler = reactivateUserHandler;
        _logger = logger;
    }

    /// <summary>
    /// Returns a paginated list of users with optional search and filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetUsersResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersQuery(page, pageSize, search, role, status);
        var result = await _getUsersHandler.HandleAsync(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Creates a new user with the specified role.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateUserCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Role,
            request.Phone);

        var result = await _createUserHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "DUPLICATE_EMAIL" => Problem(
                    detail: "A user with this email address already exists.",
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Conflict"),
                "INVALID_ROLE" => Problem(
                    detail: "The specified role is not valid.",
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Unprocessable Entity"),
                _ => Problem(
                    detail: result.Error,
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Validation Error"),
            };
        }

        var userId = result.Value!.UserId;
        var userDto = new AdminUserDto(
            userId,
            $"{request.FirstName.Trim()} {request.LastName.Trim()}",
            request.Email.Trim().ToLowerInvariant(),
            request.Role,
            "Active",
            request.Phone,
            null);

        return CreatedAtAction(nameof(GetUsers), new { page = 1 }, userDto);
    }

    /// <summary>
    /// Updates an existing user's details and role.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateUserCommand(
            id,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Role,
            request.Phone);

        var result = await _updateUserHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "USER_NOT_FOUND" => Problem(
                    detail: "The specified user was not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found"),
                "DUPLICATE_EMAIL" => Problem(
                    detail: "A user with this email address already exists.",
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Conflict"),
                "INVALID_ROLE" => Problem(
                    detail: "The specified role is not valid.",
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Unprocessable Entity"),
                _ => Problem(
                    detail: result.Error,
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Validation Error"),
            };
        }

        var userDto = new AdminUserDto(
            id,
            $"{request.FirstName.Trim()} {request.LastName.Trim()}",
            request.Email.Trim().ToLowerInvariant(),
            request.Role,
            "Active",
            request.Phone,
            null);

        return Ok(userDto);
    }

    /// <summary>
    /// Deactivates a user account. Admins cannot deactivate their own account.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var adminUserId = GetCurrentUserId();
        if (adminUserId is null)
        {
            return Unauthorized();
        }

        var command = new DeactivateUserCommand(id, adminUserId.Value);
        var result = await _deactivateUserHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "SELF_DEACTIVATION" => Problem(
                    detail: "You cannot deactivate your own account.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request"),
                "USER_NOT_FOUND" => Problem(
                    detail: "The specified user was not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found"),
                "ALREADY_INACTIVE" => Problem(
                    detail: "The user account is already inactive.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request"),
                _ => Problem(
                    detail: result.Error,
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Error"),
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Reactivates a previously deactivated user account.
    /// </summary>
    [HttpPost("{id:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateUser(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new ReactivateUserCommand(id);
        var result = await _reactivateUserHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "USER_NOT_FOUND" => Problem(
                    detail: "The specified user was not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found"),
                "ALREADY_ACTIVE" => Problem(
                    detail: "The user account is already active.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request"),
                _ => Problem(
                    detail: result.Error,
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Error"),
            };
        }

        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue("sub")
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(sub, out var userId) ? userId : null;
    }
}

// DTOs for request bodies
public sealed record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string? Phone);

public sealed record UpdateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string? Phone);
