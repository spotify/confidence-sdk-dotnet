using Microsoft.Extensions.Logging;
using OpenFeature;
using OpenFeature.Model;
using Spotify.Confidence.OpenFeature;
using Spotify.Confidence.Sdk;
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
    // Create the Confidence OpenFeature provider with logging configuration
    var confidenceOptions = new ConfidenceOptions
    {
        ClientSecret = clientKey,
        TimeoutSeconds = 30,
        LogLevel = LogLevel.Debug // Configure SDK logging level (defaults to Information)
    };
    
    var confidenceProvider = new ConfidenceProvider(confidenceOptions);
    
    // Set the provider in the OpenFeature API
    Api.Instance.SetProviderAsync(confidenceProvider).Wait();

    // Get the OpenFeature client - this is the standardized API!
    var client = Api.Instance.GetClient();

    Console.WriteLine("🚀 OpenFeature Example with Confidence Provider");
    Console.WriteLine("===============================================\n");

    // Demo 1: Boolean flag evaluation with different users
    Console.WriteLine("📊 Demo 1: Boolean Flag Evaluation");
    await DemoBooleanFlag(client, logger);

    // Demo 2: String flag evaluation
    Console.WriteLine("\n📝 Demo 2: String Flag Evaluation");
    await DemoStringFlag(client, logger);

    // Demo 3: Structured flag evaluation (JSON)
    Console.WriteLine("\n🏗️  Demo 3: Structured Flag Evaluation");
    await DemoStructuredFlag(client, logger);

    Console.WriteLine("\n✅ All demos completed successfully!");
    return 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred");
    return 1;
}

static async Task DemoBooleanFlag(IFeatureClient client, ILogger logger)
{
    var contexts = new[]
    {
        ("enabled_user", EvaluationContext.Builder()
            .SetTargetingKey("enabled_value_visitor")
            .Set("visitor_id", "enabled_value_visitor")
            .Build()),
        ("disabled_user", EvaluationContext.Builder()
            .SetTargetingKey("disabled_value_visitor")
            .Set("visitor_id", "disabled_value_visitor")
            .Build())
    };

    foreach (var (userName, context) in contexts)
    {
        // Get the structured flag first
        var structureDetails = await client.GetObjectDetailsAsync("hawkflag", new Value(), context);
        var structure = structureDetails.Value.AsStructure;

        // Extract the enabled value from the structure (handle case where flag doesn't match)
        var isEnabled = false;
        if (structure != null && structure.ContainsKey("enabled"))
        {
            isEnabled = structure.GetValue("enabled")?.AsBoolean ?? false;
        }

        Console.WriteLine($"  👤 User: {userName}");
        Console.WriteLine($"     Enabled: {isEnabled}");
        Console.WriteLine($"     Variant: {structureDetails.Variant}");
        Console.WriteLine($"     Reason: {structureDetails.Reason}");
        Console.WriteLine();
    }
}

static async Task DemoStringFlag(IFeatureClient client, ILogger logger)
{
    var context = EvaluationContext.Builder()
        .SetTargetingKey("enabled_value_visitor")
        .Set("visitor_id", "enabled_value_visitor")
        .Build();

    // Get the structured flag first, then extract string values
    var structureDetails = await client.GetObjectDetailsAsync("hawkflag", new Value(), context);
    var structure = structureDetails.Value.AsStructure;

    var color = "gray";
    var message = "default";

    if (structure != null && structure.ContainsKey("color"))
    {
        color = structure.GetValue("color")?.AsString ?? "gray";
    }
    if (structure != null && structure.ContainsKey("message"))
    {
        message = structure.GetValue("message")?.AsString ?? "default";
    }

    Console.WriteLine($"  🎨 Color: {color}");
    Console.WriteLine($"  💬 Message: {message}");
    Console.WriteLine($"  📊 Variant: {structureDetails.Variant}");
    Console.WriteLine($"  🔍 Reason: {structureDetails.Reason}");

    // Display with colors (same as original example)
    var colorCode = color switch
    {
        "Yellow" => "\u001b[33m",
        "Green" => "\u001b[32m",
        _ => "\u001b[31m"
    };
    const string colorReset = "\u001b[0m";

    Console.WriteLine($"  {colorCode}🎯 Styled Message: {message}{colorReset}");
}

static async Task DemoStructuredFlag(IFeatureClient client, ILogger logger)
{
    var context = EvaluationContext.Builder()
        .SetTargetingKey("enabled_value_visitor")
        .Set("visitor_id", "enabled_value_visitor")
        .Build();

    // Get the entire flag as a structured value
    var structureDetails = await client.GetObjectDetailsAsync("hawkflag", new Value(), context);
    var structure = structureDetails.Value.AsStructure;

    Console.WriteLine("  📦 Complete Flag Structure:");
    if (structure != null && structure.Count > 0)
    {
        Console.WriteLine($"     Color: {(structure.ContainsKey("color") ? structure.GetValue("color")?.AsString ?? "N/A" : "N/A")}");
        Console.WriteLine($"     Message: {(structure.ContainsKey("message") ? structure.GetValue("message")?.AsString ?? "N/A" : "N/A")}");
        Console.WriteLine($"     Enabled: {(structure.ContainsKey("enabled") ? structure.GetValue("enabled")?.AsBoolean ?? false : false)}");
    }
    else
    {
        Console.WriteLine("     No flag values returned (flag didn't match targeting criteria)");
    }
    Console.WriteLine($"     Variant: {structureDetails.Variant}");
    Console.WriteLine($"     Reason: {structureDetails.Reason}");
}

