using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Object = UnityEngine.Object;

namespace UnityOpenFeature.Providers
{
    public class CustomDateTimeConverter : IsoDateTimeConverter
    {
        public CustomDateTimeConverter()
        {
            DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
        }
    }

    public class ConfidenceApiClient : MonoBehaviour
    {

        private string sdkId = "SDK_ID_DOTNET_CONFIDENCE";
        private string sdkVersion = "0.1.1";
        private string baseUrl = "https://resolver.confidence.dev";
        private string clientSecret;

        // Simple dictionary to store flag applies: flagKey -> AppliedFlag
        private Dictionary<string, AppliedFlag> appliedFlags = new Dictionary<string, AppliedFlag>();

        // Lock for thread-safe access to appliedFlags dictionary
        private readonly object appliedFlagsLock = new object();

        // Timer for automatic checkpoints
        private float checkpointTimer = 0f;
        private const float CHECKPOINT_INTERVAL = 10f; // 10 seconds

        // Private constructor - use Create() method instead
        private ConfidenceApiClient() { }
    
        public static ConfidenceApiClient Create(string clientSecret)
        {
            // Create a GameObject to host the client
            GameObject clientGO = new GameObject("ConfidenceApiClient");
            Object.DontDestroyOnLoad(clientGO);

            // Add the client as a component
            ConfidenceApiClient client = clientGO.AddComponent<ConfidenceApiClient>();
            client.clientSecret = clientSecret;

            return client;
        }

    
        private void Update()
        {
            checkpointTimer += Time.deltaTime;

            if (checkpointTimer >= CHECKPOINT_INTERVAL)
            {
                checkpointTimer = 0f;
                Checkpoint();
            }
        }

        public void Dispose()
        {
            SendAllBatchedFlags();
            // Destroy the GameObject this component is attached to
            if (gameObject != null)
            {
                Object.Destroy(gameObject);
            }
        }




        public async Task ResolveFlagsAsync(List<string> flagKeys, Dictionary<string, object> evaluationContext, Action<ResolveFlagsResponse, string> callback)
        {
            string url = $"{baseUrl}/v1/flags:resolve";

            // Create POST request body - add "flags/" prefix for backend
            var backendFlags = flagKeys.Select(flag => $"flags/{flag}").ToList();

            var requestBody = new ResolveFlagsRequest
            {
                flags = backendFlags,
                evaluationContext = evaluationContext,
                clientSecret = this.clientSecret,
                apply = false,
                sdk = new SdkInfo
                {
                    id = sdkId,
                    version = sdkVersion
                }
            };

            string jsonBody = JsonConvert.SerializeObject(requestBody);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                // Set headers
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                request.downloadHandler = new DownloadHandlerBuffer();

                // Set upload handler with JSON body
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Delay(100); // Small delay to prevent busy waiting
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string jsonResponse = request.downloadHandler.text;

                        // Parse the JSON response
                        var responseData = JsonConvert.DeserializeObject<ResolveFlagsResponse>(jsonResponse);

                        callback?.Invoke(responseData, null);
                    }
                    catch (Exception ex)
                    {
                        callback?.Invoke(null, $"Failed to parse response: {ex.Message}");
                    }
                }
                else
                {
                    string errorMsg = $"Network request failed: {request.error}";
                    callback?.Invoke(null, errorMsg);
                }
            }
        }

        public void ApplyFlag(string flagKey, string resolveToken)
        {
            if (string.IsNullOrEmpty(resolveToken))
            {
                return;
            }

            // Use a robust key format: flagKey|resolveToken (pipe separator to avoid conflicts)
            var key = $"{flagKey}|{resolveToken}";

            lock (appliedFlagsLock)
            {
                if (!appliedFlags.ContainsKey(key))
                {
                    var backendFlagKey = $"flags/{flagKey}";
                    appliedFlags[key] = new AppliedFlag
                    {
                        flag = backendFlagKey,
                        applyTime = DateTime.UtcNow
                    };
                }
            }
        }

        public async void Checkpoint()
        {
            Dictionary<string, AppliedFlag> flagsToProcess;

            // Atomically get and clear the flags to prevent race conditions
            lock (appliedFlagsLock)
            {
                if (appliedFlags.Count == 0)
                {
                    return;
                }

                // Create a copy of the current flags and clear the dictionary
                flagsToProcess = new Dictionary<string, AppliedFlag>(appliedFlags);
                appliedFlags.Clear();
            }

            // Group flags by resolve token (outside the lock for better performance)
            var flagsByToken = new Dictionary<string, List<AppliedFlag>>();

            foreach (var kvp in flagsToProcess)
            {
                // Parse resolve token from key format: "flagKey|resolveToken"
                var keyParts = kvp.Key.Split('|');
                if (keyParts.Length == 2)
                {
                    var resolveToken = keyParts[1];

                    if (!flagsByToken.ContainsKey(resolveToken))
                    {
                        flagsByToken[resolveToken] = new List<AppliedFlag>();
                    }

                    flagsByToken[resolveToken].Add(kvp.Value);
                }
            }

            // Send requests for each resolve token
            foreach (var tokenGroup in flagsByToken)
            {
                var resolveToken = tokenGroup.Key;
                var flagsToSend = tokenGroup.Value;

                string url = $"{baseUrl}/v1/flags:apply";

                var requestBody = new ApplyFlagsRequest
                {
                    flags = flagsToSend,
                    sendTime = DateTime.UtcNow,
                    clientSecret = this.clientSecret,
                    resolveToken = resolveToken,
                    sdk = new SdkInfo
                    {
                        id = sdkId,
                        version = sdkVersion
                    }
                };

                string jsonBody = JsonConvert.SerializeObject(requestBody);

                using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
                {
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Accept", "application/json");
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));

                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await Task.Delay(100);
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string jsonResponse = request.downloadHandler.text;
                        Debug.Log($"Checkpoint successful {jsonResponse}");
                    } else {
                        Debug.LogError($"Checkpoint failed with: {request.error}");
                    }
                }
            }
        }
        private void SendAllBatchedFlags()
        {
            Checkpoint();
        }


        [Serializable]
        private class ApplyFlagsRequest
        {
            public List<AppliedFlag> flags;
            [JsonConverter(typeof(CustomDateTimeConverter))]
            public DateTime sendTime;
            public string clientSecret;
            public string resolveToken;
            public SdkInfo sdk;
        }

        [Serializable]
        private class AppliedFlag
        {
            public string flag;
            [JsonConverter(typeof(CustomDateTimeConverter))]
            public DateTime applyTime;
        }

        [Serializable]
        private class SdkInfo
        {
            public string id;
            public string version;
        }

        [Serializable]
        private class ResolveFlagsRequest
        {
            public List<string> flags;
            public Dictionary<string, object> evaluationContext;
            public string clientSecret;
            public bool apply;
            public SdkInfo sdk;
        }

        [Serializable]
        public class ResolveFlagsResponse
        {
            public List<ResolvedFlag> resolvedFlags;
            public string resolveToken;
        }

        [Serializable]
        public class ResolvedFlag
        {
            public string flag;
            public string variant;
            public Dictionary<string, object> value;
            public string reason;
            public bool shouldApply;
        }
    }
}
