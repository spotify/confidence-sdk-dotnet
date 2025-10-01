# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is the Confidence SDK for .NET, providing feature flag and configuration management through both a native .NET SDK and OpenFeature integration. The SDK supports dot-notation for accessing nested flag properties and includes comprehensive logging and error handling.

## Architecture

### Core Components

- **Spotify.Confidence.Sdk**: Core SDK with `IConfidenceClient` interface and `ConfidenceClient` implementation
- **Spotify.Confidence.OpenFeature**: OpenFeature provider implementation (`ConfidenceProvider`)
- **Two Integration Approaches**:
  - Native SDK: Direct API access with `ConfidenceClient`
  - OpenFeature: Standards-compliant integration through `ConfidenceProvider`

### Key Features

- **Dot-notation Support**: Access nested flag properties like `"flag.property.subproperty"`
- **Multi-target Framework**: Supports .NET 8.0 and .NET 9.0
- **Comprehensive Logging**: Uses Microsoft.Extensions.Logging with configurable levels
- **Event Tracking**: Built-in analytics event tracking
- **Type-safe Flag Evaluation**: Support for boolean, string, numeric, integer, and JSON flags

## Common Development Commands

### Build and Test
```bash
# Restore dependencies
dotnet restore

# Build entire solution
dotnet build

# Build with warnings as errors (CI mode)
dotnet build --verbosity normal --no-restore /p:TreatWarningsAsErrors=true

# Run all tests
dotnet test

# Run tests for specific framework
dotnet test --framework net9.0
dotnet test --framework net8.0

# Run tests without build
dotnet test --no-build --verbosity normal
```

### Individual Project Commands
```bash
# Build specific projects
dotnet build src/Spotify.Confidence.Sdk/
dotnet build src/Spotify.Confidence.OpenFeature/

# Run specific test projects
dotnet test tests/Spotify.Confidence.Sdk.Tests/
dotnet test tests/Spotify.Confidence.OpenFeature.Tests/

# Run examples
dotnet run --project examples/SimpleExample/
dotnet run --project examples/OpenFeatureExample/
```

## Code Quality and Standards

### Static Analysis
The project uses multiple analyzers configured in project files:
- **StyleCop.Analyzers**: Code style enforcement
- **Roslynator.Analyzers**: Additional code analysis
- **SonarAnalyzer.CSharp**: Code quality and security analysis
- **Microsoft.VisualStudio.Threading.Analyzers**: Async/threading best practices
- **Built-in .NET Analysis**: `AnalysisMode=Recommended` and `AnalysisModeSecurity=All`

### StyleCop Configuration
- Company name: "Spotify"
- Copyright format: "Copyright (c) {companyName}. All rights reserved."
- System using directives placed first, outside namespace
- File naming convention follows StyleCop standards

### Code Style Requirements
- `EnforceCodeStyleInBuild=true` ensures style violations break the build
- All projects use `Nullable=enable` and `ImplicitUsings=enable`
- Latest C# language version for test projects

## Project Structure

### Source Projects
- `src/Spotify.Confidence.Sdk/`: Core SDK implementation
- `src/Spotify.Confidence.OpenFeature/`: OpenFeature provider
- Additional local provider projects may exist

### Test Projects
- `tests/Spotify.Confidence.Sdk.Tests/`: Core SDK tests using xUnit and Moq
- `tests/Spotify.Confidence.OpenFeature.Tests/`: OpenFeature provider tests

### Examples
- `examples/SimpleExample/`: Basic native SDK usage
- `examples/OpenFeatureExample/`: OpenFeature integration example
- `examples/UnitySampleGame/`: Unity 3D game demonstrating OpenFeature integration

## Dependencies and Technologies

### Core Dependencies
- **Microsoft.Extensions.Http/Polly**: HTTP client with retry policies
- **Microsoft.Extensions.Logging**: Logging abstractions and console output
- **System.Text.Json**: JSON serialization
- **OpenFeature**: Feature flag standard (OpenFeature projects only)

### Test Dependencies
- **xUnit**: Testing framework
- **Moq**: Mocking framework
- **coverlet.collector**: Code coverage collection

## Unity Integration (Experimental)

### Unity Package Architecture
The project provides **two separate Unity packages** for modular OpenFeature integration:

#### 1. Unity OpenFeature SDK (`unity-openfeature-sdk/`)
- **Package ID**: `com.unity.openfeature`
- **Purpose**: Pure OpenFeature standard implementation for Unity
- **Installation**: `https://github.com/spotify/confidence-sdk-dotnet.git?path=unity-openfeature-sdk`
- **Dependencies**: Newtonsoft.Json 3.0.2
- **Components**:
  - Core OpenFeature interfaces and implementations
  - Generic InMemoryProvider for testing
  - Unity-specific utilities and MonoBehaviour helpers

#### 2. Unity Confidence Provider (`unity-confidence-provider/`)
- **Package ID**: `com.spotify.confidence.unity`
- **Purpose**: Confidence platform integration for Unity OpenFeature
- **Installation**: `https://github.com/spotify/confidence-sdk-dotnet.git?path=unity-confidence-provider`
- **Dependencies**: Unity OpenFeature SDK, UnityWebRequest
- **Components**:
  - ConfidenceProvider implementing IFeatureProvider
  - ConfidenceApiClient for HTTP communication
  - Confidence-specific flag resolution and analytics

### Unity Development Commands
```bash
# Test Unity OpenFeature SDK compilation
cd unity-openfeature-sdk
./build-test.sh

# Test Unity Confidence Provider compilation (after restructure)
cd unity-confidence-provider
./build-test.sh

# Build Unity library DLL for examples
cd examples/UnitySampleGame/UnityLibrary
dotnet build

# Open Unity sample game in Unity Editor
# Navigate to examples/UnitySampleGame/Sample3DGame/ and open in Unity Hub
```

### Installation in Unity Projects
Add both packages to your Unity project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.unity.openfeature": "https://github.com/spotify/confidence-sdk-dotnet.git?path=unity-openfeature-sdk",
    "com.spotify.confidence.unity": "https://github.com/spotify/confidence-sdk-dotnet.git?path=unity-confidence-provider",
    "com.unity.nuget.newtonsoft-json": "3.0.2"
  }
}
```

### Unity Key Components

#### OpenFeature Core
- `UnityOpenFeature.Core.IFeatureProvider`: Provider interface
- `UnityOpenFeature.Core.OpenFeatureAPI`: Main API singleton
- `UnityOpenFeature.Client.IFeatureClient`: Client interface for flag evaluation
- `UnityOpenFeature.Unity.UnityOpenFeature`: MonoBehaviour singleton

#### Confidence Integration
- `UnityOpenFeature.Providers.ConfidenceProvider`: Confidence platform provider
- `UnityOpenFeature.Providers.ConfidenceApiClient`: HTTP client with Unity coroutines
- Dot-notation support for nested flag properties
- Automatic flag analytics and checkpointing

### Unity Sample Game Features
- **Modular Setup**: Demonstrates both OpenFeature SDK and Confidence Provider usage
- **PlayerController Integration**: Feature flags control movement speed (8x multiplier)
- **Confidence Provider Setup**: Example initialization with client secrets
- **Error Handling**: Comprehensive logging and exception handling
- **Coroutine Support**: Unity-friendly asynchronous operations

### Important Unity Notes
- **Experimental Status**: Both Unity packages are experimental and subject to change
- **Modular Design**: Can use OpenFeature SDK with other providers besides Confidence
- **GitHub Distribution**: Packages distributed via Git URLs, not Unity Package Manager registry
- **Unity Version**: Supports Unity 2020.3+ (tested with 2022.3.62f1)
- **Target Framework**: .NET Standard 2.1 for Unity compatibility
- **Client Secret Required**: Must configure valid Confidence client secret in samples

## Release and Versioning

- Uses release-please for automated releases
- Version managed in project files with `<!--x-release-please-version -->` markers
- Packages published to NuGet with Apache 2.0 license
- Symbol packages (snupkg) included for debugging
- Unity package distributed via Git URL, not Unity Package Manager registry