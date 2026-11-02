using UnityOpenFeature.Core;

namespace UnityOpenFeature.Client
{
    public interface IFeatureClient
    {
        bool GetBooleanValue(string flagKey, bool defaultValue);
        string GetStringValue(string flagKey, string defaultValue);
        int GetIntegerValue(string flagKey, int defaultValue);
        float GetFloatValue(string flagKey, float defaultValue);
        /// <summary>Gets an object flag value.</summary>
        /// <remarks>Support for list/array flag schema values is deprecated; they still resolve for
        /// compatibility, but new flags should use struct schemas. Context/event arrays are unaffected.</remarks>
        T GetObjectValue<T>(string flagKey, T defaultValue);

        ResolutionDetails<bool> GetBooleanDetails(string flagKey, bool defaultValue);
        ResolutionDetails<string> GetStringDetails(string flagKey, string defaultValue);
        ResolutionDetails<int> GetIntegerDetails(string flagKey, int defaultValue);
        ResolutionDetails<float> GetFloatDetails(string flagKey, float defaultValue);
        ResolutionDetails<T> GetObjectDetails<T>(string flagKey, T defaultValue);

        void Track(string trackingEventName, TrackingEventDetails details = null);
    }
}

