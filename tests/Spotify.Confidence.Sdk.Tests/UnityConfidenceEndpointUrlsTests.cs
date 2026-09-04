using UnityOpenFeature.Providers;
using Xunit;

namespace Spotify.Confidence.Sdk.Tests;

public class UnityConfidenceEndpointUrlsTests
{
    [Theory]
    [InlineData("https://resolver.test", "https://resolver.test/v1/flags:resolve")]
    [InlineData("https://resolver.test/custom", "https://resolver.test/custom/v1/flags:resolve")]
    [InlineData("https://resolver.test/custom/", "https://resolver.test/custom/v1/flags:resolve")]
    public void Build_ReturnsCustomResolveUrl(string baseUrl, string expected)
    {
        var url = ConfidenceEndpointUrls.Build(baseUrl, ConfidenceEndpointUrls.ResolveFlagsPath);

        Assert.Equal(expected, url);
    }

    [Theory]
    [InlineData("https://resolver.test", "https://resolver.test/v1/flags:apply")]
    [InlineData("https://resolver.test/custom", "https://resolver.test/custom/v1/flags:apply")]
    [InlineData("https://resolver.test/custom/", "https://resolver.test/custom/v1/flags:apply")]
    public void Build_ReturnsCustomApplyLoggingUrl(string baseUrl, string expected)
    {
        var url = ConfidenceEndpointUrls.Build(baseUrl, ConfidenceEndpointUrls.ApplyFlagsPath);

        Assert.Equal(expected, url);
    }

    [Theory]
    [InlineData("https://resolver.test", "https://resolver.test/v1/telemetry:upload")]
    [InlineData("https://resolver.test/custom/", "https://resolver.test/custom/v1/telemetry:upload")]
    public void Build_ReturnsCustomTelemetryUrl(string baseUrl, string expected)
    {
        var url = ConfidenceEndpointUrls.Build(baseUrl, ConfidenceEndpointUrls.TelemetryPath);

        Assert.Equal(expected, url);
    }
}
