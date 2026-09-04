using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityOpenFeature.Core;
using UnityOpenFeature.Telemetry;

namespace UnityOpenFeature.Providers
{
    [Serializable]
    public class ConfidenceProvider : IFeatureProvider
    {
        public string Name => "ConfidenceProvider";
        public bool IsReady { get; private set; }

        public event Action OnReady;

        [SerializeField] private string clientSecret;
        [SerializeField] private string baseUrl = ConfidenceApiClient.DefaultBaseUrl;
        private ConfidenceApiClient apiClient;

        private Dictionary<string, ConfidenceApiClient.ResolvedFlag> flagDictionary = new Dictionary<string, ConfidenceApiClient.ResolvedFlag>();
        private string resolveToken;

        public ConfidenceProvider(
            string clientSecret,
            string baseUrl = ConfidenceApiClient.DefaultBaseUrl,
            bool disableTelemetry = false)
        {
            this.clientSecret = clientSecret;
            this.baseUrl = ConfidenceEndpointUrls.NormalizeBaseUrl(baseUrl);
            this.apiClient = ConfidenceApiClient.Create(clientSecret, this.baseUrl, disableTelemetry);
            InitializeDictionary();
        }

        private async void FetchAndActivate(Action<bool, string> callback = null)
        {
            if (string.IsNullOrEmpty(clientSecret))
            {
                Debug.LogError("ClientSecret is required for ConfidenceProvider");
                callback?.Invoke(false, "ClientSecret is required");
                return;
            }

            var evalContext = GetEvaluationContext();

            await apiClient.ResolveFlagsAsync(new List<string>(), evalContext, (responseData, error) =>
            {
                if (error != null)
                {
                    callback?.Invoke(false, error);
                    return;
                }

                if (responseData != null && responseData.resolvedFlags != null)
                {
                    foreach (var resolvedFlag in responseData.resolvedFlags)
                    {
                        // Store flags without "flags/" prefix for easy resolution
                        var flagKey = resolvedFlag.flag.StartsWith("flags/") ?
                            resolvedFlag.flag.Substring(6) : resolvedFlag.flag;
                        flagDictionary[flagKey] = resolvedFlag;
                    }

                    // Store resolve token for apply requests
                    if (!string.IsNullOrEmpty(responseData.resolveToken))
                    {
                        this.resolveToken = responseData.resolveToken;
                    }

                    Debug.Log($"Successfully updated {responseData.resolvedFlags.Count} flags");
                    callback?.Invoke(true, "Flags fetched successfully");
                }
                else
                {
                    Debug.LogWarning("No flags received in response");
                    callback?.Invoke(false, "No flags received in response");
                }
            });
        }

        public void ApplyFlags(string flagKey)
        {
            if (string.IsNullOrEmpty(clientSecret))
            {
                Debug.LogError("ClientSecret is required for ConfidenceProvider");
                return;
            }

            if (string.IsNullOrEmpty(flagKey))
            {
                Debug.LogWarning("No flag keys provided for applyFlags");
                return;
            }

            if (string.IsNullOrEmpty(this.resolveToken))
            {
                Debug.LogError("No resolve token available. Make sure to call FetchAndActivate first.");
                return;
            }

            apiClient.ApplyFlag(flagKey, this.resolveToken);
        }

        public void Shutdown()
        {
            IsReady = false;
            flagDictionary.Clear();
            resolveToken = null;
            Debug.Log("ConfidenceProvider shutdown");
        }

        public void Initialize(Action<bool, string> callback = null)
        {
            InitializeDictionary();
            FetchAndActivate((success, error) =>
            {
                IsReady = success;
                callback?.Invoke(success, error);
                if (success)
                {
                    Debug.Log($"ConfidenceProvider initialized and flags fetched successfully");
                    OnReady?.Invoke();
                }
                else
                {
                    Debug.LogError($"ConfidenceProvider initialized but failed to fetch flags: {error}");
                }
            });
        }

        public void OnContextSet(EvaluationContext oldContext, EvaluationContext newContext, Action<bool, string> callback = null)
        {
                FetchAndActivate((success, error) =>
                {
                    if (success)
                    {
                        Debug.Log($"Context updated and flags refreshed successfully");
                    }
                    else
                    {
                        Debug.LogError($"Context updated but failed to refresh flags: {error}");
                    }
                    callback?.Invoke(success, error);
                });
        }

        private void InitializeDictionary()
        {
            flagDictionary.Clear();
        }

        public void shutdown() {
            if (apiClient != null)
            {
                apiClient.Dispose();
                apiClient = null;
            }
        }

        public ResolutionDetails<bool> ResolveBooleanValue(string flagKey, bool defaultValue)
        {
            var objectResult = ResolveObjectValueCore<object>(flagKey, defaultValue);
            
            if (objectResult.ErrorCode != ErrorCode.None)
            {
                return TrackEvaluation(ResolutionDetails<bool>.Error(flagKey, defaultValue, objectResult.ErrorCode, objectResult.ErrorMessage));
            }

            // Try to cast the object value to bool
            if (objectResult.Value is bool boolValue)
            {
                return TrackEvaluation(new ResolutionDetails<bool>(boolValue, flagKey)
                {
                    Reason = objectResult.Reason,
                    Variant = objectResult.Variant
                });
            }
            
            // Try to parse string representation to bool
            if (bool.TryParse(objectResult.Value?.ToString(), out var parsedBool))
            {
                return TrackEvaluation(new ResolutionDetails<bool>(parsedBool, flagKey)
                {
                    Reason = objectResult.Reason,
                    Variant = objectResult.Variant
                });
            }

            return TrackEvaluation(ResolutionDetails<bool>.Error(flagKey, defaultValue, ErrorCode.TypeMismatch, $"Cannot convert '{objectResult.Value}' to boolean"));
        }

        private void tryApply(ConfidenceApiClient.ResolvedFlag resolvedFlag, string rootFlagKey) {
         if (resolvedFlag.shouldApply && resolvedFlag.reason != "TARGETING_KEY_ERROR")
                {
                    _ = Task.Run(() => {
                        ApplyFlags(rootFlagKey);
                    });
                }   
        }

        public ResolutionDetails<string> ResolveStringValue(string flagKey, string defaultValue)
        {
            var objectResult = ResolveObjectValueCore<object>(flagKey, defaultValue);
            
            if (objectResult.ErrorCode != ErrorCode.None)
            {
                return TrackEvaluation(ResolutionDetails<string>.Error(flagKey, defaultValue, objectResult.ErrorCode, objectResult.ErrorMessage));
            }

            // Convert the object value to string
            var stringValue = objectResult.Value?.ToString() ?? defaultValue;
            
            return TrackEvaluation(new ResolutionDetails<string>(stringValue, flagKey)
            {
                Reason = objectResult.Reason,
                Variant = objectResult.Variant
            });
        }

        public ResolutionDetails<int> ResolveIntegerValue(string flagKey, int defaultValue)
        {
            var objectResult = ResolveObjectValueCore<object>(flagKey, defaultValue);
            
            if (objectResult.ErrorCode != ErrorCode.None)
            {
                return TrackEvaluation(ResolutionDetails<int>.Error(flagKey, defaultValue, objectResult.ErrorCode, objectResult.ErrorMessage));
            }

            // Try to cast the object value to int
            if (objectResult.Value is int intValue)
            {
                return TrackEvaluation(new ResolutionDetails<int>(intValue, flagKey)
                {
                    Reason = objectResult.Reason,
                    Variant = objectResult.Variant
                });
            }
            
            // Try to parse string representation to int
            if (int.TryParse(objectResult.Value?.ToString(), out var parsedInt))
            {
                return TrackEvaluation(new ResolutionDetails<int>(parsedInt, flagKey)
                {
                    Reason = objectResult.Reason,
                    Variant = objectResult.Variant
                });
            }

            return TrackEvaluation(ResolutionDetails<int>.Error(flagKey, defaultValue, ErrorCode.TypeMismatch, $"Cannot convert '{objectResult.Value}' to integer"));
        }

        public ResolutionDetails<float> ResolveFloatValue(string flagKey, float defaultValue)
        {
            var objectResult = ResolveObjectValueCore<object>(flagKey, defaultValue);
            
            if (objectResult.ErrorCode != ErrorCode.None)
            {
                return TrackEvaluation(ResolutionDetails<float>.Error(flagKey, defaultValue, objectResult.ErrorCode, objectResult.ErrorMessage));
            }

            // Try to cast the object value to float
            if (objectResult.Value is float floatValue)
            {
                return TrackEvaluation(new ResolutionDetails<float>(floatValue, flagKey)
                {
                    Reason = objectResult.Reason,
                    Variant = objectResult.Variant
                });
            }
            
            // Try to cast from double (common JSON numeric type)
            if (objectResult.Value is double doubleValue)
            {
                return TrackEvaluation(new ResolutionDetails<float>((float)doubleValue, flagKey)
                {
                    Reason = objectResult.Reason,
                    Variant = objectResult.Variant
                });
            }
            
            // Try to parse string representation to float
            if (float.TryParse(objectResult.Value?.ToString(), out var parsedFloat))
            {
                return TrackEvaluation(new ResolutionDetails<float>(parsedFloat, flagKey)
                {
                    Reason = objectResult.Reason,
                    Variant = objectResult.Variant
                });
            }

            return TrackEvaluation(ResolutionDetails<float>.Error(flagKey, defaultValue, ErrorCode.TypeMismatch, $"Cannot convert '{objectResult.Value}' to float"));
        }

        public ResolutionDetails<T> ResolveObjectValue<T>(string flagKey, T defaultValue)
        {
            return TrackEvaluation(ResolveObjectValueCore(flagKey, defaultValue));
        }

        private ResolutionDetails<T> ResolveObjectValueCore<T>(string flagKey, T defaultValue)
        {
            var value = ResolveValueByDotNotation(flagKey);
            if (value == null)
            {
                return ResolutionDetails<T>.Error(flagKey, defaultValue, ErrorCode.FlagNotFound, $"Flag '{flagKey}' not found");
            }

            var rootFlagKey = flagKey.Split('.')[0];
            var resolvedFlag = GetResolvedFlag(rootFlagKey);

            ResolutionDetails<T> details;
            try
            {
                if (value is T directValue)
                {
                    details = new ResolutionDetails<T>(directValue, flagKey);
                }
                else
                {
                    var jsonValue = JsonConvert.DeserializeObject<T>(value.ToString());
                    details = new ResolutionDetails<T>(jsonValue, flagKey);
                }
            }
            catch (Exception ex)
            {
                return ResolutionDetails<T>.Error(flagKey, defaultValue, ErrorCode.ParseError, $"Cannot parse: {ex.Message}");
            }

            if (resolvedFlag != null)
            {
                details.Reason = MapResolveReasonToReason(resolvedFlag.reason);
                details.Variant = resolvedFlag.variant;

                tryApply(resolvedFlag, rootFlagKey);
            }

            return details;
        }

        private object ResolveValueByDotNotation(string flagKey)
        {
            if (string.IsNullOrEmpty(flagKey))
                return null;

            // Split the flag key by dots
            string[] pathParts = flagKey.Split('.');

            if (pathParts.Length == 0) {
                return null;
            }
                

            // Get the root flag
            if (!flagDictionary.TryGetValue(pathParts[0], out var resolvedFlag)) {
            
                return null;
            }


            // Start with the resolved flag's value
            object currentValue = resolvedFlag.value;

            // Navigate through the nested structure (skip the first part since we already resolved it)
            for (int i = 1; i < pathParts.Length; i++)
            {
                string part = pathParts[i];

                if (currentValue == null) {
                    return null;
                }
                
                Debug.Log("Dict here in for");

                if (currentValue is Dictionary<string, object> dict)
                {
                    if (!dict.TryGetValue(part, out currentValue))
                        return null;
                }
                else
                {
                    // If we encounter a non-dictionary value before the end of the path
                    return null;
                }
            }
            return currentValue;
        }

        private ConfidenceApiClient.ResolvedFlag GetResolvedFlag(string flagKey)
        {
            return flagDictionary.TryGetValue(flagKey, out var resolvedFlag) ? resolvedFlag : null;
        }

        private Reason MapResolveReasonToReason(string resolveReason)
        {
            return resolveReason switch
            {
                "RESOLVE_REASON_MATCH" => UnityOpenFeature.Core.Reason.RESOLVE_REASON_MATCH,
                "RESOLVE_REASON_STALE" => UnityOpenFeature.Core.Reason.RESOLVE_REASON_STALE,
                "RESOLVE_REASON_NO_SEGMENT_MATCH" => UnityOpenFeature.Core.Reason.RESOLVE_REASON_NO_SEGMENT_MATCH,
                "RESOLVE_REASON_NO_TREATMENT_MATCH" => UnityOpenFeature.Core.Reason.RESOLVE_REASON_NO_TREATMENT_MATCH,
                "RESOLVE_REASON_TARGETING_KEY_ERROR" => UnityOpenFeature.Core.Reason.RESOLVE_REASON_TARGETING_KEY_ERROR,
                "RESOLVE_REASON_FLAG_ARCHIVED" => UnityOpenFeature.Core.Reason.RESOLVE_REASON_FLAG_ARCHIVED,
                "DEFAULT" => UnityOpenFeature.Core.Reason.DEFAULT,
                "ERROR" => UnityOpenFeature.Core.Reason.ERROR,
                _ => UnityOpenFeature.Core.Reason.RESOLVE_REASON_UNSPECIFIED
            };
        }

        private ResolutionDetails<T> TrackEvaluation<T>(ResolutionDetails<T> details)
        {
            try
            {
                if (apiClient?.Telemetry != null)
                {
                    var (reason, errorCode) = Telemetry.Telemetry.MapEvaluationResult(details.Reason, details.ErrorCode);
                    apiClient.Telemetry.TrackEvaluation(reason, errorCode);
                    apiClient.FlushTelemetryIfNeeded();
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Telemetry eval tracking error (best-effort): {ex.Message}");
            }

            return details;
        }

        public void Track(string trackingEventName, EvaluationContext context, TrackingEventDetails details)
        {
            try
            {
                var payload = new Dictionary<string, object>();

                if (details?.Data != null)
                {
                    foreach (var kvp in details.Data)
                    {
                        if (kvp.Key != "context")
                            payload[kvp.Key] = kvp.Value;
                    }
                }

                if (details?.Value != null)
                {
                    payload["value"] = details.Value.Value;
                }

                // Build context dict from EvaluationContext
                var contextDict = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(context?.TargetingKey))
                    contextDict["targeting_key"] = context.TargetingKey;
                if (context?.attributes != null)
                {
                    foreach (var attr in context.attributes)
                        contextDict[attr.Key] = attr.Value;
                }
                if (contextDict.Count > 0)
                    payload["context"] = contextDict;

                apiClient.TrackEvent($"eventDefinitions/{trackingEventName}", payload);
            }
            catch (Exception ex)
            {
                Debug.Log($"Track error (best-effort): {ex.Message}");
            }
        }

        private Dictionary<string, object> GetEvaluationContext()
        {
            var context = OpenFeatureAPI.Instance.EvaluationContext;
            var evaluationContext = new Dictionary<string, object>();

            foreach (var attr in context.attributes)
            {
                evaluationContext[attr.Key] = attr.Value;
            }

            return evaluationContext;
        }
    }
}
