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

        public bool GetBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null)
        {
            return GetBooleanDetails(flagKey, defaultValue, context).Value;
        }

        public string GetStringValue(string flagKey, string defaultValue, EvaluationContext context = null)
        {
            return GetStringDetails(flagKey, defaultValue, context).Value;
        }

        public int GetIntegerValue(string flagKey, int defaultValue, EvaluationContext context = null)
        {
            return GetIntegerDetails(flagKey, defaultValue, context).Value;
        }

        public float GetFloatValue(string flagKey, float defaultValue, EvaluationContext context = null)
        {
            return GetFloatDetails(flagKey, defaultValue, context).Value;
        }

        public T GetObjectValue<T>(string flagKey, T defaultValue, EvaluationContext context = null)
        {
            return GetObjectDetails(flagKey, defaultValue, context).Value;
        }

        public ResolutionDetails<bool> GetBooleanDetails(string flagKey, bool defaultValue, EvaluationContext context = null)
        {
            if (api.Provider == null) { Debug.LogWarning("No provider set"); return ResolutionDetails<bool>.Error(flagKey, defaultValue, ErrorCode.ProviderNotReady, "No provider"); }
            return api.Provider.ResolveBooleanValue(flagKey, defaultValue, MergeContext(context));
        }

        public ResolutionDetails<string> GetStringDetails(string flagKey, string defaultValue, EvaluationContext context = null)
        {
            if (api.Provider == null) { Debug.LogWarning("No provider set"); return ResolutionDetails<string>.Error(flagKey, defaultValue, ErrorCode.ProviderNotReady, "No provider"); }
            return api.Provider.ResolveStringValue(flagKey, defaultValue, MergeContext(context));
        }

        public ResolutionDetails<int> GetIntegerDetails(string flagKey, int defaultValue, EvaluationContext context = null)
        {
            if (api.Provider == null) { Debug.LogWarning("No provider set"); return ResolutionDetails<int>.Error(flagKey, defaultValue, ErrorCode.ProviderNotReady, "No provider"); }
            return api.Provider.ResolveIntegerValue(flagKey, defaultValue, MergeContext(context));
        }

        public ResolutionDetails<float> GetFloatDetails(string flagKey, float defaultValue, EvaluationContext context = null)
        {
            if (api.Provider == null) { Debug.LogWarning("No provider set"); return ResolutionDetails<float>.Error(flagKey, defaultValue, ErrorCode.ProviderNotReady, "No provider"); }
            return api.Provider.ResolveFloatValue(flagKey, defaultValue, MergeContext(context));
        }

        public ResolutionDetails<T> GetObjectDetails<T>(string flagKey, T defaultValue, EvaluationContext context = null)
        {
            if (api.Provider == null) { Debug.LogWarning("No provider set"); return ResolutionDetails<T>.Error(flagKey, defaultValue, ErrorCode.ProviderNotReady, "No provider"); }
            return api.Provider.ResolveObjectValue(flagKey, defaultValue, MergeContext(context));
        }

        private EvaluationContext MergeContext(EvaluationContext requestContext)
        {
            var merged = new EvaluationContext(api.EvaluationContext.TargetingKey);
            if (requestContext != null) merged.TargetingKey = requestContext.TargetingKey;
            return merged;
        }
    }
}

