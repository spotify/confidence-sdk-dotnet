using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature;
using OpenFeature.Constant;
using OpenFeature.Model;
using Spotify.Confidence.OpenFeature.Local.Logging;
using Spotify.Confidence.OpenFeature.Local.Models;

namespace Spotify.Confidence.OpenFeature.Local;

/// <summary>
/// A local OpenFeature provider for the Confidence SDK that resolves flags locally using WASM without network calls.
/// </summary>
public class ConfidenceLocalProvider : FeatureProvider, IDisposable
{
    private readonly ILogger<ConfidenceLocalProvider> _logger;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly WasmResolver? _wasmResolver;
    private const string ProviderName = "ConfidenceLocal";
    private const string DefaultWasmResourceName = "Resources.rust_guest.wasm";
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfidenceLocalProvider"/> class using the embedded WASM resolver.
    /// </summary>
    /// <param name="clientId">The client ID for authentication.</param>
    /// <param name="clientSecret">The client secret for authentication.</param>
    /// <param name="logger">Optional logger instance. If not provided, a null logger will be used.</param>
    public ConfidenceLocalProvider(string clientId, string clientSecret, ILogger<ConfidenceLocalProvider>? logger = null)
    {
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
        _logger = logger ?? NullLogger<ConfidenceLocalProvider>.Instance;
        
        try
        {
            _wasmResolver = new WasmResolver(DefaultWasmResourceName, assembly: null);
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

    public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
        string flagKey,
        bool defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceLocalProviderLogger.ResolvingBooleanFlag(_logger, flagKey, defaultValue, null);
        
        var result = await ResolveValueAsync(flagKey, context, cancellationToken);
        
        if (result.Success && result.Value is bool boolValue)
        {
            ConfidenceLocalProviderLogger.ResolvedBooleanFlag(_logger, flagKey, boolValue, null);
            return new ResolutionDetails<bool>(
                flagKey,
                boolValue,
                ErrorType.None,
                variant: result.Variant,
                reason: result.Reason ?? "RESOLVED");
        }
        
        ConfidenceLocalProviderLogger.ErrorResolvingBooleanFlag(_logger, flagKey, defaultValue, null);
        return new ResolutionDetails<bool>(
            flagKey,
            defaultValue,
            ErrorType.General,
            reason: result.Error ?? "DEFAULT");
    }

    public override async Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey,
        string defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceLocalProviderLogger.ResolvingStringFlag(_logger, flagKey, defaultValue, null);
        
        var result = await ResolveValueAsync(flagKey, context, cancellationToken);
        
        if (result.Success && result.Value is string stringValue)
        {
            ConfidenceLocalProviderLogger.ResolvedStringFlag(_logger, flagKey, stringValue, null);
            return new ResolutionDetails<string>(
                flagKey,
                stringValue,
                ErrorType.None,
                variant: result.Variant,
                reason: result.Reason ?? "RESOLVED");
        }
        
        ConfidenceLocalProviderLogger.ErrorResolvingStringFlag(_logger, flagKey, defaultValue, null);
        return new ResolutionDetails<string>(
            flagKey,
            defaultValue,
            ErrorType.General,
            reason: result.Error ?? "DEFAULT");
    }

    public override async Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey,
        int defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceLocalProviderLogger.ResolvingIntegerFlag(_logger, flagKey, defaultValue, null);
        
        var result = await ResolveValueAsync(flagKey, context, cancellationToken);
        
        if (result.Success && result.Value is int intValue)
        {
            ConfidenceLocalProviderLogger.ResolvedIntegerFlag(_logger, flagKey, intValue, null);
            return new ResolutionDetails<int>(
                flagKey,
                intValue,
                ErrorType.None,
                variant: result.Variant,
                reason: result.Reason ?? "RESOLVED");
        }
        
        ConfidenceLocalProviderLogger.ErrorResolvingIntegerFlag(_logger, flagKey, defaultValue, null);
        return new ResolutionDetails<int>(
            flagKey,
            defaultValue,
            ErrorType.General,
            reason: result.Error ?? "DEFAULT");
    }

    public override async Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey,
        double defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceLocalProviderLogger.ResolvingDoubleFlag(_logger, flagKey, defaultValue, null);
        
        var result = await ResolveValueAsync(flagKey, context, cancellationToken);
        
        if (result.Success && result.Value is double doubleValue)
        {
            ConfidenceLocalProviderLogger.ResolvedDoubleFlag(_logger, flagKey, doubleValue, null);
            return new ResolutionDetails<double>(
                flagKey,
                doubleValue,
                ErrorType.None,
                variant: result.Variant,
                reason: result.Reason ?? "RESOLVED");
        }
        
        ConfidenceLocalProviderLogger.ErrorResolvingDoubleFlag(_logger, flagKey, defaultValue, null);
        return new ResolutionDetails<double>(
            flagKey,
            defaultValue,
            ErrorType.General,
            reason: result.Error ?? "DEFAULT");
    }

    public override async Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
        string flagKey,
        Value defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceLocalProviderLogger.ResolvingStructureFlag(_logger, flagKey, defaultValue, null);
        
        var result = await ResolveValueAsync(flagKey, context, cancellationToken);
        
        if (result.Success && result.Value != null)
        {
            var value = ConvertToOpenFeatureValue(result.Value);
            ConfidenceLocalProviderLogger.ResolvedStructureFlag(_logger, flagKey, null);
            return new ResolutionDetails<Value>(
                flagKey,
                value,
                ErrorType.None,
                variant: result.Variant,
                reason: result.Reason ?? "RESOLVED");
        }
        
        ConfidenceLocalProviderLogger.ErrorResolvingStructureFlag(_logger, flagKey, null);
        return new ResolutionDetails<Value>(
            flagKey,
            defaultValue,
            ErrorType.General,
            reason: result.Error ?? "DEFAULT");
    }

    public override IImmutableList<Hook> GetProviderHooks()
    {
        return ImmutableList<Hook>.Empty;
    }

    public override Task InitializeAsync(EvaluationContext context, CancellationToken cancellationToken = default)
    {
        ConfidenceLocalProviderLogger.InitializingProvider(_logger, context?.AsDictionary(), null);
        return Task.CompletedTask;
    }

    public override Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        ConfidenceLocalProviderLogger.ShuttingDownProvider(_logger, null);
        Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Core method to resolve a flag value using the WASM resolver.
    /// </summary>
    private async Task<ResolveResponse> ResolveValueAsync(string flagKey, EvaluationContext? context, CancellationToken cancellationToken)
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

        var request = new ResolveRequest
        {
            Flag = flagKey,
            Context = ConvertEvaluationContext(context),
            ClientId = _clientId,
            ClientSecret = _clientSecret
        };

        return await _wasmResolver.ResolveAsync(request, cancellationToken);
    }

    /// <summary>
    /// Converts OpenFeature EvaluationContext to a dictionary for WASM communication.
    /// </summary>
    private static Dictionary<string, object> ConvertEvaluationContext(EvaluationContext? context)
    {
        if (context == null)
        {
            return new Dictionary<string, object>();
        }

        var result = new Dictionary<string, object>();

        // Add targeting key if present
        if (!string.IsNullOrEmpty(context.TargetingKey))
        {
            result["targeting_key"] = context.TargetingKey;
        }

        // Add all other attributes
        foreach (var kvp in context.AsDictionary())
        {
            if (kvp.Key != "targetingKey") // Avoid duplication
            {
                result[kvp.Key] = ConvertValue(kvp.Value);
            }
        }

        return result;
    }

    /// <summary>
    /// Converts OpenFeature Value to a simple object for JSON serialization.
    /// </summary>
    private static object ConvertValue(Value value)
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
    /// Converts a WASM response value back to an OpenFeature Value.
    /// </summary>
    private static Value ConvertToOpenFeatureValue(object value)
    {
        return value switch
        {
            bool b => new Value(b),
            int i => new Value(i),
            long l => new Value((int)l), // Convert long to int for OpenFeature
            double d => new Value(d),
            float f => new Value((double)f), // Convert float to double for OpenFeature
            string s => new Value(s),
            DateTime dt => new Value(dt),
            Dictionary<string, object> dict => ConvertDictionaryToStructure(dict),
            IEnumerable<object> list => new Value(list.Select(ConvertToOpenFeatureValue).ToList()),
            _ => new Value()
        };
    }

    /// <summary>
    /// Converts a dictionary to an OpenFeature Structure Value.
    /// </summary>
    private static Value ConvertDictionaryToStructure(Dictionary<string, object> dict)
    {
        var builder = Structure.Builder();
        foreach (var kvp in dict)
        {
            builder.Set(kvp.Key, ConvertToOpenFeatureValue(kvp.Value));
        }
        return new Value(builder.Build());
    }

    /// <summary>
    /// Disposes the provider and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _wasmResolver?.Dispose();
        }
        catch (Exception ex)
        {
            ConfidenceLocalProviderLogger.WasmDisposalError(_logger, ex);
        }

        _disposed = true;
    }
}
