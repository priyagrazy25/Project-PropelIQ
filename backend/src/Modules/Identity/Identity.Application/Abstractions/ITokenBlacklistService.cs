namespace Identity.Application.Abstractions;

/// <summary>
/// Blacklists invalidated JWT access tokens until their natural expiry.
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>Adds a JWT ID (jti) to the blacklist with the given TTL.</summary>
    Task BlacklistTokenAsync(string jti, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a JWT ID has been blacklisted.</summary>
    Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default);
}
