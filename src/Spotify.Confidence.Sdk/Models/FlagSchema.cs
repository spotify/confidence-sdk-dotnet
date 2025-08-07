using System.Text.Json.Serialization;

namespace Spotify.Confidence.Sdk.Models;

/// <summary>
/// Represents the schema of a flag.
/// </summary>
public class FlagSchema
{
    /// <summary>
    /// Gets or sets the schema definition.
    /// </summary>
    [JsonPropertyName("schema")]
    public Dictionary<string, object> Schema { get; set; } = new Dictionary<string, object>();
}
