using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Confidence.Flags.Resolver.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature;
using OpenFeature.Constant;
using OpenFeature.Model;
using Spotify.Confidence.Common.Utils;
using Spotify.Confidence.OpenFeature.Local.Logging;
using Spotify.Confidence.OpenFeature.Local.Models;
using Spotify.Confidence.OpenFeature.Local.Services;
using ProtobufValue = Google.Protobuf.WellKnownTypes.Value;

namespace Spotify.Confidence.OpenFeature.Local;

/// <summary>
/// A local OpenFeature provider for the Confidence SDK that resolves flags locally using WASM without network calls.
/// </summary>
public class ConfidenceLocalProvider : FeatureProvider, IDisposable
{
    public static readonly string Version = GetVersion();
    private readonly ILogger<ConfidenceLocalProvider> _logger;
    private readonly string _resolverClientSecret;
    private readonly WasmResolver? _wasmResolver;
    private readonly IAssignmentLogger _assignmentLogger;
    private readonly IResolveLogger _resolveLogger;
    private readonly ConfidenceStateService _stateService;
    private const string ProviderName = "ConfidenceLocal";
    private const string DefaultWasmResourceName = "Resources.rust_guest.wasm";
    private bool _disposed;
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfidenceLocalProvider"/> class using the embedded WASM resolver.
    /// </summary>
    /// <param name="clientId">The client ID for authentication.</param>
    /// <param name="clientSecret">The client secret for authentication and state fetching.</param>
    /// <param name="resolverClientSecret">Optional client secret specifically for resolve operations. If not provided, uses the main client secret.</param>
    /// <param name="logger">Optional logger instance. If not provided, a null logger will be used.</param>
    public ConfidenceLocalProvider(string clientId, string clientSecret, string? resolverClientSecret = null, ILogger<ConfidenceLocalProvider>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(clientId);
        ArgumentNullException.ThrowIfNull(clientSecret);
        _resolverClientSecret = resolverClientSecret ?? clientSecret;
        _logger = logger ?? NullLogger<ConfidenceLocalProvider>.Instance;
        _stateService = new ConfidenceStateService(clientId, clientSecret);
        _assignmentLogger = new AssignmentLoggerService(clientId, clientSecret);
        _resolveLogger = new ResolveLoggerService(clientId, clientSecret);

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddConsole();
        });

        try
        {
            _wasmResolver = new WasmResolver(DefaultWasmResourceName, _assignmentLogger, _resolveLogger, assembly: null, logger: loggerFactory.CreateLogger<WasmResolver>());
        }
        catch (Exception ex)
        {
            ConfidenceLocalProviderLogger.WasmInitializationFailed(_logger, DefaultWasmResourceName, ex);
            _wasmResolver = null;
        }
        
        ConfidenceLocalProviderLogger.ProviderInitialized(_logger, null);
    }

    public override Metadata? GetMetadata()
    {
        return new Metadata(ProviderName);
    }

    /// <summary>
    /// Initializes the provider by fetching resolver state from the Confidence backend
    /// and setting it in the WASM resolver.
    /// </summary>
    /// <param name="context">Initialization context (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the initialization operation</returns>
    public override async Task InitializeAsync(EvaluationContext? context, CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }
        ObjectDisposedException.ThrowIf(_disposed, new ObjectDisposedException(nameof(ConfidenceLocalProvider)));

        ConfidenceLocalProviderLogger.InitializingProvider(_logger, context, null);

        var stateBytes = await _stateService.FetchResolverStateAsync(cancellationToken);
        
        if (stateBytes == null || stateBytes.Length == 0)
        {
            ConfidenceLocalProviderLogger.ErrorFetchingResolverState(_logger, null);
            throw new InvalidOperationException("Failed to fetch resolver state from backend");
        }

        if (!_stateService.ValidateState(stateBytes))
        {
            ConfidenceLocalProviderLogger.ErrorValidatingResolverState(_logger, null);
            throw new InvalidOperationException("Fetched resolver state is invalid");
        }

        if (_wasmResolver == null)
        {
            ConfidenceLocalProviderLogger.ErrorWasmResolverNotAvailable(_logger, null);
            throw new InvalidOperationException("WASM resolver not available - check if rust_guest.wasm resource is properly embedded");
        }

        var stateSetSuccessfully = _wasmResolver.SetResolverState(stateBytes);
        
        if (!stateSetSuccessfully)
        {
            ConfidenceLocalProviderLogger.ErrorSettingResolverState(_logger, null);
            throw new InvalidOperationException("Failed to set resolver state in WASM module");
        }

        ConfidenceLocalProviderLogger.InitializationCompleted(_logger, null);
        _initialized = true;
    }

    public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
        string flagKey,
        bool defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceLocalProviderLogger.ResolvingBooleanFlag(_logger, flagKey, defaultValue, null);
        
        try
        {
            // Parse dot-notation to get base flag name
            var (baseFlagName, propertyPath) = DotNotationHelper.ParseDotNotation(flagKey);
            
            // Resolve the base flag (without dot notation)
            var result = ResolveValue(baseFlagName, context);
            
            if (!result.Success)
            {
                ConfidenceLocalProviderLogger.ErrorResolvingBooleanFlag(_logger, flagKey, defaultValue, null);
                return Task.FromResult(new ResolutionDetails<bool>(
                    flagKey,
                    defaultValue,
                    ErrorType.General,
                    reason: result.Error ?? "DEFAULT"));
            }

            // Extract value using DotNotationHelper (similar to SDK pattern)
            if (result.Value is Dictionary<string, object> flagValue)
            {
                var extractedValue = DotNotationHelper.ExtractFlagValue(flagValue, propertyPath);
                
                // Convert to bool
                bool typedValue;
                if (extractedValue is bool directBool)
                {
                    typedValue = directBool;
                }
                else
                {
                    // If we have a property path but couldn't extract the value, return default
                    if (propertyPath.Length > 0)
                    {
                        ConfidenceLocalProviderLogger.ErrorResolvingBooleanFlag(_logger, flagKey, defaultValue, null);
                        return Task.FromResult(new ResolutionDetails<bool>(
                            flagKey,
                            defaultValue,
                            ErrorType.General,
                            reason: $"Property path '{string.Join(".", propertyPath)}' not found or invalid type"));
                    }

                    // Fallback for regular flag resolution
                    typedValue = defaultValue;
                }

                ConfidenceLocalProviderLogger.ResolvedBooleanFlag(_logger, flagKey, typedValue, null);
                return Task.FromResult(new ResolutionDetails<bool>(
                    flagKey,
                    typedValue,
                    ErrorType.None,
                    variant: result.Variant,
                    reason: result.Reason ?? "RESOLVED"));
            }
        }
        catch (Exception ex)
        {
            ConfidenceLocalProviderLogger.ErrorResolvingBooleanFlag(_logger, flagKey, defaultValue, ex);
        }
        
        return Task.FromResult(new ResolutionDetails<bool>(
            flagKey,
            defaultValue,
            ErrorType.General,
            reason: "DEFAULT"));
    }

    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey,
        string defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceLocalProviderLogger.ResolvingStringFlag(_logger, flagKey, defaultValue, null);
        
        try
        {
            // Parse dot-notation to get base flag name
            var (baseFlagName, propertyPath) = DotNotationHelper.ParseDotNotation(flagKey);
            
            // Resolve the base flag (without dot notation)
            var result = ResolveValue(baseFlagName, context);
            
            if (!result.Success)
            {
                ConfidenceLocalProviderLogger.ErrorResolvingStringFlag(_logger, flagKey, defaultValue, null);
                return Task.FromResult(new ResolutionDetails<string>(
                    flagKey,
                    defaultValue,
                    ErrorType.General,
                    reason: result.Error ?? "DEFAULT"));
            }

            // Extract value using DotNotationHelper (similar to SDK pattern)
            if (result.Value is Dictionary<string, object> flagValue)
            {
                var extractedValue = DotNotationHelper.ExtractFlagValue(flagValue, propertyPath);
                
                // Convert to string
                string typedValue;
                if (extractedValue is string directString)
                {
                    typedValue = directString;
                }
                else if (extractedValue != null)
                {
                    typedValue = extractedValue.ToString() ?? defaultValue;
                }
                else
                {
                    // If we have a property path but couldn't extract the value, return default
                    if (propertyPath.Length > 0)
                    {
                        ConfidenceLocalProviderLogger.ErrorResolvingStringFlag(_logger, flagKey, defaultValue, null);
                        return Task.FromResult(new ResolutionDetails<string>(
                            flagKey,
                            defaultValue,
                            ErrorType.General,
                            reason: $"Property path '{string.Join(".", propertyPath)}' not found"));
                    }

                    // Fallback for regular flag resolution
                    typedValue = defaultValue;
                }

                ConfidenceLocalProviderLogger.ResolvedStringFlag(_logger, flagKey, typedValue, null);
                return Task.FromResult(new ResolutionDetails<string>(
                    flagKey,
                    typedValue,
                    ErrorType.None,
                    variant: result.Variant,
                    reason: result.Reason ?? "RESOLVED"));
            }
        }
        catch (Exception ex)
        {
            ConfidenceLocalProviderLogger.ErrorResolvingStringFlag(_logger, flagKey, defaultValue, ex);
        }
        
        return Task.FromResult(new ResolutionDetails<string>(
            flagKey,
            defaultValue,
            ErrorType.General,
            reason: "DEFAULT"));
    }

    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey,
        int defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceLocalProviderLogger.ResolvingIntegerFlag(_logger, flagKey, defaultValue, null);
        
        try
        {
            // Parse dot-notation to get base flag name
            var (baseFlagName, propertyPath) = DotNotationHelper.ParseDotNotation(flagKey);
            
            // Resolve the base flag (without dot notation)
            var result = ResolveValue(baseFlagName, context);
            
            if (!result.Success)
            {
                ConfidenceLocalProviderLogger.ErrorResolvingIntegerFlag(_logger, flagKey, defaultValue, null);
                return Task.FromResult(new ResolutionDetails<int>(
                    flagKey,
                    defaultValue,
                    ErrorType.General,
                    reason: result.Error ?? "DEFAULT"));
            }

            // Extract value using DotNotationHelper (similar to SDK pattern)
            if (result.Value is Dictionary<string, object> flagValue)
            {
                var extractedValue = DotNotationHelper.ExtractFlagValue(flagValue, propertyPath);
                
                // Convert to int
                int typedValue;
                if (extractedValue is int directInt)
                {
                    typedValue = directInt;
                }
                else if (extractedValue is double doubleValue && Math.Abs(doubleValue % 1) < double.Epsilon)
                {
                    typedValue = (int)doubleValue;
                }
                else if (extractedValue is long longValue)
                {
                    typedValue = (int)longValue;
                }
                else
                {
                    // If we have a property path but couldn't extract the value, return default
                    if (propertyPath.Length > 0)
                    {
                        ConfidenceLocalProviderLogger.ErrorResolvingIntegerFlag(_logger, flagKey, defaultValue, null);
                        return Task.FromResult(new ResolutionDetails<int>(
                            flagKey,
                            defaultValue,
                            ErrorType.General,
                            reason: $"Property path '{string.Join(".", propertyPath)}' not found or invalid type"));
                    }

                    // Fallback for regular flag resolution
                    typedValue = defaultValue;
                }

                ConfidenceLocalProviderLogger.ResolvedIntegerFlag(_logger, flagKey, typedValue, null);
                return Task.FromResult(new ResolutionDetails<int>(
                    flagKey,
                    typedValue,
                    ErrorType.None,
                    variant: result.Variant,
                    reason: result.Reason ?? "RESOLVED"));
            }
        }
        catch (Exception ex)
        {
            ConfidenceLocalProviderLogger.ErrorResolvingIntegerFlag(_logger, flagKey, defaultValue, ex);
        }
        
        return Task.FromResult(new ResolutionDetails<int>(
            flagKey,
            defaultValue,
            ErrorType.General,
            reason: "DEFAULT"));
    }

    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey,
        double defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceLocalProviderLogger.ResolvingDoubleFlag(_logger, flagKey, defaultValue, null);
        
        try
        {
            // Parse dot-notation to get base flag name
            var (baseFlagName, propertyPath) = DotNotationHelper.ParseDotNotation(flagKey);
            
            // Resolve the base flag (without dot notation)
            var result = ResolveValue(baseFlagName, context);
            
            if (!result.Success)
            {
                ConfidenceLocalProviderLogger.ErrorResolvingDoubleFlag(_logger, flagKey, defaultValue, null);
                return Task.FromResult(new ResolutionDetails<double>(
                    flagKey,
                    defaultValue,
                    ErrorType.General,
                    reason: result.Error ?? "DEFAULT"));
            }

            // Extract value using DotNotationHelper (similar to SDK pattern)
            if (result.Value is Dictionary<string, object> flagValue)
            {
                var extractedValue = DotNotationHelper.ExtractFlagValue(flagValue, propertyPath);
                
                // Convert to double
                double typedValue;
                if (extractedValue is double directDouble)
                {
                    typedValue = directDouble;
                }
                else if (extractedValue is int intValue)
                {
                    typedValue = (double)intValue;
                }
                else if (extractedValue is float floatValue)
                {
                    typedValue = (double)floatValue;
                }
                else if (extractedValue is long longValue)
                {
                    typedValue = (double)longValue;
                }
                else
                {
                    // If we have a property path but couldn't extract the value, return default
                    if (propertyPath.Length > 0)
                    {
                        ConfidenceLocalProviderLogger.ErrorResolvingDoubleFlag(_logger, flagKey, defaultValue, null);
                        return Task.FromResult(new ResolutionDetails<double>(
                            flagKey,
                            defaultValue,
                            ErrorType.General,
                            reason: $"Property path '{string.Join(".", propertyPath)}' not found or invalid type"));
                    }

                    // Fallback for regular flag resolution
                    typedValue = defaultValue;
                }

                ConfidenceLocalProviderLogger.ResolvedDoubleFlag(_logger, flagKey, typedValue, null);
                return Task.FromResult(new ResolutionDetails<double>(
                    flagKey,
                    typedValue,
                    ErrorType.None,
                    variant: result.Variant,
                    reason: result.Reason ?? "RESOLVED"));
            }
        }
        catch (Exception ex)
        {
            ConfidenceLocalProviderLogger.ErrorResolvingDoubleFlag(_logger, flagKey, defaultValue, ex);
        }
        
        return Task.FromResult(new ResolutionDetails<double>(
            flagKey,
            defaultValue,
            ErrorType.General,
            reason: "DEFAULT"));
    }

    public override Task<ResolutionDetails<global::OpenFeature.Model.Value>> ResolveStructureValueAsync(
        string flagKey,
        global::OpenFeature.Model.Value defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceLocalProviderLogger.ResolvingStructureFlag(_logger, flagKey, defaultValue, null);
        
        try
        {
            // Parse dot-notation to get base flag name
            var (baseFlagName, propertyPath) = DotNotationHelper.ParseDotNotation(flagKey);
            
            // Resolve the base flag (without dot notation)
            var result = ResolveValue(baseFlagName, context);
            
            if (!result.Success)
            {
                ConfidenceLocalProviderLogger.ErrorResolvingStructureFlag(_logger, flagKey, null);
                return Task.FromResult(new ResolutionDetails<global::OpenFeature.Model.Value>(
                    flagKey,
                    defaultValue,
                    ErrorType.General,
                    reason: result.Error ?? "DEFAULT"));
            }

            // Extract value using DotNotationHelper (similar to SDK pattern)
            if (result.Value is Dictionary<string, object> flagValue)
            {
                var extractedValue = DotNotationHelper.ExtractFlagValue(flagValue, propertyPath);
                
                if (extractedValue != null)
                {
                    var value = ConvertToOpenFeatureValue(extractedValue);
                    ConfidenceLocalProviderLogger.ResolvedStructureFlag(_logger, flagKey, null);
                    return Task.FromResult(new ResolutionDetails<global::OpenFeature.Model.Value>(
                        flagKey,
                        value,
                        ErrorType.None,
                        variant: result.Variant,
                        reason: result.Reason ?? "RESOLVED"));
                }
                else if (propertyPath.Length > 0)
                {
                    // Property path specified but not found
                    ConfidenceLocalProviderLogger.ErrorResolvingStructureFlag(_logger, flagKey, null);
                    return Task.FromResult(new ResolutionDetails<global::OpenFeature.Model.Value>(
                        flagKey,
                        defaultValue,
                        ErrorType.General,
                        reason: $"Property path '{string.Join(".", propertyPath)}' not found"));
                }
            }
        }
        catch (Exception ex)
        {
            ConfidenceLocalProviderLogger.ErrorResolvingStructureFlag(_logger, flagKey, ex);
        }
        
        return Task.FromResult(new ResolutionDetails<global::OpenFeature.Model.Value>(
            flagKey,
            defaultValue,
            ErrorType.General,
            reason: "DEFAULT"));
    }

    public override IImmutableList<Hook> GetProviderHooks()
    {
        return ImmutableList<Hook>.Empty;
    }

    public override Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        ConfidenceLocalProviderLogger.ShuttingDownProvider(_logger, null);
        Dispose();
        return Task.CompletedTask;
    }

    private static string GetFullFlagKey(string flagKey)
    {
        if (flagKey.StartsWith("flags/", StringComparison.Ordinal))
        {
            return flagKey;
        }
        return $"flags/{flagKey}";
    }

    /// <summary>
    /// Core method to resolve a flag value using the WASM resolver.
    /// </summary>
    private ResolveResponse ResolveValue(string flagKey, EvaluationContext? context)
    {
        if (_disposed)
        {
            return new ResolveResponse
            {
                Success = false,
                Error = "Provider has been disposed",
                Reason = "ERROR"
            };
        }

        if (_wasmResolver == null)
        {
            return new ResolveResponse
            {
                Success = false,
                Error = "WASM resolver not initialized",
                Reason = "ERROR"
            };
        }

        var request = new ResolveFlagsRequest
        {
            ClientSecret = _resolverClientSecret,
            Apply = false, // TODO: this should be true
            Flags =
            {
                GetFullFlagKey(flagKey)
            },
            EvaluationContext = ConvertDictionaryToStruct(ConvertEvaluationContext(context))
        };

        var response = _wasmResolver.Resolve(request);
        
        if (response.ResolvedFlags.Count > 0)
        {
            var resolvedFlag = response.ResolvedFlags[0];
            return new ResolveResponse
            {
                Success = true,
                Value = ToDictionary(resolvedFlag.Value),
                Variant = resolvedFlag.Variant,
                Reason = resolvedFlag.Reason.ToString()
            };
        }
        
        return new ResolveResponse
        {
            Success = false,
            Error = "No flags resolved",
            Reason = "ERROR"
        };
    }

    private static Dictionary<string, object> ToDictionary(Struct value)
    {
        return value.Fields.ToDictionary(kvp => kvp.Key, kvp => ConfidenceLocalProvider.ConvertValue(kvp.Value));
    }

    private static object ConvertValue(ProtobufValue value)
    {
        return value.KindCase switch
        {
            ProtobufValue.KindOneofCase.BoolValue => value.BoolValue,
            ProtobufValue.KindOneofCase.NumberValue => value.NumberValue,
            ProtobufValue.KindOneofCase.StringValue => value.StringValue,
            _ => value.ToString()
        };
    }

    /// <summary>
    /// Converts OpenFeature Value to a simple object for JSON serialization.
    /// </summary>
    private static object ConvertValue(global::OpenFeature.Model.Value value)
    {
        if (value.IsBoolean)
        {
            return value.AsBoolean!.Value;
        }
        
        if (value.IsString)
        {
            return value.AsString!;
        }
        
        if (value.IsNumber)
        {
            var number = value.AsDouble!.Value;

            // Check if it's an integer
            if (Math.Abs(number % 1) < double.Epsilon)
            {
                return (int)number;
            }
            return number;
        }
        
        if (value.IsDateTime)
        {
            return value.AsDateTime!.Value;
        }
        
        if (value.IsList)
        {
            return value.AsList!.Select(ConvertValue).ToList();
        }
        
        if (value.IsStructure)
        {
            var dict = new Dictionary<string, object>();
            foreach (var kvp in value.AsStructure!)
            {
                dict[kvp.Key] = ConvertValue(kvp.Value);
            }
            return dict;
        }
        
        return value.AsObject ?? new object();
    }

    /// <summary>
    /// Converts a dictionary to a protobuf Struct.
    /// </summary>
    private static Struct ConvertDictionaryToStruct(Dictionary<string, object> dictionary)
    {
        var structValue = new Struct();
        foreach (var kvp in dictionary)
        {
            structValue.Fields[kvp.Key] = ConvertObjectToValue(kvp.Value);
        }
        return structValue;
    }

    /// <summary>
    /// Converts an object to a protobuf Value.
    /// </summary>
    private static ProtobufValue ConvertObjectToValue(object obj)
    {
        return obj switch
        {
            null => ProtobufValue.ForNull(),
            bool b => ProtobufValue.ForBool(b),
            int i => ProtobufValue.ForNumber(i),
            double d => ProtobufValue.ForNumber(d),
            string s => ProtobufValue.ForString(s),
            _ => ProtobufValue.ForString(obj.ToString() ?? string.Empty)
        };
    }

    /// <summary>
    /// Converts OpenFeature EvaluationContext to a dictionary for WASM communication.
    /// </summary>
    private static Dictionary<string, object> ConvertEvaluationContext(EvaluationContext? context)
    {
        if (context == null)
        {
            return [];
        }

        var result = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(context.TargetingKey))
        {
            result["targeting_key"] = context.TargetingKey;
        }

        foreach (var kvp in context.AsDictionary())
        {
            if (kvp.Key != "targetingKey")
            {
                result[kvp.Key] = ConvertValue(kvp.Value);
            }
        }

        return result;
    }

    /// <summary>
    /// Converts a WASM response value back to an OpenFeature Value.
    /// </summary>
    private static global::OpenFeature.Model.Value ConvertToOpenFeatureValue(object value)
    {
        return value switch
        {
            bool b => new global::OpenFeature.Model.Value(b),
            int i => new global::OpenFeature.Model.Value(i),
            long l => new global::OpenFeature.Model.Value((int)l), // Convert long to int for OpenFeature
            double d => new global::OpenFeature.Model.Value(d),
            float f => new global::OpenFeature.Model.Value((double)f), // Convert float to double for OpenFeature
            string s => new global::OpenFeature.Model.Value(s),
            DateTime dt => new global::OpenFeature.Model.Value(dt),
            Dictionary<string, object> dict => ConvertDictionaryToStructure(dict),
            IEnumerable<object> list => new global::OpenFeature.Model.Value(list.Select(ConvertToOpenFeatureValue).ToList()),
            _ => new global::OpenFeature.Model.Value()
        };
    }

    /// <summary>
    /// Converts a dictionary to an OpenFeature Structure Value.
    /// </summary>
    private static global::OpenFeature.Model.Value ConvertDictionaryToStructure(Dictionary<string, object> dict)
    {
        var builder = Structure.Builder();
        foreach (var kvp in dict)
        {
            builder.Set(kvp.Key, ConvertToOpenFeatureValue(kvp.Value));
        }
        return new global::OpenFeature.Model.Value(builder.Build());
    }

    private static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Disposes the provider and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of dispose pattern.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            try
            {
                _wasmResolver?.Dispose();
                _stateService?.Dispose();
            }
            catch (Exception ex)
            {
                ConfidenceLocalProviderLogger.WasmDisposalError(_logger, ex);
            }
        }

        _disposed = true;
    }
}
