namespace Spotify.Confidence.Sdk.Telemetry;

/// <summary>
/// Manual protobuf wire-format encoder for telemetry messages.
/// Avoids a production dependency on Google.Protobuf.
/// </summary>
internal static class ProtobufEncoder
{
    // Wire types
    private const int WireTypeVarint = 0;
    private const int WireTypeLengthDelimited = 2;

    public static byte[] EncodeMonitoring(
        Library library,
        string sdkVersion,
        Platform platform,
        IReadOnlyList<ResolveLatencyTraceData> resolveTraces,
        IReadOnlyList<EvaluationTraceData> evalTraces)
    {
        using var ms = new MemoryStream();

        // field 1: library_traces (repeated, but we send one)
        var libraryTracesBytes = EncodeLibraryTraces(library, sdkVersion, resolveTraces, evalTraces);
        if (libraryTracesBytes.Length > 0)
        {
            WriteLengthDelimited(ms, fieldNumber: 1, libraryTracesBytes);
        }

        // field 2: platform (enum/varint)
        if (platform != Platform.Unspecified)
        {
            WriteTag(ms, fieldNumber: 2, WireTypeVarint);
            WriteVarint(ms, (ulong)platform);
        }

        return ms.ToArray();
    }

    private static byte[] EncodeLibraryTraces(
        Library library,
        string sdkVersion,
        IReadOnlyList<ResolveLatencyTraceData> resolveTraces,
        IReadOnlyList<EvaluationTraceData> evalTraces)
    {
        using var ms = new MemoryStream();

        // field 1: library (enum/varint)
        if (library != Library.Unspecified)
        {
            WriteTag(ms, fieldNumber: 1, WireTypeVarint);
            WriteVarint(ms, (ulong)library);
        }

        // field 2: library_version (string)
        if (!string.IsNullOrEmpty(sdkVersion))
        {
            var versionBytes = System.Text.Encoding.UTF8.GetBytes(sdkVersion);
            WriteLengthDelimited(ms, fieldNumber: 2, versionBytes);
        }

        // field 3: traces (repeated)
        foreach (var trace in resolveTraces)
        {
            var traceBytes = EncodeResolveTrace(trace);
            WriteLengthDelimited(ms, fieldNumber: 3, traceBytes);
        }

        foreach (var trace in evalTraces)
        {
            var traceBytes = EncodeEvaluationTrace(trace);
            WriteLengthDelimited(ms, fieldNumber: 3, traceBytes);
        }

        return ms.ToArray();
    }

    private static byte[] EncodeResolveTrace(ResolveLatencyTraceData data)
    {
        using var ms = new MemoryStream();

        // field 1: id (enum/varint) = TraceId.ResolveLatency
        WriteTag(ms, fieldNumber: 1, WireTypeVarint);
        WriteVarint(ms, (ulong)TraceId.ResolveLatency);

        // field 3: request_trace (length-delimited)
        var requestTraceBytes = EncodeRequestTrace(data);
        if (requestTraceBytes.Length > 0)
        {
            WriteLengthDelimited(ms, fieldNumber: 3, requestTraceBytes);
        }

        return ms.ToArray();
    }

    private static byte[] EncodeRequestTrace(ResolveLatencyTraceData data)
    {
        using var ms = new MemoryStream();

        // field 1: millisecond_duration (uint64/varint)
        if (data.MillisecondDuration > 0)
        {
            WriteTag(ms, fieldNumber: 1, WireTypeVarint);
            WriteVarint(ms, data.MillisecondDuration);
        }

        // field 2: status (enum/varint)
        if (data.Status != RequestStatus.Unspecified)
        {
            WriteTag(ms, fieldNumber: 2, WireTypeVarint);
            WriteVarint(ms, (ulong)data.Status);
        }

        return ms.ToArray();
    }

    private static byte[] EncodeEvaluationTrace(EvaluationTraceData data)
    {
        using var ms = new MemoryStream();

        // field 1: id (enum/varint) = TraceId.FlagEvaluation
        WriteTag(ms, fieldNumber: 1, WireTypeVarint);
        WriteVarint(ms, (ulong)TraceId.FlagEvaluation);

        // field 5: evaluation_trace (length-delimited)
        var evalTraceBytes = EncodeEvalTraceBody(data);
        if (evalTraceBytes.Length > 0)
        {
            WriteLengthDelimited(ms, fieldNumber: 5, evalTraceBytes);
        }

        return ms.ToArray();
    }

    private static byte[] EncodeEvalTraceBody(EvaluationTraceData data)
    {
        using var ms = new MemoryStream();

        // field 1: reason (enum/varint)
        if (data.Reason != EvaluationReason.Unspecified)
        {
            WriteTag(ms, fieldNumber: 1, WireTypeVarint);
            WriteVarint(ms, (ulong)data.Reason);
        }

        // field 2: error_code (enum/varint)
        if (data.ErrorCode != EvaluationErrorCode.Unspecified)
        {
            WriteTag(ms, fieldNumber: 2, WireTypeVarint);
            WriteVarint(ms, (ulong)data.ErrorCode);
        }

        return ms.ToArray();
    }

    internal static void WriteTag(Stream stream, int fieldNumber, int wireType)
    {
        WriteVarint(stream, (ulong)((fieldNumber << 3) | wireType));
    }

    internal static void WriteVarint(Stream stream, ulong value)
    {
        do
        {
            var b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0)
            {
                b |= 0x80;
            }

            stream.WriteByte(b);
        }
        while (value != 0);
    }

    internal static void WriteLengthDelimited(Stream stream, int fieldNumber, byte[] data)
    {
        WriteTag(stream, fieldNumber, WireTypeLengthDelimited);
        WriteVarint(stream, (ulong)data.Length);
        stream.Write(data, 0, data.Length);
    }
}
