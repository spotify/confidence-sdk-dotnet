using UnityOpenFeature.Core;
using UnityOpenFeature.Providers;
using Xunit;

namespace Spotify.Confidence.Sdk.Tests;

public class EventTrackingTests
{
    [Fact]
    public void TrackingEventDetails_Empty_HasNoValueAndNoAttributes()
    {
        var details = TrackingEventDetails.Empty;

        Assert.Null(details.Value);
        Assert.Equal(0, details.Count);
        Assert.Empty(details.AsDictionary());
    }

    [Fact]
    public void TrackingEventDetails_Builder_SetsValue()
    {
        var details = TrackingEventDetails.Builder().SetValue(42.0).Build();

        Assert.Equal(42.0, details.Value);
    }

    [Fact]
    public void TrackingEventDetails_Builder_StoresTypedAttributes()
    {
        var timestamp = new DateTime(2026, 9, 8, 10, 30, 0, DateTimeKind.Utc);

        var details = TrackingEventDetails.Builder()
            .Set("str", "hello")
            .Set("num", 123)
            .Set("big", 9_000_000_000L)
            .Set("dbl", 1.5)
            .Set("flag", true)
            .Set("when", timestamp)
            .Build();

        Assert.Equal(6, details.Count);
        Assert.Equal("hello", details.GetValue("str"));
        Assert.Equal(123, details.GetValue("num"));
        Assert.Equal(9_000_000_000L, details.GetValue("big"));
        Assert.Equal(1.5, details.GetValue("dbl"));
        Assert.Equal(true, details.GetValue("flag"));
        Assert.Equal(timestamp, details.GetValue("when"));
    }

    [Fact]
    public void TrackingEventDetails_Accessors_BehaveLikeTheOfficialSdk()
    {
        var details = TrackingEventDetails.Builder().Set("present", "yes").Build();

        Assert.True(details.ContainsKey("present"));
        Assert.False(details.ContainsKey("absent"));

        Assert.True(details.TryGetValue("present", out var found));
        Assert.Equal("yes", found);

        Assert.False(details.TryGetValue("absent", out var missing));
        Assert.Null(missing);

        Assert.Throws<KeyNotFoundException>(() => details.GetValue("absent"));
    }

    [Fact]
    public void TrackingEventDetails_IsImmutable_AfterBuild()
    {
        var builder = TrackingEventDetails.Builder().Set("first", 1);
        var details = builder.Build();

        // Reusing the builder must not mutate what it already produced.
        builder.Set("second", 2).SetValue(9.0);

        Assert.Equal(1, details.Count);
        Assert.False(details.ContainsKey("second"));
        Assert.Null(details.Value);
    }

    [Fact]
    public void TrackingEventDetails_Merge_OverwritesCollidingKeys()
    {
        var original = TrackingEventDetails.Builder()
            .Set("keep", "a")
            .Set("clash", "old")
            .SetValue(1.0)
            .Build();

        var merged = TrackingEventDetails.Builder()
            .Set("clash", "new")
            .Merge(original)
            .Build();

        Assert.Equal("a", merged.GetValue("keep"));
        Assert.Equal("old", merged.GetValue("clash"));
        Assert.Equal(1.0, merged.Value);
    }

    [Fact]
    public void TrackingEventDetails_ValueAndAttributes_CanCoexist()
    {
        var details = TrackingEventDetails.Builder()
            .SetValue(99.5)
            .Set("action", "purchase")
            .Build();

        Assert.Equal(99.5, details.Value);
        Assert.Equal(1, details.Count);
        Assert.Equal("purchase", details.GetValue("action"));
    }

    [Fact]
    public void PayloadConstruction_WithValueAndData_ProducesCorrectPayload()
    {
        var details = TrackingEventDetails.Builder()
            .SetValue(42.0)
            .Set("custom_field", "hello")
            .Set("context", "should_be_dropped")
            .Build();

        var payload = TrackingPayloadBuilder.Build(details, "user-123", new Dictionary<string, string>
        {
            { "country", "SE" }
        });

        Assert.Equal(42.0, payload["value"]);
        Assert.Equal("hello", payload["custom_field"]);

        // The reserved "context" key from the details must not overwrite the
        // context the provider derives from the evaluation context.
        var payloadContext = Assert.IsType<Dictionary<string, object>>(payload["context"]);
        Assert.Equal("user-123", payloadContext["targeting_key"]);
        Assert.Equal("SE", payloadContext["country"]);
    }

    [Fact]
    public void PayloadConstruction_DropsReservedContextKeyFromDetails()
    {
        var details = TrackingEventDetails.Builder().Set("context", "should_be_dropped").Build();

        // No targeting key and no attributes, so nothing else writes "context".
        // The assertion in PayloadConstruction_WithValueAndData cannot catch a
        // failure to strip this key, because the derived context overwrites it;
        // here there is nothing to overwrite, so the key must be absent.
        var payload = TrackingPayloadBuilder.Build(details, null, null);

        Assert.False(payload.ContainsKey("context"));
        Assert.Empty(payload);
    }

    [Fact]
    public void PayloadConstruction_WithoutValue_OmitsValueField()
    {
        var details = TrackingEventDetails.Builder().Set("action", "click").Build();

        var payload = TrackingPayloadBuilder.Build(details, null, null);

        Assert.False(payload.ContainsKey("value"));
        Assert.Equal("click", payload["action"]);
    }

    [Fact]
    public void PayloadConstruction_WithNullDetails_ProducesContextOnlyPayload()
    {
        var payload = TrackingPayloadBuilder.Build(null, "user-456", null);

        Assert.False(payload.ContainsKey("value"));
        Assert.Single(payload); // only context
        var payloadContext = Assert.IsType<Dictionary<string, object>>(payload["context"]);
        Assert.Equal("user-456", payloadContext["targeting_key"]);
    }

    [Fact]
    public void PayloadConstruction_WithNoContextOrDetails_ProducesEmptyPayload()
    {
        var payload = TrackingPayloadBuilder.Build(null, null, null);

        Assert.Empty(payload);
    }

    [Fact]
    public void PayloadConstruction_WithEmptyDetails_OmitsContextAndValue()
    {
        var payload = TrackingPayloadBuilder.Build(TrackingEventDetails.Empty, null, null);

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
}
