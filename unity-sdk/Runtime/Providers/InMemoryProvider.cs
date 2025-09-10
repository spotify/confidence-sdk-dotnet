using System;
using System.Collections.Generic;
using UnityEngine;
using UnityOpenFeature.Core;

namespace UnityOpenFeature.Providers
{
    [Serializable]
    public class InMemoryProvider : IFeatureProvider
    {
        public string Name => "InMemoryProvider";
        public bool IsReady { get; private set; }

        [SerializeField] private List<FeatureFlag> flags = new List<FeatureFlag>();
        private Dictionary<string, object> flagDictionary = new Dictionary<string, object>();

        public InMemoryProvider() { InitializeDictionary(); }

        public void SetFlag(string key, object value)
        {
            flagDictionary[key] = value;
            var existing = flags.Find(f => f.Key == key);
            if (existing != null) existing.Value = value?.ToString() ?? ""; else flags.Add(new FeatureFlag { Key = key, Value = value?.ToString() ?? "" });
        }

        public void Initialize(Action<bool, string> callback = null)
        {
            InitializeDictionary();
            IsReady = true;
            callback?.Invoke(true, "InMemoryProvider initialized");
            Debug.Log($"InMemoryProvider initialized with {flags.Count} flags");
        }

        public void OnContextSet(EvaluationContext oldContext, EvaluationContext newContext, Action<bool, string> callback = null)
        {
            // Empty implementation for InMemoryProvider - context changes don't require fetching
            callback?.Invoke(true, "InMemoryProvider context updated");
        }

        public void Shutdown()
        {
            IsReady = false;
            Debug.Log("InMemoryProvider shutdown");
        }

        private void InitializeDictionary()
        {
            flagDictionary.Clear();
            foreach (var flag in flags) flagDictionary[flag.Key] = flag.Value;
        }

        public ResolutionDetails<bool> ResolveBooleanValue(string flagKey, bool defaultValue)
        {
            if (!flagDictionary.TryGetValue(flagKey, out var value)) return ResolutionDetails<bool>.Error(flagKey, defaultValue, ErrorCode.FlagNotFound, $"Flag '{flagKey}' not found");
            return bool.TryParse(value.ToString(), out var boolValue) ? new ResolutionDetails<bool>(boolValue, flagKey) : ResolutionDetails<bool>.Error(flagKey, defaultValue, ErrorCode.TypeMismatch, $"Cannot convert '{value}' to boolean");
        }

        public ResolutionDetails<string> ResolveStringValue(string flagKey, string defaultValue)
        {
            if (!flagDictionary.TryGetValue(flagKey, out var value)) return ResolutionDetails<string>.Error(flagKey, defaultValue, ErrorCode.FlagNotFound, $"Flag '{flagKey}' not found");
            return new ResolutionDetails<string>(value?.ToString() ?? defaultValue, flagKey);
        }

        public ResolutionDetails<int> ResolveIntegerValue(string flagKey, int defaultValue)
        {
            if (!flagDictionary.TryGetValue(flagKey, out var value)) return ResolutionDetails<int>.Error(flagKey, defaultValue, ErrorCode.FlagNotFound, $"Flag '{flagKey}' not found");
            return int.TryParse(value.ToString(), out var intValue) ? new ResolutionDetails<int>(intValue, flagKey) : ResolutionDetails<int>.Error(flagKey, defaultValue, ErrorCode.TypeMismatch, $"Cannot convert '{value}' to integer");
        }

        public ResolutionDetails<float> ResolveFloatValue(string flagKey, float defaultValue)
        {
            if (!flagDictionary.TryGetValue(flagKey, out var value)) return ResolutionDetails<float>.Error(flagKey, defaultValue, ErrorCode.FlagNotFound, $"Flag '{flagKey}' not found");
            return float.TryParse(value.ToString(), out var floatValue) ? new ResolutionDetails<float>(floatValue, flagKey) : ResolutionDetails<float>.Error(flagKey, defaultValue, ErrorCode.TypeMismatch, $"Cannot convert '{value}' to float");
        }

        public ResolutionDetails<T> ResolveObjectValue<T>(string flagKey, T defaultValue)
        {
            if (!flagDictionary.TryGetValue(flagKey, out var value)) return ResolutionDetails<T>.Error(flagKey, defaultValue, ErrorCode.FlagNotFound, $"Flag '{flagKey}' not found");
            try
            {
                if (value is T directValue) return new ResolutionDetails<T>(directValue, flagKey);
                var jsonValue = JsonUtility.FromJson<T>(value.ToString());
                return new ResolutionDetails<T>(jsonValue, flagKey);
            }
            catch (Exception ex) { return ResolutionDetails<T>.Error(flagKey, defaultValue, ErrorCode.ParseError, $"Cannot parse: {ex.Message}"); }
        }

        [Serializable]
        public class FeatureFlag
        {
            public string Key;
            public string Value;
        }
    }
}

