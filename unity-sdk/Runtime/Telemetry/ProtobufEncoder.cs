using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnityOpenFeature.Telemetry
{
    internal static class ProtobufEncoder
    {
        private const int WireTypeVarint = 0;
        private const int WireTypeLengthDelimited = 2;

        public static byte[] EncodeMonitoring(
            Library library,
            string sdkVersion,
            Platform platform,
            IReadOnlyList<ResolveLatencyTraceData> resolveTraces,
            IReadOnlyList<EvaluationTraceData> evalTraces)
        {
            using (var ms = new MemoryStream())
            {
                var libraryTracesBytes = EncodeLibraryTraces(library, sdkVersion, resolveTraces, evalTraces);
                if (libraryTracesBytes.Length > 0)
                {
                    WriteLengthDelimited(ms, 1, libraryTracesBytes);
                }

                if (platform != Platform.Unspecified)
                {
                    WriteTag(ms, 2, WireTypeVarint);
                    WriteVarint(ms, (ulong)platform);
                }

                return ms.ToArray();
            }
        }

        private static byte[] EncodeLibraryTraces(
            Library library,
            string sdkVersion,
            IReadOnlyList<ResolveLatencyTraceData> resolveTraces,
            IReadOnlyList<EvaluationTraceData> evalTraces)
        {
            using (var ms = new MemoryStream())
            {
                if (library != Library.Unspecified)
                {
                    WriteTag(ms, 1, WireTypeVarint);
                    WriteVarint(ms, (ulong)library);
                }

                if (!string.IsNullOrEmpty(sdkVersion))
                {
                    var versionBytes = Encoding.UTF8.GetBytes(sdkVersion);
                    WriteLengthDelimited(ms, 2, versionBytes);
                }

                foreach (var trace in resolveTraces)
                {
                    var traceBytes = EncodeResolveTrace(trace);
                    WriteLengthDelimited(ms, 3, traceBytes);
                }

                foreach (var trace in evalTraces)
                {
                    var traceBytes = EncodeEvaluationTrace(trace);
                    WriteLengthDelimited(ms, 3, traceBytes);
                }

                return ms.ToArray();
            }
        }

        private static byte[] EncodeResolveTrace(ResolveLatencyTraceData data)
        {
            using (var ms = new MemoryStream())
            {
                WriteTag(ms, 1, WireTypeVarint);
                WriteVarint(ms, (ulong)TraceId.ResolveLatency);

                var requestTraceBytes = EncodeRequestTrace(data);
                if (requestTraceBytes.Length > 0)
                {
                    WriteLengthDelimited(ms, 3, requestTraceBytes);
                }

                return ms.ToArray();
            }
        }

        private static byte[] EncodeRequestTrace(ResolveLatencyTraceData data)
        {
            using (var ms = new MemoryStream())
            {
                if (data.MillisecondDuration > 0)
                {
                    WriteTag(ms, 1, WireTypeVarint);
                    WriteVarint(ms, data.MillisecondDuration);
                }

                if (data.Status != RequestStatus.Unspecified)
                {
                    WriteTag(ms, 2, WireTypeVarint);
                    WriteVarint(ms, (ulong)data.Status);
                }

                return ms.ToArray();
            }
        }

        private static byte[] EncodeEvaluationTrace(EvaluationTraceData data)
        {
            using (var ms = new MemoryStream())
            {
                WriteTag(ms, 1, WireTypeVarint);
                WriteVarint(ms, (ulong)TraceId.EvaluationOutcome);

                var evalTraceBytes = EncodeEvalTraceBody(data);
                if (evalTraceBytes.Length > 0)
                {
                    WriteLengthDelimited(ms, 5, evalTraceBytes);
                }

                return ms.ToArray();
            }
        }

        private static byte[] EncodeEvalTraceBody(EvaluationTraceData data)
        {
            using (var ms = new MemoryStream())
            {
                if (data.Reason != EvaluationReason.Unspecified)
                {
                    WriteTag(ms, 1, WireTypeVarint);
                    WriteVarint(ms, (ulong)data.Reason);
                }

                if (data.ErrorCode != EvaluationErrorCode.Unspecified)
                {
                    WriteTag(ms, 2, WireTypeVarint);
                    WriteVarint(ms, (ulong)data.ErrorCode);
                }

                return ms.ToArray();
            }
        }

        private static void WriteTag(Stream stream, int fieldNumber, int wireType)
        {
            WriteVarint(stream, (ulong)((fieldNumber << 3) | wireType));
        }

        private static void WriteVarint(Stream stream, ulong value)
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

        private static void WriteLengthDelimited(Stream stream, int fieldNumber, byte[] data)
        {
            WriteTag(stream, fieldNumber, WireTypeLengthDelimited);
            WriteVarint(stream, (ulong)data.Length);
            stream.Write(data, 0, data.Length);
        }
    }
}
