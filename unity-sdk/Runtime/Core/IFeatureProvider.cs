namespace UnityOpenFeature.Core
{
    public interface IFeatureProvider
    {
        string Name { get; }
        bool IsReady { get; }
        ResolutionDetails<bool> ResolveBooleanValue(string flagKey, bool defaultValue, EvaluationContext context);
        ResolutionDetails<string> ResolveStringValue(string flagKey, string defaultValue, EvaluationContext context);
        ResolutionDetails<int> ResolveIntegerValue(string flagKey, int defaultValue, EvaluationContext context);
        ResolutionDetails<float> ResolveFloatValue(string flagKey, float defaultValue, EvaluationContext context);
        ResolutionDetails<T> ResolveObjectValue<T>(string flagKey, T defaultValue, EvaluationContext context);
        void Initialize(EvaluationContext context);
        void Shutdown();
    }
}

