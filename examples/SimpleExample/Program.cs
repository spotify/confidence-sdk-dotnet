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

        Console.WriteLine("\n=== Standard Flag Evaluation ===");
        Console.WriteLine("Trying with enabled visitor...");
        await EvaluateAndPrintFlag(confidenceClient, enabledContext, logger);

        // Then try with disabled visitor
        var disabledContext = new ConfidenceContext(new Dictionary<string, object>
        {
            { "visitor_id", "disabled_value_visitor" }
        });

        Console.WriteLine("\nTrying with disabled visitor...");
        await EvaluateAndPrintFlag(confidenceClient, disabledContext, logger);

        // Demo dot-notation feature
        Console.WriteLine("\n=== NEW: Dot-Notation Feature Demo ===");
        await DemoDotNotationFeature(confidenceClient, enabledContext, logger);

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
    var flag = await client.EvaluateJsonFlagAsync("hawkflag", new Dictionary<string, object>(), context);
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

static async Task DemoDotNotationFeature(ConfidenceClient client, ConfidenceContext context, ILogger logger)
{
    Console.WriteLine("🎯 Demonstrating Dot-Notation Feature");
    Console.WriteLine("=====================================");
    Console.WriteLine("With dot-notation, you can directly access nested properties in complex flags!");
    Console.WriteLine();

    try
    {
        // First, get the entire flag structure for comparison
        var fullFlag = await client.EvaluateJsonFlagAsync("hawkflag", new Dictionary<string, object>(), context);
        Console.WriteLine("📋 Full flag structure:");
        Console.WriteLine($"   {System.Text.Json.JsonSerializer.Serialize(fullFlag.Value, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}");
        Console.WriteLine();

        // Now demonstrate dot-notation: extract individual properties directly
        Console.WriteLine("🔍 Using dot-notation to extract specific properties:");

        // Extract color directly using dot-notation
        var colorResult = await client.EvaluateStringFlagAsync("hawkflag.color", "defaultColor", context);
        Console.WriteLine($"   hawkflag.color = \"{colorResult.Value}\" (reason: {colorResult.Reason})");

        // Extract message directly using dot-notation
        var messageResult = await client.EvaluateStringFlagAsync("hawkflag.message", "defaultMessage", context);
        Console.WriteLine($"   hawkflag.message = \"{messageResult.Value}\" (reason: {messageResult.Reason})");

        Console.WriteLine();
        Console.WriteLine("✨ Benefits of dot-notation:");
        Console.WriteLine("   • Type-safe: Get the exact type you need (string, bool, number)");
        Console.WriteLine("   • Clean code: No manual dictionary navigation");
        Console.WriteLine("   • Default values: Built-in fallback if property doesn't exist");
        Console.WriteLine("   • Performance: Direct extraction without deserializing entire structure");

        // Demo with hypothetical nested structure
        Console.WriteLine();
        Console.WriteLine("🏗️  Example with deeper nesting (would work with appropriate flag structure):");
        Console.WriteLine("   await client.EvaluateBooleanFlagAsync(\"user-settings.preferences.darkMode\", false, context);");
        Console.WriteLine("   await client.EvaluateNumericFlagAsync(\"app-config.performance.cacheTimeout\", 1000.0, context);");
        Console.WriteLine("   await client.EvaluateStringFlagAsync(\"ui-config.theme.primaryColor\", \"#000000\", context);");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error demonstrating dot-notation feature");
        Console.WriteLine($"❌ Error: {ex.Message}");
    }
}
