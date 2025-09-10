using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenFeature;
using OpenFeature.Model;

namespace UnityLibrary
{
    /// <summary>
    /// Custom InMemoryProvider implementation that provides default values for feature flags
    /// This is a standalone implementation that can be used independently and integrates with OpenFeature
    /// </summary>
    public class CustomInMemoryProvider : FeatureProvider
    {
        private readonly Dictionary<string, object> _defaultFlags;
        private bool _isReady;

        public string Name => "CustomInMemoryProvider";
        public bool IsReady => _isReady;

        public CustomInMemoryProvider()
        {
            _defaultFlags = new Dictionary<string, object>();
            InitializeDefaultFlags();
        }

        /// <summary>
        /// Initialize with default values for common feature flags
        /// </summary>
        private void InitializeDefaultFlags()
        {
            // Player movement settings
            _defaultFlags["player_speed_multiplier"] = 1.5f;
            _defaultFlags["enable_double_jump"] = true;
            _defaultFlags["max_jump_count"] = 2;
            
            // Game features
            _defaultFlags["enable_debug_mode"] = false;
            _defaultFlags["show_fps_counter"] = true;
            _defaultFlags["enable_sound_effects"] = true;
            
            // UI settings
            _defaultFlags["theme_color"] = "blue";
            _defaultFlags["enable_animations"] = true;
            _defaultFlags["tutorial_enabled"] = true;
            
            // Gameplay mechanics
            _defaultFlags["difficulty_level"] = 1;
            _defaultFlags["enable_power_ups"] = true;
            _defaultFlags["auto_save_enabled"] = true;
        }

        public override Task Initialize(EvaluationContext context = null)
        {
            _isReady = true;
            // In a real implementation, you might load flags from context or external source
            Console.WriteLine($"CustomInMemoryProvider initialized with {_defaultFlags.Count} default flags");
            return Task.CompletedTask;
        }

        public override Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null)
        {
            var value = GetBooleanValue(flagKey, defaultValue);
            var result = new ResolutionDetails<bool>(flagKey, value);
            return Task.FromResult(result);
        }

        public override Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue, EvaluationContext context = null)
        {
            var value = GetStringValue(flagKey, defaultValue);
            var result = new ResolutionDetails<string>(flagKey, value);
            return Task.FromResult(result);
        }

        public override Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue, EvaluationContext context = null)
        {
            var value = GetIntegerValue(flagKey, defaultValue);
            var result = new ResolutionDetails<int>(flagKey, value);
            return Task.FromResult(result);
        }

        public override Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue, EvaluationContext context = null)
        {
            var value = (double)GetFloatValue(flagKey, (float)defaultValue);
            var result = new ResolutionDetails<double>(flagKey, value);
            return Task.FromResult(result);
        }

        public override Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue, EvaluationContext context = null)
        {
            var result = new ResolutionDetails<Value>(flagKey, defaultValue);
            return Task.FromResult(result);
        }

        public override Task Shutdown()
        {
            _isReady = false;
            Console.WriteLine("CustomInMemoryProvider shutdown");
            return Task.CompletedTask;
        }

        public override Metadata GetMetadata()
        {
            return new Metadata(Name);
        }

        /// <summary>
        /// Get a boolean value for the specified flag key
        /// </summary>
        public bool GetBooleanValue(string flagKey, bool defaultValue = false)
        {
            if (_defaultFlags.TryGetValue(flagKey, out var value))
            {
                if (value is bool boolValue) return boolValue;
                if (bool.TryParse(value?.ToString(), out var parsedBool)) return parsedBool;
            }
            return defaultValue;
        }

        /// <summary>
        /// Get a string value for the specified flag key
        /// </summary>
        public string GetStringValue(string flagKey, string defaultValue = "")
        {
            if (_defaultFlags.TryGetValue(flagKey, out var value))
            {
                return value?.ToString() ?? defaultValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Get an integer value for the specified flag key
        /// </summary>
        public int GetIntegerValue(string flagKey, int defaultValue = 0)
        {
            if (_defaultFlags.TryGetValue(flagKey, out var value))
            {
                if (value is int intValue) return intValue;
                if (int.TryParse(value?.ToString(), out var parsedInt)) return parsedInt;
            }
            return defaultValue;
        }

        /// <summary>
        /// Get a float value for the specified flag key
        /// </summary>
        public float GetFloatValue(string flagKey, float defaultValue = 0f)
        {
            if (_defaultFlags.TryGetValue(flagKey, out var value))
            {
                if (value is float floatValue) return floatValue;
                if (float.TryParse(value?.ToString(), out var parsedFloat)) return parsedFloat;
            }
            return defaultValue;
        }

        /// <summary>
        /// Get a typed value for the specified flag key
        /// </summary>
        public T GetValue<T>(string flagKey, T defaultValue = default(T))
        {
            if (_defaultFlags.TryGetValue(flagKey, out var value))
            {
                if (value is T directValue) return directValue;
                
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    // Fall back to default if conversion fails
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Allows setting custom flag values at runtime
        /// </summary>
        public void SetFlag(string key, object value)
        {
            _defaultFlags[key] = value;
        }

        /// <summary>
        /// Gets all available flag keys
        /// </summary>
        public IEnumerable<string> GetAvailableFlags()
        {
            return _defaultFlags.Keys;
        }
    }
}
