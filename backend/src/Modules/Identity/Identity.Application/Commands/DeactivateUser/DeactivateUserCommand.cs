namespace Identity.Application.Commands.DeactivateUser;

public sealed record DeactivateUserCommand(Guid TargetUserId, Guid AdminUserId);
