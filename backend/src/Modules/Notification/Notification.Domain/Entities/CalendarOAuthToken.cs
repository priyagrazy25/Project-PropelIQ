using SharedKernel.Domain;

namespace Notification.Domain.Entities;

/// <summary>
/// Stores encrypted OAuth tokens per patient per calendar provider.
/// </summary>
public sealed class CalendarOAuthToken : BaseEntity
{
    public Guid PatientId { get; set; }
    public string Provider { get; set; } = string.Empty;       // "Google" or "Outlook"
    public string EncryptedAccessToken { get; set; } = string.Empty;
    public string EncryptedRefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
}
