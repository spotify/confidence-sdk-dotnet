using Microsoft.Extensions.Logging;

namespace Spotify.Confidence.OpenFeature.Logging;

/// <summary>
/// High-performance logging delegates for ConfidenceProvider using LoggerMessage.Define for optimal performance.
/// </summary>
internal static class ConfidenceProviderLogger
{
    // Provider lifecycle events
    public static readonly Action<ILogger, Exception?> ProviderInitialized =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(2000, "ProviderInitialized"),
            "ConfidenceProvider initialized");

    public static readonly Action<ILogger, object?, Exception?> InitializingProvider =
        LoggerMessage.Define<object?>(
            LogLevel.Information,
            new EventId(2016, "InitializingProvider"),
            "Initializing ConfidenceProvider with context: {Context}");

    public static readonly Action<ILogger, Exception?> ShuttingDownProvider =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(2017, "ShuttingDownProvider"),
            "Shutting down ConfidenceProvider");

    // Boolean flag resolution events
    public static readonly Action<ILogger, string, object?, Exception?> ResolvingBooleanFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(2001, "ResolvingBooleanFlag"),
            "Resolving boolean flag '{FlagKey}' with default value: {DefaultValue}");

    public static readonly Action<ILogger, string, object?, Exception?> ResolvedBooleanFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(2002, "ResolvedBooleanFlag"),
            "Successfully resolved boolean flag '{FlagKey}' with value: {Value}");

    public static readonly Action<ILogger, string, object?, Exception?> ErrorResolvingBooleanFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Warning,
            new EventId(2003, "ErrorResolvingBooleanFlag"),
            "Error resolving boolean flag '{FlagKey}', returning default value: {DefaultValue}");

    // String flag resolution events
    public static readonly Action<ILogger, string, object?, Exception?> ResolvingStringFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(2004, "ResolvingStringFlag"),
            "Resolving string flag '{FlagKey}' with default value: {DefaultValue}");

    public static readonly Action<ILogger, string, object?, Exception?> ResolvedStringFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(2005, "ResolvedStringFlag"),
            "Successfully resolved string flag '{FlagKey}' with value: {Value}");

    public static readonly Action<ILogger, string, object?, Exception?> ErrorResolvingStringFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Warning,
            new EventId(2006, "ErrorResolvingStringFlag"),
            "Error resolving string flag '{FlagKey}', returning default value: {DefaultValue}");

    // Integer flag resolution events
    public static readonly Action<ILogger, string, object?, Exception?> ResolvingIntegerFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(2007, "ResolvingIntegerFlag"),
            "Resolving integer flag '{FlagKey}' with default value: {DefaultValue}");

    public static readonly Action<ILogger, string, object?, Exception?> ResolvedIntegerFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(2008, "ResolvedIntegerFlag"),
            "Successfully resolved integer flag '{FlagKey}' with value: {Value}");

    public static readonly Action<ILogger, string, object?, Exception?> ErrorResolvingIntegerFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Warning,
            new EventId(2009, "ErrorResolvingIntegerFlag"),
            "Error resolving integer flag '{FlagKey}', returning default value: {DefaultValue}");

    // Double flag resolution events
    public static readonly Action<ILogger, string, object?, Exception?> ResolvingDoubleFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(2010, "ResolvingDoubleFlag"),
            "Resolving double flag '{FlagKey}' with default value: {DefaultValue}");

    public static readonly Action<ILogger, string, object?, Exception?> ResolvedDoubleFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(2011, "ResolvedDoubleFlag"),
            "Successfully resolved double flag '{FlagKey}' with value: {Value}");

    public static readonly Action<ILogger, string, object?, Exception?> ErrorResolvingDoubleFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Warning,
            new EventId(2012, "ErrorResolvingDoubleFlag"),
            "Error resolving double flag '{FlagKey}', returning default value: {DefaultValue}");

    // Structure flag resolution events
    public static readonly Action<ILogger, string, object?, Exception?> ResolvingStructureFlag =
        LoggerMessage.Define<string, object?>(
            LogLevel.Debug,
            new EventId(2013, "ResolvingStructureFlag"),
            "Resolving structure flag '{FlagKey}' with default value: {DefaultValue}");

    public static readonly Action<ILogger, string, Exception?> ResolvedStructureFlag =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(2014, "ResolvedStructureFlag"),
            "Successfully resolved structure flag '{FlagKey}'");

    public static readonly Action<ILogger, string, Exception?> ErrorResolvingStructureFlag =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2015, "ErrorResolvingStructureFlag"),
            "Error resolving structure flag '{FlagKey}', returning default value");
}
