#!/bin/bash

# Unity OpenFeature SDK Build Test Script
echo "🔨 Testing Unity OpenFeature SDK compilation..."

# Unity Editor path
# Allow override via environment variable for CI environments
# Try unity-editor first (available after unity-builder action), then fallback to local macOS path
if command -v unity-editor >/dev/null 2>&1; then
    UNITY_PATH="unity-editor"
elif [ -n "$UNITY_PATH" ]; then
    UNITY_PATH="$UNITY_PATH"
else
    UNITY_PATH="/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity"
fi

# Check if Unity is installed
if ! command -v "$UNITY_PATH" >/dev/null 2>&1 && [ ! -f "$UNITY_PATH" ]; then
    echo "❌ Unity 2022.3.62f1 not found. Please install Unity or update the path."
    echo "   Tried: $UNITY_PATH"
    echo "   Available commands: $(which unity-editor 2>/dev/null || echo 'unity-editor not found')"
    exit 1
fi

echo "✅ Using Unity at: $UNITY_PATH"

# Create test project directory
TEST_DIR="../test-unity-project"
echo "📁 Creating test project at $TEST_DIR..."

# Clean up previous test if exists
rm -rf "$TEST_DIR"
mkdir -p "$TEST_DIR"

# Create Unity project
echo "🎮 Creating Unity project..."
$UNITY_PATH -createProject "$(realpath $TEST_DIR)" -quit

# Add our package to the project
echo "📦 Adding Unity OpenFeature package..."
cat > "$TEST_DIR/Packages/manifest.json" << EOF
{
  "dependencies": {
    "com.unity.openfeature": "file:../../unity-sdk",
    "com.unity.toolchain.macos-arm64-linux-x86_64": "2.0.4",
    "com.unity.modules.ai": "1.0.0",
    "com.unity.modules.androidjni": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.assetbundle": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.cloth": "1.0.0",
    "com.unity.modules.director": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.physics2d": "1.0.0",
    "com.unity.modules.screencapture": "1.0.0",
    "com.unity.modules.terrain": "1.0.0",
    "com.unity.modules.terrainphysics": "1.0.0",
    "com.unity.modules.tilemap": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.umbra": "1.0.0",
    "com.unity.modules.unityanalytics": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.unitywebrequestassetbundle": "1.0.0",
    "com.unity.modules.unitywebrequestaudio": "1.0.0",
    "com.unity.modules.unitywebrequesttexture": "1.0.0",
    "com.unity.modules.unitywebrequestwww": "1.0.0",
    "com.unity.modules.vehicles": "1.0.0",
    "com.unity.modules.video": "1.0.0",
    "com.unity.modules.vr": "1.0.0",
    "com.unity.modules.wind": "1.0.0",
    "com.unity.modules.xr": "1.0.0"
  }
}
EOF

# Build and test compilation
echo "🔧 Testing compilation..."
LOG_FILE="$TEST_DIR/build.log"

$UNITY_PATH -batchmode -quit -projectPath "$(realpath $TEST_DIR)" -logFile "$LOG_FILE"

# Check results
if [ $? -eq 0 ]; then
    echo "✅ BUILD SUCCESSFUL! Unity OpenFeature SDK compiled without errors."
    echo "📝 Build log saved to: $LOG_FILE"
    
    # Show any warnings (but not errors since build was successful)
    if grep -q "warning" "$LOG_FILE"; then
        echo "⚠️  Warnings found:"
        grep "warning" "$LOG_FILE" | head -5
    fi
else
    echo "❌ BUILD FAILED! Compilation errors found."
    echo "📝 Error details:"
    grep -A 3 -B 3 "error" "$LOG_FILE" | head -20
    exit 1
fi

# Clean up test project
echo "🧹 Cleaning up test project..."
rm -rf "$TEST_DIR"

echo "🎉 Unity OpenFeature SDK build test completed successfully!"
