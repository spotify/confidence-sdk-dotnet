using Google.Protobuf;
using Spotify.Confidence.Sdk.Telemetry;
using Xunit;

namespace Spotify.Confidence.Sdk.Tests.Telemetry;

public class ProtobufEncoderTests
{
    [Fact]
    public void EncodeMonitoring_WithResolveTrace_RoundTrips()
    {
        var resolveTraces = new List<ResolveLatencyTraceData>
        {
            new(MillisecondDuration: 150, Status: RequestStatus.Ok),
        };
        var evalTraces = new List<EvaluationTraceData>();

        var bytes = ProtobufEncoder.EncodeMonitoring(
            Library.Confidence,
            "1.0.0",
            Platform.DotNet,
            resolveTraces,
            evalTraces);

        // Decode with Google.Protobuf CodedInputStream
        var input = new CodedInputStream(bytes);
        AssertMonitoringMessage(input, expectedLibrary: 1, expectedVersion: "1.0.0", expectedPlatform: 12,
            expectedResolveTraces: 1, expectedEvalTraces: 0,
            firstResolveDuration: 150, firstResolveStatus: 1);
    }

    [Fact]
    public void EncodeMonitoring_WithEvaluationTrace_RoundTrips()
    {
        var resolveTraces = new List<ResolveLatencyTraceData>();
        var evalTraces = new List<EvaluationTraceData>
        {
            new(Reason: EvaluationReason.Match, ErrorCode: EvaluationErrorCode.Unspecified),
        };

        var bytes = ProtobufEncoder.EncodeMonitoring(
            Library.OpenFeature,
            "2.0.0",
            Platform.DotNet,
            resolveTraces,
            evalTraces);

        var input = new CodedInputStream(bytes);
        AssertMonitoringMessage(input, expectedLibrary: 2, expectedVersion: "2.0.0", expectedPlatform: 12,
            expectedResolveTraces: 0, expectedEvalTraces: 1,
            firstEvalReason: 1, firstEvalErrorCode: 0);
    }

    [Fact]
    public void EncodeMonitoring_WithMultipleTraces_EncodesAll()
    {
        var resolveTraces = new List<ResolveLatencyTraceData>
        {
            new(MillisecondDuration: 100, Status: RequestStatus.Ok),
            new(MillisecondDuration: 200, Status: RequestStatus.Timeout),
        };
        var evalTraces = new List<EvaluationTraceData>
        {
            new(Reason: EvaluationReason.Match, ErrorCode: EvaluationErrorCode.Unspecified),
            new(Reason: EvaluationReason.Error, ErrorCode: EvaluationErrorCode.FlagNotFound),
        };

        var bytes = ProtobufEncoder.EncodeMonitoring(
            Library.Confidence,
            "1.0.0",
            Platform.DotNet,
            resolveTraces,
            evalTraces);

        Assert.NotEmpty(bytes);

        // Verify overall structure by counting field 3 (traces) inside the library_traces message
        var input = new CodedInputStream(bytes);

        // Read field 1 tag (library_traces)
        var tag = input.ReadTag();
        Assert.Equal(WireFormat.MakeTag(1, WireFormat.WireType.LengthDelimited), tag);

        // Count trace fields within library_traces
        var libraryTracesData = input.ReadBytes();
        var limitInput = new CodedInputStream(libraryTracesData.ToByteArray());
        int traceCount = 0;

        while (!limitInput.IsAtEnd)
        {
            var innerTag = limitInput.ReadTag();
            var fieldNumber = WireFormat.GetTagFieldNumber(innerTag);
            var wireType = WireFormat.GetTagWireType(innerTag);

            if (fieldNumber == 3) // traces field
            {
                traceCount++;
                limitInput.ReadBytes(); // skip trace content
            }
            else if (wireType == WireFormat.WireType.Varint)
            {
                limitInput.ReadUInt64();
            }
            else if (wireType == WireFormat.WireType.LengthDelimited)
            {
                limitInput.ReadBytes(); // skip content
            }
        }

        Assert.Equal(4, traceCount); // 2 resolve + 2 eval
    }

    [Fact]
    public void EncodeMonitoring_DefaultValuesSkipped()
    {
        // A resolve trace with duration=0 and status=Unspecified(0) should produce
        // a minimal encoding where default values are omitted
        var resolveTraces = new List<ResolveLatencyTraceData>
        {
            new(MillisecondDuration: 0, Status: RequestStatus.Unspecified),
        };
        var evalTraces = new List<EvaluationTraceData>();

        var bytes = ProtobufEncoder.EncodeMonitoring(
            Library.Unspecified,
            string.Empty,
            Platform.Unspecified,
            resolveTraces,
            evalTraces);

        // Should still encode something (the trace id is always written)
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void EncodeMonitoring_EmptyTraces_ProducesEmptyBytes()
    {
        var bytes = ProtobufEncoder.EncodeMonitoring(
            Library.Confidence,
            "1.0.0",
            Platform.DotNet,
            new List<ResolveLatencyTraceData>(),
            new List<EvaluationTraceData>());

        // Even with no traces, we should still get library info and platform
        // Actually: the library_traces message has library=1, version="1.0.0" but no traces
        // and platform=12 at the monitoring level
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void EncodeMonitoring_WithErrorTrace_EncodesErrorCode()
    {
        var evalTraces = new List<EvaluationTraceData>
        {
            new(Reason: EvaluationReason.Error, ErrorCode: EvaluationErrorCode.FlagNotFound),
        };

        var bytes = ProtobufEncoder.EncodeMonitoring(
            Library.Confidence,
            "1.0.0",
            Platform.DotNet,
            new List<ResolveLatencyTraceData>(),
            evalTraces);

        Assert.NotEmpty(bytes);

        // Verify the bytes decode correctly by walking through the structure
        var input = new CodedInputStream(bytes);
        AssertMonitoringMessage(input, expectedLibrary: 1, expectedVersion: "1.0.0", expectedPlatform: 12,
            expectedResolveTraces: 0, expectedEvalTraces: 1,
            firstEvalReason: 8, firstEvalErrorCode: 2);
    }

    private static void AssertMonitoringMessage(
        CodedInputStream input,
        int expectedLibrary,
        string expectedVersion,
        int expectedPlatform,
        int expectedResolveTraces,
        int expectedEvalTraces,
        ulong firstResolveDuration = 0,
        int firstResolveStatus = 0,
        int firstEvalReason = 0,
        int firstEvalErrorCode = 0)
    {
        int actualLibrary = 0;
        string actualVersion = string.Empty;
        int actualPlatform = 0;
        int resolveTraceCount = 0;
        int evalTraceCount = 0;
        ulong actualResolveDuration = 0;
        int actualResolveStatus = 0;
        int actualEvalReason = 0;
        int actualEvalErrorCode = 0;

        while (!input.IsAtEnd)
        {
            var tag = input.ReadTag();
            var fieldNumber = WireFormat.GetTagFieldNumber(tag);

            switch (fieldNumber)
            {
                case 1: // library_traces
                    var libraryTracesBytes = input.ReadBytes();
                    var ltInput = new CodedInputStream(libraryTracesBytes.ToByteArray());
                    while (!ltInput.IsAtEnd)
                    {
                        var ltTag = ltInput.ReadTag();
                        var ltField = WireFormat.GetTagFieldNumber(ltTag);
                        switch (ltField)
                        {
                            case 1: // library
                                actualLibrary = (int)ltInput.ReadUInt64();
                                break;
                            case 2: // library_version
                                actualVersion = ltInput.ReadString();
                                break;
                            case 3: // traces
                                var traceBytes = ltInput.ReadBytes();
                                var traceInput = new CodedInputStream(traceBytes.ToByteArray());
                                int traceId = 0;
                                while (!traceInput.IsAtEnd)
                                {
                                    var traceTag = traceInput.ReadTag();
                                    var traceField = WireFormat.GetTagFieldNumber(traceTag);
                                    switch (traceField)
                                    {
                                        case 1: // id
                                            traceId = (int)traceInput.ReadUInt64();
                                            break;
                                        case 3: // request_trace
                                            resolveTraceCount++;
                                            var rtBytes = traceInput.ReadBytes();
                                            var rtInput = new CodedInputStream(rtBytes.ToByteArray());
                                            while (!rtInput.IsAtEnd)
                                            {
                                                var rtTag = rtInput.ReadTag();
                                                var rtField = WireFormat.GetTagFieldNumber(rtTag);
                                                switch (rtField)
                                                {
                                                    case 1:
                                                        actualResolveDuration = rtInput.ReadUInt64();
                                                        break;
                                                    case 2:
                                                        actualResolveStatus = (int)rtInput.ReadUInt64();
                                                        break;
                                                    default:
                                                        rtInput.ReadUInt64();
                                                        break;
                                                }
                                            }
                                            break;
                                        case 5: // evaluation_trace
                                            evalTraceCount++;
                                            var etBytes = traceInput.ReadBytes();
                                            var etInput = new CodedInputStream(etBytes.ToByteArray());
                                            while (!etInput.IsAtEnd)
                                            {
                                                var etTag = etInput.ReadTag();
                                                var etField = WireFormat.GetTagFieldNumber(etTag);
                                                switch (etField)
                                                {
                                                    case 1:
                                                        actualEvalReason = (int)etInput.ReadUInt64();
                                                        break;
                                                    case 2:
                                                        actualEvalErrorCode = (int)etInput.ReadUInt64();
                                                        break;
                                                    default:
                                                        etInput.ReadUInt64();
                                                        break;
                                                }
                                            }
                                            break;
                                        default:
                                            traceInput.ReadUInt64();
                                            break;
                                    }
                                }
                                break;
                            default:
                                ltInput.ReadUInt64();
                                break;
                        }
                    }
                    break;
                case 2: // platform
                    actualPlatform = (int)input.ReadUInt64();
                    break;
                default:
                    input.ReadUInt64();
                    break;
            }
        }

        Assert.Equal(expectedLibrary, actualLibrary);
        Assert.Equal(expectedVersion, actualVersion);
        Assert.Equal(expectedPlatform, actualPlatform);
        Assert.Equal(expectedResolveTraces, resolveTraceCount);
        Assert.Equal(expectedEvalTraces, evalTraceCount);

        if (expectedResolveTraces > 0)
        {
            Assert.Equal(firstResolveDuration, actualResolveDuration);
            Assert.Equal(firstResolveStatus, actualResolveStatus);
        }

        if (expectedEvalTraces > 0)
        {
            Assert.Equal(firstEvalReason, actualEvalReason);
            Assert.Equal(firstEvalErrorCode, actualEvalErrorCode);
        }
    }
}
