using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Spotify.Confidence.Sdk;
using Spotify.Confidence.Sdk.Models;
using Spotify.Confidence.Sdk.Options;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Spotify.Confidence.Sdk.Tests;

/// <summary>
/// Tests to verify that custom default values are properly handled and returned.
/// </summary>
public class DefaultValueParameterTests
{
    private readonly ConfidenceClient _client;
    private readonly Mock<HttpMessageHandler> _mockHandler;

    public DefaultValueParameterTests()
    {
        var options = new ConfidenceOptions
        {
            ClientSecret = "test-client-secret",
            ResolveUrl = "https://api.test.com",
            EventUrl = "https://api.test.com",
            LogLevel = LogLevel.None,
        };

        _mockHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://api.test.com")
        };

        _client = new ConfidenceClient(options);

        // Use reflection to set the HTTP client
        var resolveField = typeof(ConfidenceClient).GetField("_resolveClient",
            BindingFlags.NonPublic | BindingFlags.Instance);
        resolveField?.SetValue(_client, httpClient);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EvaluateBooleanFlagAsync_WithDifferentDefaults_ReturnsCorrectDefault(bool customDefault)
    {
        // Arrange
        const string flagKey = "test-flag";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = new ResolvedFlag[0] // Flag not found
        });

        // Act
        var result = await _client.EvaluateBooleanFlagAsync(flagKey, customDefault, context);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(customDefault, result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("default")]
    [InlineData("custom-default")]
    [InlineData("very-long-custom-default-value-with-special-chars-!@#$%")]
    public async Task EvaluateStringFlagAsync_WithDifferentDefaults_ReturnsCorrectDefault(string customDefault)
    {
        // Arrange
        const string flagKey = "test-flag";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = new ResolvedFlag[0] // Flag not found
        });

        // Act
        var result = await _client.EvaluateStringFlagAsync(flagKey, customDefault, context);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(customDefault, result.Value);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(99.99)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    public async Task EvaluateNumericFlagAsync_WithDifferentDefaults_ReturnsCorrectDefault(double customDefault)
    {
        // Arrange
        const string flagKey = "test-flag";
        var context = new ConfidenceContext(new Dictionary<string, object> { { "user_id", "user123" } });

        SetupMockResponse(HttpStatusCode.OK, new ResolveResponse
        {
            ResolvedFlags = new ResolvedFlag[0] // Flag not found
        });

        // Act
        var result = await _client.EvaluateNumericFlagAsync(flagKey, customDefault, context);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(customDefault, result.Value);
    }

    private void SetupMockResponse<T>(HttpStatusCode statusCode, T? responseObject)
    {
        var response = new HttpResponseMessage
        {
            StatusCode = statusCode
        };

        if (responseObject != null)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(responseObject, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
            });
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<System.Threading.CancellationToken>())
            .ReturnsAsync(response);
    }
}