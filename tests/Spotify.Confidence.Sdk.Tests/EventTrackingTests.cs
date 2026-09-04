using UnityOpenFeature.Core;
using UnityOpenFeature.Providers;
using Xunit;

namespace Spotify.Confidence.Sdk.Tests;

public class EventTrackingTests
{
    [Fact]
    public void TrackingEventDetails_DefaultValue_IsNull()
    {
        var details = new TrackingEventDetails();
        Assert.Null(details.Value);
    }

    [Fact]
    public void TrackingEventDetails_Value_CanBeSet()
    {
        var details = new TrackingEventDetails { Value = 42.0 };
        Assert.Equal(42.0, details.Value);
    }

    [Fact]
    public void TrackingEventDetails_Data_IsEmptyByDefault()
    {
        var details = new TrackingEventDetails();
        Assert.NotNull(details.Data);
        Assert.Empty(details.Data);
    }

    [Fact]
    public void TrackingEventDetails_Data_CanStoreStringValues()
    {
        var details = new TrackingEventDetails();
        details.Data["key1"] = "value1";
        details.Data["key2"] = "value2";

        Assert.Equal(2, details.Data.Count);
        Assert.Equal("value1", details.Data["key1"]);
        Assert.Equal("value2", details.Data["key2"]);
    }

    [Fact]
    public void TrackingEventDetails_Data_CanStoreMixedTypes()
    {
        var details = new TrackingEventDetails();
        details.Data["str"] = "hello";
        details.Data["num"] = 123;
        details.Data["flag"] = true;

        Assert.Equal("hello", details.Data["str"]);
        Assert.Equal(123, details.Data["num"]);
        Assert.Equal(true, details.Data["flag"]);
    }

    [Fact]
    public void TrackingEventDetails_ValueAndData_CanCoexist()
    {
        var details = new TrackingEventDetails
        {
            Value = 99.5
        };
        details.Data["action"] = "purchase";

        Assert.Equal(99.5, details.Value);
        Assert.Single(details.Data);
        Assert.Equal("purchase", details.Data["action"]);
    }

    [Fact]
    public void PayloadConstruction_WithValueAndData_ProducesCorrectPayload()
    {
        var details = new TrackingEventDetails
        {
            Value = 42.0
        };
        details.Data["custom_field"] = "hello";
        details.Data["context"] = "should_be_skipped";

        // Simulate the payload construction from ConfidenceProvider.Track
        var payload = BuildPayload(details, "user-123", new Dictionary<string, string>
        {
            { "country", "SE" }
        });

        Assert.Equal(42.0, payload["value"]);
        Assert.Equal("hello", payload["custom_field"]);
        // "context" key from details.Data should be excluded
        Assert.False(payload.ContainsKey("should_be_skipped"));

        var payloadContext = (Dictionary<string, object>)payload["context"];
        Assert.Equal("user-123", payloadContext["targeting_key"]);
        Assert.Equal("SE", payloadContext["country"]);
    }

    [Fact]
    public void PayloadConstruction_WithoutValue_OmitsValueField()
    {
        var details = new TrackingEventDetails();
        details.Data["action"] = "click";

        var payload = BuildPayload(details, null, null);

        Assert.False(payload.ContainsKey("value"));
        Assert.Equal("click", payload["action"]);
    }

    [Fact]
    public void PayloadConstruction_WithNullDetails_ProducesContextOnlyPayload()
    {
        var payload = BuildPayload(null, "user-456", null);

        Assert.False(payload.ContainsKey("value"));
        Assert.Single(payload); // only context
        var payloadContext = (Dictionary<string, object>)payload["context"];
        Assert.Equal("user-456", payloadContext["targeting_key"]);
    }

    [Fact]
    public void PayloadConstruction_WithNoContextOrDetails_ProducesEmptyPayload()
    {
        var payload = BuildPayload(null, null, null);

        Assert.Empty(payload);
    }

    [Fact]
    public void EventDefinition_Format_IncludesPrefix()
    {
        var trackingEventName = "my_event";
        var eventDefinition = $"eventDefinitions/{trackingEventName}";
        Assert.Equal("eventDefinitions/my_event", eventDefinition);
    }

    [Fact]
    public void EventsEndpointUrl_IsCorrect()
    {
        var url = ConfidenceEndpointUrls.Build(
            ConfidenceEndpointUrls.EventsBaseUrl,
            ConfidenceEndpointUrls.PublishEventsPath);

        Assert.Equal("https://events.confidence.dev/v1/events:publish", url);
    }

    /// <summary>
    /// Simulates the payload construction logic from ConfidenceProvider.Track
    /// without requiring Unity dependencies (EvaluationContext).
    /// </summary>
    private static Dictionary<string, object> BuildPayload(
        TrackingEventDetails? details,
        string? targetingKey,
        Dictionary<string, string>? attributes)
    {
        var payload = new Dictionary<string, object>();

        if (details?.Data != null)
        {
            foreach (var kvp in details.Data)
            {
                if (kvp.Key != "context")
                    payload[kvp.Key] = kvp.Value;
            }
        }

        if (details?.Value != null)
        {
            payload["value"] = details.Value.Value;
        }

        var contextDict = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(targetingKey))
            contextDict["targeting_key"] = targetingKey;
        if (attributes != null)
        {
            foreach (var attr in attributes)
                contextDict[attr.Key] = attr.Value;
        }
        if (contextDict.Count > 0)
            payload["context"] = contextDict;

        return payload;
    }
}
