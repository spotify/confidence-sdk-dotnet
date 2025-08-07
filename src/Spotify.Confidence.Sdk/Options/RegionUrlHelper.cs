namespace Spotify.Confidence.Sdk.Options;

/// <summary>
/// Helper class for managing region-specific URLs.
/// </summary>
internal static class RegionUrlHelper
{
    /// <summary>
    /// Gets the resolver URL for the specified region.
    /// </summary>
    /// <param name="region">The region to get the URL for.</param>
    /// <returns>The resolver URL for the region.</returns>
    public static string GetResolverUrl(Region region) => region switch
    {
        Region.EU => "https://resolver.eu.confidence.dev",
        Region.US => "https://resolver.us.confidence.dev",
        Region.Global => "https://resolver.confidence.dev",
        _ => "https://resolver.confidence.dev"
    };

    /// <summary>
    /// Gets the event tracking URL for the specified region.
    /// </summary>
    /// <param name="region">The region to get the URL for.</param>
    /// <returns>The event tracking URL for the region.</returns>
    public static string GetEventTrackingUrl(Region region) => region switch
    {
        Region.EU => "https://events.eu.confidence.dev",
        Region.US => "https://events.us.confidence.dev",
        Region.Global => "https://events.confidence.dev",
        _ => "https://events.confidence.dev"
    };
}
