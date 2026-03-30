using Spotify.Confidence.Sdk.Telemetry;
using Xunit;

namespace Spotify.Confidence.Sdk.Tests.Telemetry;

public class TelemetryReasonMappingTests
{
    [Theory]
    [InlineData("RESOLVE_REASON_MATCH", (int)EvaluationReason.Match)]
    [InlineData("RESOLVE_REASON_UNSPECIFIED", (int)EvaluationReason.Unspecified_)]
    [InlineData("RESOLVE_REASON_NO_SEGMENT_MATCH", (int)EvaluationReason.NoSegmentMatch)]
    [InlineData("RESOLVE_REASON_NO_TREATMENT_MATCH", (int)EvaluationReason.NoSegmentMatch)]
    [InlineData("RESOLVE_REASON_FLAG_ARCHIVED", (int)EvaluationReason.Archived)]
    [InlineData("RESOLVE_REASON_TARGETING_KEY_ERROR", (int)EvaluationReason.TargetingKeyError)]
    [InlineData("ERROR", (int)EvaluationReason.Error)]
    public void MapEvaluationReason_KnownApiReasons_MapsCorrectly(string apiReason, int expectedInt)
    {
        var expected = (EvaluationReason)expectedInt;
        var (reason, errorCode) = Sdk.Telemetry.Telemetry.MapEvaluationReason(apiReason, null);
        Assert.Equal(expected, reason);
        Assert.Equal(EvaluationErrorCode.Unspecified, errorCode);
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
    public void MapEvaluationReason_ParseError_ReturnsParseError()
    {
        var (reason, errorCode) = Sdk.Telemetry.Telemetry.MapEvaluationReason(null, "Failed to parse flag value");
        Assert.Equal(EvaluationReason.Error, reason);
        Assert.Equal(EvaluationErrorCode.ParseError, errorCode);
    }

    [Fact]
    public void MapEvaluationReason_TypeMismatchError_ReturnsParseError()
    {
        var (reason, errorCode) = Sdk.Telemetry.Telemetry.MapEvaluationReason(null, "Type mismatch for flag value");
        Assert.Equal(EvaluationReason.Error, reason);
        Assert.Equal(EvaluationErrorCode.ParseError, errorCode);
    }

    [Fact]
    public void MapEvaluationReason_CannotConvertError_ReturnsParseError()
    {
        var (reason, errorCode) = Sdk.Telemetry.Telemetry.MapEvaluationReason(null, "Cannot convert value to expected type");
        Assert.Equal(EvaluationReason.Error, reason);
        Assert.Equal(EvaluationErrorCode.ParseError, errorCode);
    }

    [Fact]
    public void MapEvaluationReason_CancelledError_ReturnsGeneralError()
    {
        var (reason, errorCode) = Sdk.Telemetry.Telemetry.MapEvaluationReason(null, "Request was cancelled");
        Assert.Equal(EvaluationReason.Error, reason);
        Assert.Equal(EvaluationErrorCode.GeneralError, errorCode);
    }

    [Fact]
    public void MapEvaluationReason_GenericError_ReturnsGeneralError()
    {
        var (reason, errorCode) = Sdk.Telemetry.Telemetry.MapEvaluationReason(null, "Failed to communicate with Confidence API");
        Assert.Equal(EvaluationReason.Error, reason);
        Assert.Equal(EvaluationErrorCode.GeneralError, errorCode);
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
