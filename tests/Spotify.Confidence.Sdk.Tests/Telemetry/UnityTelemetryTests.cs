using Google.Protobuf;
using UnityOpenFeature.Core;
using UnityOpenFeature.Telemetry;
using Xunit;

namespace Spotify.Confidence.Sdk.Tests.Telemetry;

public class UnityTelemetryTests
{
    [Fact]
    public void EncodedHeaderValue_UsesUnityPlatformAndCanonicalEvaluationValues()
    {
        var telemetry = new UnityOpenFeature.Telemetry.Telemetry(Platform.Unity, "1.2.3");
        telemetry.TrackEvaluation(EvaluationReason.Error, EvaluationErrorCode.TypeMismatch);

        var header = telemetry.EncodedHeaderValue();

        Assert.NotNull(header);
        var input = new CodedInputStream(Convert.FromBase64String(header));
        Assert.Equal(WireFormat.MakeTag(1, WireFormat.WireType.LengthDelimited), input.ReadTag());
        var libraryTraces = new CodedInputStream(input.ReadBytes().ToByteArray());

        Assert.Equal(WireFormat.MakeTag(1, WireFormat.WireType.Varint), libraryTraces.ReadTag());
        Assert.Equal((uint)Library.OpenFeature, libraryTraces.ReadUInt32());
        Assert.Equal(WireFormat.MakeTag(2, WireFormat.WireType.LengthDelimited), libraryTraces.ReadTag());
        Assert.Equal("1.2.3", libraryTraces.ReadString());
        Assert.Equal(WireFormat.MakeTag(3, WireFormat.WireType.LengthDelimited), libraryTraces.ReadTag());

        var trace = new CodedInputStream(libraryTraces.ReadBytes().ToByteArray());
        Assert.Equal(WireFormat.MakeTag(1, WireFormat.WireType.Varint), trace.ReadTag());
        Assert.Equal((uint)TraceId.FlagEvaluation, trace.ReadUInt32());
        Assert.Equal(WireFormat.MakeTag(5, WireFormat.WireType.LengthDelimited), trace.ReadTag());

        var evaluation = new CodedInputStream(trace.ReadBytes().ToByteArray());
        Assert.Equal(WireFormat.MakeTag(1, WireFormat.WireType.Varint), evaluation.ReadTag());
        Assert.Equal((uint)EvaluationReason.Error, evaluation.ReadUInt32());
        Assert.Equal(WireFormat.MakeTag(2, WireFormat.WireType.Varint), evaluation.ReadTag());
        Assert.Equal((uint)EvaluationErrorCode.TypeMismatch, evaluation.ReadUInt32());

        Assert.Equal(WireFormat.MakeTag(2, WireFormat.WireType.Varint), input.ReadTag());
        Assert.Equal((uint)Platform.Unity, input.ReadUInt32());
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
}
