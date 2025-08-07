using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.Extensions.Http;
using Spotify.Confidence.Sdk.Exceptions;
using Spotify.Confidence.Sdk.Logging;
using Spotify.Confidence.Sdk.Models;
using Spotify.Confidence.Sdk.Options;

namespace Spotify.Confidence.Sdk;

/// <summary>
/// Client for interacting with the Confidence service.
/// </summary>
public class ConfidenceClient : IConfidenceClient, IDisposable
{
    private readonly HttpClient _resolveClient;
    private readonly HttpClient _trackingClient;
    private readonly string _clientSecret;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<ConfidenceClient> _logger;
    private bool _disposed;
    private const string RESOLVE_FLAGS_ENDPOINT = "v1/flags:resolve";
    private const string TRACK_EVENTS_ENDPOINT = "v1/events:publish";
    private const string SDK_ID = "SDK_ID_DOTNET_CONFIDENCE";
    internal static readonly string SDK_VERSION = GetSdkVersion();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfidenceClient"/> class.
    /// Creates a new instance of <see cref="ConfidenceClient"/>.
    /// </summary>
    /// <param name="options">The client options.</param>
    /// <param name="logger">Optional logger instance. If not provided, a console logger with the configured LogLevel will be created.</param>
    public ConfidenceClient(ConfidenceOptions options, ILogger<ConfidenceClient>? logger = null)
    {
        options.Validate();

        _logger = logger ?? CreateDefaultLogger(options.LogLevel);
        _clientSecret = options.ClientSecret;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        var handler = new HttpClientHandler();
        var policy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(options.MaxRetries, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        _resolveClient = new HttpClient(new PolicyHttpMessageHandler(policy)
        {
            InnerHandler = handler
        })
        {
            BaseAddress = new Uri(options.ResolveUrl),
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
        };

        _trackingClient = new HttpClient(new PolicyHttpMessageHandler(policy)
        {
            InnerHandler = new HttpClientHandler()
        })
        {
            BaseAddress = new Uri(options.EventUrl),
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
        };

        ConfigureHttpClient(_resolveClient);
        ConfigureHttpClient(_trackingClient);
        
        ConfidenceClientLogger.ClientInitialized(_logger, options.ResolveUrl, options.EventUrl, options.TimeoutSeconds, options.MaxRetries, null);
    }

    /// <inheritdoc />
    public async Task<EvaluationResult<bool>> EvaluateBooleanFlagAsync(
        string flagKey,
        ConfidenceContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceClientLogger.EvaluatingBooleanFlag(_logger, flagKey, context?.Attributes, null);
        return await ResolveFlagAsync(flagKey, false, context, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EvaluationResult<string>> EvaluateStringFlagAsync(
        string flagKey,
        ConfidenceContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceClientLogger.EvaluatingStringFlag(_logger, flagKey, context?.Attributes, null);
        return await ResolveFlagAsync(flagKey, string.Empty, context, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EvaluationResult<double>> EvaluateNumericFlagAsync(
        string flagKey,
        ConfidenceContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceClientLogger.EvaluatingNumericFlag(_logger, flagKey, context?.Attributes, null);
        return await ResolveFlagAsync(flagKey, 0.0, context, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EvaluationResult<object>> EvaluateJsonFlagAsync(
        string flagKey,
        ConfidenceContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceClientLogger.EvaluatingJsonFlag(_logger, flagKey, context?.Attributes, null);
        return await ResolveFlagAsync<object>(flagKey, new Dictionary<string, object>(), context, cancellationToken);
    }

    /// <inheritdoc />
    public async Task TrackAsync(
        string eventName,
        IDictionary<string, object>? data = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceClientLogger.TrackingEvent(_logger, eventName, data, null);
        
        var currentTime = DateTimeOffset.UtcNow;
        var iso8601Time = currentTime.ToString("O");

        var eventData = new Dictionary<string, object>();
        if (data != null)
        {
            foreach (var kvp in data)
            {
                if (kvp.Key == "context")
                {
                    throw new ArgumentException("Invalid key \"context\" inside the data");
                }
                eventData[kvp.Key] = kvp.Value;
            }
        }

        var @event = new Event
        {
            EventDefinition = $"eventDefinitions/{eventName}",
            EventTime = iso8601Time,
            Payload = eventData
        };

        var batch = new EventBatchRequest
        {
            ClientSecret = _clientSecret,
            Sdk = new SdkInfo { Id = SDK_ID, Version = SDK_VERSION },
            SendTime = iso8601Time,
            Events = new List<Event> { @event }
        };

        try
        {
            await SendRequestAsync<object>(_trackingClient, TRACK_EVENTS_ENDPOINT, batch, cancellationToken);
            ConfidenceClientLogger.EventTracked(_logger, eventName, null);
        }
        catch (OperationCanceledException)
        {
            ConfidenceClientLogger.EventTrackingCancelled(_logger, eventName, null);
        }
        catch (ConfidenceException ex)
        {
            ConfidenceClientLogger.EventTrackingFailedConfidence(_logger, eventName, ex);
        }
        catch (HttpRequestException ex)
        {
            ConfidenceClientLogger.EventTrackingFailedNetwork(_logger, eventName, ex);
        }
    }

    /// <inheritdoc />
    public async Task<EvaluationResult<T>> ResolveFlagAsync<T>(
        string flagKey,
        T defaultValue,
        ConfidenceContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceClientLogger.ResolvingFlag(_logger, flagKey, typeof(T).Name, defaultValue, null);
        
        try
        {
            var request = new EvaluationRequest(flagKey, context, _clientSecret);
            var response = await SendRequestAsync<ResolveResponse>(_resolveClient, RESOLVE_FLAGS_ENDPOINT, request, cancellationToken);

            var flag = GetResolvedFlagOrDefault(response, flagKey);
            if (flag == null)
            {
                ConfidenceClientLogger.FlagNotFound(_logger, flagKey, defaultValue, null);
                return EvaluationResult.Failure(defaultValue, $"Flag '{flagKey}' not found in response");
            }

            var value = flag.Value.TryGetValue("value", out var wrappedValue) ? wrappedValue : flag.Value;

            T typedValue;
            try
            {
                if (value is JsonElement element)
                {
                    if (typeof(T) == typeof(bool))
                    {
                        typedValue = (T)(object)(element.ValueKind == JsonValueKind.True);
                    }
                    else if (typeof(T) == typeof(string))
                    {
                        typedValue = (T)(object)element.GetString()!;
                    }
                    else if (typeof(T) == typeof(double))
                    {
                        typedValue = (T)(object)element.GetDouble();
                    }
                    else
                    {
                        // For complex objects, deserialize using JsonSerializer
                        var json = element.GetRawText();
                        typedValue = JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? defaultValue;
                    }
                }
                else
                {
                    typedValue = value is T t ? t : defaultValue;
                }
            }
            catch (Exception ex)
            {
                ConfidenceClientLogger.FlagParsingFailed(_logger, flagKey, defaultValue, ex);
                return EvaluationResult.Failure(defaultValue, "Failed to parse flag value", ex);
            }

            ConfidenceClientLogger.FlagResolved(_logger, flagKey, typedValue, flag.Reason, flag.Variant, null);
            return EvaluationResult.Success(typedValue, flag.Reason, flag.Variant);
        }
        catch (OperationCanceledException ex)
        {
            ConfidenceClientLogger.FlagResolutionCancelled(_logger, flagKey, defaultValue, ex);
            return EvaluationResult.Failure(defaultValue, "Request was cancelled", ex);
        }
        catch (ConfidenceException ex)
        {
            ConfidenceClientLogger.FlagResolutionFailedConfidence(_logger, flagKey, defaultValue, ex);
            return EvaluationResult.Failure(defaultValue, ex.Message, ex);
        }
        catch (Exception ex)
        {
            ConfidenceClientLogger.FlagResolutionFailedUnexpected(_logger, flagKey, defaultValue, ex);
            return EvaluationResult.Failure(defaultValue, "An unexpected error occurred", ex);
        }
    }

    internal static string GetFullFlagKey(string flagKey)
    {
        if (flagKey.StartsWith("flags/", StringComparison.Ordinal))
        {
            return flagKey;
        }
        return $"flags/{flagKey}";
    }

    private static ILogger<ConfidenceClient> CreateDefaultLogger(LogLevel logLevel)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(logLevel)
                .AddConsole();
        });
        
        return loggerFactory.CreateLogger<ConfidenceClient>();
    }

    private static string GetSdkVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "unknown";
    }

    private static ResolvedFlag? GetResolvedFlagOrDefault(ResolveResponse response, string flagKey)
    {
        var fullFlagKey = GetFullFlagKey(flagKey);
        var flag = response.ResolvedFlags.FirstOrDefault(f =>
            string.Equals(f.Flag, fullFlagKey, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(f.Flag, flagKey, StringComparison.OrdinalIgnoreCase));

        if (flag != null)
        {
            // Initialize Value dictionary if it's null
            flag.Value ??= new Dictionary<string, object>();
        }

        return flag;
    }

    private async Task<T> SendRequestAsync<T>(HttpClient client, string path, object request, CancellationToken cancellationToken)
    {
        var baseUrl = client.BaseAddress?.ToString() ?? "unknown";
        ConfidenceClientLogger.SendingRequest(_logger, typeof(T).Name, baseUrl, path, null);
        
        try
        {
            var response = await client.PostAsJsonAsync(
                path,
                request,
                _jsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();
            ConfidenceClientLogger.ReceivedSuccessfulResponse(_logger, (int)response.StatusCode, typeof(T).Name, null);

            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
            if (result is null)
            {
                ConfidenceClientLogger.ReceivedNullResponse(_logger, typeof(T).Name, null);
                throw new ConfidenceException("Received null response from Confidence API");
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            ConfidenceClientLogger.RequestFailedHttp(_logger, typeof(T).Name, baseUrl, path, ex);
            throw new ConfidenceException("Failed to communicate with Confidence API", ex);
        }
        catch (JsonException ex)
        {
            ConfidenceClientLogger.RequestFailedDeserialization(_logger, typeof(T).Name, ex);
            throw new ConfidenceException("Failed to deserialize response from Confidence API", ex);
        }
        catch (OperationCanceledException ex)
        {
            ConfidenceClientLogger.RequestCancelled(_logger, typeof(T).Name, baseUrl, path, ex);
            throw; // Re-throw cancellation exceptions without wrapping
        }
        catch (ConfidenceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ConfidenceClientLogger.RequestFailedUnexpected(_logger, typeof(T).Name, baseUrl, path, ex);
            throw new ConfidenceException("An unexpected error occurred", ex);
        }
    }

    private static void ConfigureHttpClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the resources used by this instance.
    /// </summary>
    /// <param name="disposing">Whether this method is being called from Dispose().</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            ConfidenceClientLogger.DisposingClient(_logger, null);
            _resolveClient.Dispose();
            _trackingClient.Dispose();
        }

        _disposed = true;
    }
}
