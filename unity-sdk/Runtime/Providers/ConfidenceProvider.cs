using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityOpenFeature.Core;

namespace UnityOpenFeature.Providers
{
    [Serializable]
    public class ConfidenceProvider : IFeatureProvider
    {
        public string Name => "ConfidenceProvider";
        public bool IsReady { get; private set; }

        [SerializeField] private string clientSecret;
        private ConfidenceApiClient apiClient;

        private Dictionary<string, ConfidenceApiClient.ResolvedFlag> flagDictionary = new Dictionary<string, ConfidenceApiClient.ResolvedFlag>();
        private string resolveToken;

        public ConfidenceProvider(string clientSecret)
        {
            this.clientSecret = clientSecret;
            this.apiClient = ConfidenceApiClient.Create(clientSecret);
            InitializeDictionary();
        }

        public async void FetchAndActivate(List<string> flagKeys, Action<bool, string> callback = null)
        {
            if (string.IsNullOrEmpty(clientSecret))
            {
                Debug.LogError("ClientSecret is required for ConfidenceProvider");
                callback?.Invoke(false, "ClientSecret is required");
                return;
            }

            if (flagKeys == null || flagKeys.Count == 0)
            {
                Debug.LogWarning("No flag keys provided for fetchAndActivate");
                callback?.Invoke(false, "No flag keys provided");
                return;
            }

            var evalContext = GetEvaluationContext();

            await apiClient.ResolveFlagsAsync(flagKeys, evalContext, (responseData, error) =>
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
                        Debug.Log("resolveToken is not null" + responseData.resolveToken);
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

        public void Initialize(EvaluationContext context)
        {
            InitializeDictionary();
            IsReady = true;
            Debug.Log($"ConfidenceProvider initialized with client secret");
        }

        private void InitializeDictionary()
        {
            flagDictionary.Clear();
            // Initialize with any default flags if needed
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
            var objectResult = ResolveObjectValue<object>(flagKey, defaultValue);
            
            if (objectResult.ErrorCode != ErrorCode.None)
            {
                return ResolutionDetails<bool>.Error(flagKey, defaultValue, objectResult.ErrorCode, objectResult.ErrorMessage);
            }

            // Try to cast the object value to bool
            if (objectResult.Value is bool boolValue)
            {
                return new ResolutionDetails<bool>(boolValue, flagKey)
                {
                    Reason = objectResult.Reason,
                    Variant = objectResult.Variant
                };
            }
            
            // Try to parse string representation to bool
            if (bool.TryParse(objectResult.Value?.ToString(), out var parsedBool))
            {
                return new ResolutionDetails<bool>(parsedBool, flagKey)
                {
                    Reason = objectResult.Reason,
                    Variant = objectResult.Variant
                };
            }

            return ResolutionDetails<bool>.Error(flagKey, defaultValue, ErrorCode.TypeMismatch, $"Cannot convert '{objectResult.Value}' to boolean");
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
            var objectResult = ResolveObjectValue<object>(flagKey, defaultValue);
            
            if (objectResult.ErrorCode != ErrorCode.None)
            {
                return ResolutionDetails<string>.Error(flagKey, defaultValue, objectResult.ErrorCode, objectResult.ErrorMessage);
            }

            // Convert the object value to string
            var stringValue = objectResult.Value?.ToString() ?? defaultValue;
            
            return new ResolutionDetails<string>(stringValue, flagKey)
            {
                Reason = objectResult.Reason,
                Variant = objectResult.Variant
            };
        }

        public ResolutionDetails<int> ResolveIntegerValue(string flagKey, int defaultValue)
        {
            var objectResult = ResolveObjectValue<object>(flagKey, defaultValue);
            
            if (objectResult.ErrorCode != ErrorCode.None)
            {
                return ResolutionDetails<int>.Error(flagKey, defaultValue, objectResult.ErrorCode, objectResult.ErrorMessage);
            }

            // Try to cast the object value to int
            if (objectResult.Value is int intValue)
            {
                return new ResolutionDetails<int>(intValue, flagKey)
                {
                    Reason = objectResult.Reason,
                    Variant = objectResult.Variant
                };
            }
            
            // Try to parse string representation to int
            if (int.TryParse(objectResult.Value?.ToString(), out var parsedInt))
            {
                return new ResolutionDetails<int>(parsedInt, flagKey)
                {
                    Reason = objectResult.Reason,
                    Variant = objectResult.Variant
                };
            }

            return ResolutionDetails<int>.Error(flagKey, defaultValue, ErrorCode.TypeMismatch, $"Cannot convert '{objectResult.Value}' to integer");
        }

        public ResolutionDetails<float> ResolveFloatValue(string flagKey, float defaultValue)
        {
            var objectResult = ResolveObjectValue<object>(flagKey, defaultValue);
            
            if (objectResult.ErrorCode != ErrorCode.None)
            {
                return ResolutionDetails<float>.Error(flagKey, defaultValue, objectResult.ErrorCode, objectResult.ErrorMessage);
            }

            // Try to cast the object value to float
            if (objectResult.Value is float floatValue)
            {
                return new ResolutionDetails<float>(floatValue, flagKey)
                {
                    Reason = objectResult.Reason,
                    Variant = objectResult.Variant
                };
            }
            
            // Try to cast from double (common JSON numeric type)
            if (objectResult.Value is double doubleValue)
            {
                return new ResolutionDetails<float>((float)doubleValue, flagKey)
                {
                    Reason = objectResult.Reason,
                    Variant = objectResult.Variant
                };
            }
            
            // Try to parse string representation to float
            if (float.TryParse(objectResult.Value?.ToString(), out var parsedFloat))
            {
                return new ResolutionDetails<float>(parsedFloat, flagKey)
                {
                    Reason = objectResult.Reason,
                    Variant = objectResult.Variant
                };
            }

            return ResolutionDetails<float>.Error(flagKey, defaultValue, ErrorCode.TypeMismatch, $"Cannot convert '{objectResult.Value}' to float");
        }

        public ResolutionDetails<T> ResolveObjectValue<T>(string flagKey, T defaultValue)
        {
            var value = ResolveValueByDotNotation(flagKey);
            if (value == null)
                return ResolutionDetails<T>.Error(flagKey, defaultValue, ErrorCode.FlagNotFound, $"Flag '{flagKey}' not found");

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
                details = ResolutionDetails<T>.Error(flagKey, defaultValue, ErrorCode.ParseError, $"Cannot parse: {ex.Message}");
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
            Debug.Log("currentValue is not null for flagKey: " + flagKey + " and currentValue: " + JsonConvert.SerializeObject(currentValue));
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
                "MATCH" => Reason.TargetingMatch,
                "NO_MATCH" => Reason.Default,
                "ERROR" => Reason.Error,
                _ => Reason.Unknown
            };
        }

        private Dictionary<string, object> GetEvaluationContext()
        {
            var context = OpenFeatureAPI.Instance.EvaluationContext;
            var evaluationContext = new Dictionary<string, object>();

            // Get attributes from context directly (now public)
            foreach (var attr in context.attributes)
            {
                evaluationContext[attr.Key] = attr.Value;
            }

            return evaluationContext;
        }
    }
}