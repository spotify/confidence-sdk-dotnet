using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

using Moq;
using Moq.Protected;
using Polly;
using Polly.Extensions.Http;
using Spotify.Confidence.Sdk.Exceptions;
using Spotify.Confidence.Sdk.Models;
using Spotify.Confidence.Sdk.Options;
using Xunit;

namespace Spotify.Confidence.Sdk.Tests;

public class ConfidenceClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly ConfidenceOptions _options;
    private readonly ConfidenceClient _client;

    public ConfidenceClientTests()
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
    public async Task EvaluateBooleanFlagAsync_SuccessfulResponse_ReturnsResult()
    {
        // Arrange
        var flagKey = "test-flag";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });
        var expectedResult = new EvaluationResult<bool>(true, "test-reason", "test-variant");

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = new[]
            {
                new ResolvedFlag
                {
                    Flag = "flags/test-flag",
                    Value = new Dictionary<string, object> { { "value", true } },
                    Reason = "test-reason",
                    Variant = "test-variant"
                }
            }
        });

        // Act
        var result = await _client.EvaluateBooleanFlagAsync(flagKey, false, context);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResult.Value, result.Value);
        Assert.Equal(expectedResult.Reason, result.Reason);
        Assert.Equal(expectedResult.Variant, result.Variant);

        VerifyRequest("v1/flags:resolve", HttpMethod.Post);
    }

    [Fact]
    public async Task EvaluateStringFlagAsync_SuccessfulResponse_ReturnsResult()
    {
        // Arrange
        var flagKey = "test-flag";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });
        var expectedResult = new EvaluationResult<string>("test-value", "test-reason", "test-variant");

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = new[]
            {
                new ResolvedFlag
                {
                    Flag = "flags/test-flag",
                    Value = new Dictionary<string, object> { { "value", "test-value" } },
                    Reason = "test-reason",
                    Variant = "test-variant"
                }
            }
        });

        // Act
        var result = await _client.EvaluateStringFlagAsync(flagKey, "default", context);

        // Assert
        Assert.Equal(expectedResult.Value, result.Value);
        Assert.Equal(expectedResult.Reason, result.Reason);
        Assert.Equal(expectedResult.Variant, result.Variant);

        VerifyRequest("v1/flags:resolve", HttpMethod.Post);
    }

    [Fact]
    public async Task EvaluateNumericFlagAsync_SuccessfulResponse_ReturnsResult()
    {
        // Arrange
        var flagKey = "test-flag";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });
        var expectedResult = new EvaluationResult<double>(42.0, "test-reason", "test-variant");

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = new[]
            {
                new ResolvedFlag
                {
                    Flag = "flags/test-flag",
                    Value = new Dictionary<string, object> { { "value", 42.0 } },
                    Reason = "test-reason",
                    Variant = "test-variant"
                }
            }
        });

        // Act
        var result = await _client.EvaluateNumericFlagAsync(flagKey, 0.0, context);

        // Assert
        Assert.Equal(expectedResult.Value, result.Value);
        Assert.Equal(expectedResult.Reason, result.Reason);
        Assert.Equal(expectedResult.Variant, result.Variant);

        VerifyRequest("v1/flags:resolve", HttpMethod.Post);
    }

    [Fact]
    public async Task EvaluateJsonFlagAsync_SuccessfulResponse_ReturnsResult()
    {
        // Arrange
        var flagKey = "test-flag";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });
        var expectedResult = new EvaluationResult<object>(
            new { test = "value" },
            "test-reason",
            "test-variant");

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = new[]
            {
                new ResolvedFlag
                {
                    Flag = "flags/test-flag",
                    Value = new Dictionary<string, object> { { "value", new { test = "value" } } },
                    Reason = "test-reason",
                    Variant = "test-variant"
                }
            }
        });

        // Act
        var result = await _client.EvaluateJsonFlagAsync(flagKey, new Dictionary<string, object>(), context);

        // Assert
        var expectedJson = JsonSerializer.Serialize(expectedResult.Value);
        var actualJson = JsonSerializer.Serialize(result.Value);
        Assert.Equal(expectedJson, actualJson);
        Assert.Equal(expectedResult.Reason, result.Reason);
        Assert.Equal(expectedResult.Variant, result.Variant);

        VerifyRequest("v1/flags:resolve", HttpMethod.Post);
    }

    [Fact]
    public async Task EvaluateBooleanFlagAsync_ApiError_ReturnsFailureResult()
    {
        // Arrange
        var flagKey = "test-flag";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = Array.Empty<ResolvedFlag>()
        });

        // Act
        var result = await _client.EvaluateBooleanFlagAsync(flagKey, false, context);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal($"Flag '{flagKey}' not found in response", result.ErrorMessage);
        Assert.Equal("ERROR", result.Reason);
        Assert.False(result.Value); // Should return default value (false for bool)
    }

    [Fact]
    public async Task EvaluateBooleanFlagAsync_HttpError_ReturnsFailureResult()
    {
        // Arrange
        var flagKey = "test-flag";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });

        SetupMockResponse<ResolveResponse>(HttpStatusCode.InternalServerError, null);

        // Act
        var result = await _client.EvaluateBooleanFlagAsync(flagKey, false, context);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Failed to communicate with Confidence API", result.ErrorMessage);
        Assert.Equal("ERROR", result.Reason);
        Assert.False(result.Value); // Should return default value (false for bool)
    }

    [Fact]
    public async Task TrackAsync_SuccessfulResponse_SendsEvent()
    {
        // Arrange
        var eventName = "test-event";
        var data = new Dictionary<string, object>
        {
            { "test", "value" }
        };

        SetupMockResponse(HttpStatusCode.OK, new object());

        // Act
        await _client.TrackAsync(eventName, data);

        // Assert
        VerifyRequest("v1/events:publish", HttpMethod.Post);
    }

    [Fact]
    public async Task TrackAsync_WithContextKey_ThrowsArgumentException()
    {
        // Arrange
        var eventName = "test-event";
        var data = new Dictionary<string, object>
        {
            { "context", "value" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _client.TrackAsync(eventName, data));
    }

    [Fact]
    public async Task ResolveFlagAsync_WithCustomType_SuccessfulResponse_ReturnsResult()
    {
        // Arrange
        var flagKey = "test-flag";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });
        var defaultValue = new TestObject { Name = "default" };
        var expectedValue = new TestObject { Name = "test" };

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = new[]
            {
                new ResolvedFlag
                {
                    Flag = "flags/test-flag",
                    Value = new Dictionary<string, object> { { "value", expectedValue } },
                    Reason = "test-reason",
                    Variant = "test-variant"
                }
            }
        });

        // Act
        var result = await _client.ResolveFlagAsync(flagKey, defaultValue, context);

        // Assert
        Assert.Equal(expectedValue.Name, ((TestObject)result.Value).Name);
        Assert.Equal("test-reason", result.Reason);
        Assert.Equal("test-variant", result.Variant);

        VerifyRequest("v1/flags:resolve", HttpMethod.Post);
    }

    [Fact]
    public async Task EvaluationRequest_HasCorrectPayloadStructure()
    {
        // Arrange
        var flagKey = "test-flag";
        var context = new ConfidenceContext(new Dictionary<string, object>
        {
            { "user_id", "user123" },
            { "environment", "production" },
            { "user_type", "premium" },
            { "region", "us-east" }
        });
        var capturedRequest = string.Empty;

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var resolveResponse = new ResolveResponse
        {
            ResolvedFlags = new[]
            {
                new ResolvedFlag
                {
                    Flag = "flags/test-flag",
                    Value = new Dictionary<string, object> { { "value", true } },
                    Reason = "test-reason",
                    Variant = "test-variant"
                }
            }
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
        var json = JsonSerializer.Serialize(resolveResponse, options);
        response.Content = new StringContent(json, Encoding.UTF8, "application/json");

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                if (req.Content != null)
                {
                    capturedRequest = await req.Content.ReadAsStringAsync();
                }
            })
            .ReturnsAsync(response);

        // Act
        await _client.EvaluateBooleanFlagAsync(flagKey, false, context);

        // Assert
        Assert.NotEmpty(capturedRequest);

        var requestObj = JsonSerializer.Deserialize<JsonElement>(capturedRequest);

        // Verify client_secret is present
        Assert.True(requestObj.TryGetProperty("client_secret", out var clientSecretElement));
        Assert.Equal("test-client-secret", clientSecretElement.GetString());

        // Verify apply is true
        Assert.True(requestObj.TryGetProperty("apply", out var applyElement));
        Assert.True(applyElement.GetBoolean());

        // Verify flags array contains properly formatted flag key
        Assert.True(requestObj.TryGetProperty("flags", out var flagsElement));
        Assert.Equal(JsonValueKind.Array, flagsElement.ValueKind);
        var flagsArray = flagsElement.EnumerateArray().ToArray();
        Assert.Single(flagsArray);
        Assert.Equal("flags/test-flag", flagsArray[0].GetString());

        // Verify evaluation_context contains targeting_key and attributes
        Assert.True(requestObj.TryGetProperty("evaluation_context", out var contextElement));
        Assert.True(contextElement.TryGetProperty("user_id", out var userIdElement));
        Assert.Equal("user123", userIdElement.GetString());

        // Verify custom attributes are included
        Assert.True(contextElement.TryGetProperty("environment", out var envElement));
        Assert.Equal("production", envElement.GetString());
        Assert.True(contextElement.TryGetProperty("user_type", out var userTypeElement));
        Assert.Equal("premium", userTypeElement.GetString());
        Assert.True(contextElement.TryGetProperty("region", out var regionElement));
        Assert.Equal("us-east", regionElement.GetString());

        // Verify SDK information
        Assert.True(requestObj.TryGetProperty("sdk", out var sdkElement));
        Assert.True(sdkElement.TryGetProperty("id", out var idElement));
        Assert.Equal("SDK_ID_DOTNET_CONFIDENCE", idElement.GetString());
        Assert.True(sdkElement.TryGetProperty("version", out var versionElement));
        Assert.Equal("0.0.1.0", versionElement.GetString());
    }

    private class TestObject
    {
        public string Name { get; set; } = string.Empty;
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
                Encoding.UTF8,
                "application/json");
        }

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => true),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void VerifyRequest(string path, HttpMethod method)
    {
        var expectedUri = new Uri($"{_options.ResolveUrl}/{path}");

        _mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().EndsWith(path)),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateBooleanFlagAsync_WithCancelledToken_ReturnsFailureResult()
    {
        // Arrange
        var flagKey = "test-flag";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled token

        // Act
        var result = await _client.EvaluateBooleanFlagAsync(flagKey, false, context, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.ErrorMessage ?? string.Empty); // Any error message is fine
        Assert.NotNull(result.Exception);
        Assert.IsType<TaskCanceledException>(result.Exception);
        Assert.Equal("ERROR", result.Reason);
        Assert.False(result.Value); // Should return default value (false for bool)
    }

    [Fact]
    public async Task TrackAsync_WithCancelledToken_CompletesSuccessfully()
    {
        // Arrange
        var eventName = "test-event";
        var data = new Dictionary<string, object> { { "test", "value" } };
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled token

        // Act - TrackAsync should complete successfully even when cancelled (fire-and-forget behavior)
        await _client.TrackAsync(eventName, data, cts.Token);
        
        // Assert - If we reach here, the method completed without throwing
        Assert.True(true); // Test passes if no exception was thrown
    }

    [Fact]
    public async Task EvaluateBooleanFlagAsync_WithTimeoutCancellation_ReturnsFailureResult()
    {
        // Arrange
        var flagKey = "test-flag";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });
        
        // Set up mock to simulate a delayed response that respects cancellation
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                // Simulate a long-running request that checks for cancellation
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50)); // Short timeout

        // Act
        var result = await _client.EvaluateBooleanFlagAsync(flagKey, false, context, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.ErrorMessage ?? string.Empty); // Any error message is fine
        Assert.NotNull(result.Exception);
        Assert.IsType<TaskCanceledException>(result.Exception);
        Assert.Equal("ERROR", result.Reason);
        Assert.False(result.Value); // Should return default value (false for bool)
    }

}
