using UnityOpenFeature.Core;

namespace UnityOpenFeature.Client
{
    public interface IFeatureClient
    {
        bool GetBooleanValue(string flagKey, bool defaultValue, EvaluationContext context = null);
        string GetStringValue(string flagKey, string defaultValue, EvaluationContext context = null);
        int GetIntegerValue(string flagKey, int defaultValue, EvaluationContext context = null);
        float GetFloatValue(string flagKey, float defaultValue, EvaluationContext context = null);
        T GetObjectValue<T>(string flagKey, T defaultValue, EvaluationContext context = null);
        
        ResolutionDetails<bool> GetBooleanDetails(string flagKey, bool defaultValue, EvaluationContext context = null);
        ResolutionDetails<string> GetStringDetails(string flagKey, string defaultValue, EvaluationContext context = null);
        ResolutionDetails<int> GetIntegerDetails(string flagKey, int defaultValue, EvaluationContext context = null);
        ResolutionDetails<float> GetFloatDetails(string flagKey, float defaultValue, EvaluationContext context = null);
        ResolutionDetails<T> GetObjectDetails<T>(string flagKey, T defaultValue, EvaluationContext context = null);
    }
}

