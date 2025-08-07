using Microsoft.Extensions.Logging;
using Spotify.Confidence.Sdk;
using Spotify.Confidence.Sdk.Exceptions;
using Spotify.Confidence.Sdk.Models;
using Spotify.Confidence.Sdk.Options;

// Set up logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Debug)
        .AddJsonConsole(options => options.IncludeScopes = true);
});

var logger = loggerFactory.CreateLogger<Program>();

// Read the client secret from env
var clientKey = Environment.GetEnvironmentVariable("CONFIDENCE_DOTNET_CLIENT");
if (string.IsNullOrEmpty(clientKey))
{
    Console.WriteLine("No client key defined");
    return 1;
}

try
{
    // Create and configure the Confidence client with region-based configuration
    var confidenceOptions = new ConfidenceOptions
    {
        ClientSecret = clientKey,
        TimeoutSeconds = 30,
        LogLevel = LogLevel.Information // Configure SDK logging level (defaults to Information)
    };
    
    var confidenceClient = new ConfidenceClient(confidenceOptions);
    // Note: SDK will create its own console logger with the configured LogLevel
    // You can still pass your own logger if you want custom formatting or destinations

    try
    {
        // First try with enabled visitor
        var enabledContext = new ConfidenceContext(new Dictionary<string, object>
        {
            { "visitor_id", "enabled_value_visitor" }
        });

        Console.WriteLine("\nTrying with enabled visitor...");
        await EvaluateAndPrintFlag(confidenceClient, enabledContext, logger);

        // Then try with disabled visitor
        var disabledContext = new ConfidenceContext(new Dictionary<string, object>
        {
            { "visitor_id", "disabled_value_visitor" }
        });

        Console.WriteLine("\nTrying with disabled visitor...");
        await EvaluateAndPrintFlag(confidenceClient, disabledContext, logger);

        // Track an event similar to the Go demo
        Console.WriteLine("\nTracking an event...");
        await confidenceClient.TrackAsync("checkout-complete", new Dictionary<string, object>
        {
            { "orderId", 1234 },
            { "total", 100.0 },
            { "items", new[] { "item1", "item2" } }
        });
        Console.WriteLine("Event sent");

        return 0;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred");
        return 1;
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred");
    return 1;
}

static async Task EvaluateAndPrintFlag(ConfidenceClient client, ConfidenceContext context, ILogger logger)
{
    // Get the flag value with the context
    var flag = await client.EvaluateJsonFlagAsync("hawkflag", context);
    logger.LogInformation("Flag value: {Value}", flag.Value);

    var structure = flag.Value as Dictionary<string, object>;
    var colorValue = structure?.GetValueOrDefault("color")?.ToString() ?? "defaultColor";
    var messageValue = structure?.GetValueOrDefault("message")?.ToString() ?? "defaultValue";

    // ANSI color codes
    const string colorYellow = "\u001b[33m";
    const string colorGreen = "\u001b[32m";
    const string colorRed = "\u001b[31m";
    const string colorDefault = "\u001b[0m";

    Console.WriteLine($" Color --> {colorValue}");

    var colorCode = colorValue switch
    {
        "Yellow" => colorYellow,
        "Green" => colorGreen,
        _ => colorRed
    };

    Console.WriteLine($"{colorCode}Message --> {messageValue}{colorDefault}");
}
