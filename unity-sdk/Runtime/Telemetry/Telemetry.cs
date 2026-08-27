#nullable enable

using System.Collections.Generic;
using UnityOpenFeature.Core;

namespace UnityOpenFeature.Telemetry
{
    internal class EvaluationTraceData
    {
        public EvaluationReason Reason { get; }
        public EvaluationErrorCode ErrorCode { get; }

        public EvaluationTraceData(EvaluationReason reason, EvaluationErrorCode errorCode)
        {
            Reason = reason;
            ErrorCode = errorCode;
        }
    }

    internal class ResolveLatencyTraceData
    {
        public ulong MillisecondDuration { get; }
        public RequestStatus Status { get; }

        public ResolveLatencyTraceData(ulong millisecondDuration, RequestStatus status)
        {
            MillisecondDuration = millisecondDuration;
            Status = status;
        }
    }

    internal sealed class Telemetry
    {
        private const int MaxBufferedTraces = 100;

        private readonly object _lock = new object();
        private readonly Library _library;
        private readonly Platform _platform;
        private readonly string _sdkVersion;
        private List<EvaluationTraceData> _evalTraces = new List<EvaluationTraceData>();
        private List<ResolveLatencyTraceData> _resolveTraces = new List<ResolveLatencyTraceData>();
        private int _traceCount;

        internal Telemetry(Platform platform, string sdkVersion, Library library = Library.OpenFeature)
        {
            _library = library;
            _platform = platform;
            _sdkVersion = sdkVersion;
        }

        internal void TrackEvaluation(EvaluationReason reason, EvaluationErrorCode errorCode)
        {
            lock (_lock)
            {
                if (_traceCount < MaxBufferedTraces)
                {
                    _evalTraces.Add(new EvaluationTraceData(reason, errorCode));
                    _traceCount++;
                }
            }
        }

        internal void TrackResolveLatency(ulong durationMs, RequestStatus status)
        {
            lock (_lock)
            {
                if (_traceCount < MaxBufferedTraces)
                {
                    _resolveTraces.Add(new ResolveLatencyTraceData(durationMs, status));
                    _traceCount++;
                }
            }
        }

        internal bool IsFull
        {
            get
            {
                lock (_lock)
                {
                    return _traceCount >= MaxBufferedTraces;
                }
            }
        }

        internal TelemetrySnapshot? TakeSnapshot()
        {
            lock (_lock)
            {
                if (_traceCount == 0)
                {
                    return null;
                }

                var snapshot = new TelemetrySnapshot(
                    _library,
                    _sdkVersion,
                    _platform,
                    _resolveTraces,
                    _evalTraces);
                _evalTraces = new List<EvaluationTraceData>();
                _resolveTraces = new List<ResolveLatencyTraceData>();
                _traceCount = 0;

                return snapshot;
            }
        }

        internal void Restore(TelemetrySnapshot snapshot)
        {
            lock (_lock)
            {
                foreach (var trace in snapshot.ResolveTraces)
                {
                    if (_traceCount >= MaxBufferedTraces)
                    {
                        return;
                    }

                    _resolveTraces.Add(trace);
                    _traceCount++;
                }

                foreach (var trace in snapshot.EvaluationTraces)
                {
                    if (_traceCount >= MaxBufferedTraces)
                    {
                        return;
                    }

                    _evalTraces.Add(trace);
                    _traceCount++;
                }
            }
        }

        internal static (EvaluationReason reason, EvaluationErrorCode errorCode) MapEvaluationResult(
            Reason reason,
            ErrorCode errorCode)
        {
            if (errorCode != ErrorCode.None)
            {
                return (EvaluationReason.Error, MapErrorCode(errorCode));
            }

            switch (reason)
            {
                case Reason.RESOLVE_REASON_MATCH:
                    return (EvaluationReason.TargetingMatch, EvaluationErrorCode.Unspecified);
                case Reason.RESOLVE_REASON_NO_SEGMENT_MATCH:
                case Reason.RESOLVE_REASON_NO_TREATMENT_MATCH:
                case Reason.DEFAULT:
                    return (EvaluationReason.Default, EvaluationErrorCode.Unspecified);
                case Reason.RESOLVE_REASON_STALE:
                    return (EvaluationReason.Stale, EvaluationErrorCode.Unspecified);
                case Reason.RESOLVE_REASON_FLAG_ARCHIVED:
                    return (EvaluationReason.Disabled, EvaluationErrorCode.Unspecified);
                case Reason.RESOLVE_REASON_TARGETING_KEY_ERROR:
                    return (EvaluationReason.Error, EvaluationErrorCode.TargetingKeyMissing);
                case Reason.ERROR:
                    return (EvaluationReason.Error, EvaluationErrorCode.General);
                default:
                    return (EvaluationReason.Unspecified, EvaluationErrorCode.Unspecified);
            }
        }

        private static EvaluationErrorCode MapErrorCode(ErrorCode errorCode)
        {
            switch (errorCode)
            {
                case ErrorCode.ProviderNotReady:
                    return EvaluationErrorCode.ProviderNotReady;
                case ErrorCode.FlagNotFound:
                    return EvaluationErrorCode.FlagNotFound;
                case ErrorCode.ParseError:
                    return EvaluationErrorCode.ParseError;
                case ErrorCode.TypeMismatch:
                    return EvaluationErrorCode.TypeMismatch;
                case ErrorCode.TargetingKeyMissing:
                    return EvaluationErrorCode.TargetingKeyMissing;
                case ErrorCode.InvalidContext:
                    return EvaluationErrorCode.InvalidContext;
                default:
                    return EvaluationErrorCode.General;
            }
        }
    }

    internal sealed class TelemetrySnapshot
    {
        internal Library Library { get; }
        internal string SdkVersion { get; }
        internal Platform Platform { get; }
        internal IReadOnlyList<ResolveLatencyTraceData> ResolveTraces { get; }
        internal IReadOnlyList<EvaluationTraceData> EvaluationTraces { get; }
        internal int TraceCount => ResolveTraces.Count + EvaluationTraces.Count;

        internal TelemetrySnapshot(
            Library library,
            string sdkVersion,
            Platform platform,
            IReadOnlyList<ResolveLatencyTraceData> resolveTraces,
            IReadOnlyList<EvaluationTraceData> evaluationTraces)
        {
            Library = library;
            SdkVersion = sdkVersion;
            Platform = platform;
            ResolveTraces = resolveTraces;
            EvaluationTraces = evaluationTraces;
        }

        internal Dictionary<string, object> ToMonitoringPayload()
        {
            var traces = new List<Dictionary<string, object>>(TraceCount);
            foreach (var trace in ResolveTraces)
            {
                traces.Add(new Dictionary<string, object>
                {
                    { "id", "TRACE_ID_RESOLVE_LATENCY" },
                    {
                        "requestTrace",
                        new Dictionary<string, object>
                        {
                            { "millisecondDuration", trace.MillisecondDuration },
                            { "status", RequestStatusName(trace.Status) },
                        }
                    },
                });
            }

            foreach (var trace in EvaluationTraces)
            {
                traces.Add(new Dictionary<string, object>
                {
                    { "id", "TRACE_ID_FLAG_EVALUATION" },
                    {
                        "evaluationTrace",
                        new Dictionary<string, object>
                        {
                            { "reason", EvaluationReasonName(trace.Reason) },
                            { "errorCode", EvaluationErrorCodeName(trace.ErrorCode) },
                        }
                    },
                });
            }

            var libraryTraces = new Dictionary<string, object>
            {
                { "library", LibraryName(Library) },
                { "libraryVersion", SdkVersion },
                { "traces", traces },
            };

            return new Dictionary<string, object>
            {
                { "libraryTraces", new List<Dictionary<string, object>> { libraryTraces } },
                { "platform", PlatformName(Platform) },
            };
        }

        private static string PlatformName(Platform platform)
        {
            return platform == Platform.Unity ? "PLATFORM_UNITY" : "PLATFORM_UNSPECIFIED";
        }

        private static string LibraryName(Library library)
        {
            switch (library)
            {
                case Library.Confidence:
                    return "LIBRARY_CONFIDENCE";
                case Library.OpenFeature:
                    return "LIBRARY_OPEN_FEATURE";
                default:
                    return "LIBRARY_UNKNOWN";
            }
        }

        private static string RequestStatusName(RequestStatus status)
        {
            switch (status)
            {
                case RequestStatus.Success:
                    return "STATUS_SUCCESS";
                case RequestStatus.Error:
                    return "STATUS_ERROR";
                case RequestStatus.Timeout:
                    return "STATUS_TIMEOUT";
                case RequestStatus.Cached:
                    return "STATUS_CACHED";
                default:
                    return "STATUS_UNSPECIFIED";
            }
        }

        private static string EvaluationReasonName(EvaluationReason reason)
        {
            switch (reason)
            {
                case EvaluationReason.TargetingMatch:
                    return "EVALUATION_REASON_TARGETING_MATCH";
                case EvaluationReason.Default:
                    return "EVALUATION_REASON_DEFAULT";
                case EvaluationReason.Stale:
                    return "EVALUATION_REASON_STALE";
                case EvaluationReason.Disabled:
                    return "EVALUATION_REASON_DISABLED";
                case EvaluationReason.Cached:
                    return "EVALUATION_REASON_CACHED";
                case EvaluationReason.Static:
                    return "EVALUATION_REASON_STATIC";
                case EvaluationReason.Split:
                    return "EVALUATION_REASON_SPLIT";
                case EvaluationReason.Error:
                    return "EVALUATION_REASON_ERROR";
                default:
                    return "EVALUATION_REASON_UNSPECIFIED";
            }
        }

        private static string EvaluationErrorCodeName(EvaluationErrorCode errorCode)
        {
            switch (errorCode)
            {
                case EvaluationErrorCode.ProviderNotReady:
                    return "EVALUATION_ERROR_CODE_PROVIDER_NOT_READY";
                case EvaluationErrorCode.FlagNotFound:
                    return "EVALUATION_ERROR_CODE_FLAG_NOT_FOUND";
                case EvaluationErrorCode.ParseError:
                    return "EVALUATION_ERROR_CODE_PARSE_ERROR";
                case EvaluationErrorCode.TypeMismatch:
                    return "EVALUATION_ERROR_CODE_TYPE_MISMATCH";
                case EvaluationErrorCode.TargetingKeyMissing:
                    return "EVALUATION_ERROR_CODE_TARGETING_KEY_MISSING";
                case EvaluationErrorCode.InvalidContext:
                    return "EVALUATION_ERROR_CODE_INVALID_CONTEXT";
                case EvaluationErrorCode.ProviderFatal:
                    return "EVALUATION_ERROR_CODE_PROVIDER_FATAL";
                case EvaluationErrorCode.General:
                    return "EVALUATION_ERROR_CODE_GENERAL";
                default:
                    return "EVALUATION_ERROR_CODE_UNSPECIFIED";
            }
        }
    }
}
