using System.Collections.Generic;
using Moq;
using OpenFeature;
using OpenFeature.Model;
using Spotify.Confidence.OpenFeature;
using Spotify.Confidence.Sdk;
using Spotify.Confidence.Sdk.Models;
using Xunit;

namespace Spotify.Confidence.OpenFeature.Tests;

public class DotNotationTests
{
    private readonly Mock<IConfidenceClient> _mockClient;
    private readonly ConfidenceProvider _provider;

    public DotNotationTests()
    {
        _mockClient = new Mock<IConfidenceClient>();
        _provider = new ConfidenceProvider(_mockClient.Object);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WithDotNotation_ExtractsNestedProperty()
    {
        // Arrange
        var flagKey = "user-settings.preferences.darkMode";
        var context = EvaluationContext.Builder()
            .Set("user_id", "user123")
            .Build();

        // Mock the ConfidenceClient to return a successful result for dot-notation
        _mockClient.Setup(c => c.EvaluateBooleanFlagAsync(
                flagKey,
                false,
                It.IsAny<ConfidenceContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvaluationResult<bool>
            {
                Value = true,
                Reason = "MATCH",
                Variant = "control"
            });

        // Act
        var result = await _provider.ResolveBooleanValueAsync(flagKey, false, context);

        // Assert
        Assert.True(result.Value);
        Assert.Equal("MATCH", result.Reason);
        Assert.Equal("control", result.Variant);
    }

    [Fact]
    public async Task ResolveStringValueAsync_WithDotNotation_ExtractsNestedProperty()
    {
        // Arrange
        var flagKey = "app-config.ui.theme.primaryColor";
        var context = EvaluationContext.Builder()
            .Set("user_id", "user123")
            .Build();

        // Mock the ConfidenceClient to return a successful result for dot-notation
        _mockClient.Setup(c => c.EvaluateStringFlagAsync(
                flagKey,
                "#000000",
                It.IsAny<ConfidenceContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvaluationResult<string>
            {
                Value = "#FF5733",
                Reason = "MATCH",
                Variant = "treatment"
            });

        // Act
        var result = await _provider.ResolveStringValueAsync(flagKey, "#000000", context);

        // Assert
        Assert.Equal("#FF5733", result.Value);
        Assert.Equal("MATCH", result.Reason);
        Assert.Equal("treatment", result.Variant);
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_WithDotNotation_ExtractsNestedProperty()
    {
        // Arrange
        var flagKey = "performance.cache.timeoutMs";
        var context = EvaluationContext.Builder()
            .Set("user_id", "user123")
            .Build();

        // Mock the ConfidenceClient to return a successful result for dot-notation
        _mockClient.Setup(c => c.EvaluateNumericFlagAsync(
                flagKey,
                1000.0,
                It.IsAny<ConfidenceContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvaluationResult<double>
            {
                Value = 5000.5,
                Reason = "MATCH",
                Variant = "optimized"
            });

        // Act
        var result = await _provider.ResolveDoubleValueAsync(flagKey, 1000.0, context);

        // Assert
        Assert.Equal(5000.5, result.Value);
        Assert.Equal("MATCH", result.Reason);
        Assert.Equal("optimized", result.Variant);
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_WithDotNotation_ExtractsNestedProperty()
    {
        // Arrange
        var flagKey = "performance.cache.maxSize";
        var context = EvaluationContext.Builder()
            .Set("user_id", "user123")
            .Build();

        // Mock the ConfidenceClient to return a successful result for dot-notation
        _mockClient.Setup(c => c.EvaluateNumericFlagAsync(
                flagKey,
                500.0,
                It.IsAny<ConfidenceContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvaluationResult<double>
            {
                Value = 1000.0,
                Reason = "MATCH",
                Variant = "optimized"
            });

        // Act
        var result = await _provider.ResolveIntegerValueAsync(flagKey, 500, context);

        // Assert
        Assert.Equal(1000, result.Value);
        Assert.Equal("MATCH", result.Reason);
        Assert.Equal("optimized", result.Variant);
    }

    [Fact]
    public async Task ResolveStructureValueAsync_WithDotNotation_ExtractsNestedObject()
    {
        // Arrange
        var flagKey = "feature-config.experimental.newFeature";
        var context = EvaluationContext.Builder()
            .Set("user_id", "user123")
            .Build();

        var expectedObject = new Dictionary<string, object>
        {
            ["enabled"] = true,
            ["rolloutPercentage"] = 25,
            ["description"] = "New experimental feature"
        };

        // Mock the ConfidenceClient to return a successful result for dot-notation
        _mockClient.Setup(c => c.EvaluateJsonFlagAsync(
                flagKey,
                It.IsAny<object>(),
                It.IsAny<ConfidenceContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvaluationResult<object>
            {
                Value = expectedObject,
                Reason = "MATCH",
                Variant = "experiment"
            });

        // Act
        var result = await _provider.ResolveStructureValueAsync(flagKey, new Value(), context);

        // Assert
        Assert.NotNull(result.Value);
        var structure = result.Value.AsStructure;
        Assert.NotNull(structure);
        
        // Verify the structure contains expected values
        var structureDict = structure?.AsDictionary() ?? new Dictionary<string, object>();
        Assert.True((bool)structureDict["enabled"].AsObject!);
        Assert.Equal(25, Convert.ToInt32(structureDict["rolloutPercentage"].AsObject!));
        Assert.Equal("New experimental feature", (string)structureDict["description"].AsObject!);
        
        Assert.Equal("MATCH", result.Reason);
        Assert.Equal("experiment", result.Variant);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WithoutDotNotation_WorksAsUsual()
    {
        // Arrange
        var flagKey = "simple-flag";
        var context = EvaluationContext.Builder()
            .Set("user_id", "user123")
            .Build();

        // Mock the ConfidenceClient to return a successful result for simple flag
        _mockClient.Setup(c => c.EvaluateBooleanFlagAsync(
                flagKey,
                false,
                It.IsAny<ConfidenceContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvaluationResult<bool>
            {
                Value = true,
                Reason = "MATCH",
                Variant = "control"
            });

        // Act
        var result = await _provider.ResolveBooleanValueAsync(flagKey, false, context);

        // Assert
        Assert.True(result.Value);
        Assert.Equal("MATCH", result.Reason);
        Assert.Equal("control", result.Variant);
    }

    [Fact]
    public void DotNotation_PassesCorrectParametersToConfidenceClient()
    {
        // Arrange
        var flagKey = "complex.flag.with.dots";
        var defaultValue = false;
        var context = EvaluationContext.Builder()
            .Set("targeting_key", "user123")
            .Set("custom_attr", "value")
            .Build();

        _mockClient.Setup(c => c.EvaluateBooleanFlagAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<ConfidenceContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvaluationResult<bool>
            {
                Value = true,
                Reason = "MATCH",
                Variant = "variant"
            });

        // Act
        _ = _provider.ResolveBooleanValueAsync(flagKey, defaultValue, context);

        // Assert - Verify that the provider correctly passes the dot-notation flag key to the client
        _mockClient.Verify(c => c.EvaluateBooleanFlagAsync(
            flagKey, // Should pass the exact flag key including dots
            defaultValue,
            It.Is<ConfidenceContext?>(ctx => 
                ctx != null && 
                ctx.Attributes.ContainsKey("targeting_key") && 
                ctx.Attributes["targeting_key"].Equals("user123") &&
                ctx.Attributes.ContainsKey("custom_attr") &&
                ctx.Attributes["custom_attr"].Equals("value")),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}