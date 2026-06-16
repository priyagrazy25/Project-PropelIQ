using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notification.Application.Channels;
using Polly;
using Polly.CircuitBreaker;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Notification.Infrastructure.Channels;

public sealed class SendGridEmailChannel : IEmailChannel
{
    private readonly ILogger<SendGridEmailChannel> _logger;
    private readonly SendGridClient? _client;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly ResiliencePipeline _resiliencePipeline;

    public SendGridEmailChannel(IConfiguration configuration, ILogger<SendGridEmailChannel> logger)
    {
        _logger = logger;

        var apiKey = configuration["SendGrid:ApiKey"] ?? string.Empty;
        _fromEmail = configuration["SendGrid:FromEmail"] ?? "noreply@upap.com";
        _fromName = configuration["SendGrid:FromName"] ?? "Unified Patient Access";

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _client = new SendGridClient(apiKey);
        }

        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(2),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Email retry attempt {Attempt} after {Delay}ms",
                        args.AttemptNumber + 1,
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                MinimumThroughput = 3,
                SamplingDuration = TimeSpan.FromSeconds(60),
                BreakDuration = TimeSpan.FromSeconds(120),
                OnOpened = args =>
                {
                    _logger.LogError("Email circuit breaker OPENED — halting email dispatch for {Duration}s", args.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Email circuit breaker CLOSED — resuming email dispatch");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<ChannelResult> SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (_client is null)
        {
            _logger.LogWarning("SendGrid API key not configured — skipping email to {Email}", toEmail);
            return new ChannelResult(false, "SendGrid API key not configured");
        }

        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, body, body);
                var response = await _client.SendEmailAsync(msg, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Body.ReadAsStringAsync(ct);
                    throw new InvalidOperationException($"SendGrid returned {response.StatusCode}: {responseBody}");
                }
            }, cancellationToken);

            _logger.LogInformation("Email sent to {Email}", toEmail);
            return new ChannelResult(true);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Email circuit breaker is open — skipping email to {Email}", toEmail);
            return new ChannelResult(false, "Email circuit breaker open");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email after retries");
            return new ChannelResult(false, ex.Message);
        }
    }

    public async Task<ChannelResult> SendWithAttachmentAsync(
        string toEmail,
        string subject,
        string body,
        byte[] attachmentContent,
        string attachmentFileName,
        string attachmentMimeType,
        CancellationToken cancellationToken = default)
    {
        if (_client is null)
        {
            _logger.LogWarning("SendGrid API key not configured — skipping email with attachment to {Email}", toEmail);
            return new ChannelResult(false, "SendGrid API key not configured");
        }

        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, body, body);

                msg.AddAttachment(new Attachment
                {
                    Content = Convert.ToBase64String(attachmentContent),
                    Filename = attachmentFileName,
                    Type = attachmentMimeType,
                    Disposition = "attachment"
                });

                var response = await _client.SendEmailAsync(msg, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Body.ReadAsStringAsync(ct);
                    throw new InvalidOperationException($"SendGrid returned {response.StatusCode}: {responseBody}");
                }
            }, cancellationToken);

            _logger.LogInformation("Email with attachment sent to {Email}", toEmail);
            return new ChannelResult(true);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Email circuit breaker is open — skipping email with attachment to {Email}", toEmail);
            return new ChannelResult(false, "Email circuit breaker open");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachment after retries");
            return new ChannelResult(false, ex.Message);
        }
    }
}
