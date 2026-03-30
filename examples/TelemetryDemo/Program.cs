using Microsoft.Extensions.Logging;
using Spotify.Confidence.Sdk;
using Spotify.Confidence.Sdk.Models;
using Spotify.Confidence.Sdk.Options;

// --- Parse args ---
string? clientSecret = null;
string? flagName = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--secret" when i + 1 < args.Length:
            clientSecret = args[++i];
            break;
        case "--flag" when i + 1 < args.Length:
            flagName = args[++i];
            break;
    }
}

clientSecret ??= Environment.GetEnvironmentVariable("CONFIDENCE_CLIENT_SECRET");

if (string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(flagName))
{
    Console.WriteLine("Usage: dotnet run -- --secret <client-secret> --flag <flag-name>");
    Console.WriteLine("  or set CONFIDENCE_CLIENT_SECRET env var and pass --flag <flag-name>");
    return 1;
}

Console.WriteLine("=== Confidence .NET SDK Telemetry Demo ===\n");

// --- Intercepting handler to capture telemetry headers ---
var interceptor = new TelemetryInterceptor();

var options = new ConfidenceOptions
{
    ClientSecret = clientSecret,
    TimeoutSeconds = 30,
    LogLevel = LogLevel.Warning,
};

var client = new ConfidenceClient(options);

// Inject our intercepting handler via reflection so we can see the telemetry header
InjectInterceptor(client, interceptor);

var context = new ConfidenceContext(new Dictionary<string, object>
{
    { "targeting_key", "telemetry-demo-user" },
});

// --- Round 1: First resolve - should NOT have telemetry header (no data yet) ---
Console.WriteLine("[Round 1] First resolve call (no telemetry data accumulated yet)");
var result1 = await client.EvaluateBooleanFlagAsync(flagName, false, context);
PrintResult("Round 1", result1);
PrintTelemetryHeader("Round 1", interceptor.LastTelemetryHeader);

// --- Round 2: Second resolve - SHOULD have telemetry header from Round 1 ---
Console.WriteLine("\n[Round 2] Second resolve call (should include telemetry from Round 1)");
var result2 = await client.EvaluateBooleanFlagAsync(flagName, false, context);
PrintResult("Round 2", result2);
PrintTelemetryHeader("Round 2", interceptor.LastTelemetryHeader);

// --- Round 3: Rapid-fire 5 resolves to accumulate more telemetry ---
Console.WriteLine("\n[Round 3] Rapid-fire 5 resolves to accumulate traces...");
for (int i = 0; i < 5; i++)
{
    await client.EvaluateBooleanFlagAsync(flagName, false, context);
}
Console.WriteLine("  ...done. Next resolve will carry 5 latency + 5 eval traces.");

Console.WriteLine("\n[Round 4] Resolve with accumulated batch telemetry");
var result4 = await client.EvaluateBooleanFlagAsync(flagName, false, context);
PrintResult("Round 4", result4);
PrintTelemetryHeader("Round 4", interceptor.LastTelemetryHeader);

// --- Round 5: Event tracking - should NOT have telemetry header ---
Console.WriteLine("\n[Round 5] Event tracking (telemetry header should NOT be on events)");
interceptor.LastTelemetryHeader = null;
await client.TrackAsync("telemetry-demo-event", new Dictionary<string, object>
{
    { "demo", true },
});
PrintTelemetryHeader("Round 5 (event)", interceptor.LastTelemetryHeader);

// --- Round 6: Non-existent flag (tests error path telemetry) ---
Console.WriteLine("\n[Round 6] Resolve non-existent flag (error path telemetry)");
var result6 = await client.EvaluateBooleanFlagAsync("this-flag-does-not-exist-12345", false, context);
PrintResult("Round 6", result6);

Console.WriteLine("\n[Round 7] Resolve after error (should carry error eval trace)");
var result7 = await client.EvaluateBooleanFlagAsync(flagName, false, context);
PrintResult("Round 7", result7);
PrintTelemetryHeader("Round 7", interceptor.LastTelemetryHeader);

// --- Disabled telemetry demo ---
Console.WriteLine("\n[Round 8] Telemetry disabled (header should never appear)");
var disabledOptions = new ConfidenceOptions
{
    ClientSecret = clientSecret,
    TimeoutSeconds = 30,
    LogLevel = LogLevel.Warning,
    TelemetryDisabled = true,
};
var disabledClient = new ConfidenceClient(disabledOptions);
var disabledInterceptor = new TelemetryInterceptor();
InjectInterceptor(disabledClient, disabledInterceptor);

await disabledClient.EvaluateBooleanFlagAsync(flagName, false, context);
await disabledClient.EvaluateBooleanFlagAsync(flagName, false, context);
PrintTelemetryHeader("Round 8 (disabled)", disabledInterceptor.LastTelemetryHeader);

Console.WriteLine("\n=== Demo complete! ===");
return 0;

// --- Helpers ---

static void PrintResult<T>(string label, EvaluationResult<T> result)
{
    if (result.IsSuccess)
    {
        Console.WriteLine($"  [{label}] Value={result.Value}, Reason={result.Reason}, Variant={result.Variant}");
    }
    else
    {
        Console.WriteLine($"  [{label}] FAILURE: {result.ErrorMessage}");
    }
}

static void PrintTelemetryHeader(string label, string? header)
{
    if (header == null)
    {
        Console.WriteLine($"  [{label}] X-CONFIDENCE-TELEMETRY: (none)");
    }
    else
    {
        var bytes = Convert.FromBase64String(header);
        Console.WriteLine($"  [{label}] X-CONFIDENCE-TELEMETRY: {header}");
        Console.WriteLine($"  [{label}]   (base64 decoded to {bytes.Length} bytes of protobuf)");
    }
}

static void InjectInterceptor(ConfidenceClient client, TelemetryInterceptor interceptor)
{
    var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

    // Get the existing resolve client's handler chain and wrap it
    var resolveField = typeof(ConfidenceClient).GetField("_resolveClient", flags)!;
    var trackingField = typeof(ConfidenceClient).GetField("_trackingClient", flags)!;

    var oldResolve = (HttpClient)resolveField.GetValue(client)!;
    var oldTracking = (HttpClient)trackingField.GetValue(client)!;

    // Create new HttpClients with our interceptor wrapping the original handler
    var newResolve = new HttpClient(new InterceptingHandler(interceptor, new HttpClientHandler()))
    {
        BaseAddress = oldResolve.BaseAddress,
        Timeout = oldResolve.Timeout,
    };
    var newTracking = new HttpClient(new InterceptingHandler(interceptor, new HttpClientHandler()))
    {
        BaseAddress = oldTracking.BaseAddress,
        Timeout = oldTracking.Timeout,
    };

    resolveField.SetValue(client, newResolve);
    trackingField.SetValue(client, newTracking);
}

// --- Intercepting handler ---

class TelemetryInterceptor
{
    public string? LastTelemetryHeader { get; set; }
}

class InterceptingHandler : DelegatingHandler
{
    private readonly TelemetryInterceptor _interceptor;

    public InterceptingHandler(TelemetryInterceptor interceptor, HttpMessageHandler inner) : base(inner)
    {
        _interceptor = interceptor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _interceptor.LastTelemetryHeader = request.Headers.TryGetValues("X-CONFIDENCE-TELEMETRY", out var values)
            ? values.FirstOrDefault()
            : null;

        return base.SendAsync(request, cancellationToken);
    }
}
