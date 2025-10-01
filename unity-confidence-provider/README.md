# Unity Confidence Provider

⚠️ **EXPERIMENTAL** - This provider is currently in experimental phase and is subject to change.

A Confidence platform provider for the Unity OpenFeature SDK, enabling Unity games and applications to integrate with Spotify's Confidence feature flag platform.

## Overview

This provider implements the OpenFeature specification for the Confidence platform, providing feature flag evaluation, analytics tracking, and real-time configuration management for Unity projects.

## Prerequisites

This package requires the Unity OpenFeature SDK. Install both packages:

```json
{
  "dependencies": {
    "com.unity.openfeature": "https://github.com/spotify/confidence-sdk-dotnet.git?path=unity-openfeature-sdk",
    "com.spotify.confidence.unity": "https://github.com/spotify/confidence-sdk-dotnet.git?path=unity-confidence-provider",
    "com.unity.modules.unitywebrequest": "1.0.0"
  }
}
```

## Quick Start

```csharp
using UnityOpenFeature.Core;
using UnityOpenFeature.Providers;

// Initialize Confidence provider
var provider = new ConfidenceProvider("your-client-secret");
OpenFeatureAPI.Instance.SetProvider(provider);

// Set evaluation context
var context = new EvaluationContext("user-123")
    .SetAttribute("country", "SE")
    .SetAttribute("premium", true);
OpenFeatureAPI.Instance.SetEvaluationContext(context);

// Get the client and evaluate flags
var client = OpenFeatureAPI.Instance.GetClient();
bool newFeatureEnabled = client.GetBooleanValue("new-feature.enabled", false);
```

## Features

- **Confidence Platform Integration**: Direct connection to Confidence API
- **Dot-notation Support**: Access nested flag properties like `"flag.property.subproperty"`
- **Automatic Analytics**: Built-in flag application tracking
- **Unity Coroutines**: Async operations using Unity's coroutine system
- **Error Handling**: Comprehensive error handling and logging
- **Checkpointing**: Automatic batched analytics submission

## Configuration

### Getting a Client Secret

1. Sign up for Confidence at [confidence.spotify.com](https://confidence.spotify.com)
2. Create a new project
3. Generate a client secret from your project settings

### Provider Initialization

```csharp
// Basic initialization
var provider = new ConfidenceProvider("your-client-secret");

// With callback handling
provider.Initialize((success, error) => {
    if (success) {
        Debug.Log("Confidence provider initialized successfully");
    } else {
        Debug.LogError($"Failed to initialize: {error}");
    }
});
```

## Advanced Usage

### Dot-notation for Nested Properties

```csharp
// Access nested properties directly
bool darkMode = client.GetBooleanValue("ui-config.theme.darkMode", false);
int timeout = client.GetIntegerValue("api-config.timeout", 5000);
string welcomeMessage = client.GetStringValue("messages.welcome.text", "Hello!");
```

### Manual Flag Application

```csharp
var provider = new ConfidenceProvider("your-client-secret");
// Flags are automatically applied when evaluated
// Manual application is handled internally
```

## Testing and Compilation

Test the provider compilation:

```bash
cd unity-confidence-provider
./build-test.sh
```

## Package Structure

```
unity-confidence-provider/
├── Runtime/
│   ├── ConfidenceProvider.cs      # Main provider implementation
│   ├── ConfidenceApiClient.cs     # HTTP client for Confidence API
│   └── ObjectDictWrapper.cs       # Utility for object handling
├── package.json                   # Unity package manifest
└── build-test.sh                  # Compilation test script
```

## Dependencies

- Unity 2020.3+
- Unity OpenFeature SDK
- UnityWebRequest (builtin)

## Error Handling

The provider includes comprehensive error handling:

- Network failures gracefully fallback to default values
- Invalid client secrets are logged with clear error messages
- Flag parsing errors return default values with detailed logging
- Automatic retry logic for transient failures

## Contributing

This is an experimental provider. Please report any issues or suggestions through the main repository's issue tracker.