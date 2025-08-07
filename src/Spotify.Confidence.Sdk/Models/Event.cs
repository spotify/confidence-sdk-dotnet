using System.Text.Json.Serialization;

namespace Spotify.Confidence.Sdk.Models;

/// <summary>
/// Represents an event to be tracked.
/// </summary>
internal class Event
{
    /// <summary>
    /// Gets or sets the event definition.
    /// </summary>
    [JsonPropertyName("event_definition")]
    public string EventDefinition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event time in ISO 8601 format.
    /// </summary>
    [JsonPropertyName("event_time")]
    public string EventTime { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event payload.
    /// </summary>
    [JsonPropertyName("payload")]
    public IDictionary<string, object> Payload { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Represents a batch of events to be tracked.
/// </summary>
internal class EventBatchRequest
{
    /// <summary>
    /// Gets or sets the client secret for authentication.
    /// </summary>
    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SDK information.
    /// </summary>
    [JsonPropertyName("sdk")]
    public SdkInfo Sdk { get; set; } = new SdkInfo();

    /// <summary>
    /// Gets or sets the send time in ISO 8601 format.
    /// </summary>
    [JsonPropertyName("send_time")]
    public string SendTime { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the events to track.
    /// </summary>
    [JsonPropertyName("events")]
    public IList<Event> Events { get; set; } = new List<Event>();
}
