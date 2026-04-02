using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Spotify.Confidence.Sdk.Telemetry;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:ParameterNamesMustBeginWithLowerCaseLetter", Justification = "Record positional parameters are PascalCase by convention")]
internal record EvaluationTraceData(EvaluationReason Reason, EvaluationErrorCode ErrorCode);

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:ParameterNamesMustBeginWithLowerCaseLetter", Justification = "Record positional parameters are PascalCase by convention")]
internal record ResolveLatencyTraceData(ulong MillisecondDuration, RequestStatus Status);

internal sealed class Telemetry
{
    private const int MaxTraces = 100;
    private static readonly string SdkVersion = GetSdkVersion();

    private readonly object _lock = new();
    private readonly Platform _platform;
    private List<EvaluationTraceData> _evalTraces = new();
    private List<ResolveLatencyTraceData> _resolveTraces = new();
    private volatile Library _currentLibrary = Library.Confidence;

    internal Library CurrentLibrary
    {
        get => _currentLibrary;
        set => _currentLibrary = value;
    }

    internal Telemetry(Platform platform)
    {
        _platform = platform;
    }

    internal void TrackEvaluation(EvaluationReason reason, EvaluationErrorCode errorCode)
    {
        lock (_lock)
        {
            if (_evalTraces.Count < MaxTraces)
            {
                _evalTraces.Add(new EvaluationTraceData(reason, errorCode));
            }
        }
    }

    internal void TrackResolveLatency(ulong durationMs, RequestStatus status)
    {
        lock (_lock)
        {
            if (_resolveTraces.Count < MaxTraces)
            {
                _resolveTraces.Add(new ResolveLatencyTraceData(durationMs, status));
            }
        }
    }

    internal string? EncodedHeaderValue()
    {
        List<EvaluationTraceData> evalSnapshot;
        List<ResolveLatencyTraceData> resolveSnapshot;

        lock (_lock)
        {
            if (_evalTraces.Count == 0 && _resolveTraces.Count == 0)
            {
                return null;
            }

            evalSnapshot = _evalTraces;
            resolveSnapshot = _resolveTraces;
            _evalTraces = new List<EvaluationTraceData>();
            _resolveTraces = new List<ResolveLatencyTraceData>();
        }

        var bytes = ProtobufEncoder.EncodeMonitoring(
            _currentLibrary,
            SdkVersion,
            _platform,
            resolveSnapshot,
            evalSnapshot);

        return Convert.ToBase64String(bytes);
    }

    internal static (EvaluationReason Reason, EvaluationErrorCode ErrorCode) MapEvaluationReason(
        string? apiReason,
        string? errorMessage)
    {
        if (errorMessage != null)
        {
            // Only match "not found" which is a well-known pattern from our own code.
            // Everything else maps to General — no brittle string parsing.
            var errorCode = errorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? EvaluationErrorCode.FlagNotFound
                : EvaluationErrorCode.General;
            return (EvaluationReason.Error, errorCode);
        }

        return apiReason switch
        {
            "RESOLVE_REASON_MATCH" => (EvaluationReason.TargetingMatch, EvaluationErrorCode.Unspecified),
            "RESOLVE_REASON_NO_SEGMENT_MATCH" => (EvaluationReason.Default, EvaluationErrorCode.Unspecified),
            "RESOLVE_REASON_NO_TREATMENT_MATCH" => (EvaluationReason.Default, EvaluationErrorCode.Unspecified),
            "RESOLVE_REASON_STALE" => (EvaluationReason.Stale, EvaluationErrorCode.Unspecified),
            "RESOLVE_REASON_FLAG_ARCHIVED" => (EvaluationReason.Disabled, EvaluationErrorCode.Unspecified),
            "RESOLVE_REASON_TARGETING_KEY_ERROR" => (EvaluationReason.Error, EvaluationErrorCode.TargetingKeyMissing),
            "ERROR" => (EvaluationReason.Error, EvaluationErrorCode.General),
            _ => (EvaluationReason.Unspecified, EvaluationErrorCode.Unspecified),
        };
    }

    private static string GetSdkVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "unknown";
    }
}
