using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notification.Application.Channels;
using Polly;
using Polly.CircuitBreaker;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Notification.Infrastructure.Channels;

public sealed class TwilioSmsChannel : ISmsChannel
{
    private readonly ILogger<TwilioSmsChannel> _logger;
    private readonly string _fromNumber;
    private readonly ResiliencePipeline _resiliencePipeline;

    public TwilioSmsChannel(IConfiguration configuration, ILogger<TwilioSmsChannel> logger)
    {
        _logger = logger;

        var accountSid = configuration["Twilio:AccountSid"] ?? string.Empty;
        var authToken = configuration["Twilio:AuthToken"] ?? string.Empty;
        _fromNumber = configuration["Twilio:FromNumber"] ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(accountSid) && !string.IsNullOrWhiteSpace(authToken))
        {
            TwilioClient.Init(accountSid, authToken);
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
                        "SMS retry attempt {Attempt} after {Delay}ms",
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
                    _logger.LogError("SMS circuit breaker OPENED — halting SMS dispatch for {Duration}s", args.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("SMS circuit breaker CLOSED — resuming SMS dispatch");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<ChannelResult> SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await MessageResource.CreateAsync(
                    to: new PhoneNumber(phoneNumber),
                    from: new PhoneNumber(_fromNumber),
                    body: message);
            }, cancellationToken);

            _logger.LogInformation("SMS sent to {Phone}", phoneNumber[..Math.Min(4, phoneNumber.Length)] + "****");
            return new ChannelResult(true);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("SMS circuit breaker is open — skipping SMS to {Phone}", phoneNumber[..Math.Min(4, phoneNumber.Length)] + "****");
            return new ChannelResult(false, "SMS circuit breaker open");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS after retries");
            return new ChannelResult(false, ex.Message);
        }
    }
}
