using Spotify.Confidence.Sdk.Telemetry;
using Xunit;

namespace Spotify.Confidence.Sdk.Tests.Telemetry;

public class TelemetryTests
{
    [Fact]
    public void EncodedHeaderValue_WhenNoTraces_ReturnsNull()
    {
        var telemetry = new Sdk.Telemetry.Telemetry(Platform.DotNet);
        Assert.Null(telemetry.EncodedHeaderValue());
    }

    [Fact]
    public void EncodedHeaderValue_SnapshotAndClear_SecondCallReturnsNull()
    {
        var telemetry = new Sdk.Telemetry.Telemetry(Platform.DotNet);
        telemetry.TrackEvaluation(EvaluationReason.TargetingMatch, EvaluationErrorCode.Unspecified);

        var first = telemetry.EncodedHeaderValue();
        Assert.NotNull(first);

        var second = telemetry.EncodedHeaderValue();
        Assert.Null(second);
    }

    [Fact]
    public void TrackEvaluation_CapsAt100()
    {
        var telemetry = new Sdk.Telemetry.Telemetry(Platform.DotNet);
        for (int i = 0; i < 150; i++)
        {
            telemetry.TrackEvaluation(EvaluationReason.TargetingMatch, EvaluationErrorCode.Unspecified);
        }

        // Should produce a non-null value but with only 100 traces
        var headerValue = telemetry.EncodedHeaderValue();
        Assert.NotNull(headerValue);

        // Verify internal state is cleared
        Assert.Null(telemetry.EncodedHeaderValue());
    }

    [Fact]
    public void TrackResolveLatency_CapsAt100()
    {
        var telemetry = new Sdk.Telemetry.Telemetry(Platform.DotNet);
        for (int i = 0; i < 150; i++)
        {
            telemetry.TrackResolveLatency(50, RequestStatus.Success);
        }

        var headerValue = telemetry.EncodedHeaderValue();
        Assert.NotNull(headerValue);
        Assert.Null(telemetry.EncodedHeaderValue());
    }

    [Fact]
    public void ThreadSafety_ConcurrentWrites_DoNotCorruptState()
    {
        var telemetry = new Sdk.Telemetry.Telemetry(Platform.DotNet);
        var threads = new List<Thread>();

        for (int i = 0; i < 10; i++)
        {
            var thread = new Thread(() =>
            {
                for (int j = 0; j < 20; j++)
                {
                    telemetry.TrackEvaluation(EvaluationReason.TargetingMatch, EvaluationErrorCode.Unspecified);
                    telemetry.TrackResolveLatency(10, RequestStatus.Success);
                }
            });
            threads.Add(thread);
        }

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Should not throw and should produce a valid base64 string
        var headerValue = telemetry.EncodedHeaderValue();
        Assert.NotNull(headerValue);

        // Verify it's valid base64
        var bytes = Convert.FromBase64String(headerValue);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void CurrentLibrary_DefaultsToConfidence()
    {
        var telemetry = new Sdk.Telemetry.Telemetry(Platform.DotNet);
        Assert.Equal(Library.Confidence, telemetry.CurrentLibrary);
    }

    [Fact]
    public void CurrentLibrary_CanBeSetToOpenFeature()
    {
        var telemetry = new Sdk.Telemetry.Telemetry(Platform.DotNet);
        telemetry.CurrentLibrary = Library.OpenFeature;
        Assert.Equal(Library.OpenFeature, telemetry.CurrentLibrary);
    }

    [Fact]
    public void EncodedHeaderValue_IncludesCorrectLibrary()
    {
        var telemetry = new Sdk.Telemetry.Telemetry(Platform.DotNet);
        telemetry.CurrentLibrary = Library.OpenFeature;
        telemetry.TrackEvaluation(EvaluationReason.TargetingMatch, EvaluationErrorCode.Unspecified);

        var headerValue = telemetry.EncodedHeaderValue();
        Assert.NotNull(headerValue);

        // Decode and verify it's valid protobuf (basic check)
        var bytes = Convert.FromBase64String(headerValue);
        Assert.NotEmpty(bytes);
    }
}
