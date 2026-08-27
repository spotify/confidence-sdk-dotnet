namespace Spotify.Confidence.Sdk.Telemetry;

internal enum Platform
{
    Unspecified = 0,
    DotNet = 12,
}

internal enum Library
{
    Unspecified = 0,
    Confidence = 1,
    OpenFeature = 2,
}

internal enum TraceId
{
    Unspecified = 0,
    ResolveLatency = 1,
    FlagEvaluation = 3,
}

internal enum RequestStatus
{
    Unspecified = 0,
    Success = 1,
    Error = 2,
    Timeout = 3,
}

internal enum EvaluationReason
{
    Unspecified = 0,
    TargetingMatch = 1,
    Default = 2,
    Stale = 3,
    Disabled = 4,
    Cached = 5,
    Static = 6,
    Split = 7,
    Error = 8,
}

internal enum EvaluationErrorCode
{
    Unspecified = 0,
    ProviderNotReady = 1,
    FlagNotFound = 2,
    ParseError = 3,
    TypeMismatch = 4,
    TargetingKeyMissing = 5,
    InvalidContext = 6,
    ProviderFatal = 7,
    General = 8,
}
