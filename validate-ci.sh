#!/bin/bash

echo "🔍 Validating CI configuration before pushing to git..."
echo

# Test 1: Check YAML syntax
echo "1. Checking YAML syntax..."
if command -v yamllint >/dev/null 2>&1; then
    yamllint .github/workflows/pr-validation.yml .github/workflows/release-please.yml 2>/dev/null | head -10 || echo "❌ YAML syntax issues found (see above)"
    echo "✅ YAML syntax check completed"
else
    echo "⚠️ yamllint not installed - skipping YAML validation"
fi
echo

# Test 2: Check workflow files exist
echo "2. Checking workflow files..."
if [ -f ".github/workflows/pr-validation.yml" ]; then
    echo "✅ pr-validation.yml exists"
else
    echo "❌ pr-validation.yml missing"
fi

if [ -f ".github/workflows/release-please.yml" ]; then
    echo "✅ release-please.yml exists"
else
    echo "❌ release-please.yml missing"
fi
echo

# Test 3: Check build script
echo "3. Checking Unity build script..."
if [ -x "unity-sdk/build-test.sh" ]; then
    echo "✅ build-test.sh is executable"
    echo "   Script supports unity-editor detection: $(grep -q 'command -v unity-editor' unity-sdk/build-test.sh && echo '✅' || echo '❌')"
else
    echo "❌ build-test.sh is not executable"
fi
echo

# Test 4: Check Unity installation locally
echo "4. Checking Unity installation..."
UNITY_PATH="/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity"
if [ -f "$UNITY_PATH" ]; then
    echo "✅ Unity 2022.3.62f1 found at expected path"
    echo "   Unity version: $($UNITY_PATH --version 2>/dev/null || echo 'unknown')"
else
    echo "⚠️  Unity not found at expected path (this is OK for CI)"
    echo "   CI will use game-ci/unity-builder action"
fi
echo

# Test 5: Check GitHub Actions availability
echo "5. Checking GitHub Actions availability..."
echo "   game-ci/unity-builder: $(curl -s -o /dev/null -w "%{http_code}" https://api.github.com/repos/game-ci/unity-builder | grep -q '200' && echo '✅ Available' || echo '❌ Not found')"

echo

echo "🎯 Summary:"
echo "   - CI workflows use game-ci/unity-builder action"
echo "   - Build script supports both local and CI environments"
echo "   - Unity installation handled automatically by CI action"
echo

echo "💡 Recommendations:"
echo "   1. Test locally: cd unity-sdk && ./build-test.sh"
echo "   2. Create a test PR to trigger CI pipeline"
echo "   3. Monitor CI logs for any Unity-related issues"
echo
echo "🔧 Current Setup:"
echo "   - game-ci/unity-builder@v4 for automated Unity installation"
echo "   - Unity 2022.3.62f1 for all builds"
echo "   - Build script auto-detects Unity (CI or local)"
echo "   - Simplified CI configuration with fewer moving parts"
echo
echo "🚀 Ready to push when you're confident!"
