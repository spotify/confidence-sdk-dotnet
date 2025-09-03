using Microsoft.Extensions.Logging;

namespace Spotify.Confidence.OpenFeature.Local.Logging;

/// <summary>
/// High-performance logging delegates for ConfidenceStateService using LoggerMessage.Define for optimal performance.
/// </summary>
internal static partial class ConfidenceStateServiceLogger
{
    // gRPC state fetching events
    public static readonly Action<ILogger, Exception?> FetchingResolverStateViaGrpc =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(4000, "FetchingResolverStateViaGrpc"),
            "Fetching resolver state from Confidence backend via gRPC");

    public static readonly Action<ILogger, object, Exception?> GrpcResponseReceived =
        LoggerMessage.Define<object>(
            LogLevel.Information,
            new EventId(4001, "GrpcResponseReceived"),
            "gRPC response received from Confidence backend: {Response}");

    public static readonly Action<ILogger, Exception?> SuccessfullyFetchedResolverStateViaGrpc =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(4001, "SuccessfullyFetchedResolverStateViaGrpc"),
            "Successfully fetched resolver state via gRPC");

    public static readonly Action<ILogger, object, Exception?> GrpcErrorOccurred =
        LoggerMessage.Define<object>(
            LogLevel.Error,
            new EventId(4002, "GrpcErrorOccurred"),
            "gRPC error occurred while fetching resolver state. Status: {StatusCode}");

    public static readonly Action<ILogger, Exception?> RequestCanceled =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(4003, "RequestCanceled"),
            "Request was canceled while fetching resolver state");

    public static readonly Action<ILogger, Exception?> UnexpectedErrorOccurred =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(4004, "UnexpectedErrorOccurred"),
            "Unexpected error occurred while fetching resolver state");

    // State validation events
    public static readonly Action<ILogger, Exception?> ResolverStateNotValidJsonObject =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(4005, "ResolverStateNotValidJsonObject"),
            "Resolver state is not a valid JSON object");

    public static readonly Action<ILogger, Exception?> ResolverStateValidationPassed =
        LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(4006, "ResolverStateValidationPassed"),
            "Resolver state validation passed");

    public static readonly Action<ILogger, Exception?> ResolverStateContainsInvalidProtobuf =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(4007, "ResolverStateContainsInvalidProtobuf"),
            "Resolver state contains invalid protobuf");

    public static readonly Action<ILogger, Exception?> NoResolverStateReceived =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(4008, "NoResolverStateReceived"),
            "No resolver state received from streaming gRPC call");

    // Disposal events
    public static readonly Action<ILogger, Exception?> ErrorDisposingGrpcChannel =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(4009, "ErrorDisposingGrpcChannel"),
            "Error disposing gRPC channel");
}
