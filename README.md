# Confidence SDK for .NET

This is the official .NET SDK for the Spotify Confidence platform. It provides seamless integration with the [OpenFeature](https://openfeature.dev/) standard, along with a native .NET SDK for more advanced use cases.

## Requirements

- .NET 8.0 or later
- .NET Framework 4.6.2 or later

## Installation

### OpenFeature Integration (Recommended)

For most applications, we recommend using the OpenFeature provider:

```shell
dotnet add package Spotify.Confidence.OpenFeature
dotnet add package OpenFeature
```

### Native SDK

For direct API access:

```shell
dotnet add package Spotify.Confidence.Sdk
```

## Quick Start with OpenFeature

The OpenFeature integration provides a standardized way to use feature flags that's compatible with other providers:

```csharp
using OpenFeature;
using OpenFeature.Model;
using Spotify.Confidence.OpenFeature;
using Spotify.Confidence.Sdk;
using Spotify.Confidence.Sdk.Options;

// Create and set the Confidence OpenFeature provider
var confidenceProvider = new ConfidenceProvider(new ConfidenceOptions
{
    ClientSecret = "your-client-secret",
    LogLevel = LogLevel.Debug // Configure logging level (optional)
});
await Api.Instance.SetProviderAsync(confidenceProvider);

// Get the OpenFeature client
var client = Api.Instance.GetClient();

// Create evaluation context
var context = EvaluationContext.Builder()
    .SetTargetingKey("user123")
    .Set("country", "SE")
    .Set("premium", true)
    .Build();

// Evaluate flags using the standard OpenFeature API
var booleanFlag = await client.GetBooleanDetailsAsync("my-flag.enabled", false, context);
var stringFlag = await client.GetStringDetailsAsync("my-flag.color", "blue", context);
var structuredFlag = await client.GetObjectDetailsAsync("my-flag", new Value(), context);

// NEW: Use dot-notation to access nested properties directly
var darkMode = await client.GetBooleanValueAsync("my-flag.preferences.darkMode", false, context);
var timeout = await client.GetDoubleValueAsync("my-flag.config.timeout", 5000.0, context);

Console.WriteLine($"Enabled: {booleanFlag.Value}");
Console.WriteLine($"Color: {stringFlag.Value}");
Console.WriteLine($"Variant: {booleanFlag.Variant}");
```

## Native SDK Usage

For advanced use cases where you need direct access to the Confidence API:

```csharp
using Spotify.Confidence.Sdk;
using Spotify.Confidence.Sdk.Options;

// Create client options
var options = new ConfidenceOptions
{
    ClientSecret = "your-client-secret",
    LogLevel = LogLevel.Debug // Configure logging level (optional)
};

// Create a client instance
var client = new ConfidenceClient(options);

// Create an evaluation context
var context = new ConfidenceContext(new Dictionary<string, object>
{
    { "targeting_key", "user123" },
    { "country", "SE" },
    { "premium", true }
});

// Evaluate flags
var booleanResult = await client.EvaluateBooleanFlagAsync("my-flag.enabled", false, context);
var jsonResult = await client.EvaluateJsonFlagAsync("my-flag", new {}, context);

// NEW: Use dot-notation to access nested properties directly
var darkMode = await client.EvaluateBooleanFlagAsync("my-flag.preferences.darkMode", false, context);
var timeout = await client.EvaluateNumericFlagAsync("my-flag.config.timeout", 5000.0, context);

Console.WriteLine($"Flag value: {booleanResult.Value}");

// Track events
await client.TrackAsync("button-clicked", new Dictionary<string, object>
{
    { "button_id", "checkout" },
    { "user_type", "premium" }
});
```

## Features

### OpenFeature Provider Features
- **Standard OpenFeature API** - Compatible with OpenFeature ecosystem
- **Type-safe evaluations** - Boolean, string, numeric, and structured data support
- **Provider switching** - Easy migration between feature flag providers
- **Vendor-agnostic** - Standard interface across different feature flag services
- **🎯 Dot-notation support** - Direct access to nested flag properties

### Native SDK Features
- **Direct API access** - Full control over Confidence-specific features
- **Event tracking** - Built-in analytics and event collection
- **Advanced configuration** - Custom retry policies and regional endpoints
- **Type-safe flag evaluation** - Boolean, string, numeric, and JSON support
- **Async/await support** - Non-blocking operations
- **Thread-safe implementation** - Safe for concurrent use
- **🎯 Dot-notation support** - Direct access to nested flag properties

## 🎯 Dot-Notation Feature

Confidence .NET SDK now supports **dot-notation** for accessing nested properties in complex flags. This powerful feature allows you to directly extract specific values from structured flags without manual navigation.

### What is Dot-Notation?

Dot-notation allows you to access nested properties using a simple string syntax like `"flag.property.subproperty"`. Instead of retrieving the entire flag structure and manually navigating through dictionaries, you can directly extract the value you need.

### Benefits

- **Type-safe extraction** - Get exactly the type you need (bool, string, double)
- **Cleaner code** - No manual dictionary navigation or null checking
- **Built-in defaults** - Automatic fallback values when properties don't exist
- **Better performance** - Direct extraction without deserializing entire structures
- **OpenFeature compatible** - Works with standard OpenFeature API methods

### Usage Examples

#### With Native SDK

```csharp
// Traditional approach (still supported)
var fullFlag = await client.EvaluateJsonFlagAsync("user-settings", new {}, context);
var dictionary = (Dictionary<string, object>)fullFlag.Value;
var darkMode = (bool)dictionary["preferences"]["darkMode"]; // Manual navigation

// NEW: Dot-notation approach
var darkMode = await client.EvaluateBooleanFlagAsync("user-settings.preferences.darkMode", false, context);
var timeout = await client.EvaluateNumericFlagAsync("app-config.performance.cacheTimeout", 5000.0, context);
var primaryColor = await client.EvaluateStringFlagAsync("ui-theme.colors.primary", "#000000", context);
```

#### With OpenFeature

```csharp
// Traditional approach (still supported)
var fullStructure = await client.GetObjectValueAsync("user-settings", new Value(), context);
var structure = fullStructure.AsStructure;
var darkMode = structure.GetValue("preferences")?.AsStructure?.GetValue("darkMode")?.AsBoolean ?? false;

// NEW: Dot-notation approach
var darkMode = await client.GetBooleanValueAsync("user-settings.preferences.darkMode", false, context);
var timeout = await client.GetDoubleValueAsync("app-config.performance.cacheTimeout", 5000.0, context);
var primaryColor = await client.GetStringValueAsync("ui-theme.colors.primary", "#000000", context);
```

### Supported Patterns

```csharp
// Simple property access
"flag.property"

// Deep nesting
"flag.level1.level2.level3.property"

// Real-world examples
"user-settings.preferences.darkMode"
"app-config.database.connectionTimeout"
"feature-toggles.experimental.newFeature.enabled"
"ui-theme.layout.sidebar.width"
```

### Error Handling

When using dot-notation, the SDK provides clear error handling:

```csharp
var result = await client.EvaluateBooleanFlagAsync("nonexistent.property", false, context);
// If property path doesn't exist:
// - Returns the default value (false)
// - result.IsSuccess = false
// - result.ErrorMessage contains details about the missing property
```

### Flag Structure Requirements

For dot-notation to work, your flags should contain structured data. Example flag structure:

```json
{
  "user-settings": {
    "value": {
      "preferences": {
        "darkMode": true,
        "language": "en",
        "notifications": {
          "email": true,
          "push": false
        }
      },
      "profile": {
        "name": "John Doe",
        "avatar": "https://example.com/avatar.jpg"
      }
    }
  }
}
```

With this structure, you can use:
- `"user-settings.preferences.darkMode"` → `true`
- `"user-settings.preferences.language"` → `"en"`
- `"user-settings.preferences.notifications.email"` → `true`
- `"user-settings.profile.name"` → `"John Doe"`

### Backward Compatibility

Dot-notation is completely backward compatible:
- All existing code continues to work unchanged
- You can mix traditional and dot-notation approaches
- No breaking changes to existing APIs

## Logging

The Confidence SDK includes comprehensive logging support using the standard .NET logging abstractions. This allows you to monitor SDK operations, debug issues, and integrate with your application's logging infrastructure.

### Default Logging Behavior

By default, the SDK logs at **Information** level and above, outputting to the console. When you create a client without providing a logger, the SDK automatically creates a console logger that respects the configured log level.

```csharp
var options = new ConfidenceOptions
{
    ClientSecret = "your-client-secret",
    LogLevel = LogLevel.Information // Default level
};

var client = new ConfidenceClient(options);
// SDK creates its own console logger with Information level
```

### Configuring Log Levels

You can control the verbosity of SDK logging by setting the `LogLevel` property in `ConfidenceOptions`:

```csharp
// For detailed debugging (includes HTTP requests, flag evaluations, etc.)
var options = new ConfidenceOptions
{
    ClientSecret = "your-client-secret",
    LogLevel = LogLevel.Debug
};

// For production (only warnings and errors)
var productionOptions = new ConfidenceOptions
{
    ClientSecret = "your-client-secret",
    LogLevel = LogLevel.Warning
};

// For minimal logging (only critical errors)
var minimalOptions = new ConfidenceOptions
{
    ClientSecret = "your-client-secret",
    LogLevel = LogLevel.Error
};
```

### Disabling Logging Completely

To disable logging entirely, set the log level to `None`:

```csharp
var options = new ConfidenceOptions
{
    ClientSecret = "your-client-secret",
    LogLevel = LogLevel.None // No logging output
};

var client = new ConfidenceClient(options);
```

### Using Custom Loggers

For advanced scenarios, you can provide your own logger instance to integrate with your application's logging infrastructure:

```csharp
var logger = new YourLogger();
var client = new ConfidenceClient(options, logger);
```

### OpenFeature Provider Logging

The OpenFeature provider also supports the same logging configuration:

```csharp
var options = new ConfidenceOptions
{
    ClientSecret = "your-client-secret",
    LogLevel = LogLevel.Debug // Enable debug logging
};

var provider = new ConfidenceProvider(options);
// Or with custom logger:
var provider = new ConfidenceProvider(options, customLogger);
```

### Log Level Guide

| Level | Description | Use Case |
|-------|-------------|----------|
| `Trace` | Not used by SDK | - |
| `Debug` | Detailed operational information | Development, troubleshooting |
| `Information` | General operational messages | Default for most applications |
| `Warning` | Potentially harmful situations | Production monitoring |
| `Error` | Error events that don't stop the application | Critical error tracking |
| `Critical` | Not used by SDK | - |
| `None` | No logging output | When logging is not needed |

### What Gets Logged

- **Information**: SDK initialization, provider lifecycle events
- **Debug**: Flag evaluations, HTTP requests, successful operations
- **Warning**: Non-critical failures (event tracking failures, flag not found)
- **Error**: Critical failures (API communication errors, parsing failures)

### Example Debug Output

When `LogLevel.Debug` is enabled, you'll see detailed logs like:

```
info: Spotify.Confidence.Sdk.ConfidenceClient[0]
      ConfidenceClient initialized with resolve URL: https://resolver.confidence.dev, timeout: 10s, max retries: 3

dbug: Spotify.Confidence.Sdk.ConfidenceClient[0]
      Resolving flag 'my-feature' of type Boolean with default value: False

dbug: Spotify.Confidence.Sdk.ConfidenceClient[0]
      Successfully resolved flag 'my-feature' with value: True, reason: MATCH, variant: treatment
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for details on how to contribute to this project.

## License

Apache 2.0 - See [LICENSE](LICENSE) for more information. 