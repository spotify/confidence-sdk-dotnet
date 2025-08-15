using Microsoft.Extensions.Logging;
using OpenFeature;
using OpenFeature.Model;
using Spotify.Confidence.OpenFeature.Local;
using DotNetEnv;

// Setup logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Debug)
        .AddConsole();
});

var logger = loggerFactory.CreateLogger<Program>();

try
{
    logger.LogInformation("Starting Local Provider Example");

    // Load environment variables from .env file
    Env.Load();

    // Get credentials from environment variables
    var clientId = Environment.GetEnvironmentVariable("CONFIDENCE_CLIENT_ID");
    var clientSecret = Environment.GetEnvironmentVariable("CONFIDENCE_CLIENT_SECRET");

    if (string.IsNullOrEmpty(clientId))
    {
        throw new InvalidOperationException("CONFIDENCE_CLIENT_ID environment variable is required. Please set it in your .env file.");
    }

    if (string.IsNullOrEmpty(clientSecret))
    {
        throw new InvalidOperationException("CONFIDENCE_CLIENT_SECRET environment variable is required. Please set it in your .env file.");
    }

    logger.LogInformation("Loaded credentials from environment variables");

    // Create the local provider with embedded WASM resource (rust_guest.wasm)
    var localProvider = new ConfidenceLocalProvider(
        clientId: clientId,
        clientSecret: clientSecret,
        logger: loggerFactory.CreateLogger<ConfidenceLocalProvider>());

    // Set the provider
    await Api.Instance.SetProviderAsync(localProvider);
    logger.LogInformation("Local provider set successfully");

    // Get the OpenFeature client
    var client = Api.Instance.GetClient();

    // Create evaluation context
    var context = EvaluationContext.Builder()
        .SetTargetingKey("user123")
        .Set("country", "SE")
        .Set("premium", true)
        .Set("age", 25)
        .Build();

    logger.LogInformation("Evaluating flags with context: targeting_key=user123, country=SE, premium=true, age=25");

    // Evaluate different types of flags
    var booleanFlag = await client.GetBooleanDetailsAsync("feature.newUI", false, context);
    logger.LogInformation("Boolean flag 'feature.newUI': {Value} (variant: {Variant}, reason: {Reason})", 
        booleanFlag.Value, booleanFlag.Variant, booleanFlag.Reason);
/*
    var stringFlag = await client.GetStringDetailsAsync("theme.color", "blue", context);
    logger.LogInformation("String flag 'theme.color': {Value} (variant: {Variant}, reason: {Reason})", 
        stringFlag.Value, stringFlag.Variant, stringFlag.Reason);

    var intFlag = await client.GetIntegerDetailsAsync("limits.maxItems", 10, context);
    logger.LogInformation("Integer flag 'limits.maxItems': {Value} (variant: {Variant}, reason: {Reason})", 
        intFlag.Value, intFlag.Variant, intFlag.Reason);

    var doubleFlag = await client.GetDoubleDetailsAsync("pricing.discount", 0.1, context);
    logger.LogInformation("Double flag 'pricing.discount': {Value} (variant: {Variant}, reason: {Reason})", 
        doubleFlag.Value, doubleFlag.Variant, doubleFlag.Reason);

    // Example with nested flag access
    var nestedFlag = await client.GetBooleanDetailsAsync("config.features.darkMode", false, context);
    logger.LogInformation("Nested flag 'config.features.darkMode': {Value} (variant: {Variant}, reason: {Reason})", 
        nestedFlag.Value, nestedFlag.Variant, nestedFlag.Reason);
*/
    // Clean up
    localProvider.Dispose();
    logger.LogInformation("Local provider disposed successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error in Local Provider Example");
    throw;
}

logger.LogInformation("Local Provider Example completed");
