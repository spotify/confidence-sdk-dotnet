using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Spotify.Confidence.Sdk.Options;
using Xunit;

namespace Spotify.Confidence.Sdk.Tests;

public class TestLogEntry
{
    public LogLevel Level { get; set; }
    public string? Message { get; set; }
    public Exception? Exception { get; set; }
}

public class TestLogger<T> : ILogger<T>
{
    private readonly List<TestLogEntry> _logs = new();
    private readonly LogLevel _minLevel;

    public TestLogger(LogLevel minLevel = LogLevel.Trace)
    {
        _minLevel = minLevel;
    }

    public IReadOnlyList<TestLogEntry> Logs => _logs.AsReadOnly();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        _logs.Add(new TestLogEntry
        {
            Level = logLevel,
            Message = formatter(state, exception),
            Exception = exception
        });
    }
}

public class ConfidenceClientLoggingTests
{
    [Fact]
    public void ConfidenceClient_WithWarningLogLevel_DoesNotLogInfoOrDebug()
    {
        // Arrange
        var testLogger = new TestLogger<ConfidenceClient>(LogLevel.Warning);
        
        var options = new ConfidenceOptions
        {
            ClientSecret = "test-secret",
            LogLevel = LogLevel.Warning,
            ResolveUrl = "https://api.test.com",
            EventUrl = "https://api.test.com"
        };

        // Act
        var client = new ConfidenceClient(options, testLogger);

        // Assert
        var logs = testLogger.Logs;
        
        // Should not contain Information or Debug level logs
        Assert.DoesNotContain(logs, log => log.Level == LogLevel.Information);
        Assert.DoesNotContain(logs, log => log.Level == LogLevel.Debug);
        
        // Verify no initialization log (which is Information level) was captured
        Assert.DoesNotContain(logs, log => log.Message?.Contains("ConfidenceClient initialized") == true);
    }

    [Fact]
    public void ConfidenceClient_WithInformationLogLevel_DoesNotLogDebug()
    {
        // Arrange
        var testLogger = new TestLogger<ConfidenceClient>(LogLevel.Information);
        
        var options = new ConfidenceOptions
        {
            ClientSecret = "test-secret",
            LogLevel = LogLevel.Information,
            ResolveUrl = "https://api.test.com",
            EventUrl = "https://api.test.com"
        };

        // Act
        var client = new ConfidenceClient(options, testLogger);

        // Assert
        var logs = testLogger.Logs;
        
        // Should not contain Debug level logs
        Assert.DoesNotContain(logs, log => log.Level == LogLevel.Debug);
        
        // Should contain Information level logs (like initialization)
        Assert.Contains(logs, log => log.Level == LogLevel.Information);
        Assert.Contains(logs, log => log.Message?.Contains("ConfidenceClient initialized") == true);
    }

    [Fact]
    public async Task ConfidenceClient_WithDebugLogLevel_LogsDebugAndInformation()
    {
        // Arrange
        var testLogger = new TestLogger<ConfidenceClient>(LogLevel.Debug);
        
        var options = new ConfidenceOptions
        {
            ClientSecret = "test-secret",
            LogLevel = LogLevel.Debug,
            ResolveUrl = "https://api.test.com",
            EventUrl = "https://api.test.com"
        };

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("""
                {
                    "resolved_flags": [],
                    "resolve_token": "test-token"
                }
                """, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://api.test.com")
        };

        var client = new ConfidenceClient(options, testLogger);

        // Use reflection to set the HTTP client (similar to existing tests)
        var resolveField = typeof(ConfidenceClient).GetField("_resolveClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        resolveField?.SetValue(client, httpClient);

        // Act - Perform an operation that generates debug logs
        await client.EvaluateBooleanFlagAsync("test-flag", false);

        // Assert
        var logs = testLogger.Logs;
        
        // Should contain Information level logs (like initialization)
        Assert.Contains(logs, log => log.Level == LogLevel.Information);
        Assert.Contains(logs, log => log.Message?.Contains("ConfidenceClient initialized") == true);
        
        // Should contain Debug level logs from flag evaluation
        Assert.Contains(logs, log => log.Level == LogLevel.Debug);
        Assert.Contains(logs, log => log.Message?.Contains("Resolving flag") == true);
    }

    [Fact]
    public void ConfidenceClient_WithNoneLogLevel_DoesNotLogAnything()
    {
        // Arrange
        var testLogger = new TestLogger<ConfidenceClient>(LogLevel.None);
        
        var options = new ConfidenceOptions
        {
            ClientSecret = "test-secret",
            LogLevel = LogLevel.None,
            ResolveUrl = "https://api.test.com",
            EventUrl = "https://api.test.com"
        };

        // Act
        var client = new ConfidenceClient(options, testLogger);

        // Assert
        var logs = testLogger.Logs;
        
        // Should not contain any logs
        Assert.Empty(logs);
    }
}