namespace Identity.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, string role, string fullName, Guid? patientId = null);
    string GenerateRefreshToken();
}
