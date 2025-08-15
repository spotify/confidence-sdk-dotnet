using System.Text.Json.Serialization;

namespace Spotify.Confidence.OpenFeature.Local.Models;

/// <summary>
/// Represents a request to resolve a feature flag using the WASM resolver.
/// </summary>
public class ResolveRequest
{
    /// <summary>
    /// Gets or sets the flag key to resolve.
    /// </summary>
    [JsonPropertyName("flag")]
    public string Flag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the evaluation context for the flag resolution.
    /// </summary>
    [JsonPropertyName("context")]
    public Dictionary<string, object> Context { get; set; } = new();

    /// <summary>
    /// Gets or sets the client ID for authentication.
    /// </summary>
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client secret for authentication.
    /// </summary>
    [JsonPropertyName("clientSecret")]
    public string ClientSecret { get; set; } = string.Empty;
}
