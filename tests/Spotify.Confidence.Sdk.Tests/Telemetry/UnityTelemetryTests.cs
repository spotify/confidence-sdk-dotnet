using UnityOpenFeature.Core;
using UnityOpenFeature.Telemetry;
using Xunit;

namespace Spotify.Confidence.Sdk.Tests.Telemetry;

public class UnityTelemetryTests
{
    [Fact]
    public void TakeSnapshot_UsesCanonicalJsonValues()
    {
        var telemetry = new UnityOpenFeature.Telemetry.Telemetry(Platform.Unity, "1.2.3");
        telemetry.TrackEvaluation(EvaluationReason.Error, EvaluationErrorCode.TypeMismatch);
        telemetry.TrackResolveLatency(123, RequestStatus.Success);

        var snapshot = telemetry.TakeSnapshot();

        Assert.NotNull(snapshot);
        var payload = snapshot.ToMonitoringPayload();
        Assert.Equal("PLATFORM_UNITY", payload["platform"]);
        var libraries = Assert.IsType<List<Dictionary<string, object>>>(payload["libraryTraces"]);
        var library = Assert.Single(libraries);
        Assert.Equal("LIBRARY_OPEN_FEATURE", library["library"]);
        Assert.Equal("1.2.3", library["libraryVersion"]);

        var traces = Assert.IsType<List<Dictionary<string, object>>>(library["traces"]);
        var request = Assert.IsType<Dictionary<string, object>>(traces[0]["requestTrace"]);
        Assert.Equal("STATUS_SUCCESS", request["status"]);
        var evaluation = Assert.IsType<Dictionary<string, object>>(traces[1]["evaluationTrace"]);
        Assert.Equal("EVALUATION_REASON_ERROR", evaluation["reason"]);
        Assert.Equal("EVALUATION_ERROR_CODE_TYPE_MISMATCH", evaluation["errorCode"]);
    }

    [Theory]
    [InlineData(Reason.RESOLVE_REASON_MATCH, ErrorCode.None, (int)EvaluationReason.TargetingMatch, (int)EvaluationErrorCode.Unspecified)]
    [InlineData(Reason.RESOLVE_REASON_NO_SEGMENT_MATCH, ErrorCode.None, (int)EvaluationReason.Default, (int)EvaluationErrorCode.Unspecified)]
    [InlineData(Reason.RESOLVE_REASON_STALE, ErrorCode.None, (int)EvaluationReason.Stale, (int)EvaluationErrorCode.Unspecified)]
    [InlineData(Reason.RESOLVE_REASON_FLAG_ARCHIVED, ErrorCode.None, (int)EvaluationReason.Disabled, (int)EvaluationErrorCode.Unspecified)]
    [InlineData(Reason.RESOLVE_REASON_TARGETING_KEY_ERROR, ErrorCode.None, (int)EvaluationReason.Error, (int)EvaluationErrorCode.TargetingKeyMissing)]
    [InlineData(Reason.ERROR, ErrorCode.TypeMismatch, (int)EvaluationReason.Error, (int)EvaluationErrorCode.TypeMismatch)]
    public void MapEvaluationResult_UsesStructuredResult(
        Reason reason,
        ErrorCode errorCode,
        int expectedReason,
        int expectedErrorCode)
    {
        var result = UnityOpenFeature.Telemetry.Telemetry.MapEvaluationResult(reason, errorCode);

        Assert.Equal((EvaluationReason)expectedReason, result.reason);
        Assert.Equal((EvaluationErrorCode)expectedErrorCode, result.errorCode);
    }

    [Fact]
    public void TakeSnapshot_CapsCombinedTraceCount()
    {
        var telemetry = new UnityOpenFeature.Telemetry.Telemetry(Platform.Unity, "1.2.3");
        for (int i = 0; i < 75; i++)
        {
            telemetry.TrackEvaluation(EvaluationReason.TargetingMatch, EvaluationErrorCode.Unspecified);
            telemetry.TrackResolveLatency(ulong.MaxValue, RequestStatus.Success);
        }

        var snapshot = telemetry.TakeSnapshot();

        Assert.NotNull(snapshot);
        Assert.Equal(100, snapshot.TraceCount);
    }

    [Fact]
    public void Restore_RequeuesFailedSnapshotWithinLimit()
    {
        var telemetry = new UnityOpenFeature.Telemetry.Telemetry(Platform.Unity, "1.2.3");
        telemetry.TrackEvaluation(EvaluationReason.TargetingMatch, EvaluationErrorCode.Unspecified);
        var failedSnapshot = telemetry.TakeSnapshot();
        Assert.NotNull(failedSnapshot);

        telemetry.Restore(failedSnapshot);
        var retriedSnapshot = telemetry.TakeSnapshot();

        Assert.NotNull(retriedSnapshot);
        Assert.Equal(1, retriedSnapshot.TraceCount);
    }
}
