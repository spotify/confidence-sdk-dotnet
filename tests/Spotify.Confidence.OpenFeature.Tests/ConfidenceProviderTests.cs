using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using OpenFeature;
using OpenFeature.Model;
using Spotify.Confidence.Sdk;
using Spotify.Confidence.Sdk.Models;
using Spotify.Confidence.Sdk.Options;
using Xunit;

namespace Spotify.Confidence.OpenFeature.Tests;

public class ConfidenceProviderTests
{
    private readonly Mock<IConfidenceClient> _mockClient;
    private readonly ConfidenceProvider _provider;

    public ConfidenceProviderTests()
    {
        _mockClient = new Mock<IConfidenceClient>();
        // Use internal constructor for testing with mocked client
        _provider = new ConfidenceProvider(_mockClient.Object);
    }

    [Fact]
    public void GetMetadata_ReturnsCorrectProviderName()
    {
        // Act
        var metadata = _provider.GetMetadata();

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Confidence", metadata!.Name);
    }

    [Fact]
    public void Constructor_WithOptions_CreatesProvider()
    {
        // Arrange
        var options = new ConfidenceOptions
        {
            ClientSecret = "test-secret",
            LogLevel = LogLevel.None // Disable logging in tests
        };

        // Act
        var provider = new ConfidenceProvider(options);

        // Assert
        Assert.NotNull(provider);
        var metadata = provider.GetMetadata();
        Assert.NotNull(metadata);
        Assert.Equal("Confidence", metadata!.Name);
    }

    [Fact]
    public async Task InitializeAsync_Succeeds()
    {
        // Arrange
        var context = EvaluationContext.Builder().Build();

        // Act & Assert
        await _provider.InitializeAsync(context);
    }

    [Fact]
    public async Task ShutdownAsync_Succeeds()
    {
        // Act & Assert
        await _provider.ShutdownAsync();
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WhenSuccessful_ReturnsResolvedValue()
    {
        // Arrange
        const string flagKey = "test-flag";
        const bool defaultValue = true;
        const bool resolvedValue = false;
        const string variant = "test-variant";

        _mockClient.Setup(c => c.EvaluateBooleanFlagAsync(flagKey, It.IsAny<bool>(), It.IsAny<ConfidenceContext>(), default))
            .ReturnsAsync(new EvaluationResult<bool>
            {
                Value = resolvedValue,
                Variant = variant,
                Reason = "TARGETING_MATCH"
            });

        // Act
        var result = await _provider.ResolveBooleanValueAsync(flagKey, defaultValue);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(resolvedValue, result.Value);
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(variant, result.Variant);
        Assert.Equal("TARGETING_MATCH", result.Reason);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WhenError_ReturnsDefaultValue()
    {
        // Arrange
        const string flagKey = "test-flag";
        const bool defaultValue = true;

        _mockClient.Setup(c => c.EvaluateBooleanFlagAsync(flagKey, It.IsAny<bool>(), It.IsAny<ConfidenceContext>(), default))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await _provider.ResolveBooleanValueAsync(flagKey, defaultValue);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(defaultValue, result.Value);
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal("Error resolving flag", result.Reason);
    }

    [Fact]
    public async Task ResolveStringValueAsync_WhenSuccessful_ReturnsResolvedValue()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string defaultValue = "default";
        const string resolvedValue = "resolved";
        const string variant = "test-variant";

        _mockClient.Setup(c => c.EvaluateStringFlagAsync(flagKey, It.IsAny<string>(), It.IsAny<ConfidenceContext>(), default))
            .ReturnsAsync(new EvaluationResult<string>
            {
                Value = resolvedValue,
                Variant = variant,
                Reason = "TARGETING_MATCH"
            });

        // Act
        var result = await _provider.ResolveStringValueAsync(flagKey, defaultValue);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(resolvedValue, result.Value);
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(variant, result.Variant);
        Assert.Equal("TARGETING_MATCH", result.Reason);
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_WhenSuccessful_ReturnsResolvedValue()
    {
        // Arrange
        const string flagKey = "test-flag";
        const int defaultValue = 42;
        const double resolvedValue = 84;
        const string variant = "test-variant";

        _mockClient.Setup(c => c.EvaluateNumericFlagAsync(flagKey, It.IsAny<double>(), It.IsAny<ConfidenceContext>(), default))
            .ReturnsAsync(new EvaluationResult<double>
            {
                Value = resolvedValue,
                Variant = variant,
                Reason = "TARGETING_MATCH"
            });

        // Act
        var result = await _provider.ResolveIntegerValueAsync(flagKey, defaultValue);

        // Assert
        Assert.NotNull(result);
        Assert.Equal((int)resolvedValue, result.Value);
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(variant, result.Variant);
        Assert.Equal("TARGETING_MATCH", result.Reason);
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_WhenSuccessful_ReturnsResolvedValue()
    {
        // Arrange
        const string flagKey = "test-flag";
        const double defaultValue = 3.14;
        const double resolvedValue = 6.28;
        const string variant = "test-variant";

        _mockClient.Setup(c => c.EvaluateNumericFlagAsync(flagKey, It.IsAny<double>(), It.IsAny<ConfidenceContext>(), default))
            .ReturnsAsync(new EvaluationResult<double>
            {
                Value = resolvedValue,
                Variant = variant,
                Reason = "TARGETING_MATCH"
            });

        // Act
        var result = await _provider.ResolveDoubleValueAsync(flagKey, defaultValue);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(resolvedValue, result.Value);
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(variant, result.Variant);
        Assert.Equal("TARGETING_MATCH", result.Reason);
    }

    [Fact]
    public async Task ResolveStructureValueAsync_WhenSuccessful_ReturnsResolvedValue()
    {
        // Arrange
        const string flagKey = "test-flag";
        var defaultStructure = Structure.Builder().Set("test", new Value("default")).Build();
        var defaultValue = new Value(defaultStructure);
        var resolvedObject = new Dictionary<string, object> { { "test", "resolved" } };
        const string variant = "test-variant";

        _mockClient.Setup(c => c.EvaluateJsonFlagAsync(flagKey, It.IsAny<object>(), It.IsAny<ConfidenceContext>(), default))
            .ReturnsAsync(new EvaluationResult<object>
            {
                Value = resolvedObject,
                Variant = variant,
                Reason = "TARGETING_MATCH"
            });

        // Act
        var result = await _provider.ResolveStructureValueAsync(flagKey, defaultValue);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Value.AsStructure);
        Assert.Equal("resolved", result.Value.AsStructure!.GetValue("test").AsString);
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(variant, result.Variant);
        Assert.Equal("TARGETING_MATCH", result.Reason);
    }

    [Fact]
    public void GetProviderHooks_ReturnsEmptyList()
    {
        // Act
        var hooks = _provider.GetProviderHooks();

        // Assert
        Assert.NotNull(hooks);
        Assert.Empty(hooks);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WithTargetingKey_PassesKeyToClient()
    {
        // Arrange
        const string flagKey = "test-flag";
        const bool defaultValue = true;
        const string targetingKey = "user123";
        ConfidenceContext? capturedContext = null;

        var context = EvaluationContext.Builder()
            .Set("targetingKey", targetingKey)
            .Build();

        _mockClient.Setup(c => c.EvaluateBooleanFlagAsync(flagKey, It.IsAny<bool>(), It.IsAny<ConfidenceContext>(), default))
            .Callback<string, bool, ConfidenceContext?, CancellationToken>((_, _, ctx, _) => capturedContext = ctx)
            .ReturnsAsync(new EvaluationResult<bool>
            {
                Value = defaultValue,
                Reason = "DEFAULT"
            });

        // Act
        await _provider.ResolveBooleanValueAsync(flagKey, defaultValue, context);

        // Assert
        Assert.NotNull(capturedContext);
        Assert.True(capturedContext!.Attributes.ContainsKey("targeting_key"));
        Assert.Equal(targetingKey, capturedContext.Attributes["targeting_key"]);
    }

    [Fact]
    public async Task ResolveStructureValueAsync_WithJsonElementAllTypes_ReturnsCorrectValues()
    {
        // Arrange
        const string flagKey = "test-flag";
        var defaultStructure = Structure.Builder().Set("test", new Value("default")).Build();
        var defaultValue = new Value(defaultStructure);
        
        var jsonDoc = JsonDocument.Parse(@"{
            ""boolTrue"": true,
            ""boolFalse"": false,
            ""intValue"": 42,
            ""doubleValue"": 3.14,
            ""stringValue"": ""hello world"",
            ""emptyString"": """",
            ""nullValue"": null,
            ""largeInteger"": 9007199254740991,
            ""zero"": 0,
            ""negative"": -42,
            ""emptyObject"": {},
            ""emptyArray"": [],
            ""arrayValue"": [1, 2, 3, ""four""],
            ""nested"": {
                ""innerValue"": ""test"",
                ""innerNumber"": 123
            },
            ""config"": {
                ""enabled"": true,
                ""threshold"": 50.5,
                ""tags"": [""a"", ""b"", ""c""],
                ""metadata"": {
                    ""version"": ""1.0"",
                    ""debug"": false
                }
            }
        }");
        
        var resolvedObject = new Dictionary<string, object>();
        foreach (var property in jsonDoc.RootElement.EnumerateObject())
        {
            resolvedObject[property.Name] = property.Value;
        }
        
        const string variant = "test-variant";

        _mockClient.Setup(c => c.EvaluateJsonFlagAsync(flagKey, It.IsAny<object>(), It.IsAny<ConfidenceContext>(), default))
            .ReturnsAsync(new EvaluationResult<object>
            {
                Value = resolvedObject,
                Variant = variant,
                Reason = "TARGETING_MATCH"
            });

        // Act
        var result = await _provider.ResolveStructureValueAsync(flagKey, defaultValue);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Value.AsStructure);
        Assert.Equal(flagKey, result.FlagKey);
        Assert.Equal(variant, result.Variant);
        Assert.Equal("TARGETING_MATCH", result.Reason);
        
        var structure = result.Value.AsStructure!;
        
        // Boolean values
        Assert.True(structure.GetValue("boolTrue").AsBoolean);
        Assert.False(structure.GetValue("boolFalse").AsBoolean);
        
        // Numeric values
        Assert.Equal(42, structure.GetValue("intValue").AsInteger);
        Assert.Equal(3.14, structure.GetValue("doubleValue").AsDouble);
        Assert.Equal(0, structure.GetValue("zero").AsInteger);
        Assert.Equal(-42, structure.GetValue("negative").AsInteger);
        // Large integers should be handled as doubles since they don't fit in int32
        Assert.Equal(9007199254740991.0, structure.GetValue("largeInteger").AsDouble);
        
        // String values
        Assert.Equal("hello world", structure.GetValue("stringValue").AsString);
        Assert.Equal("", structure.GetValue("emptyString").AsString);
        
        // Null value
        Assert.True(structure.GetValue("nullValue").IsNull);
        
        // Empty containers
        var emptyObject = structure.GetValue("emptyObject").AsStructure;
        Assert.NotNull(emptyObject);
        Assert.Equal(0, emptyObject!.Count);
        
        var emptyArray = structure.GetValue("emptyArray").AsList;
        Assert.NotNull(emptyArray);
        Assert.Empty(emptyArray!);
        
        // Array with mixed types
        var arrayValue = structure.GetValue("arrayValue").AsList;
        Assert.NotNull(arrayValue);
        Assert.Equal(4, arrayValue!.Count);
        Assert.Equal(1, arrayValue[0].AsInteger);
        Assert.Equal(2, arrayValue[1].AsInteger);
        Assert.Equal(3, arrayValue[2].AsInteger);
        Assert.Equal("four", arrayValue[3].AsString);
        
        // Nested object
        var nestedStructure = structure.GetValue("nested").AsStructure;
        Assert.NotNull(nestedStructure);
        Assert.Equal("test", nestedStructure!.GetValue("innerValue").AsString);
        Assert.Equal(123, nestedStructure.GetValue("innerNumber").AsInteger);
        
        // Complex nested structure
        var config = structure.GetValue("config").AsStructure;
        Assert.NotNull(config);
        Assert.True(config!.GetValue("enabled").AsBoolean);
        Assert.Equal(50.5, config.GetValue("threshold").AsDouble);
        
        var tags = config.GetValue("tags").AsList;
        Assert.NotNull(tags);
        Assert.Equal(3, tags!.Count);
        Assert.Equal("a", tags[0].AsString);
        Assert.Equal("b", tags[1].AsString);
        Assert.Equal("c", tags[2].AsString);
        
        var metadata = config.GetValue("metadata").AsStructure;
        Assert.NotNull(metadata);
        Assert.Equal("1.0", metadata!.GetValue("version").AsString);
        Assert.False(metadata.GetValue("debug").AsBoolean);
        
        jsonDoc.Dispose();
    }
}
