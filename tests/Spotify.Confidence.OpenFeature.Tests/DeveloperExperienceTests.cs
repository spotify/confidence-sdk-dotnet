using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using OpenFeature;
using OpenFeature.Model;
using Spotify.Confidence.OpenFeature;
using Spotify.Confidence.Sdk;
using Spotify.Confidence.Sdk.Models;
using Xunit;

namespace Spotify.Confidence.OpenFeature.Tests;

/// <summary>
/// Tests that simulate real developer experience using OpenFeature with Confidence provider.
/// These tests cover both successful flag evaluation (hits) and fallback scenarios (misses).
/// </summary>
public class DeveloperExperienceTests
{
    private readonly Mock<IConfidenceClient> _mockClient;
    private readonly ConfidenceProvider _provider;

    public DeveloperExperienceTests()
    {
        // Setup mock confidence client to control responses
        _mockClient = new Mock<IConfidenceClient>();

        // Create Confidence provider with mocked client
        _provider = new ConfidenceProvider(_mockClient.Object);
    }

    [Fact]
    public async Task FeatureFlag_WhenApiReturnsValue_DeveloperGetsExpectedResult()
    {
        // Arrange - Setup a successful response from Confidence
        const string flagKey = "show-new-feature";
        const bool flagValue = true;
        const bool defaultValue = false;

        _mockClient.Setup(c => c.EvaluateBooleanFlagAsync(
                flagKey,
                defaultValue,
                It.IsAny<ConfidenceContext>(),
                default))
            .ReturnsAsync(new EvaluationResult<bool>
            {
                Value = flagValue,
                Variant = "treatment",
                Reason = "TARGETING_MATCH"
            });

        var context = EvaluationContext.Builder()
            .Set("user_id", "user-123")
            .Set("email", "user@example.com")
            .Build();

        // Act - Developer uses OpenFeature provider naturally
        var result = await _provider.ResolveBooleanValueAsync(flagKey, defaultValue, context);

        // Assert - Developer gets the flag value from Confidence
        Assert.True(result.Value);
        Assert.Equal("TARGETING_MATCH", result.Reason);
        Assert.Equal("treatment", result.Variant);
    }

    [Fact]
    public async Task FeatureFlag_WhenApiReturnsNoFlag_DeveloperGetsDefaultValue()
    {
        // Arrange - Setup Confidence to return failure (flag not found)
        const string flagKey = "non-existent-flag";
        const bool defaultValue = false;

        _mockClient.Setup(c => c.EvaluateBooleanFlagAsync(
                flagKey,
                defaultValue,
                It.IsAny<ConfidenceContext>(),
                default))
            .ReturnsAsync(EvaluationResult.Failure(defaultValue, "Flag not found"));

        var context = EvaluationContext.Builder()
            .Set("user_id", "user-456")
            .Build();

        // Act - Developer requests a flag that doesn't exist
        var result = await _provider.ResolveBooleanValueAsync(flagKey, defaultValue, context);

        // Assert - Developer gets their specified default value
        Assert.False(result.Value);
        // When the ConfidenceClient returns a failure result, that gets passed through
        Assert.Equal("ERROR", result.Reason);
    }

    [Fact]
    public async Task StringFlag_WhenApiReturnsValue_DeveloperGetsExpectedString()
    {
        // Arrange - Setup Confidence to return a string value
        const string flagKey = "welcome-message";
        const string flagValue = "Welcome to our new app!";
        const string defaultValue = "Welcome!";

        _mockClient.Setup(c => c.EvaluateStringFlagAsync(
                flagKey,
                defaultValue,
                It.IsAny<ConfidenceContext>(),
                default))
            .ReturnsAsync(new EvaluationResult<string>
            {
                Value = flagValue,
                Variant = "personalized",
                Reason = "TARGETING_MATCH"
            });

        var context = EvaluationContext.Builder()
            .Set("user_id", "user-789")
            .Set("subscription_tier", "premium")
            .Build();

        // Act - Developer gets string configuration
        var result = await _provider.ResolveStringValueAsync(flagKey, defaultValue, context);

        // Assert
        Assert.Equal(flagValue, result.Value);
        Assert.Equal("TARGETING_MATCH", result.Reason);
        Assert.Equal("personalized", result.Variant);
    }

    [Fact]
    public async Task NumericFlag_WhenApiReturnsValue_DeveloperGetsExpectedNumber()
    {
        // Arrange - Setup Confidence to return a numeric value
        const string flagKey = "max-retry-count";
        const double flagValue = 5.0;
        const int defaultValue = 3;

        _mockClient.Setup(c => c.EvaluateNumericFlagAsync(
                flagKey,
                (double)defaultValue,
                It.IsAny<ConfidenceContext>(),
                default))
            .ReturnsAsync(new EvaluationResult<double>
            {
                Value = flagValue,
                Variant = "high-reliability",
                Reason = "TARGETING_MATCH"
            });

        var context = EvaluationContext.Builder()
            .Set("user_id", "user-999")
            .Set("plan", "enterprise")
            .Build();

        // Act - Developer gets numeric configuration (using integer method which calls numeric internally)
        var result = await _provider.ResolveIntegerValueAsync(flagKey, defaultValue, context);

        // Assert
        Assert.Equal((int)flagValue, result.Value);
        Assert.Equal("TARGETING_MATCH", result.Reason);
        Assert.Equal("high-reliability", result.Variant);
    }

    [Fact]
    public async Task ComplexFlag_WhenApiReturnsConfiguration_DeveloperGetsStructuredData()
    {
        // Arrange - Setup Confidence to return a complex configuration object
        const string flagKey = "user-config";
        var configObject = new Dictionary<string, object>
        {
            { "theme", "dark" },
            { "features", new Dictionary<string, object>
                {
                    { "chat", true },
                    { "notifications", false },
                    { "analytics", true }
                }
            },
            { "limits", new Dictionary<string, object>
                {
                    { "api_calls_per_hour", 1000 },
                    { "storage_gb", 50 }
                }
            }
        };

        var defaultValue = new Value(Structure.Builder().Build()); // empty default

        _mockClient.Setup(c => c.EvaluateJsonFlagAsync(
                flagKey,
                It.IsAny<object>(),
                It.IsAny<ConfidenceContext>(),
                default))
            .ReturnsAsync(new EvaluationResult<object>
            {
                Value = configObject,
                Variant = "premium",
                Reason = "TARGETING_MATCH"
            });

        var context = EvaluationContext.Builder()
            .Set("user_id", "premium-user-123")
            .Set("plan", "premium")
            .Build();

        // Act - Developer gets complex configuration
        var result = await _provider.ResolveStructureValueAsync(flagKey, defaultValue, context);

        // Assert - Developer can access nested configuration
        Assert.NotNull(result.Value);
        var configStruct = result.Value.AsStructure!;
        Assert.Equal("dark", configStruct.AsDictionary()["theme"].AsString);

        var features = configStruct.AsDictionary()["features"].AsStructure!.AsDictionary();
        Assert.True(features["chat"].AsBoolean);
        Assert.False(features["notifications"].AsBoolean);

        Assert.Equal("TARGETING_MATCH", result.Reason);
        Assert.Equal("premium", result.Variant);
    }

    [Fact]
    public async Task MultipleFlags_WithMixedResults_DeveloperGetsExpectedBehavior()
    {
        // Arrange - Some flags exist, others don't
        _mockClient.Setup(c => c.EvaluateBooleanFlagAsync(
                "new-dashboard",
                true,
                It.IsAny<ConfidenceContext>(),
                default))
            .ReturnsAsync(new EvaluationResult<bool>
            {
                Value = true,
                Variant = "enabled",
                Reason = "TARGETING_MATCH"
            });

        _mockClient.Setup(c => c.EvaluateStringFlagAsync(
                "experiment-variant",
                "control",
                It.IsAny<ConfidenceContext>(),
                default))
            .ReturnsAsync(EvaluationResult.Failure("control", "Flag not found"));

        _mockClient.Setup(c => c.EvaluateNumericFlagAsync(
                "max-connections",
                10.0,
                It.IsAny<ConfidenceContext>(),
                default))
            .ReturnsAsync(new EvaluationResult<double>
            {
                Value = 25.0,
                Variant = "high-limit",
                Reason = "TARGETING_MATCH"
            });

        var context = EvaluationContext.Builder()
            .Set("user_id", "test-user")
            .Set("region", "us-west")
            .Build();

        // Act - Developer requests multiple flags in realistic usage
        var dashboardResult = await _provider.ResolveBooleanValueAsync("new-dashboard", true, context);
        var experimentResult = await _provider.ResolveStringValueAsync("experiment-variant", "control", context);
        var connectionsResult = await _provider.ResolveIntegerValueAsync("max-connections", 10, context);

        // Assert - Developer gets mix of real values and defaults
        Assert.True(dashboardResult.Value); // From API
        Assert.Equal("TARGETING_MATCH", dashboardResult.Reason);

        Assert.Equal("control", experimentResult.Value); // Default (flag not found)
        Assert.Equal("ERROR", experimentResult.Reason);

        Assert.Equal(25, connectionsResult.Value); // From API
        Assert.Equal("TARGETING_MATCH", connectionsResult.Reason);
    }

    [Fact]
    public async Task FeatureFlag_WhenApiError_DeveloperGetsDefaultValueGracefully()
    {
        // Arrange - Setup Confidence to throw exceptions (simulating API errors)
        _mockClient.Setup(c => c.EvaluateBooleanFlagAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<ConfidenceContext>(),
                default))
            .ThrowsAsync(new Exception("API Error"));

        _mockClient.Setup(c => c.EvaluateStringFlagAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ConfidenceContext>(),
                default))
            .ThrowsAsync(new Exception("API Error"));

        var context = EvaluationContext.Builder()
            .Set("user_id", "user-error-test")
            .Build();

        // Act - Developer's code continues to work despite API issues
        var boolResult = await _provider.ResolveBooleanValueAsync("some-feature", false, context);
        var stringResult = await _provider.ResolveStringValueAsync("config-value", "default-config", context);

        // Assert - Application continues functioning with defaults
        Assert.False(boolResult.Value);
        Assert.Equal("Error resolving flag", boolResult.Reason);

        Assert.Equal("default-config", stringResult.Value);
        Assert.Equal("Error resolving flag", stringResult.Reason);
    }

    [Fact]
    public async Task DeveloperWorkflow_TypicalFeatureFlagUsage_WorksSeamlessly()
    {
        // Arrange - Setup realistic feature flag configuration
        _mockClient.Setup(c => c.EvaluateBooleanFlagAsync(
                "new-payment-flow",
                false,
                It.IsAny<ConfidenceContext>(),
                default))
            .ReturnsAsync(new EvaluationResult<bool>
            {
                Value = true,
                Variant = "beta-enabled",
                Reason = "TARGETING_MATCH"
            });

        _mockClient.Setup(c => c.EvaluateNumericFlagAsync(
                "max-upload-size-mb",
                10.0,
                It.IsAny<ConfidenceContext>(),
                default))
            .ReturnsAsync(new EvaluationResult<double>
            {
                Value = 100.0,
                Variant = "pro-tier",
                Reason = "TARGETING_MATCH"
            });

        var themeConfig = new Dictionary<string, object>
        {
            { "theme", "dark" },
            { "sidebar", "collapsed" },
            { "accent_color", "#007acc" }
        };

        _mockClient.Setup(c => c.EvaluateJsonFlagAsync(
                "ui-theme",
                It.IsAny<object>(),
                It.IsAny<ConfidenceContext>(),
                default))
            .ReturnsAsync(new EvaluationResult<object>
            {
                Value = themeConfig,
                Variant = "pro-dark",
                Reason = "TARGETING_MATCH"
            });

        // Setup feature-rollout-percent to return failure (simulating flag not found)
        _mockClient.Setup(c => c.EvaluateNumericFlagAsync(
                "feature-rollout-percent",
                0.0,
                It.IsAny<ConfidenceContext>(),
                default))
            .ReturnsAsync(EvaluationResult.Failure(0.0, "Flag not found"));

        // Act - Simulate a typical developer workflow
        var userContext = EvaluationContext.Builder()
            .Set("user_id", "customer-42")
            .Set("subscription", "pro")
            .Set("region", "europe")
            .Set("beta_user", true)
            .Build();

        // Check if new payment flow is enabled
        var paymentResult = await _provider.ResolveBooleanValueAsync("new-payment-flow", false, userContext);

        // Get maximum file upload size
        var uploadResult = await _provider.ResolveIntegerValueAsync("max-upload-size-mb", 10, userContext);

        // Get UI theme configuration
        var themeResult = await _provider.ResolveStructureValueAsync(
            "ui-theme",
            new Value(Structure.Builder().Set("theme", new Value("light")).Build()),
            userContext);

        // Get feature rollout percentage (this flag doesn't exist in response)
        var rolloutResult = await _provider.ResolveDoubleValueAsync("feature-rollout-percent", 0.0, userContext);

        // Assert - Developer gets expected behavior for their application
        Assert.True(paymentResult.Value); // Beta users get new payment flow
        Assert.Equal("TARGETING_MATCH", paymentResult.Reason);

        Assert.Equal(100, uploadResult.Value); // Pro users get higher upload limit
        Assert.Equal("TARGETING_MATCH", uploadResult.Reason);

        var theme = themeResult.Value.AsStructure!.AsDictionary()["theme"].AsString;
        Assert.Equal("dark", theme); // Pro users get dark theme
        Assert.Equal("TARGETING_MATCH", themeResult.Reason);

        Assert.Equal(0.0, rolloutResult.Value); // Flag not found, gets default
        Assert.Equal("ERROR", rolloutResult.Reason);
    }


}
