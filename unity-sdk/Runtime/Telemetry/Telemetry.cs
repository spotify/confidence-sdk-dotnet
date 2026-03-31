using System;
using System.Collections.Generic;

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
        private readonly Platform _platform;
        private readonly string _sdkVersion;
        private List<EvaluationTraceData> _evalTraces = new List<EvaluationTraceData>();
        private List<ResolveLatencyTraceData> _resolveTraces = new List<ResolveLatencyTraceData>();
        private volatile Library _currentLibrary = Library.OpenFeature;

        internal Library CurrentLibrary
        {
            get { return _currentLibrary; }
            set { _currentLibrary = value; }
        }

        internal Telemetry(Platform platform, string sdkVersion)
        {
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
                _currentLibrary,
                _sdkVersion,
                _platform,
                resolveSnapshot,
                evalSnapshot);

            return Convert.ToBase64String(bytes);
        }

        internal static (EvaluationReason reason, EvaluationErrorCode errorCode) MapEvaluationReason(
            string apiReason,
            string errorMessage)
        {
            if (errorMessage != null)
            {
                var errorCode = MapErrorMessageToCode(errorMessage);
                return (EvaluationReason.Error, errorCode);
            }

            EvaluationReason reason;
            switch (apiReason)
            {
                case "RESOLVE_REASON_MATCH":
                    reason = EvaluationReason.Match;
                    break;
                case "RESOLVE_REASON_UNSPECIFIED":
                    reason = EvaluationReason.Unspecified_;
                    break;
                case "RESOLVE_REASON_NO_SEGMENT_MATCH":
                case "RESOLVE_REASON_NO_TREATMENT_MATCH":
                    reason = EvaluationReason.NoSegmentMatch;
                    break;
                case "RESOLVE_REASON_FLAG_ARCHIVED":
                    reason = EvaluationReason.Archived;
                    break;
                case "RESOLVE_REASON_TARGETING_KEY_ERROR":
                    reason = EvaluationReason.TargetingKeyError;
                    break;
                case "ERROR":
                    reason = EvaluationReason.Error;
                    break;
                default:
                    reason = EvaluationReason.Unspecified;
                    break;
            }

            return (reason, EvaluationErrorCode.Unspecified);
        }

        private static EvaluationErrorCode MapErrorMessageToCode(string errorMessage)
        {
            var lower = errorMessage.ToLowerInvariant();

            if (lower.Contains("not found"))
                return EvaluationErrorCode.FlagNotFound;

            if (lower.Contains("parse") || lower.Contains("type mismatch") || lower.Contains("cannot convert"))
                return EvaluationErrorCode.ParseError;

            return EvaluationErrorCode.GeneralError;
        }
    }
}
