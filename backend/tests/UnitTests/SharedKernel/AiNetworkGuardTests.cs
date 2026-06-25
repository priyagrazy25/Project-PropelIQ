using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SharedKernel.Validation;

namespace UnitTests.SharedKernel;

public sealed class AiNetworkGuardTests
{
    [Fact]
    public async Task StartAsync_DoesNothing_WhenEndpointNotConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var guard = new AiNetworkGuard(config, Mock.Of<ILogger<AiNetworkGuard>>());

        await guard.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_Throws_WhenEndpointIsInvalidUrl()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ollama:Endpoint"] = "not-a-url"
            })
            .Build();

        var guard = new AiNetworkGuard(config, Mock.Of<ILogger<AiNetworkGuard>>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => guard.StartAsync(CancellationToken.None));
        Assert.Contains("AIR-S01 violation", ex.Message);
    }

    [Fact]
    public async Task StartAsync_Throws_WhenEndpointResolvesToExternalAddress()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ollama:Endpoint"] = "http://8.8.8.8:11434"
            })
            .Build();

        var guard = new AiNetworkGuard(config, Mock.Of<ILogger<AiNetworkGuard>>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => guard.StartAsync(CancellationToken.None));
        Assert.Contains("must run locally", ex.Message);
    }

    [Theory]
    [InlineData("http://localhost:11434")]
    [InlineData("http://127.0.0.1:11434")]
    public async Task StartAsync_AllowsLocalEndpoints(string endpoint)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ollama:Endpoint"] = endpoint
            })
            .Build();

        var guard = new AiNetworkGuard(config, Mock.Of<ILogger<AiNetworkGuard>>());

        await guard.StartAsync(CancellationToken.None);
    }
}
