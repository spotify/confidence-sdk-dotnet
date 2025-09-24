using System;

namespace UnityOpenFeature.Core
{
    public interface IFeatureProvider
    {
        string Name { get; }
        bool IsReady { get; }
        ResolutionDetails<bool> ResolveBooleanValue(string flagKey, bool defaultValue);
        ResolutionDetails<string> ResolveStringValue(string flagKey, string defaultValue);
        ResolutionDetails<int> ResolveIntegerValue(string flagKey, int defaultValue);
        ResolutionDetails<float> ResolveFloatValue(string flagKey, float defaultValue);
        ResolutionDetails<T> ResolveObjectValue<T>(string flagKey, T defaultValue);
        void Initialize(Action<bool, string> callback = null);
        void OnContextSet(EvaluationContext oldContext, EvaluationContext newContext, Action<bool, string> callback = null);
        void Shutdown();

        event Action OnReady;
    }
}

