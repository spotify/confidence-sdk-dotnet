using System;
using Microsoft.Extensions.Logging;

namespace Spotify.Confidence.OpenFeature.Local.Logging;

/// <summary>
/// High-performance logging for WasmResolver using LoggerMessage.Define.
/// </summary>
internal static partial class WasmResolverLogger
{
    [LoggerMessage(5000, LogLevel.Warning, "WASM module needs imports, creating basic imports: {Error}")]
    public static partial void ImportsNeeded(ILogger logger, string error);

    [LoggerMessage(5001, LogLevel.Information, "WASM module instantiated successfully with {ImportCount} imports")]
    public static partial void ModuleInstantiated(ILogger logger, int importCount);

    [LoggerMessage(5002, LogLevel.Information, "✅ Found WASM export: {ExportName}")]
    public static partial void ExportFound(ILogger logger, string exportName);

    [LoggerMessage(5003, LogLevel.Warning, "❌ WASM export '{ExportName}' not found: {Error}")]
    public static partial void ExportNotFound(ILogger logger, string exportName, string error, Exception? exception);

    [LoggerMessage(5004, LogLevel.Information, "WASM resolver initialized with result: {Result}")]
    public static partial void ResolverInitialized(ILogger logger, object? result);

    [LoggerMessage(5005, LogLevel.Warning, "❌ WASM export 'wasm_msg_guest_set_resolver_state' not found or failed: {Error}")]
    public static partial void SetResolverStateFailed(ILogger logger, string error, Exception? exception);

    [LoggerMessage(5006, LogLevel.Information, "WASM resolver initialized successfully from {Source}")]
    public static partial void WasmResolverInitialized(ILogger logger, string source);

    [LoggerMessage(5007, LogLevel.Error, "Failed to initialize WASM resolver from {Source}")]
    public static partial void WasmResolverInitializationFailed(ILogger logger, string source, Exception exception);

    [LoggerMessage(5008, LogLevel.Trace, "WASM called {FunctionName} with ptr={Ptr}")]
    public static partial void WasmImportCalled(ILogger logger, string functionName, int ptr);

    [LoggerMessage(5009, LogLevel.Trace, "WASM called {FunctionName} with ptr={Ptr}, returning timestamp={Timestamp}")]
    public static partial void WasmImportCalledWithTimestamp(ILogger logger, string functionName, int ptr, long timestamp);

    [LoggerMessage(5010, LogLevel.Trace, "WASM called {FunctionName}, returning threadId={ThreadId}")]
    public static partial void WasmImportCalledWithThreadId(ILogger logger, string functionName, int threadId);

    [LoggerMessage(5011, LogLevel.Error, "Error resolving flag {FlagKey} using WASM module")]
    public static partial void FlagResolutionError(ILogger logger, string flagKey, Exception exception);

    [LoggerMessage(5012, LogLevel.Warning, "WASM resolver functions not initialized")]
    public static partial void ResolverFunctionsNotInitialized(ILogger logger);

    [LoggerMessage(5013, LogLevel.Warning, "Error disposing WASM resolver resources")]
    public static partial void DisposalError(ILogger logger, Exception exception);

    [LoggerMessage(5014, LogLevel.Information, "Successfully set resolver state ({Length} bytes)")]
    public static partial void SetResolverStateSuccess(ILogger logger, int length);
}
