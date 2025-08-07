using System.Text.Json.Serialization;

namespace Spotify.Confidence.Sdk.Models;

/// <summary>
/// Represents the context for flag evaluation.
/// </summary>
public class ConfidenceContext
{
    /// <summary>
    /// Gets or sets the attributes used for flag evaluation.
    /// These are key-value pairs that can be used for targeting.
    /// </summary>
    [JsonPropertyName("attributes")]
    public IDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfidenceContext"/> class.
    /// Creates a new instance of <see cref="ConfidenceContext"/>.
    /// </summary>
    public ConfidenceContext()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfidenceContext"/> class.
    /// Creates a new instance of <see cref="ConfidenceContext"/> with the specified attributes.
    /// </summary>
    /// <param name="attributes">The attributes to use.</param>
    public ConfidenceContext(IDictionary<string, object> attributes)
    {
        Attributes = attributes;
    }
}
