using System.Text.Json;
using Spotify.Confidence.Sdk.Models;
using Spotify.Confidence.Sdk.Utils;
using Xunit;

namespace Spotify.Confidence.Sdk.Tests;

public class DotNotationTests
{
    [Theory]
    [InlineData("simple-flag", "simple-flag", new string[0])]
    [InlineData("flag.property", "flag", new string[] { "property" })]
    [InlineData("flag.user.settings", "flag", new string[] { "user", "settings" })]
    [InlineData("flag.user.preferences.theme.color", "flag", new string[] { "user", "preferences", "theme", "color" })]
    [InlineData("", "", new string[0])]
    public void ParseDotNotation_ValidInput_ReturnsCorrectParts(string input, string expectedFlagName, string[] expectedPropertyPath)
    {
        // Act
        var (flagName, propertyPath) = DotNotationHelper.ParseDotNotation(input);

        // Assert
        Assert.Equal(expectedFlagName, flagName);
        Assert.Equal(expectedPropertyPath, propertyPath);
    }

    [Fact]
    public void ExtractFlagValue_SimpleValue_ReturnsValue()
    {
        // Arrange
        var flagValue = new Dictionary<string, object>
        {
            ["value"] = "test-value"
        };
        var propertyPath = Array.Empty<string>();

        // Act
        var result = DotNotationHelper.ExtractFlagValue(flagValue, propertyPath);

        // Assert
        Assert.Equal("test-value", result);
    }

    [Fact]
    public void ExtractFlagValue_NoValueKey_ReturnsEntireDictionary()
    {
        // Arrange
        var flagValue = new Dictionary<string, object>
        {
            ["property1"] = "value1",
            ["property2"] = "value2"
        };
        var propertyPath = Array.Empty<string>();

        // Act
        var result = DotNotationHelper.ExtractFlagValue(flagValue, propertyPath);

        // Assert
        Assert.Equal(flagValue, result);
    }

    [Fact]
    public void ExtractFlagValue_WithPropertyPath_NavigatesToProperty()
    {
        // Arrange
        var nestedObject = new Dictionary<string, object>
        {
            ["theme"] = "dark",
            ["language"] = "en"
        };
        var flagValue = new Dictionary<string, object>
        {
            ["value"] = new Dictionary<string, object>
            {
                ["user"] = new Dictionary<string, object>
                {
                    ["settings"] = nestedObject
                }
            }
        };
        var propertyPath = new[] { "user", "settings", "theme" };

        // Act
        var result = DotNotationHelper.ExtractFlagValue(flagValue, propertyPath);

        // Assert
        Assert.Equal("dark", result);
    }

    [Fact]
    public void ExtractFlagValue_PropertyNotFound_ReturnsNull()
    {
        // Arrange
        var flagValue = new Dictionary<string, object>
        {
            ["value"] = new Dictionary<string, object>
            {
                ["user"] = new Dictionary<string, object>
                {
                    ["settings"] = new Dictionary<string, object>
                    {
                        ["theme"] = "dark"
                    }
                }
            }
        };
        var propertyPath = new[] { "user", "settings", "nonexistent" };

        // Act
        var result = DotNotationHelper.ExtractFlagValue(flagValue, propertyPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractFlagValue_WithJsonElement_NavigatesToProperty()
    {
        // Arrange
        var jsonString = """
        {
            "user": {
                "settings": {
                    "theme": "dark",
                    "language": "en"
                }
            }
        }
        """;
        var jsonElement = JsonDocument.Parse(jsonString).RootElement;
        var flagValue = new Dictionary<string, object>
        {
            ["value"] = jsonElement
        };
        var propertyPath = new[] { "user", "settings", "theme" };

        // Act
        var result = DotNotationHelper.ExtractFlagValue(flagValue, propertyPath);

        // Assert
        var resultElement = Assert.IsType<JsonElement>(result);
        Assert.Equal("dark", resultElement.GetString());
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    [InlineData("test-string", "\"test-string\"")]
    [InlineData(42.5, "42.5")]
    public void ExtractTypedValue_DirectConversion_ReturnsCorrectType<T>(T expectedValue, string jsonValue)
    {
        // Arrange
        var flag = new ResolvedFlag
        {
            Flag = "test-flag",
            Reason = "MATCH",
            Variant = "control",
            Value = new Dictionary<string, object>
            {
                ["value"] = JsonDocument.Parse(jsonValue).RootElement
            }
        };
        var flagKey = "test-flag";

        // Act
        var (result, errorMessage) = DotNotationHelper.ExtractTypedValue(flag, flagKey, default(T)!);

        // Assert
        Assert.Null(errorMessage);
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void ExtractTypedValue_WithDotNotation_ExtractsNestedProperty()
    {
        // Arrange
        var jsonString = """
        {
            "user": {
                "settings": {
                    "darkMode": true
                }
            }
        }
        """;
        var flag = new ResolvedFlag
        {
            Flag = "user-config",
            Reason = "MATCH",
            Variant = "control",
            Value = new Dictionary<string, object>
            {
                ["value"] = JsonDocument.Parse(jsonString).RootElement
            }
        };
        var flagKey = "user-config.user.settings.darkMode";

        // Act
        var (result, errorMessage) = DotNotationHelper.ExtractTypedValue<bool>(flag, flagKey, false);

        // Assert
        Assert.Null(errorMessage);
        Assert.True(result);
    }

    [Fact]
    public void ExtractTypedValue_PropertyNotFound_ReturnsError()
    {
        // Arrange
        var flag = new ResolvedFlag
        {
            Flag = "test-flag",
            Reason = "MATCH",
            Variant = "control",
            Value = new Dictionary<string, object>
            {
                ["value"] = new Dictionary<string, object>
                {
                    ["existing"] = "value"
                }
            }
        };
        var flagKey = "test-flag.nonexistent";

        // Act
        var (result, errorMessage) = DotNotationHelper.ExtractTypedValue<string>(flag, flagKey, "default");

        // Assert
        Assert.NotNull(errorMessage);
        Assert.Contains("Property path 'nonexistent' not found", errorMessage);
        Assert.Equal("default", result);
    }

    [Fact]
    public void ExtractTypedValue_ComplexObject_DeserializesCorrectly()
    {
        // Arrange
        var jsonString = """
        {
            "config": {
                "name": "test-config",
                "value": 123,
                "enabled": true
            }
        }
        """;
        var flag = new ResolvedFlag
        {
            Flag = "app-config",
            Reason = "MATCH",
            Variant = "control",
            Value = new Dictionary<string, object>
            {
                ["value"] = JsonDocument.Parse(jsonString).RootElement
            }
        };
        var flagKey = "app-config.config";

        // Act
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var (result, errorMessage) = DotNotationHelper.ExtractTypedValue<TestConfig>(flag, flagKey, new TestConfig(), jsonOptions);

        // Assert
        Assert.Null(errorMessage);
        Assert.Equal("test-config", result.Name);
        Assert.Equal(123, result.Value);
        Assert.True(result.Enabled);
    }

    [Fact]
    public void NavigateToProperty_EmptyPath_ReturnsOriginalValue()
    {
        // Arrange
        var value = "test-value";
        var propertyPath = Array.Empty<string>();

        // Act
        var result = DotNotationHelper.NavigateToProperty(value, propertyPath);

        // Assert
        Assert.Equal("test-value", result);
    }

    [Fact]
    public void NavigateToProperty_NullValue_ReturnsNull()
    {
        // Arrange
        object? value = null;
        var propertyPath = new[] { "property" };

        // Act
        var result = DotNotationHelper.NavigateToProperty(value, propertyPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NavigateToProperty_DeepNesting_NavigatesCorrectly()
    {
        // Arrange
        var value = new Dictionary<string, object>
        {
            ["level1"] = new Dictionary<string, object>
            {
                ["level2"] = new Dictionary<string, object>
                {
                    ["level3"] = "deep-value"
                }
            }
        };
        var propertyPath = new[] { "level1", "level2", "level3" };

        // Act
        var result = DotNotationHelper.NavigateToProperty(value, propertyPath);

        // Assert
        Assert.Equal("deep-value", result);
    }

    private class TestConfig
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
        public bool Enabled { get; set; }
    }
}