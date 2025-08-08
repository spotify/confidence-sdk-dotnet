using System;
using System.Collections.Generic;
using Moq;
using OpenFeature.Model;
using Spotify.Confidence.OpenFeature;
using Spotify.Confidence.Sdk;
using Spotify.Confidence.Sdk.Models;
using Xunit;

namespace Spotify.Confidence.OpenFeature.Tests;

/// <summary>
/// Tests to verify that custom default values are properly passed through and returned by the ConfidenceProvider.
/// </summary>
public class DefaultValueParameterTests
{
    private readonly Mock<IConfidenceClient> _mockClient;
    private readonly ConfidenceProvider _provider;

    public DefaultValueParameterTests()
    {
        _mockClient = new Mock<IConfidenceClient>();
        _provider = new ConfidenceProvider(_mockClient.Object);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_PassesDefaultValueToClient()
    {
        // Arrange
        const string flagKey = "test-flag";
        const bool customDefault = true;
        bool capturedDefaultValue = false;

        _mockClient.Setup(c => c.EvaluateBooleanFlagAsync(
                It.IsAny<string>(), 
                It.IsAny<bool>(), 
                It.IsAny<ConfidenceContext>(), 
                default))
            .Callback<string, bool, ConfidenceContext?, System.Threading.CancellationToken>((_, defaultVal, _, _) => 
                capturedDefaultValue = defaultVal)
            .ReturnsAsync(new EvaluationResult<bool>
            {
                Value = false, // Different from default to ensure we're testing the parameter passing
                Reason = "TARGETING_MATCH"
            });

        // Act
        await _provider.ResolveBooleanValueAsync(flagKey, customDefault);

        // Assert
        Assert.Equal(customDefault, capturedDefaultValue);
        _mockClient.Verify(c => c.EvaluateBooleanFlagAsync(flagKey, customDefault, It.IsAny<ConfidenceContext>(), default), Times.Once);
    }

    [Fact]
    public async Task ResolveStringValueAsync_PassesDefaultValueToClient()
    {
        // Arrange
        const string flagKey = "test-flag";
        const string customDefault = "my-custom-default";
        string capturedDefaultValue = "";

        _mockClient.Setup(c => c.EvaluateStringFlagAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<ConfidenceContext>(), 
                default))
            .Callback<string, string, ConfidenceContext?, System.Threading.CancellationToken>((_, defaultVal, _, _) => 
                capturedDefaultValue = defaultVal)
            .ReturnsAsync(new EvaluationResult<string>
            {
                Value = "resolved-value",
                Reason = "TARGETING_MATCH"
            });

        // Act
        await _provider.ResolveStringValueAsync(flagKey, customDefault);

        // Assert
        Assert.Equal(customDefault, capturedDefaultValue);
        _mockClient.Verify(c => c.EvaluateStringFlagAsync(flagKey, customDefault, It.IsAny<ConfidenceContext>(), default), Times.Once);
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_PassesDefaultValueToClient()
    {
        // Arrange
        const string flagKey = "test-flag";
        const int customDefault = 42;
        double capturedDefaultValue = 0;

        _mockClient.Setup(c => c.EvaluateNumericFlagAsync(
                It.IsAny<string>(), 
                It.IsAny<double>(), 
                It.IsAny<ConfidenceContext>(), 
                default))
            .Callback<string, double, ConfidenceContext?, System.Threading.CancellationToken>((_, defaultVal, _, _) => 
                capturedDefaultValue = defaultVal)
            .ReturnsAsync(new EvaluationResult<double>
            {
                Value = 100.0,
                Reason = "TARGETING_MATCH"
            });

        // Act
        await _provider.ResolveIntegerValueAsync(flagKey, customDefault);

        // Assert
        Assert.Equal(customDefault, capturedDefaultValue);
        _mockClient.Verify(c => c.EvaluateNumericFlagAsync(flagKey, customDefault, It.IsAny<ConfidenceContext>(), default), Times.Once);
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_PassesDefaultValueToClient()
    {
        // Arrange
        const string flagKey = "test-flag";
        const double customDefault = 3.14159;
        double capturedDefaultValue = 0;

        _mockClient.Setup(c => c.EvaluateNumericFlagAsync(
                It.IsAny<string>(), 
                It.IsAny<double>(), 
                It.IsAny<ConfidenceContext>(), 
                default))
            .Callback<string, double, ConfidenceContext?, System.Threading.CancellationToken>((_, defaultVal, _, _) => 
                capturedDefaultValue = defaultVal)
            .ReturnsAsync(new EvaluationResult<double>
            {
                Value = 2.718,
                Reason = "TARGETING_MATCH"
            });

        // Act
        await _provider.ResolveDoubleValueAsync(flagKey, customDefault);

        // Assert
        Assert.Equal(customDefault, capturedDefaultValue);
        _mockClient.Verify(c => c.EvaluateNumericFlagAsync(flagKey, customDefault, It.IsAny<ConfidenceContext>(), default), Times.Once);
    }

    [Fact]
    public async Task ResolveStructureValueAsync_PassesDefaultValueToClient()
    {
        // Arrange
        const string flagKey = "test-flag";
        var customStructure = Structure.Builder()
            .Set("key1", new Value("value1"))
            .Set("key2", new Value(42))
            .Build();
        var customDefault = new Value(customStructure);
        object? capturedDefaultValue = null;

        _mockClient.Setup(c => c.EvaluateJsonFlagAsync(
                It.IsAny<string>(), 
                It.IsAny<object>(), 
                It.IsAny<ConfidenceContext>(), 
                default))
            .Callback<string, object, ConfidenceContext?, System.Threading.CancellationToken>((_, defaultVal, _, _) => 
                capturedDefaultValue = defaultVal)
            .ReturnsAsync(new EvaluationResult<object>
            {
                Value = new Dictionary<string, object> { { "resolved", "value" } },
                Reason = "TARGETING_MATCH"
            });

        // Act
        await _provider.ResolveStructureValueAsync(flagKey, customDefault);

        // Assert
        Assert.NotNull(capturedDefaultValue);
        // The provider should pass defaultValue.AsObject to the client
        Assert.Equal(customDefault.AsObject, capturedDefaultValue);
        _mockClient.Verify(c => c.EvaluateJsonFlagAsync(
            flagKey, 
            It.Is<object>(o => o == customDefault.AsObject), 
            It.IsAny<ConfidenceContext>(), 
            default), Times.Once);
    }

}