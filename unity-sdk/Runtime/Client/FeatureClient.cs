using UnityEngine;
using UnityOpenFeature.Core;

namespace UnityOpenFeature.Client
{
    public class FeatureClient : IFeatureClient
    {
        private readonly IOpenFeatureAPI api;
        private readonly string domain;

        public FeatureClient(IOpenFeatureAPI api, string domain)
        {
            this.api = api;
            this.domain = domain;
        }

        public bool GetBooleanValue(string flagKey, bool defaultValue)
        {
            return GetBooleanDetails(flagKey, defaultValue).Value;
        }

        public string GetStringValue(string flagKey, string defaultValue)
        {
            return GetStringDetails(flagKey, defaultValue).Value;
        }

        public int GetIntegerValue(string flagKey, int defaultValue)
        {
            return GetIntegerDetails(flagKey, defaultValue).Value;
        }

        public float GetFloatValue(string flagKey, float defaultValue)
        {
            return GetFloatDetails(flagKey, defaultValue).Value;
        }

        public T GetObjectValue<T>(string flagKey, T defaultValue)
        {
            return GetObjectDetails(flagKey, defaultValue).Value;
        }

        public ResolutionDetails<bool> GetBooleanDetails(string flagKey, bool defaultValue)
        {
            if (api.Provider == null) { Debug.LogWarning("No provider set"); return ResolutionDetails<bool>.Error(flagKey, defaultValue, ErrorCode.ProviderNotReady, "No provider"); }
            return api.Provider.ResolveBooleanValue(flagKey, defaultValue);
        }

        public ResolutionDetails<string> GetStringDetails(string flagKey, string defaultValue)
        {
            if (api.Provider == null) { Debug.LogWarning("No provider set"); return ResolutionDetails<string>.Error(flagKey, defaultValue, ErrorCode.ProviderNotReady, "No provider"); }
            return api.Provider.ResolveStringValue(flagKey, defaultValue);
        }

        public ResolutionDetails<int> GetIntegerDetails(string flagKey, int defaultValue)
        {
            if (api.Provider == null) { Debug.LogWarning("No provider set"); return ResolutionDetails<int>.Error(flagKey, defaultValue, ErrorCode.ProviderNotReady, "No provider"); }
            return api.Provider.ResolveIntegerValue(flagKey, defaultValue);
        }

        public ResolutionDetails<float> GetFloatDetails(string flagKey, float defaultValue)
        {
            if (api.Provider == null) { Debug.LogWarning("No provider set"); return ResolutionDetails<float>.Error(flagKey, defaultValue, ErrorCode.ProviderNotReady, "No provider"); }
            return api.Provider.ResolveFloatValue(flagKey, defaultValue);
        }

        public ResolutionDetails<T> GetObjectDetails<T>(string flagKey, T defaultValue)
        {
            if (api.Provider == null) { Debug.LogWarning("No provider set"); return ResolutionDetails<T>.Error(flagKey, defaultValue, ErrorCode.ProviderNotReady, "No provider"); }
            return api.Provider.ResolveObjectValue<T>(flagKey, defaultValue);
        }
    }
}

