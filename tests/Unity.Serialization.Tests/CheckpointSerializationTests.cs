using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Xunit;

namespace Unity.Serialization.Tests;

/// <summary>
/// Simulates what IL2CPP does on iOS: the parameterless constructor of CustomDateTimeConverter
/// gets stripped because it's only invoked via reflection (through [JsonConverter(typeof(...))]).
/// We simulate this by making the converter require a constructor parameter,
/// which is equivalent to what Newtonsoft.Json sees when IL2CPP has stripped the default constructor.
/// </summary>
public class ConverterWithoutParameterlessConstructor : IsoDateTimeConverter
{
    public ConverterWithoutParameterlessConstructor(string format)
    {
        DateTimeFormat = format;
    }
}

/// <summary>
/// Mirrors the Unity SDK's ApplyFlagsRequest/AppliedFlag model pattern from ConfidenceApiClient.cs,
/// using the attribute-based [JsonConverter] approach that breaks under IL2CPP.
/// </summary>
[Serializable]
public class ApplyFlagsRequestWithAttribute
{
    public List<AppliedFlagWithAttribute>? flags;
    [JsonConverter(typeof(ConverterWithoutParameterlessConstructor))]
    public DateTime sendTime;
    public string? clientSecret;
    public string? resolveToken;
}

[Serializable]
public class AppliedFlagWithAttribute
{
    public string? flag;
    [JsonConverter(typeof(ConverterWithoutParameterlessConstructor))]
    public DateTime applyTime;
}

/// <summary>
/// Models WITHOUT [JsonConverter] attribute — the fix for the IL2CPP issue.
/// DateTime formatting is handled via JsonSerializerSettings instead.
/// </summary>
[Serializable]
public class ApplyFlagsRequestFixed
{
    public List<AppliedFlagFixed>? flags;
    public DateTime sendTime;
    public string? clientSecret;
    public string? resolveToken;
}

[Serializable]
public class AppliedFlagFixed
{
    public string? flag;
    public DateTime applyTime;
}

public class CheckpointSerializationTests
{
    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        Converters = new List<JsonConverter>
        {
            new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'" }
        }
    };

    /// <summary>
    /// Reproduces the iOS IL2CPP crash: when the JsonConverter attribute references a converter
    /// whose parameterless constructor has been stripped by IL2CPP, Newtonsoft.Json throws
    /// "No parameterless constructor defined for 'CustomDateTimeConverter'".
    ///
    /// We simulate this by using a converter that only has a parameterized constructor,
    /// which is equivalent to what happens after IL2CPP stripping.
    /// </summary>
    [Fact]
    public void Checkpoint_Serialization_Fails_When_Converter_Has_No_Parameterless_Constructor()
    {
        var request = new ApplyFlagsRequestWithAttribute
        {
            flags = new List<AppliedFlagWithAttribute>
            {
                new AppliedFlagWithAttribute
                {
                    flag = "flags/my-flag",
                    applyTime = new DateTime(2024, 6, 15, 12, 30, 45, 123, DateTimeKind.Utc)
                }
            },
            sendTime = new DateTime(2024, 6, 15, 12, 30, 46, 456, DateTimeKind.Utc),
            clientSecret = "test-secret",
            resolveToken = "test-token"
        };

        // This is exactly what happens on iOS with IL2CPP:
        // Newtonsoft.Json tries to instantiate the converter via reflection and fails.
        var ex = Assert.Throws<JsonException>(() => JsonConvert.SerializeObject(request));
        Assert.Contains("ConverterWithoutParameterlessConstructor", ex.Message);
    }

    /// <summary>
    /// Demonstrates the fix: remove [JsonConverter] attributes from models and pass the
    /// converter via JsonSerializerSettings instead. This avoids reflection-based instantiation
    /// and works correctly under IL2CPP on iOS.
    /// </summary>
    [Fact]
    public void Checkpoint_Serialization_Succeeds_With_Settings_Based_Converter()
    {
        var request = new ApplyFlagsRequestFixed
        {
            flags = new List<AppliedFlagFixed>
            {
                new AppliedFlagFixed
                {
                    flag = "flags/my-flag",
                    applyTime = new DateTime(2024, 6, 15, 12, 30, 45, 123, DateTimeKind.Utc)
                }
            },
            sendTime = new DateTime(2024, 6, 15, 12, 30, 46, 456, DateTimeKind.Utc),
            clientSecret = "test-secret",
            resolveToken = "test-token"
        };

        var json = JsonConvert.SerializeObject(request, JsonSettings);

        Assert.Contains("\"sendTime\":\"2024-06-15T12:30:46.456Z\"", json);
        Assert.Contains("\"applyTime\":\"2024-06-15T12:30:45.123Z\"", json);
        Assert.Contains("\"flag\":\"flags/my-flag\"", json);
        Assert.Contains("\"clientSecret\":\"test-secret\"", json);
    }
}
