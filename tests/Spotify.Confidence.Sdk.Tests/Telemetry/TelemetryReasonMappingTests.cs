using Spotify.Confidence.Sdk.Telemetry;
using Xunit;

namespace Spotify.Confidence.Sdk.Tests.Telemetry;

public class TelemetryReasonMappingTests
{
    [Theory]
    [InlineData("RESOLVE_REASON_MATCH", (int)EvaluationReason.TargetingMatch, (int)EvaluationErrorCode.Unspecified)]
    [InlineData("RESOLVE_REASON_NO_SEGMENT_MATCH", (int)EvaluationReason.Default, (int)EvaluationErrorCode.Unspecified)]
    [InlineData("RESOLVE_REASON_NO_TREATMENT_MATCH", (int)EvaluationReason.Default, (int)EvaluationErrorCode.Unspecified)]
    [InlineData("RESOLVE_REASON_STALE", (int)EvaluationReason.Stale, (int)EvaluationErrorCode.Unspecified)]
    [InlineData("RESOLVE_REASON_FLAG_ARCHIVED", (int)EvaluationReason.Disabled, (int)EvaluationErrorCode.Unspecified)]
    [InlineData("RESOLVE_REASON_TARGETING_KEY_ERROR", (int)EvaluationReason.Error, (int)EvaluationErrorCode.TargetingKeyMissing)]
    [InlineData("ERROR", (int)EvaluationReason.Error, (int)EvaluationErrorCode.General)]
    public void MapEvaluationReason_KnownApiReasons_MapsCorrectly(string apiReason, int expectedReasonInt, int expectedErrorCodeInt)
    {
        var expectedReason = (EvaluationReason)expectedReasonInt;
        var expectedErrorCode = (EvaluationErrorCode)expectedErrorCodeInt;
        var (reason, errorCode) = Sdk.Telemetry.Telemetry.MapEvaluationReason(apiReason, null);
        Assert.Equal(expectedReason, reason);
        Assert.Equal(expectedErrorCode, errorCode);
    }

    [Fact]
    public void MapEvaluationReason_UnknownReason_ReturnsUnspecified()
    {
        var (reason, errorCode) = Sdk.Telemetry.Telemetry.MapEvaluationReason("SOME_UNKNOWN_REASON", null);
        Assert.Equal(EvaluationReason.Unspecified, reason);
        Assert.Equal(EvaluationErrorCode.Unspecified, errorCode);
    }

    [Fact]
    public void MapEvaluationReason_NullReason_ReturnsUnspecified()
    {
        var (reason, errorCode) = Sdk.Telemetry.Telemetry.MapEvaluationReason(null, null);
        Assert.Equal(EvaluationReason.Unspecified, reason);
        Assert.Equal(EvaluationErrorCode.Unspecified, errorCode);
    }

    [Fact]
    public void MapEvaluationReason_NotFoundError_ReturnsFlagNotFound()
    {
        var (reason, errorCode) = Sdk.Telemetry.Telemetry.MapEvaluationReason(null, "Flag 'my-flag' not found in response");
        Assert.Equal(EvaluationReason.Error, reason);
        Assert.Equal(EvaluationErrorCode.FlagNotFound, errorCode);
    }

    [Fact]
    public void MapEvaluationReason_GenericError_ReturnsGeneral()
    {
        var (reason, errorCode) = Sdk.Telemetry.Telemetry.MapEvaluationReason(null, "Failed to communicate with Confidence API");
        Assert.Equal(EvaluationReason.Error, reason);
        Assert.Equal(EvaluationErrorCode.General, errorCode);
    }

    [Fact]
    public void MapEvaluationReason_CancelledError_ReturnsGeneral()
    {
        var (reason, errorCode) = Sdk.Telemetry.Telemetry.MapEvaluationReason(null, "Request was cancelled");
        Assert.Equal(EvaluationReason.Error, reason);
        Assert.Equal(EvaluationErrorCode.General, errorCode);
    }

    [Fact]
    public void MapEvaluationReason_ErrorMessageTakesPrecedence()
    {
        // When both reason and error message are provided, error message wins
        var (reason, errorCode) = Sdk.Telemetry.Telemetry.MapEvaluationReason("RESOLVE_REASON_MATCH", "Flag 'x' not found in response");
        Assert.Equal(EvaluationReason.Error, reason);
        Assert.Equal(EvaluationErrorCode.FlagNotFound, errorCode);
    }
}
