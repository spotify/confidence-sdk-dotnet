namespace Spotify.Confidence.Sdk.Options;

/// <summary>
/// Helper class for endpoint URL handling.
/// </summary>
internal static class EndpointUrlHelper
{
    /// <summary>
    /// Creates an HTTP client base address that preserves custom base paths.
    /// </summary>
    /// <param name="baseUrl">The configured endpoint base URL.</param>
    /// <returns>The normalized base address.</returns>
    public static Uri CreateBaseAddress(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("Base URL is required", nameof(baseUrl));
        }

        var normalizedBaseUrl = baseUrl.EndsWith('/')
            ? baseUrl
            : $"{baseUrl}/";

        return new Uri(normalizedBaseUrl);
    }
}
