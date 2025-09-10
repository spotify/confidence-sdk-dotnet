# Unity OpenFeature Sample Game

A simple Unity 2D game demonstrating the Unity OpenFeature SDK integration with feature flags.

## Overview

This sample game shows how to use the experimental Unity OpenFeature SDK to control game behavior through feature flags. The game uses the Confidence provider to evaluate flags that affect player movement speed and other gameplay elements.

## Setup and Running

### Prerequisites
- Unity 2020.3+ (tested with Unity 2022.3.62f1)
- Unity Hub installed

### Import and Open in Unity

1. Open Unity Hub
2. Click "Add" or "Open" project
3. Navigate to the `Sample3DGame` folder within this directory
4. Select the folder and click "Open"

### Running the Game

1. Once the project opens in Unity Editor
2. Open the SampleScene (Assets/Scenes/SampleScene.unity)
3. Press the Play button in the Unity Editor

The game is a simple 2D platformer where you can move left/right and jump.

## Configuration

### Adding Your Client Secret

⚠️ **Important**: Before running, you must add your own Confidence client secret.

1. Open `Assets/Scripts/PlayerController.cs`
2. Find line 28 where the Confidence provider is initialized
3. Replace the placeholder client secret with your actual Confidence client secret:

```csharp
// Change this line:
provider = new ConfidenceProvider("CLIENT_SECRET");
// To:
provider = new ConfidenceProvider("your-actual-client-secret-here");
```

### Getting a Client Secret

1. Sign up for Confidence at [confidence.spotify.com](https://confidence.spotify.com)
2. Create a new project
3. Generate a client secret from your project settings
4. Replace the placeholder in the code

## Feature Flags Used

- `vahid-test.enabled`: Controls enhanced player movement speed (8x normal speed when enabled)

## SDK Integration

This sample demonstrates:
- Confidence provider setup
- Feature flag evaluation
- Unity coroutine integration
- Error handling for flag operations

## Troubleshooting

- If the game doesn't start properly, check the Unity console for OpenFeature initialization errors
- Ensure your client secret is valid and has the necessary permissions
- Make sure all Unity packages are properly installed

## Related Files

- `Assets/Scripts/PlayerController.cs` - Main game logic with OpenFeature integration
- `Assets/Plugins/UnityLibrary.dll` - Unity OpenFeature SDK assembly
- `Sample3DGame.csproj` - Unity project file
