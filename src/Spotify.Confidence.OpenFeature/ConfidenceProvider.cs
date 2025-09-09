using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using Spotify.Confidence.OpenFeature.Logging;
using Spotify.Confidence.Sdk;
using Spotify.Confidence.Sdk.Models;
using Spotify.Confidence.Sdk.Options;

namespace Spotify.Confidence.OpenFeature;

/// <summary>
/// An OpenFeature provider for the Confidence SDK.
/// </summary>
public class ConfidenceProvider : FeatureProvider
{
    private readonly IConfidenceClient _confidenceClient;
    private readonly ILogger<ConfidenceProvider> _logger;
    private const string ProviderName = "Confidence";

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfidenceProvider"/> class.
    /// </summary>
    /// <param name="options">The options for the Confidence client.</param>
    /// <param name="logger">Optional logger instance. If not provided, a console logger with the configured LogLevel will be created.</param>
    public ConfidenceProvider(ConfidenceOptions options, ILogger<ConfidenceProvider>? logger = null)
    {
        _logger = logger ?? CreateDefaultLogger(options.LogLevel);
        _confidenceClient = new ConfidenceClient(options, CreateConfidenceClientLogger(options.LogLevel));
        ConfidenceProviderLogger.ProviderInitialized(_logger, null);
    }

    internal ConfidenceProvider(IConfidenceClient confidenceClient, ILogger<ConfidenceProvider>? logger = null)
    {
        _confidenceClient = confidenceClient;
        _logger = logger ?? NullLogger<ConfidenceProvider>.Instance;
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
        ConfidenceProviderLogger.ResolvingBooleanFlag(_logger, flagKey, defaultValue, null);

        try
        {
            var result = await _confidenceClient.EvaluateBooleanFlagAsync(flagKey, defaultValue, CreateConfidenceContext(context), cancellationToken);
            ConfidenceProviderLogger.ResolvedBooleanFlag(_logger, flagKey, result.Value, null);
            return new ResolutionDetails<bool>(
                flagKey,
                result.Value,
                ErrorType.None,
                variant: result.Variant,
                reason: result.Reason ?? "DEFAULT");
        }
        catch (Exception ex)
        {
            ConfidenceProviderLogger.ErrorResolvingBooleanFlag(_logger, flagKey, defaultValue, ex);
            return new ResolutionDetails<bool>(flagKey, defaultValue, ErrorType.General, reason: "Error resolving flag");
        }
    }

    public override async Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey,
        string defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceProviderLogger.ResolvingStringFlag(_logger, flagKey, defaultValue, null);

        try
        {
            var result = await _confidenceClient.EvaluateStringFlagAsync(flagKey, defaultValue, CreateConfidenceContext(context), cancellationToken);
            ConfidenceProviderLogger.ResolvedStringFlag(_logger, flagKey, result.Value, null);
            return new ResolutionDetails<string>(
                flagKey,
                result.Value,
                ErrorType.None,
                variant: result.Variant,
                reason: result.Reason ?? "DEFAULT");
        }
        catch (Exception ex)
        {
            ConfidenceProviderLogger.ErrorResolvingStringFlag(_logger, flagKey, defaultValue, ex);
            return new ResolutionDetails<string>(flagKey, defaultValue, ErrorType.General, reason: "Error resolving flag");
        }
    }

    public override async Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey,
        int defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceProviderLogger.ResolvingIntegerFlag(_logger, flagKey, defaultValue, null);

        try
        {
            var result = await _confidenceClient.EvaluateNumericFlagAsync(flagKey, defaultValue, CreateConfidenceContext(context), cancellationToken);
            var intValue = (int)result.Value;
            ConfidenceProviderLogger.ResolvedIntegerFlag(_logger, flagKey, intValue, null);
            return new ResolutionDetails<int>(
                flagKey,
                intValue,
                ErrorType.None,
                variant: result.Variant,
                reason: result.Reason ?? "DEFAULT");
        }
        catch (Exception ex)
        {
            ConfidenceProviderLogger.ErrorResolvingIntegerFlag(_logger, flagKey, defaultValue, ex);
            return new ResolutionDetails<int>(flagKey, defaultValue, ErrorType.General, reason: "Error resolving flag");
        }
    }

    public override async Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey,
        double defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceProviderLogger.ResolvingDoubleFlag(_logger, flagKey, defaultValue, null);

        try
        {
            var result = await _confidenceClient.EvaluateNumericFlagAsync(flagKey, defaultValue, CreateConfidenceContext(context), cancellationToken);
            ConfidenceProviderLogger.ResolvedDoubleFlag(_logger, flagKey, result.Value, null);
            return new ResolutionDetails<double>(
                flagKey,
                result.Value,
                ErrorType.None,
                variant: result.Variant,
                reason: result.Reason ?? "DEFAULT");
        }
        catch (Exception ex)
        {
            ConfidenceProviderLogger.ErrorResolvingDoubleFlag(_logger, flagKey, defaultValue, ex);
            return new ResolutionDetails<double>(flagKey, defaultValue, ErrorType.General, reason: "Error resolving flag");
        }
    }

    public override async Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
        string flagKey,
        Value defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ConfidenceProviderLogger.ResolvingStructureFlag(_logger, flagKey, defaultValue, null);

        try
        {
            var result = await _confidenceClient.EvaluateJsonFlagAsync(flagKey, defaultValue.AsObject ?? new Dictionary<string, object>(), CreateConfidenceContext(context), cancellationToken);
            var structure = ConvertToStructure(result.Value);
            var value = new Value(structure);
            ConfidenceProviderLogger.ResolvedStructureFlag(_logger, flagKey, null);
            return new ResolutionDetails<Value>(
                flagKey,
                value,
                ErrorType.None,
                variant: result.Variant,
                reason: result.Reason ?? "DEFAULT");
        }
        catch (Exception ex)
        {
            ConfidenceProviderLogger.ErrorResolvingStructureFlag(_logger, flagKey, ex);
            return new ResolutionDetails<Value>(flagKey, defaultValue, ErrorType.General, reason: "Error resolving flag");
        }
    }

    public override IImmutableList<Hook> GetProviderHooks()
    {
        return ImmutableList<Hook>.Empty;
    }

    public override Task InitializeAsync(EvaluationContext context, CancellationToken cancellationToken = default)
    {
        ConfidenceProviderLogger.InitializingProvider(_logger, context?.AsDictionary(), null);
        return Task.CompletedTask;
    }

    public override Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        ConfidenceProviderLogger.ShuttingDownProvider(_logger, null);
        return Task.CompletedTask;
    }

    private static ConfidenceContext? CreateConfidenceContext(EvaluationContext? context)
    {
        if (context == null)
        {
            return null;
        }

        var attributes = new Dictionary<string, object>();

        // Add targeting key as a regular attribute if it exists
        if (!string.IsNullOrEmpty(context.TargetingKey))
        {
            attributes["targeting_key"] = context.TargetingKey;
        }

        // Add all other attributes from the OpenFeature context
        foreach (var kvp in context.AsDictionary())
        {
            // Skip targetingKey as it's already handled above
            if (kvp.Key != "targetingKey")
            {
                attributes[kvp.Key] = ConvertFromValue(kvp.Value);
            }
        }

        return new ConfidenceContext(attributes);
    }

    private static object ConvertFromValue(Value value)
    {
        return value.AsObject ?? new object();
    }

    private static Structure ConvertToStructure(object? value)
    {
        if (value == null)
        {
            return Structure.Empty;
        }

        if (value is IDictionary<string, object> dict)
        {
            var builder = Structure.Builder();
            foreach (var kvp in dict)
            {
                builder.Set(kvp.Key, ConvertToValue(kvp.Value));
            }
            return builder.Build();
        }

        throw new ArgumentException($"Cannot convert type {value.GetType()} to Structure");
    }

    private static Value ConvertToValue(object? value)
    {
        if (value == null)
        {
            return new Value();
        }

        return value switch
        {
            bool b => new Value(b),
            int i => new Value(i),
            double d => new Value(d),
            string s => new Value(s),
            DateTime dt => new Value(dt),
            IDictionary<string, object> dict => new Value(ConvertToStructure(dict)),
            IList<object> list => new Value(list.Select(ConvertToValue).ToList()),
            JsonElement jsonElement => ConvertJsonElementToValue(jsonElement),
            _ => throw new ArgumentException($"Cannot convert type {value.GetType()} to Value")
        };
    }

    private static Value ConvertJsonElementToValue(JsonElement jsonElement)
    {
        return jsonElement.ValueKind switch
        {
            JsonValueKind.True => new Value(true),
            JsonValueKind.False => new Value(false),
            JsonValueKind.Number => jsonElement.TryGetInt32(out var intValue) ? new Value(intValue) : new Value(jsonElement.GetDouble()),
            JsonValueKind.String => new Value(jsonElement.GetString() ?? string.Empty),
            JsonValueKind.Object => new Value(ConvertJsonObjectToStructure(jsonElement)),
            JsonValueKind.Array => new Value(jsonElement.EnumerateArray().Select(ConvertJsonElementToValue).ToList()),
            JsonValueKind.Null => new Value(),
            JsonValueKind.Undefined => new Value(),
            _ => throw new ArgumentException($"Cannot convert JsonElement with ValueKind {jsonElement.ValueKind} to Value")
        };
    }

    private static Structure ConvertJsonObjectToStructure(JsonElement jsonObject)
    {
        var builder = Structure.Builder();
        foreach (var property in jsonObject.EnumerateObject())
        {
            builder.Set(property.Name, ConvertJsonElementToValue(property.Value));
        }
        return builder.Build();
    }

    private static ILogger<ConfidenceProvider> CreateDefaultLogger(LogLevel logLevel)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(logLevel)
                .AddConsole();
        });

        return loggerFactory.CreateLogger<ConfidenceProvider>();
    }

    private static ILogger<ConfidenceClient> CreateConfidenceClientLogger(LogLevel logLevel)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(logLevel)
                .AddConsole();
        });

        return loggerFactory.CreateLogger<ConfidenceClient>();
    }
}
