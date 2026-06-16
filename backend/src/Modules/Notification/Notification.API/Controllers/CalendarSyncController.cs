using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notification.Application.Abstractions;
using Notification.Domain.Entities;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/notification/calendar")]
public class CalendarSyncController : ControllerBase
{
    private readonly ICalendarOAuthTokenRepository _tokenRepository;
    private readonly ICalendarSyncRecordRepository _syncRecordRepository;
    private readonly IDataProtector _protector;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CalendarSyncController> _logger;

    public CalendarSyncController(
        ICalendarOAuthTokenRepository tokenRepository,
        ICalendarSyncRecordRepository syncRecordRepository,
        IDataProtectionProvider dataProtectionProvider,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<CalendarSyncController> logger)
    {
        _tokenRepository = tokenRepository;
        _syncRecordRepository = syncRecordRepository;
        _protector = dataProtectionProvider.CreateProtector("CalendarOAuth");
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Initiates OAuth flow for the specified calendar provider.
    /// </summary>
    [Authorize]
    [HttpGet("connect/{provider}")]
    public IActionResult Connect(string provider)
    {
        var patientId = GetPatientId();
        if (patientId is null)
            return Unauthorized();

        var redirectUrl = provider.ToLowerInvariant() switch
        {
            "google" => BuildGoogleAuthUrl(patientId.Value),
            "outlook" => BuildOutlookAuthUrl(patientId.Value),
            _ => null
        };

        if (redirectUrl is null)
            return BadRequest(new { error = "Unsupported calendar provider. Use 'google' or 'outlook'." });

        return Ok(new { authUrl = redirectUrl });
    }

    /// <summary>
    /// OAuth callback for Google Calendar. Exchanges authorization code for tokens.
    /// </summary>
    [HttpGet("callback/google")]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return BadRequest(new { error = "Missing authorization code or state." });

        if (!Guid.TryParse(state, out var patientId))
            return BadRequest(new { error = "Invalid state parameter." });

        var clientId = _configuration["CalendarSync:Google:ClientId"];
        var clientSecret = _configuration["CalendarSync:Google:ClientSecret"];
        var redirectUri = _configuration["CalendarSync:Google:RedirectUri"];

        using var httpClient = _httpClientFactory.CreateClient();
        var tokenResponse = await httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId ?? string.Empty,
                ["client_secret"] = clientSecret ?? string.Empty,
                ["redirect_uri"] = redirectUri ?? string.Empty,
                ["grant_type"] = "authorization_code"
            }),
            cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Google OAuth token exchange failed for patient {PatientId}", patientId);
            return OAuthCallbackErrorHtml("Failed to exchange authorization code.");
        }

        var tokens = await tokenResponse.Content.ReadFromJsonAsync<OAuthTokenResponse>(cancellationToken);
        if (tokens is null)
            return OAuthCallbackErrorHtml("Invalid token response.");

        await StoreTokensAsync(patientId, "Google", tokens, cancellationToken);

        _logger.LogInformation("Google Calendar connected for patient {PatientId}", patientId);
        return OAuthCallbackHtml(tokens.AccessToken);
    }

    /// <summary>
    /// OAuth callback for Outlook Calendar. Exchanges authorization code for tokens.
    /// </summary>
    [HttpGet("callback/outlook")]
    public async Task<IActionResult> OutlookCallback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return BadRequest(new { error = "Missing authorization code or state." });

        if (!Guid.TryParse(state, out var patientId))
            return BadRequest(new { error = "Invalid state parameter." });

        var clientId = _configuration["CalendarSync:Outlook:ClientId"];
        var clientSecret = _configuration["CalendarSync:Outlook:ClientSecret"];
        var redirectUri = _configuration["CalendarSync:Outlook:RedirectUri"];
        var tenantId = _configuration["CalendarSync:Outlook:TenantId"] ?? "common";

        using var httpClient = _httpClientFactory.CreateClient();
        var tokenResponse = await httpClient.PostAsync(
            $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId ?? string.Empty,
                ["client_secret"] = clientSecret ?? string.Empty,
                ["redirect_uri"] = redirectUri ?? string.Empty,
                ["grant_type"] = "authorization_code",
                ["scope"] = "Calendars.ReadWrite offline_access"
            }),
            cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Outlook OAuth token exchange failed for patient {PatientId}", patientId);
            return OAuthCallbackErrorHtml("Failed to exchange authorization code.");
        }

        var tokens = await tokenResponse.Content.ReadFromJsonAsync<OAuthTokenResponse>(cancellationToken);
        if (tokens is null)
            return OAuthCallbackErrorHtml("Invalid token response.");

        await StoreTokensAsync(patientId, "Outlook", tokens, cancellationToken);

        _logger.LogInformation("Outlook Calendar connected for patient {PatientId}", patientId);
        return OAuthCallbackHtml(tokens.AccessToken);
    }

    /// <summary>
    /// Returns the sync status for a given appointment.
    /// </summary>
    [Authorize]
    [HttpGet("status/{appointmentId:guid}")]
    public async Task<IActionResult> GetSyncStatus(Guid appointmentId, CancellationToken cancellationToken)
    {
        var records = await _syncRecordRepository.GetByAppointmentAsync(appointmentId, cancellationToken);
        return Ok(records.Select(r => new
        {
            r.Provider,
            r.ExternalEventId,
            r.Status,
            r.LastSyncedAt
        }));
    }

    /// <summary>
    /// Disconnects a calendar provider for the current patient.
    /// </summary>
    [Authorize]
    [HttpDelete("disconnect/{provider}")]
    public async Task<IActionResult> Disconnect(string provider, CancellationToken cancellationToken)
    {
        var patientId = GetPatientId();
        if (patientId is null)
            return Unauthorized();

        var normalizedProvider = provider.ToLowerInvariant() switch
        {
            "google" => "Google",
            "outlook" => "Outlook",
            _ => null
        };

        if (normalizedProvider is null)
            return BadRequest(new { error = "Unsupported calendar provider." });

        await _tokenRepository.DeleteAsync(patientId.Value, normalizedProvider, cancellationToken);

        _logger.LogInformation(
            "Calendar provider {Provider} disconnected for patient {PatientId}",
            normalizedProvider, patientId.Value);

        return Ok(new { message = $"{normalizedProvider} Calendar disconnected." });
    }

    private string BuildGoogleAuthUrl(Guid patientId)
    {
        var clientId = _configuration["CalendarSync:Google:ClientId"];
        var redirectUri = _configuration["CalendarSync:Google:RedirectUri"];
        var scope = Uri.EscapeDataString("https://www.googleapis.com/auth/calendar");

        return $"https://accounts.google.com/o/oauth2/v2/auth" +
               $"?client_id={clientId}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri ?? string.Empty)}" +
               $"&response_type=code" +
               $"&scope={scope}" +
               $"&access_type=offline" +
               $"&prompt=consent" +
               $"&state={patientId}";
    }

    private string BuildOutlookAuthUrl(Guid patientId)
    {
        var clientId = _configuration["CalendarSync:Outlook:ClientId"];
        var redirectUri = _configuration["CalendarSync:Outlook:RedirectUri"];
        var tenantId = _configuration["CalendarSync:Outlook:TenantId"] ?? "common";
        var scope = Uri.EscapeDataString("Calendars.ReadWrite offline_access");

        return $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize" +
               $"?client_id={clientId}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri ?? string.Empty)}" +
               $"&response_type=code" +
               $"&scope={scope}" +
               $"&state={patientId}";
    }

    private async Task StoreTokensAsync(
        Guid patientId,
        string provider,
        OAuthTokenResponse tokens,
        CancellationToken cancellationToken)
    {
        var oauthToken = new CalendarOAuthToken
        {
            PatientId = patientId,
            Provider = provider,
            EncryptedAccessToken = _protector.Protect(tokens.AccessToken ?? string.Empty),
            EncryptedRefreshToken = _protector.Protect(tokens.RefreshToken ?? string.Empty),
            AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn),
        };

        await _tokenRepository.UpsertAsync(oauthToken, cancellationToken);
    }

    private Guid? GetPatientId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private ContentResult OAuthCallbackHtml(string? accessToken)
    {
        var html = $$"""
            <!DOCTYPE html>
            <html><head><title>Calendar Connected</title></head>
            <body>
            <p>Calendar connected. This window will close automatically.</p>
            <script>
              if (window.opener) {
                window.opener.postMessage(
                  { type: 'calendar-oauth-callback', token: '{{accessToken?.Replace("'", "\\'")}}' },
                  '*'
                );
              }
              setTimeout(function() { window.close(); }, 300);
            </script>
            </body></html>
            """;

        return Content(html, "text/html");
    }

    private ContentResult OAuthCallbackErrorHtml(string error)
    {
        var html = $$"""
            <!DOCTYPE html>
            <html><head><title>Calendar Connection Failed</title></head>
            <body>
            <p>Calendar connection failed. This window will close automatically.</p>
            <script>
              if (window.opener) {
                window.opener.postMessage(
                  { type: 'calendar-oauth-callback', error: '{{error.Replace("'", "\\'")}}' },
                  '*'
                );
              }
              setTimeout(function() { window.close(); }, 300);
            </script>
            </body></html>
            """;

        return Content(html, "text/html");
    }

    private sealed record OAuthTokenResponse(
        string? AccessToken,
        string? RefreshToken,
        int ExpiresIn)
    {
        // Support JSON property names from OAuth providers
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; init; } = AccessToken;

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; } = RefreshToken;

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; } = ExpiresIn;
    }
}
