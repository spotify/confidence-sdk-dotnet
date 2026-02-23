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
        // Arrange - schema properties are directly in flagValue (matching actual API structure)
        var nestedObject = new Dictionary<string, object>
        {
            ["theme"] = "dark",
            ["language"] = "en"
        };
        var flagValue = new Dictionary<string, object>
        {
            ["user"] = new Dictionary<string, object>
            {
                ["settings"] = nestedObject
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
        // Arrange - schema properties directly in flagValue (matching actual API structure)
        var flagValue = new Dictionary<string, object>
        {
            ["user"] = new Dictionary<string, object>
            {
                ["settings"] = new Dictionary<string, object>
                {
                    ["theme"] = "dark"
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
        // Arrange - JsonElement containing nested structure, directly in flagValue
        var jsonString = """
        {
            "settings": {
                "theme": "dark",
                "language": "en"
            }
        }
        """;
        var jsonElement = JsonDocument.Parse(jsonString).RootElement;
        var flagValue = new Dictionary<string, object>
        {
            ["user"] = jsonElement
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
    public void ExtractTypedValue_IntegerConversion_ReturnsCorrectType()
    {
        // Arrange
        var flag = new ResolvedFlag
        {
            Flag = "test-flag",
            Reason = "MATCH",
            Variant = "control",
            Value = new Dictionary<string, object>
            {
                ["value"] = JsonDocument.Parse("42").RootElement
            }
        };
        var flagKey = "test-flag";

        // Act
        var (result, errorMessage) = DotNotationHelper.ExtractTypedValue<int>(flag, flagKey, 0);

        // Assert
        Assert.Null(errorMessage);
        Assert.Equal(42, result);
        Assert.IsType<int>(result);
    }

    [Fact]
    public void ExtractTypedValue_LongConversion_ReturnsCorrectType()
    {
        // Arrange
        var flag = new ResolvedFlag
        {
            Flag = "test-flag",
            Reason = "MATCH",
            Variant = "control",
            Value = new Dictionary<string, object>
            {
                ["value"] = JsonDocument.Parse("9223372036854775807").RootElement // Max long value
            }
        };
        var flagKey = "test-flag";

        // Act
        var (result, errorMessage) = DotNotationHelper.ExtractTypedValue<long>(flag, flagKey, 0L);

        // Assert
        Assert.Null(errorMessage);
        Assert.Equal(9223372036854775807L, result);
        Assert.IsType<long>(result);
    }

    [Fact]
    public void ExtractTypedValue_FloatConversion_ReturnsCorrectType()
    {
        // Arrange
        var flag = new ResolvedFlag
        {
            Flag = "test-flag",
            Reason = "MATCH",
            Variant = "control",
            Value = new Dictionary<string, object>
            {
                ["value"] = JsonDocument.Parse("3.14159").RootElement
            }
        };
        var flagKey = "test-flag";

        // Act
        var (result, errorMessage) = DotNotationHelper.ExtractTypedValue<float>(flag, flagKey, 0.0f);

        // Assert
        Assert.Null(errorMessage);
        Assert.Equal(3.14159f, result, precision: 5);
        Assert.IsType<float>(result);
    }

    [Fact]
    public void ExtractTypedValue_DecimalConversion_ReturnsCorrectType()
    {
        // Arrange
        var flag = new ResolvedFlag
        {
            Flag = "test-flag",
            Reason = "MATCH",
            Variant = "control",
            Value = new Dictionary<string, object>
            {
                ["value"] = JsonDocument.Parse("123.456789").RootElement
            }
        };
        var flagKey = "test-flag";

        // Act
        var (result, errorMessage) = DotNotationHelper.ExtractTypedValue<decimal>(flag, flagKey, 0.0m);

        // Assert
        Assert.Null(errorMessage);
        Assert.Equal(123.456789m, result);
        Assert.IsType<decimal>(result);
    }

    [Theory]
    [InlineData("42", 42)]
    [InlineData("0", 0)]
    [InlineData("-123", -123)]
    [InlineData("2147483647", 2147483647)] // Max int value
    [InlineData("-2147483648", -2147483648)] // Min int value
    public void ExtractTypedValue_IntegerVariousValues_ReturnsCorrectValues(string jsonValue, int expectedValue)
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
        var (result, errorMessage) = DotNotationHelper.ExtractTypedValue<int>(flag, flagKey, 0);

        // Assert
        Assert.Null(errorMessage);
        Assert.Equal(expectedValue, result);
    }

    [Theory]
    [InlineData("3.14", 3.14f)]
    [InlineData("0.0", 0.0f)]
    [InlineData("-2.5", -2.5f)]
    [InlineData("1.23e10", 1.23e10f)]
    public void ExtractTypedValue_FloatVariousValues_ReturnsCorrectValues(string jsonValue, float expectedValue)
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
        var (result, errorMessage) = DotNotationHelper.ExtractTypedValue<float>(flag, flagKey, 0.0f);

        // Assert
        Assert.Null(errorMessage);
        Assert.Equal(expectedValue, result, precision: 5);
    }

    [Fact]
    public void ExtractTypedValue_WithDotNotation_ExtractsNestedNumericTypes()
    {
        // Arrange - schema properties directly in Value (matching actual API structure)
        var settingsJson = """
        {
            "timeout": 5000,
            "maxRetries": 9223372036854775807,
            "threshold": 2.5,
            "budget": 999.99
        }
        """;
        var flag = new ResolvedFlag
        {
            Flag = "app-config",
            Reason = "MATCH",
            Variant = "control",
            Value = new Dictionary<string, object>
            {
                ["settings"] = JsonDocument.Parse(settingsJson).RootElement
            }
        };

        // Act & Assert - Test int extraction
        var (intResult, intError) = DotNotationHelper.ExtractTypedValue<int>(flag, "app-config.settings.timeout", 0);
        Assert.Null(intError);
        Assert.Equal(5000, intResult);

        // Act & Assert - Test long extraction
        var (longResult, longError) = DotNotationHelper.ExtractTypedValue<long>(flag, "app-config.settings.maxRetries", 0L);
        Assert.Null(longError);
        Assert.Equal(9223372036854775807L, longResult);

        // Act & Assert - Test float extraction
        var (floatResult, floatError) = DotNotationHelper.ExtractTypedValue<float>(flag, "app-config.settings.threshold", 0.0f);
        Assert.Null(floatError);
        Assert.Equal(2.5f, floatResult);

        // Act & Assert - Test decimal extraction
        var (decimalResult, decimalError) = DotNotationHelper.ExtractTypedValue<decimal>(flag, "app-config.settings.budget", 0.0m);
        Assert.Null(decimalError);
        Assert.Equal(999.99m, decimalResult);
    }

    [Fact]
    public void ExtractTypedValue_WithDotNotation_ExtractsNestedProperty()
    {
        // Arrange - schema properties directly in Value (matching actual API structure)
        var userJson = """
        {
            "settings": {
                "darkMode": true
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
                ["user"] = JsonDocument.Parse(userJson).RootElement
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
        // Arrange - schema properties directly in Value (matching actual API structure)
        var flag = new ResolvedFlag
        {
            Flag = "test-flag",
            Reason = "MATCH",
            Variant = "control",
            Value = new Dictionary<string, object>
            {
                ["existing"] = "value"
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
        // Arrange - schema properties directly in Value (matching actual API structure)
        var configJson = """
        {
            "name": "test-config",
            "value": 123,
            "enabled": true
        }
        """;
        var flag = new ResolvedFlag
        {
            Flag = "app-config",
            Reason = "MATCH",
            Variant = "control",
            Value = new Dictionary<string, object>
            {
                ["config"] = JsonDocument.Parse(configJson).RootElement
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

    #region Value Property Collision Tests

    [Fact]
    public void ExtractFlagValue_FlagWithPrimitiveValueProperty_CanAccessOtherProperties()
    {
        // Arrange - flag has a property named "value" (primitive) alongside other properties
        // This simulates the actual API response where values are JsonElements
        var flagValue = new Dictionary<string, object>
        {
            ["value"] = JsonDocument.Parse("\"the-value-property\"").RootElement,
            ["timeout"] = JsonDocument.Parse("5000").RootElement,
            ["enabled"] = JsonDocument.Parse("true").RootElement
        };
        var propertyPath = new[] { "timeout" };

        // Act
        var result = DotNotationHelper.ExtractFlagValue(flagValue, propertyPath);

        // Assert - should navigate to "timeout", not get stuck on "value"
        var resultElement = Assert.IsType<JsonElement>(result);
        Assert.Equal(5000, resultElement.GetInt32());
    }

    [Fact]
    public void ExtractFlagValue_FlagWithPrimitiveValueProperty_CanAccessValuePropertyDirectly()
    {
        // Arrange - flag has a property named "value" (primitive)
        var flagValue = new Dictionary<string, object>
        {
            ["value"] = JsonDocument.Parse("\"the-value-property\"").RootElement,
            ["timeout"] = JsonDocument.Parse("5000").RootElement
        };
        var propertyPath = new[] { "value" };

        // Act
        var result = DotNotationHelper.ExtractFlagValue(flagValue, propertyPath);

        // Assert - should return the "value" property contents
        var resultElement = Assert.IsType<JsonElement>(result);
        Assert.Equal("the-value-property", resultElement.GetString());
    }

    [Fact]
    public void ExtractFlagValue_FlagWithPrimitiveValueProperty_NoPathReturnsValueForBackwardCompat()
    {
        // Arrange - flag has a property named "value" (primitive)
        var flagValue = new Dictionary<string, object>
        {
            ["value"] = JsonDocument.Parse("\"the-value-property\"").RootElement,
            ["timeout"] = JsonDocument.Parse("5000").RootElement
        };
        var propertyPath = Array.Empty<string>();

        // Act
        var result = DotNotationHelper.ExtractFlagValue(flagValue, propertyPath);

        // Assert - backward compatibility: with no path, return "value" content
        var resultElement = Assert.IsType<JsonElement>(result);
        Assert.Equal("the-value-property", resultElement.GetString());
    }

    [Fact]
    public void ExtractTypedValue_FlagWithValueProperty_CanAccessNestedProperties()
    {
        // Arrange - flag structure where "value" is a sibling property, not a wrapper
        var flag = new ResolvedFlag
        {
            Flag = "my-config",
            Reason = "MATCH",
            Variant = "control",
            Value = new Dictionary<string, object>
            {
                ["value"] = JsonDocument.Parse("\"configured-value\"").RootElement,
                ["timeout"] = JsonDocument.Parse("5000").RootElement,
                ["retries"] = JsonDocument.Parse("3").RootElement
            }
        };

        // Act - access the "timeout" property via dot-notation
        var (timeoutResult, timeoutError) = DotNotationHelper.ExtractTypedValue<int>(flag, "my-config.timeout", 0);

        // Assert
        Assert.Null(timeoutError);
        Assert.Equal(5000, timeoutResult);
    }

    [Fact]
    public void ExtractTypedValue_FlagWithValueProperty_CanAccessValuePropertyViaDotNotation()
    {
        // Arrange
        var flag = new ResolvedFlag
        {
            Flag = "my-config",
            Reason = "MATCH",
            Variant = "control",
            Value = new Dictionary<string, object>
            {
                ["value"] = JsonDocument.Parse("\"configured-value\"").RootElement,
                ["timeout"] = JsonDocument.Parse("5000").RootElement
            }
        };

        // Act - explicitly access the "value" property via dot-notation
        var (valueResult, valueError) = DotNotationHelper.ExtractTypedValue<string>(flag, "my-config.value", "default");

        // Assert
        Assert.Null(valueError);
        Assert.Equal("configured-value", valueResult);
    }

    [Fact]
    public void ExtractTypedValue_FlagWithNestedValueProperty_NavigatesCorrectly()
    {
        // Arrange - flag with primitive "value" property and a nested "config" object
        var configJson = """{"value": "nested-value", "setting": 42}""";
        var flag = new ResolvedFlag
        {
            Flag = "app",
            Reason = "MATCH",
            Variant = "control",
            Value = new Dictionary<string, object>
            {
                ["value"] = JsonDocument.Parse("\"outer-value\"").RootElement,
                ["config"] = JsonDocument.Parse(configJson).RootElement
            }
        };

        // This test verifies we can navigate past the root "value" property to access config.setting
        var (settingResult, settingError) = DotNotationHelper.ExtractTypedValue<int>(flag, "app.config.setting", 0);

        Assert.Null(settingError);
        Assert.Equal(42, settingResult);

        // Also verify we can access the nested "value" property inside config
        var (nestedValueResult, nestedValueError) = DotNotationHelper.ExtractTypedValue<string>(flag, "app.config.value", "default");

        Assert.Null(nestedValueError);
        Assert.Equal("nested-value", nestedValueResult);
    }

    [Fact]
    public void ExtractTypedValue_FlagWithObjectValueProperty_CanAccessSiblings()
    {
        // Arrange - flag where "value" is an object (not primitive), with sibling properties
        var valueObjectJson = """{"nested_data": "inside-value", "count": 10}""";
        var flag = new ResolvedFlag
        {
            Flag = "complex-flag",
            Reason = "MATCH",
            Variant = "control",
            Value = new Dictionary<string, object>
            {
                ["value"] = JsonDocument.Parse(valueObjectJson).RootElement,
                ["timeout"] = JsonDocument.Parse("5000").RootElement,
                ["name"] = JsonDocument.Parse("\"my-flag\"").RootElement
            }
        };

        // Test accessing sibling properties when "value" is an object
        var (timeoutResult, timeoutError) = DotNotationHelper.ExtractTypedValue<int>(flag, "complex-flag.timeout", 0);
        Assert.Null(timeoutError);
        Assert.Equal(5000, timeoutResult);

        var (nameResult, nameError) = DotNotationHelper.ExtractTypedValue<string>(flag, "complex-flag.name", "default");
        Assert.Null(nameError);
        Assert.Equal("my-flag", nameResult);

        // Test accessing nested properties inside the "value" object
        var (nestedDataResult, nestedDataError) = DotNotationHelper.ExtractTypedValue<string>(flag, "complex-flag.value.nested_data", "default");
        Assert.Null(nestedDataError);
        Assert.Equal("inside-value", nestedDataResult);

        var (countResult, countError) = DotNotationHelper.ExtractTypedValue<int>(flag, "complex-flag.value.count", 0);
        Assert.Null(countError);
        Assert.Equal(10, countResult);
    }

    #endregion

    private class TestConfig
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
        public bool Enabled { get; set; }
    }
}
