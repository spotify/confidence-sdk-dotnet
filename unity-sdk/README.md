# Unity OpenFeature SDK

⚠️ **EXPERIMENTAL** - This SDK is currently in experimental phase and is subject to change.

A Unity-compatible implementation of the OpenFeature specification for feature flag management in Unity games and applications.

## Overview

This SDK provides a Unity wrapper around the OpenFeature standard, enabling developers to use feature flags in their Unity projects. It integrates with the Confidence platform for flag evaluation and management.

## Usage
The unity-sdk can be added to the package.json file like:
https://github.com/spotify/confidence-sdk-dotnet.git?path=unity-sdk

## Features

- Unity-compatible OpenFeature implementation
- Confidence provider integration
- Support for boolean, string, number, and object flag types
- Asynchronous flag evaluation
- Unity coroutine support
- Event tracking with buffered, batched delivery

## Testing and Compilation

To test if the SDK compiles correctly with Unity:

1. Ensure Unity 2022.3.62f1 is installed at `/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity`

## Package Structure

```
unity-sdk/
├── Runtime/
│   ├── Client/           # Feature client implementations
│   ├── Core/            # Core OpenFeature interfaces and classes
│   ├── Providers/       # Provider implementations (Confidence, InMemory)
│   └── Unity/           # Unity-specific utilities
├── package.json         # Unity package manifest
└── build-test.sh        # Compilation test script
```

## Usage

1. Add the package to your Unity project's `Packages/manifest.json`:
   ```json
   {
     "dependencies": {
       "com.unity.openfeature": "file:../path/to/unity-sdk",
       "com.unity.modules.unitywebrequest": "1.0.0"
     }
   }
   ```

2. Initialize the provider in your scripts:
   ```csharp
   using UnityOpenFeature.Providers;
   using UnityOpenFeature.Core;

   var provider = new ConfidenceProvider("your-client-secret");
   OpenFeatureAPI.Instance.SetProvider(provider);
   var client = OpenFeatureAPI.Instance.GetClient();

   // Use the client to evaluate flags
   var flagValue = client.GetBooleanValue("my-feature-flag", false);
   ```

   To use a custom resolver base URL for both resolve and apply logging requests:
   ```csharp
   var provider = new ConfidenceProvider(
       "your-client-secret",
       "https://confidence-proxy.example.com/resolver");
   ```
   The SDK appends `v1/flags:resolve` and `v1/flags:apply`.

## Event tracking

Track an event through the client. `TrackingEventDetails` is immutable and is
assembled with a builder, mirroring the OpenFeature .NET SDK:

```csharp
using UnityOpenFeature.Core;

client.Track("checkout_completed", TrackingEventDetails.Builder()
    .SetValue(42.0)                  // the event's predefined numeric value
    .Set("currency", "SEK")
    .Set("items", 3)
    .Build());

// Or, for an event with no details:
client.Track("app_opened", TrackingEventDetails.Empty);
```

The evaluation context's targeting key and attributes are attached
automatically under a reserved `context` key in the published payload. An
attribute named `context` in the event details is therefore dropped rather
than allowed to overwrite it.

### Buffering and delivery

Tracked events are buffered in memory and published in batches. Two things are
worth knowing before relying on them:

- **Buffer size is capped at 100 events.** A flush is triggered automatically
  once the buffer reaches 80% of that, so under normal throughput it drains
  before filling.
- **If the buffer is genuinely full, the incoming event is dropped.** The
  already-buffered events are kept, since they have waited longest and are
  closest to being delivered. Drops are counted in
  `ConfidenceApiClient.DroppedEventCount` and a rate-limited warning is logged
  (on the first drop, then every 100). A non-zero `DroppedEventCount` means
  events were lost and never published — treat it as a signal that events are
  being produced faster than they can be delivered.
- **Buffered events and flag applies are flushed on application pause, focus
  loss and quit**, so backgrounding does not silently discard them. Delivery is
  still best-effort: a process killed outright, or an unreachable backend, will
  lose whatever is still buffered.

## Dependencies

- Unity 2020.3+
- com.unity.nuget.newtonsoft-json: 3.0.2
- UnityWebRequest 1.0.0 (Builtin)


## Compile check

Run the build test script from the unity-sdk directory:
   ```bash
   ./build-test.sh
   ```

The script will:
- Create a temporary Unity test project
- Add the Unity OpenFeature package as a local dependency
- Attempt to build the project
- Report success/failure and any warnings or errors
- Clean up the temporary project

### Expected Output

- ✅ BUILD SUCCESSFUL! - SDK compiled without errors
- ❌ BUILD FAILED! - Compilation errors found with details

## Contributing

This is an experimental SDK. Please report any issues or suggestions through the main repository's issue tracker.
