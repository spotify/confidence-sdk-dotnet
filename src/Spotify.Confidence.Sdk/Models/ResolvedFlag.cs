using System.Text.Json.Serialization;

namespace Spotify.Confidence.Sdk.Models;

/// <summary>
/// Represents a resolved flag from the Confidence service.
/// </summary>
public class ResolvedFlag
{
    /// <summary>
    /// Gets or sets the flag key.
    /// </summary>
    public string Flag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the variant.
    /// </summary>
    public string Variant { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public Dictionary<string, object> Value { get; set; } = new();

    /// <summary>
    /// Gets or sets the flag schema.
    /// </summary>
    public FlagSchema FlagSchema { get; set; } = new();
}
