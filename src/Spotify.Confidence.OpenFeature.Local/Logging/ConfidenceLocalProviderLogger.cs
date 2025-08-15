using Microsoft.Extensions.Logging;

namespace Spotify.Confidence.OpenFeature.Local.Logging;

/// <summary>
/// High-performance logging delegates for ConfidenceLocalProvider using LoggerMessage.Define for optimal performance.
/// </summary>
internal static class ConfidenceLocalProviderLogger
{
    // Provider lifecycle events
    public static readonly Action<ILogger, Exception?> ProviderInitialized =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(3000, "LocalProviderInitialized"),
            "ConfidenceLocalProvider initialized");

    public static readonly Action<ILogger, object?, Exception?> InitializingProvider =
        LoggerMessage.Define<object?>(
            LogLevel.Information,
            new EventId(3016, "InitializingLocalProvider"),
            "Initializing ConfidenceLocalProvider with context: {Context}");

    public static readonly Action<ILogger, Exception?> ShuttingDownProvider =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(3017, "ShuttingDownLocalProvider"),
            "Shutting down ConfidenceLocalProvider");

    public static readonly Action<ILogger, string, Exception?> WasmInitializationFailed =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3018, "WasmInitializationFailed"),
            "Failed to initialize WASM resolver from embedded resource {WasmResourceName}");

    public static readonly Action<ILogger, Exception?> WasmDisposalError =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(3019, "WasmDisposalError"),
            "Error disposing WASM resolver");

    // Boolean flag resolution events
    public static readonly Action<ILogger, string, object?, Exception?> ResolvingBooleanFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(3001, "ResolvingLocalBooleanFlag"),
            "Resolving local boolean flag '{FlagKey}' with default value: {DefaultValue}");

    public static readonly Action<ILogger, string, object?, Exception?> ResolvedBooleanFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(3002, "ResolvedLocalBooleanFlag"),
            "Successfully resolved local boolean flag '{FlagKey}' with value: {Value}");

    public static readonly Action<ILogger, string, object?, Exception?> ErrorResolvingBooleanFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Warning,
            new EventId(3003, "ErrorResolvingLocalBooleanFlag"),
            "Error resolving local boolean flag '{FlagKey}', returning default value: {DefaultValue}");

    // String flag resolution events
    public static readonly Action<ILogger, string, object?, Exception?> ResolvingStringFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(3004, "ResolvingLocalStringFlag"),
            "Resolving local string flag '{FlagKey}' with default value: {DefaultValue}");

    public static readonly Action<ILogger, string, object?, Exception?> ResolvedStringFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(3005, "ResolvedLocalStringFlag"),
            "Successfully resolved local string flag '{FlagKey}' with value: {Value}");

    public static readonly Action<ILogger, string, object?, Exception?> ErrorResolvingStringFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Warning,
            new EventId(3006, "ErrorResolvingLocalStringFlag"),
            "Error resolving local string flag '{FlagKey}', returning default value: {DefaultValue}");

    // Integer flag resolution events
    public static readonly Action<ILogger, string, object?, Exception?> ResolvingIntegerFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(3007, "ResolvingLocalIntegerFlag"),
            "Resolving local integer flag '{FlagKey}' with default value: {DefaultValue}");

    public static readonly Action<ILogger, string, object?, Exception?> ResolvedIntegerFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(3008, "ResolvedLocalIntegerFlag"),
            "Successfully resolved local integer flag '{FlagKey}' with value: {Value}");

    public static readonly Action<ILogger, string, object?, Exception?> ErrorResolvingIntegerFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Warning,
            new EventId(3009, "ErrorResolvingLocalIntegerFlag"),
            "Error resolving local integer flag '{FlagKey}', returning default value: {DefaultValue}");

    // Double flag resolution events
    public static readonly Action<ILogger, string, object?, Exception?> ResolvingDoubleFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(3010, "ResolvingLocalDoubleFlag"),
            "Resolving local double flag '{FlagKey}' with default value: {DefaultValue}");

    public static readonly Action<ILogger, string, object?, Exception?> ResolvedDoubleFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(3011, "ResolvedLocalDoubleFlag"),
            "Successfully resolved local double flag '{FlagKey}' with value: {Value}");

    public static readonly Action<ILogger, string, object?, Exception?> ErrorResolvingDoubleFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Warning,
            new EventId(3012, "ErrorResolvingLocalDoubleFlag"),
            "Error resolving local double flag '{FlagKey}', returning default value: {DefaultValue}");

    // Structure flag resolution events
    public static readonly Action<ILogger, string, object?, Exception?> ResolvingStructureFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(3013, "ResolvingLocalStructureFlag"),
            "Resolving local structure flag '{FlagKey}' with default value: {DefaultValue}");

    public static readonly Action<ILogger, string, Exception?> ResolvedStructureFlag =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(3014, "ResolvedLocalStructureFlag"),
            "Successfully resolved local structure flag '{FlagKey}'");

    public static readonly Action<ILogger, string, Exception?> ErrorResolvingStructureFlag =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(3015, "ErrorResolvingLocalStructureFlag"),
            "Error resolving local structure flag '{FlagKey}', returning default value");
}
