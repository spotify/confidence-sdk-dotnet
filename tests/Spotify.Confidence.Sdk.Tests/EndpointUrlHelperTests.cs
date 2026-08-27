using Spotify.Confidence.Sdk.Options;
using Xunit;

namespace Spotify.Confidence.Sdk.Tests;

public class EndpointUrlHelperTests
{
    [Theory]
    [InlineData("https://resolver.test", "https://resolver.test/")]
    [InlineData("https://resolver.test/custom", "https://resolver.test/custom/")]
    [InlineData("https://resolver.test/custom/", "https://resolver.test/custom/")]
    public void CreateBaseAddress_PreservesConfiguredBaseUrl(string baseUrl, string expected)
    {
        var uri = EndpointUrlHelper.CreateBaseAddress(baseUrl);

        Assert.Equal(expected, uri.ToString());
    }

    [Theory]
    [InlineData("https://resolver.test", "v1/flags:resolve", "https://resolver.test/v1/flags:resolve")]
    [InlineData("https://resolver.test/custom", "v1/flags:resolve", "https://resolver.test/custom/v1/flags:resolve")]
    [InlineData("https://events.test/custom", "v1/events:publish", "https://events.test/custom/v1/events:publish")]
    public void CreateBaseAddress_AllowsSdkPathsToAppendToCustomBaseUrl(string baseUrl, string path, string expected)
    {
        var uri = new Uri(EndpointUrlHelper.CreateBaseAddress(baseUrl), path);

        Assert.Equal(expected, uri.ToString());
    }
}
