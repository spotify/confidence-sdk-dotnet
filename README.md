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
var booleanResult = await client.EvaluateBooleanFlagAsync("my-flag.enabled", context);
var jsonResult = await client.EvaluateJsonFlagAsync("my-flag", context);

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

### Native SDK Features
- **Direct API access** - Full control over Confidence-specific features
- **Event tracking** - Built-in analytics and event collection
- **Advanced configuration** - Custom retry policies and regional endpoints
- **Type-safe flag evaluation** - Boolean, string, numeric, and JSON support
- **Async/await support** - Non-blocking operations
- **Thread-safe implementation** - Safe for concurrent use

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