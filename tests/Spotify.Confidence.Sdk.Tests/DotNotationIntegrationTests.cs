using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Polly.Extensions.Http;
using Polly;
using Spotify.Confidence.Sdk.Models;
using Spotify.Confidence.Sdk.Options;
using Xunit;

namespace Spotify.Confidence.Sdk.Tests;

public class DotNotationIntegrationTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly ConfidenceOptions _options;
    private readonly ConfidenceClient _client;

    public DotNotationIntegrationTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _options = new ConfidenceOptions
        {
            ClientSecret = "test-client-secret",
            ResolveUrl = "https://api.test.com",
            EventUrl = "https://api.test.com",
            LogLevel = LogLevel.None // Disable logging in tests
        };

        var handler = _mockHandler.Object;
        var policy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(1));

        var resolveClient = new HttpClient(new PolicyHttpMessageHandler(policy)
        {
            InnerHandler = handler
        })
        {
            BaseAddress = new Uri(_options.ResolveUrl)
        };

        var trackingClient = new HttpClient(new PolicyHttpMessageHandler(policy)
        {
            InnerHandler = handler
        })
        {
            BaseAddress = new Uri(_options.EventUrl)
        };

        _client = new ConfidenceClient(_options);

        // Use reflection to set both private HttpClient fields
        var resolveField = typeof(ConfidenceClient).GetField("_resolveClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var trackingField = typeof(ConfidenceClient).GetField("_trackingClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        resolveField?.SetValue(_client, resolveClient);
        trackingField?.SetValue(_client, trackingClient);
    }

    [Fact]
    public async Task EvaluateBooleanFlagAsync_WithDotNotation_ExtractsNestedProperty()
    {
        // Arrange
        var flagKey = "user-settings.preferences.darkMode";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });

        var flagValue = new Dictionary<string, object>
        {
            ["value"] = JsonDocument.Parse("""
            {
                "preferences": {
                    "darkMode": true,
                    "theme": "modern"
                },
                "profile": {
                    "name": "John Doe"
                }
            }
            """).RootElement
        };

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = new[]
            {
                new ResolvedFlag
                {
                    Flag = "flags/user-settings",
                    Variant = "control",
                    Reason = "MATCH",
                    Value = flagValue
                }
            }
        });

        // Act
        var result = await _client.EvaluateBooleanFlagAsync(flagKey, false, context);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.Equal("MATCH", result.Reason);
        Assert.Equal("control", result.Variant);
    }

    [Fact]
    public async Task EvaluateStringFlagAsync_WithDotNotation_ExtractsNestedProperty()
    {
        // Arrange
        var flagKey = "app-config.ui.theme.primaryColor";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });

        var flagValue = new Dictionary<string, object>
        {
            ["value"] = JsonDocument.Parse("""
            {
                "ui": {
                    "theme": {
                        "primaryColor": "#FF5733",
                        "secondaryColor": "#33FF57"
                    },
                    "layout": "grid"
                }
            }
            """).RootElement
        };

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = new[]
            {
                new ResolvedFlag
                {
                    Flag = "flags/app-config",
                    Variant = "treatment",
                    Reason = "MATCH",
                    Value = flagValue
                }
            }
        });

        // Act
        var result = await _client.EvaluateStringFlagAsync(flagKey, "#000000", context);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("#FF5733", result.Value);
        Assert.Equal("MATCH", result.Reason);
        Assert.Equal("treatment", result.Variant);
    }

    [Fact]
    public async Task EvaluateNumericFlagAsync_WithDotNotation_ExtractsNestedProperty()
    {
        // Arrange
        var flagKey = "performance.cache.timeoutMs";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });

        var flagValue = new Dictionary<string, object>
        {
            ["value"] = JsonDocument.Parse("""
            {
                "cache": {
                    "timeoutMs": 5000.5,
                    "maxSize": 1000
                },
                "database": {
                    "connectionTimeout": 30000
                }
            }
            """).RootElement
        };

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = new[]
            {
                new ResolvedFlag
                {
                    Flag = "flags/performance",
                    Variant = "optimized",
                    Reason = "MATCH",
                    Value = flagValue
                }
            }
        });

        // Act
        var result = await _client.EvaluateNumericFlagAsync(flagKey, 1000.0, context);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5000.5, result.Value);
        Assert.Equal("MATCH", result.Reason);
        Assert.Equal("optimized", result.Variant);
    }

    [Fact]
    public async Task EvaluateJsonFlagAsync_WithDotNotation_ExtractsNestedObject()
    {
        // Arrange
        var flagKey = "feature-config.experimental.newFeature";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });

        var flagValue = new Dictionary<string, object>
        {
            ["value"] = JsonDocument.Parse("""
            {
                "experimental": {
                    "newFeature": {
                        "enabled": true,
                        "rolloutPercentage": 25,
                        "description": "New experimental feature"
                    }
                },
                "stable": {
                    "existingFeature": {
                        "enabled": true
                    }
                }
            }
            """).RootElement
        };

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = new[]
            {
                new ResolvedFlag
                {
                    Flag = "flags/feature-config",
                    Variant = "experiment",
                    Reason = "MATCH",
                    Value = flagValue
                }
            }
        });

        // Act
        var result = await _client.EvaluateJsonFlagAsync(flagKey, new { }, context);

        // Assert
        Assert.True(result.IsSuccess);
        var resultElement = Assert.IsType<JsonElement>(result.Value);
        Assert.True(resultElement.GetProperty("enabled").GetBoolean());
        Assert.Equal(25, resultElement.GetProperty("rolloutPercentage").GetInt32());
        Assert.Equal("New experimental feature", resultElement.GetProperty("description").GetString());
        Assert.Equal("MATCH", result.Reason);
        Assert.Equal("experiment", result.Variant);
    }

    [Fact]
    public async Task EvaluateBooleanFlagAsync_WithDotNotation_PropertyNotFound_ReturnsDefault()
    {
        // Arrange
        var flagKey = "user-settings.nonexistent.property";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });

        var flagValue = new Dictionary<string, object>
        {
            ["value"] = JsonDocument.Parse("""
            {
                "preferences": {
                    "darkMode": true
                }
            }
            """).RootElement
        };

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = new[]
            {
                new ResolvedFlag
                {
                    Flag = "flags/user-settings",
                    Variant = "control",
                    Reason = "MATCH",
                    Value = flagValue
                }
            }
        });

        // Act
        var result = await _client.EvaluateBooleanFlagAsync(flagKey, false, context);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(result.Value); // Should return default value
        Assert.Contains("Property path 'nonexistent.property' not found", result.ErrorMessage);
        Assert.Equal("ERROR", result.Reason);
    }

    [Fact]
    public async Task EvaluateBooleanFlagAsync_WithoutDotNotation_WorksAsUsual()
    {
        // Arrange
        var flagKey = "simple-flag";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });

        var flagValue = new Dictionary<string, object>
        {
            ["value"] = JsonDocument.Parse("true").RootElement
        };

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = new[]
            {
                new ResolvedFlag
                {
                    Flag = "flags/simple-flag",
                    Variant = "control",
                    Reason = "MATCH",
                    Value = flagValue
                }
            }
        });

        // Act
        var result = await _client.EvaluateBooleanFlagAsync(flagKey, false, context);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.Equal("MATCH", result.Reason);
        Assert.Equal("control", result.Variant);
    }

    [Fact]
    public async Task EvaluateBooleanFlagAsync_WithDotNotation_FlagNotFound_ReturnsFailure()
    {
        // Arrange
        var flagKey = "nonexistent-flag.property";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = Array.Empty<ResolvedFlag>()
        });

        // Act
        var result = await _client.EvaluateBooleanFlagAsync(flagKey, true, context);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.Value); // Should return default value
        Assert.Equal("Flag 'nonexistent-flag' not found in response", result.ErrorMessage);
        Assert.Equal("ERROR", result.Reason);
    }

    [Fact]
    public async Task EvaluateBooleanFlagAsync_WithDeepDotNotation_ExtractsCorrectly()
    {
        // Arrange
        var flagKey = "complex-config.level1.level2.level3.level4.deepProperty";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });

        var flagValue = new Dictionary<string, object>
        {
            ["value"] = JsonDocument.Parse("""
            {
                "level1": {
                    "level2": {
                        "level3": {
                            "level4": {
                                "deepProperty": true,
                                "otherProperty": "test"
                            }
                        }
                    }
                }
            }
            """).RootElement
        };

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = new[]
            {
                new ResolvedFlag
                {
                    Flag = "flags/complex-config",
                    Variant = "deep",
                    Reason = "MATCH",
                    Value = flagValue
                }
            }
        });

        // Act
        var result = await _client.EvaluateBooleanFlagAsync(flagKey, false, context);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.Equal("MATCH", result.Reason);
        Assert.Equal("deep", result.Variant);
    }

    private void SetupMockResponse<T>(HttpStatusCode statusCode, T? content)
    {
        var response = new HttpResponseMessage(statusCode);
        if (content != null)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            var json = JsonSerializer.Serialize(content, options);
            // Convert property names to snake case
            json = json.Replace("\"Flag\":", "\"flag\":")
                      .Replace("\"Value\":", "\"value\":")
                      .Replace("\"Variant\":", "\"variant\":")
                      .Replace("\"Reason\":", "\"reason\":")
                      .Replace("\"FlagSchema\":", "\"flag_schema\":");
            response.Content = new StringContent(
                json,
                System.Text.Encoding.UTF8,
                "application/json");
        }

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}