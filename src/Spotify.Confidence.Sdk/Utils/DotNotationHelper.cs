using System.Text.Json;
using Spotify.Confidence.Sdk.Models;

namespace Spotify.Confidence.Sdk.Utils;

/// <summary>
/// Helper class for handling dot-notation parsing and value navigation in confidence flags.
/// </summary>
internal static class DotNotationHelper
{
    /// <summary>
    /// Parses a dot-notation string into flag name and property path.
    /// </summary>

    /// <param name="flagKey">The flag key that may contain dot notation (e.g., "flag.property.subproperty").</param>
    /// <returns>A tuple containing the base flag name and the property path.</returns>
    public static (string FlagName, string[] PropertyPath) ParseDotNotation(string flagKey)
    {
        if (string.IsNullOrEmpty(flagKey))
        {
            return (string.Empty, Array.Empty<string>());
        }

        var parts = flagKey.Split('.');
        if (parts.Length == 1)
        {
            // No dot notation, return the flag key as is
            return (flagKey, Array.Empty<string>());
        }

        // First part is the flag name, rest is the property path
        var flagName = parts[0];
        var propertyPath = parts.Skip(1).ToArray();

        return (flagName, propertyPath);
    }

    /// <summary>
    /// Navigates through nested object properties using a property path.
    /// </summary>

    /// <param name="value">The starting value (typically flag.Value or the "value" property).</param>
    /// <param name="propertyPath">Array of property names to navigate through.</param>
    /// <returns>The value at the end of the property path, or null if not found.</returns>
    public static object? NavigateToProperty(object? value, string[] propertyPath)
    {
        if (value == null || propertyPath.Length == 0)
        {
            return value;
        }

        var currentValue = value;

        foreach (var propertyName in propertyPath)
        {
            currentValue = GetPropertyValue(currentValue, propertyName);
            if (currentValue == null)
            {
                return null; // Property not found or is null
            }
        }

        return currentValue;
    }

    /// <summary>
    /// Gets a property value from an object, handling both Dictionary and JsonElement types.
    /// </summary>

    /// <param name="obj">The object to get the property from.</param>
    /// <param name="propertyName">The name of the property to get.</param>
    /// <returns>The property value, or null if not found.</returns>
    private static object? GetPropertyValue(object? obj, string propertyName)
    {
        if (obj == null)
        {
            return null;
        }

        // Handle Dictionary<string, object>
        if (obj is Dictionary<string, object> dictionary)
        {
            return dictionary.TryGetValue(propertyName, out var value) ? value : null;
        }

        // Handle JsonElement
        if (obj is JsonElement jsonElement &&
            jsonElement.ValueKind == JsonValueKind.Object &&
            jsonElement.TryGetProperty(propertyName, out var property))
        {
            return property;
        }

        // Handle IDictionary<string, object> (more general case)
        if (obj is IDictionary<string, object> iDictionary)
        {
            return iDictionary.TryGetValue(propertyName, out var value) ? value : null;
        }

        return null;
    }

    /// <summary>
    /// Extracts the appropriate value from a ResolvedFlag, handling both dot-notation and regular flags.
    /// </summary>

    /// <param name="flagValue">The Value dictionary from a ResolvedFlag.</param>
    /// <param name="propertyPath">The property path to navigate (empty for regular flags).</param>
    /// <returns>The extracted value.</returns>
    public static object? ExtractFlagValue(Dictionary<string, object> flagValue, string[] propertyPath)
    {
        // If there's a property path, always navigate directly from the root dictionary.
        // This ensures that user-defined properties named "value" (whether primitive or object)
        // don't interfere with accessing sibling properties.
        if (propertyPath.Length > 0)
        {
            return NavigateToProperty(flagValue, propertyPath);
        }

        // No property path - for backward compatibility, return the "value" content if it exists.
        // This handles the common case where a simple flag has a single "value" property.
        if (flagValue.TryGetValue("value", out var wrappedValue))
        {
            return wrappedValue;
        }

        return flagValue;
    }

    /// <summary>
    /// Extracts and converts a typed value from a ResolvedFlag using dot-notation.
    /// </summary>

    /// <typeparam name="T">The target type to convert the value to.</typeparam>
    /// <param name="flag">The resolved flag.</param>
    /// <param name="flagKey">The original flag key (may contain dot-notation).</param>
    /// <param name="defaultValue">The default value to return if extraction fails.</param>
    /// <param name="jsonOptions">JSON serializer options for complex type conversion.</param>
    /// <returns>A tuple containing the typed value and any error message.</returns>
    public static (T Value, string? ErrorMessage) ExtractTypedValue<T>(
        ResolvedFlag flag,
        string flagKey,
        T defaultValue,
        JsonSerializerOptions? jsonOptions = null)
    {
        try
        {
            // Parse dot-notation to get property path
            var (_, propertyPath) = ParseDotNotation(flagKey);

            // Extract the raw value
            var value = ExtractFlagValue(flag.Value, propertyPath);

            if (value == null && propertyPath.Length > 0)
            {
                return (defaultValue, $"Property path '{string.Join(".", propertyPath)}' not found in flag '{flag.Flag}'");
            }

            // Convert to target type
            var typedValue = ConvertToType<T>(value, defaultValue, jsonOptions);
            return (typedValue, null);
        }
        catch (Exception ex)
        {
            return (defaultValue, $"Failed to extract value from flag: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts a value to the target type, handling JsonElement and other conversions.
    /// </summary>

    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="defaultValue">The default value if conversion fails.</param>
    /// <param name="jsonOptions">JSON serializer options.</param>
    /// <returns>The converted value.</returns>
    private static T ConvertToType<T>(object? value, T defaultValue, JsonSerializerOptions? jsonOptions = null)
    {
        if (value == null)
        {
            return defaultValue;
        }

        // Handle JsonElement
        if (value is JsonElement element)
        {
            if (typeof(T) == typeof(bool))
            {
                return (T)(object)(element.ValueKind == JsonValueKind.True);
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)(element.GetString() ?? string.Empty);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)element.GetDouble();
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)element.GetInt32();
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)element.GetInt64();
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)element.GetSingle();
            }
            else if (typeof(T) == typeof(decimal))
            {
                return (T)(object)element.GetDecimal();
            }
            else
            {
                // For complex objects, deserialize using JsonSerializer
                var json = element.GetRawText();
                return JsonSerializer.Deserialize<T>(json, jsonOptions) ?? defaultValue;
            }
        }

        // Direct type conversion
        if (value is T directValue)
        {
            return directValue;
        }

        return defaultValue;
    }
}
