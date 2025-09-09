# Unity OpenFeature SDK

⚠️ **EXPERIMENTAL** - This SDK is currently in experimental phase and is subject to change.

A Unity-compatible implementation of the OpenFeature specification for feature flag management in Unity games and applications.

## Overview

This SDK provides a Unity wrapper around the OpenFeature standard, enabling developers to use feature flags in their Unity projects. It integrates with the Confidence platform for flag evaluation and management.

## Features

- Unity-compatible OpenFeature implementation
- Confidence provider integration
- Support for boolean, string, number, and object flag types
- Asynchronous flag evaluation
- Unity coroutine support

## Testing and Compilation

To test if the SDK compiles correctly with Unity:

1. Ensure Unity 2022.3.62f1 is installed at `/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity`
2. Run the build test script from the unity-sdk directory:
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
       "com.unity.openfeature": "file:../path/to/unity-sdk"
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

## Dependencies

- Unity 2020.3+
- com.unity.nuget.newtonsoft-json: 3.0.2

## Contributing

This is an experimental SDK. Please report any issues or suggestions through the main repository's issue tracker.
