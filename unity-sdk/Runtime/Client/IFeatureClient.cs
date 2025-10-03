using UnityOpenFeature.Core;

namespace UnityOpenFeature.Client
{
    public interface IFeatureClient
    {
        bool GetBooleanValue(string flagKey, bool defaultValue);
        string GetStringValue(string flagKey, string defaultValue);
        int GetIntegerValue(string flagKey, int defaultValue);
        float GetFloatValue(string flagKey, float defaultValue);
        T GetObjectValue<T>(string flagKey, T defaultValue);

        ResolutionDetails<bool> GetBooleanDetails(string flagKey, bool defaultValue);
        ResolutionDetails<string> GetStringDetails(string flagKey, string defaultValue);
        ResolutionDetails<int> GetIntegerDetails(string flagKey, int defaultValue);
        ResolutionDetails<float> GetFloatDetails(string flagKey, float defaultValue);
        ResolutionDetails<T> GetObjectDetails<T>(string flagKey, T defaultValue);
    }
}

