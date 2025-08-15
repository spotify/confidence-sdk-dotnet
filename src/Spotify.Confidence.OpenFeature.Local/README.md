# Spotify.Confidence.OpenFeature.Local

A local OpenFeature provider for the Confidence SDK that resolves flags locally using WebAssembly without network calls.

## Overview

This provider enables local flag evaluation by loading a WebAssembly module that contains the flag resolution logic. This is similar to the approach used in the [confidence-sdk-java local provider](https://github.com/spotify/confidence-sdk-java/tree/main/openfeature-provider-local).

## Features

- 🚀 **Local Evaluation**: No network calls required - all flag resolution happens locally
- 🔧 **WASM Integration**: Uses Wasmtime .NET for loading and executing WebAssembly modules
- 📊 **OpenFeature Compatible**: Implements the standard OpenFeature provider interface
- 🏗️ **Type Safe**: Supports all OpenFeature value types (boolean, string, integer, double, structure)
- 📝 **Logging Support**: Comprehensive logging with high-performance LoggerMessage delegates
- 🎯 **Context Support**: Full evaluation context support for targeting

## Installation

```shell
dotnet add package Spotify.Confidence.OpenFeature.Local
```

## Usage

```csharp
using OpenFeature;
using OpenFeature.Model;
using Spotify.Confidence.OpenFeature.Local;

// Create the local provider with embedded WASM resolver (rust_guest.wasm)
var localProvider = new ConfidenceLocalProvider(
    clientId: "your-client-id",
    clientSecret: "your-client-secret");

// Set the provider
await Api.Instance.SetProviderAsync(localProvider);

// Get the OpenFeature client
var client = Api.Instance.GetClient();

// Create evaluation context
var context = EvaluationContext.Builder()
    .SetTargetingKey("user123")
    .Set("country", "SE")
    .Set("premium", true)
    .Build();

// Evaluate flags
var booleanFlag = await client.GetBooleanValueAsync("my-flag.enabled", false, context);
var stringFlag = await client.GetStringValueAsync("my-flag.color", "blue", context);
var intFlag = await client.GetIntegerValueAsync("my-flag.limit", 100, context);
var doubleFlag = await client.GetDoubleValueAsync("my-flag.ratio", 0.5, context);

// Clean up when done
localProvider.Dispose();
```

### Request/Response Format

The WASM module receives requests as JSON with this structure:

```json
{
  "flag": "my-flag.enabled",
  "context": {
    "targeting_key": "user123",
    "country": "SE",
    "premium": true
  },
  "clientId": "your-client-id",
  "clientSecret": "your-client-secret"
}
```

And should return responses as JSON:

```json
{
  "value": true,
  "variant": "variant-name",
  "reason": "RESOLVED",
  "success": true,
  "error": null
}
```

## Architecture

```
┌─────────────────────────────────────┐
│     ConfidenceLocalProvider         │
│  (OpenFeature Provider Interface)   │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│           WasmResolver              │
│    (WASM Module Wrapper)            │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│         Wasmtime Engine             │
│      (WASM Runtime)                 │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│       Your WASM Module              │
│    (Flag Resolution Logic)          │
└─────────────────────────────────────┘
```

## Dependencies

- **Wasmtime**: WebAssembly runtime for .NET
- **OpenFeature**: Feature flag SDK standard
- **System.Text.Json**: JSON serialization for WASM communication
- **Microsoft.Extensions.Logging**: Logging infrastructure

## Error Handling

The provider gracefully handles various error conditions:

- **WASM Module Loading Errors**: If the WASM file cannot be loaded, the provider will return default values
- **Function Call Errors**: If WASM functions fail, appropriate error responses are returned
- **Memory Management**: Automatic cleanup of WASM memory allocations
- **Type Conversion Errors**: Safe conversion between WASM and OpenFeature types

## Performance Considerations

- WASM modules are loaded once during provider initialization
- Memory allocations in WASM are managed automatically
- JSON serialization is optimized for the request/response cycle
- High-performance logging delegates minimize overhead

## Development

To extend or modify the local provider:

1. Clone the repository
2. Make your changes to the source code
3. Run tests: `dotnet test`
4. Build: `dotnet build`
