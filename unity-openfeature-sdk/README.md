# Unity OpenFeature SDK

⚠️ **EXPERIMENTAL** - This SDK is currently in experimental phase and is subject to change.

A Unity-compatible implementation of the OpenFeature specification for feature flag management in Unity games and applications.

## Overview

This SDK provides a Unity wrapper around the OpenFeature standard, enabling developers to use feature flags in their Unity projects with any OpenFeature-compatible provider.

## Installation

Add the package to your Unity project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.unity.openfeature": "https://github.com/spotify/confidence-sdk-dotnet.git?path=unity-openfeature-sdk",
    "com.unity.nuget.newtonsoft-json": "3.0.2"
  }
}
```

## Features

- Unity-compatible OpenFeature implementation
- Support for boolean, string, number, and object flag types
- Asynchronous flag evaluation
- Unity coroutine support
- Provider-agnostic design
- Built-in InMemory provider for testing

## Basic Usage

```csharp
using UnityOpenFeature.Core;
using UnityOpenFeature.Providers;

// Set up a provider (InMemory for testing)
var provider = new InMemoryProvider();
provider.SetFlag("feature-enabled", true);
provider.SetFlag("max-players", 10);

// Initialize OpenFeature API
OpenFeatureAPI.Instance.SetProvider(provider);
var client = OpenFeatureAPI.Instance.GetClient();

// Evaluate flags
bool featureEnabled = client.GetBooleanValue("feature-enabled", false);
int maxPlayers = client.GetIntegerValue("max-players", 5);
```

## Using with Providers

This SDK is designed to work with OpenFeature providers. For Confidence platform integration, also install the Unity Confidence Provider:

```json
{
  "dependencies": {
    "com.unity.openfeature": "https://github.com/spotify/confidence-sdk-dotnet.git?path=unity-openfeature-sdk",
    "com.spotify.confidence.unity": "https://github.com/spotify/confidence-sdk-dotnet.git?path=unity-confidence-provider"
  }
}
```

## Testing and Compilation

To test if the SDK compiles correctly with Unity:

```bash
cd unity-openfeature-sdk
./build-test.sh
```

Ensure Unity 2022.3.62f1 is installed at `/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity`

## Package Structure

```
unity-openfeature-sdk/
├── Runtime/
│   ├── Core/            # Core OpenFeature interfaces and classes
│   ├── Client/          # Feature client implementations
│   ├── Providers/       # InMemory provider for testing
│   └── Unity/           # Unity-specific utilities
├── package.json         # Unity package manifest
└── build-test.sh        # Compilation test script
```

## Dependencies

- Unity 2020.3+
- com.unity.nuget.newtonsoft-json: 3.0.2

## Contributing

This is an experimental SDK. Please report any issues or suggestions through the main repository's issue tracker.