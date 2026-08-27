using System;
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
        private const int MaxTraces = 100;

        private readonly object _lock = new object();
        private readonly Library _library;
        private readonly Platform _platform;
        private readonly string _sdkVersion;
        private List<EvaluationTraceData> _evalTraces = new List<EvaluationTraceData>();
        private List<ResolveLatencyTraceData> _resolveTraces = new List<ResolveLatencyTraceData>();

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

        internal string EncodedHeaderValue()
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
                _library,
                _sdkVersion,
                _platform,
                resolveSnapshot,
                evalSnapshot);

            return Convert.ToBase64String(bytes);
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
}
