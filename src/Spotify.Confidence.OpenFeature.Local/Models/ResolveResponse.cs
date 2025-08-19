using System.Text.Json.Serialization;

namespace Spotify.Confidence.OpenFeature.Local.Models;

/// <summary>
/// Represents the response from a WASM flag resolution.
/// </summary>
public class ResolveResponse
{
    /// <summary>
    /// Gets or sets the resolved value as a dictionary.
    /// </summary>
    [JsonPropertyName("value")]
    public Dictionary<string, object>? Value { get; set; }

    /// <summary>
    /// Gets or sets the variant identifier.
    /// </summary>
    [JsonPropertyName("variant")]
    public string? Variant { get; set; }

    /// <summary>
    /// Gets or sets the reason for the resolution result.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the resolution was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets any error message if the resolution failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
