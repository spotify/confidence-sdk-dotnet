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
    EvaluationOutcome = 2,
}

internal enum RequestStatus
{
    Unspecified = 0,
    Ok = 1,
    Timeout = 2,
    Error = 3,
}

internal enum EvaluationReason
{
    Unspecified = 0,
    Match = 1,
    Unspecified_ = 2,
    NoSegmentMatch = 3,
    Archived = 4,
    TargetingKeyError = 5,
    ProviderNotReady = 6,
    DefaultValue = 7,
    Error = 8,
}

internal enum EvaluationErrorCode
{
    Unspecified = 0,
    ProviderNotReady = 1,
    FlagNotFound = 2,
    ParseError = 3,
    TypeMismatch = 4,
    GeneralError = 5,
    InvalidContext = 6,
    TargetingKeyMissing = 7,
}
