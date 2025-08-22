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
    var resolverClientSecret = Environment.GetEnvironmentVariable("CONFIDENCE_RESOLVER_CLIENT_SECRET");

    if (string.IsNullOrEmpty(clientId))
    {
        throw new InvalidOperationException("CONFIDENCE_CLIENT_ID environment variable is required. Please set it in your .env file.");
    }

    if (string.IsNullOrEmpty(clientSecret))
    {
        throw new InvalidOperationException("CONFIDENCE_CLIENT_SECRET environment variable is required. Please set it in your .env file.");
    }

    logger.LogInformation("Loaded credentials from environment variables");
    
    if (!string.IsNullOrEmpty(resolverClientSecret))
    {
        logger.LogInformation("Using separate client secret for resolver operations");
    }

    // Create the local provider with embedded WASM resource (rust_guest.wasm)
    var localProvider = new ConfidenceLocalProvider(
        clientId: clientId,
        clientSecret: clientSecret,
        resolverClientSecret: resolverClientSecret,
        logger: loggerFactory.CreateLogger<ConfidenceLocalProvider>());

    // Set the provider - this will trigger initialization
    try 
    {
        logger.LogInformation("Setting provider and starting initialization...");
        await Api.Instance.SetProviderAsync(localProvider);
        logger.LogInformation("✅ Provider initialization completed successfully!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Provider initialization failed: {Message}", ex.Message);
        logger.LogError("Please check the provider configuration and logs above for more details.");
        return 1; // Exit with error code since provider failed to initialize
    }

    // Get the OpenFeature client (only proceed if provider initialized successfully)
    var client = Api.Instance.GetClient();

    // Create evaluation context
    var context = EvaluationContext.Builder()
        .SetTargetingKey("user123")
        .Set("user_id", "nicklas")
        .Set("age", 25)
        .Build();

    logger.LogInformation("Evaluating flags with context: targeting_key=user123, country=SE, premium=true, age=25");

    // Evaluate different types of flags
    var jsonFlag = await client.GetObjectDetailsAsync("recommendations", new OpenFeature.Model.Value(), context);
    logger.LogInformation("JSON flag 'recommendations': {Value} (variant: {Variant}, reason: {Reason})", 
        jsonFlag.Value, jsonFlag.Variant, jsonFlag.Reason);

/*
    var stringFlag = await client.GetStringDetailsAsync("tutorial-feature.title", "Hello World", context);
    logger.LogInformation("String flag 'tutorial-feature.title': {Value} (variant: {Variant}, reason: {Reason})", 
        stringFlag.Value, stringFlag.Variant, stringFlag.Reason);
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
    return 1; // Exit with error code
}

logger.LogInformation("Local Provider Example completed");
return 0; // Success exit code
