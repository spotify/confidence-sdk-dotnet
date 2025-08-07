using Microsoft.Extensions.Logging;

namespace Spotify.Confidence.Sdk.Logging;

/// <summary>
/// High-performance logging delegates for ConfidenceClient using LoggerMessage.Define for optimal performance.
/// </summary>
internal static class ConfidenceClientLogger
{
    // Client lifecycle events
    public static readonly Action<ILogger, string, string, int, int, Exception?> ClientInitialized =
        LoggerMessage.Define<string, string, int, int>(
            LogLevel.Information,
            new EventId(1000, "ClientInitialized"),
            "ConfidenceClient initialized with resolve URL: {ResolveUrl}, event URL: {EventUrl}, timeout: {TimeoutSeconds}s, max retries: {MaxRetries}");

    public static readonly Action<ILogger, Exception?> DisposingClient =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1024, "DisposingClient"),
            "Disposing ConfidenceClient resources");

    // Flag evaluation events
    public static readonly Action<ILogger, string, object?, Exception?> EvaluatingBooleanFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(1001, "EvaluatingBooleanFlag"),
            "Evaluating boolean flag '{FlagKey}' with context: {Context}");

    public static readonly Action<ILogger, string, object?, Exception?> EvaluatingStringFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(1002, "EvaluatingStringFlag"),
            "Evaluating string flag '{FlagKey}' with context: {Context}");

    public static readonly Action<ILogger, string, object?, Exception?> EvaluatingNumericFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(1003, "EvaluatingNumericFlag"),
            "Evaluating numeric flag '{FlagKey}' with context: {Context}");

    public static readonly Action<ILogger, string, object?, Exception?> EvaluatingJsonFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(1004, "EvaluatingJsonFlag"),
            "Evaluating JSON flag '{FlagKey}' with context: {Context}");

    // Flag resolution events
    public static readonly Action<ILogger, string, string, object?, Exception?> ResolvingFlag =
        LoggerMessage.Define<string, string, object?>(
            LogLevel.Debug,
            new EventId(1010, "ResolvingFlag"),
            "Resolving flag '{FlagKey}' of type {Type} with default value: {DefaultValue}");

    public static readonly Action<ILogger, string, object?, Exception?> FlagNotFound =
        LoggerMessage.Define<string, object?>(
            LogLevel.Warning,
            new EventId(1011, "FlagNotFound"),
            "Flag '{FlagKey}' not found in response, returning default value: {DefaultValue}");

    public static readonly Action<ILogger, string, object?, Exception?> FlagParsingFailed =
        LoggerMessage.Define<string, object?>(
            LogLevel.Error,
            new EventId(1012, "FlagParsingFailed"),
            "Failed to parse flag value for '{FlagKey}', returning default value: {DefaultValue}");

    public static readonly Action<ILogger, string, object?, string, string, Exception?> FlagResolved =
        LoggerMessage.Define<string, object?, string, string>(
            LogLevel.Debug,
            new EventId(1013, "FlagResolved"),
            "Successfully resolved flag '{FlagKey}' with value: {Value}, reason: {Reason}, variant: {Variant}");

    public static readonly Action<ILogger, string, object?, Exception?> FlagResolutionCancelled =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(1014, "FlagResolutionCancelled"),
            "Flag resolution cancelled for '{FlagKey}', returning default value: {DefaultValue}");

    public static readonly Action<ILogger, string, object?, Exception?> FlagResolutionFailedConfidence =
        LoggerMessage.Define<string, object?>(
            LogLevel.Error,
            new EventId(1015, "FlagResolutionFailedConfidence"),
            "Confidence API error while resolving flag '{FlagKey}', returning default value: {DefaultValue}");

    public static readonly Action<ILogger, string, object?, Exception?> FlagResolutionFailedUnexpected =
        LoggerMessage.Define<string, object?>(
            LogLevel.Error,
            new EventId(1016, "FlagResolutionFailedUnexpected"),
            "Unexpected error while resolving flag '{FlagKey}', returning default value: {DefaultValue}");

    // Event tracking events
    public static readonly Action<ILogger, string, object?, Exception?> TrackingEvent =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(1005, "TrackingEvent"),
            "Tracking event '{EventName}' with data: {Data}");

    public static readonly Action<ILogger, string, Exception?> EventTracked =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(1006, "EventTracked"),
            "Successfully tracked event '{EventName}'");

    public static readonly Action<ILogger, string, Exception?> EventTrackingCancelled =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1007, "EventTrackingCancelled"),
            "Event tracking cancelled for '{EventName}'");

    public static readonly Action<ILogger, string, Exception?> EventTrackingFailedConfidence =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1008, "EventTrackingFailedConfidence"),
            "Failed to track event '{EventName}' due to Confidence API error");

    public static readonly Action<ILogger, string, Exception?> EventTrackingFailedNetwork =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1009, "EventTrackingFailedNetwork"),
            "Failed to track event '{EventName}' due to network error");

    // HTTP request events
    public static readonly Action<ILogger, string, string, string, Exception?> SendingRequest =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            new EventId(1017, "SendingRequest"),
            "Sending {RequestType} request to {BaseUrl}{Path}");

    public static readonly Action<ILogger, int, string, Exception?> ReceivedSuccessfulResponse =
        LoggerMessage.Define<int, string>(
            LogLevel.Debug,
            new EventId(1018, "ReceivedSuccessfulResponse"),
            "Received successful response with status {StatusCode} for {RequestType} request");

    public static readonly Action<ILogger, string, Exception?> ReceivedNullResponse =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1019, "ReceivedNullResponse"),
            "Received null response from Confidence API for {RequestType} request");

    public static readonly Action<ILogger, string, string, string, Exception?> RequestFailedHttp =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(1020, "RequestFailedHttp"),
            "Failed to communicate with Confidence API for {RequestType} request to {BaseUrl}{Path}");

    public static readonly Action<ILogger, string, Exception?> RequestFailedDeserialization =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1021, "RequestFailedDeserialization"),
            "Failed to deserialize response from Confidence API for {RequestType} request");

    public static readonly Action<ILogger, string, string, string, Exception?> RequestCancelled =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            new EventId(1022, "RequestCancelled"),
            "{RequestType} request to {BaseUrl}{Path} was cancelled");

    public static readonly Action<ILogger, string, string, string, Exception?> RequestFailedUnexpected =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(1023, "RequestFailedUnexpected"),
            "Unexpected error occurred during {RequestType} request to {BaseUrl}{Path}");
}