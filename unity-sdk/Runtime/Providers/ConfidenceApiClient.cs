using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine.Scripting;
using UnityOpenFeature.Telemetry;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;

namespace UnityOpenFeature.Providers
{
    [Preserve]
    public class CustomDateTimeConverter : IsoDateTimeConverter
    {
        [Preserve]
        public CustomDateTimeConverter()
        {
            DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
        }
    }

    public class ConfidenceApiClient : MonoBehaviour
    {

        private string sdkId = "SDK_ID_DOTNET_CONFIDENCE";
        private const string SdkVersion = "0.3.2"; // x-release-please-version
        public const string DefaultBaseUrl = ConfidenceEndpointUrls.DefaultBaseUrl;
        private string baseUrl = DefaultBaseUrl;
        private string clientSecret;

        // Simple dictionary to store flag applies: flagKey -> AppliedFlag
        private Dictionary<string, AppliedFlag> appliedFlags = new Dictionary<string, AppliedFlag>();

        // Lock for thread-safe access to appliedFlags dictionary
        private readonly object appliedFlagsLock = new object();

        // Timer for automatic checkpoints
        private float checkpointTimer = 0f;
        private const float CHECKPOINT_INTERVAL = 10f; // 10 seconds

        private const float TelemetryFlushIntervalSeconds = 30f;
        private const float MaxTelemetryRetryDelaySeconds = 300f;
        private static readonly System.Random TelemetryRandom = new System.Random();
        private static readonly object TelemetryRandomLock = new object();
        private float telemetryFlushInterval = TelemetryFlushIntervalSeconds;
        private float telemetryFlushTimer;
        private float telemetryRetryDelay;
        private int consecutiveTelemetryFailures;
        private bool telemetryFlushInProgress;
        private Task telemetryFlushTask = Task.CompletedTask;
        internal Telemetry.Telemetry Telemetry { get; private set; }

        // Private constructor - use Create() method instead
        private ConfidenceApiClient() { }
    
        public static ConfidenceApiClient Create(
            string clientSecret,
            string baseUrl = DefaultBaseUrl,
            bool disableTelemetry = false)
        {
            // Create a GameObject to host the client
            GameObject clientGO = new GameObject("ConfidenceApiClient");
            Object.DontDestroyOnLoad(clientGO);

            // Add the client as a component
            ConfidenceApiClient client = clientGO.AddComponent<ConfidenceApiClient>();
            client.clientSecret = clientSecret;
            client.baseUrl = ConfidenceEndpointUrls.NormalizeBaseUrl(baseUrl);
            if (!disableTelemetry)
            {
                client.Telemetry = new Telemetry.Telemetry(Platform.Unity, SdkVersion);
                lock (TelemetryRandomLock)
                {
                    client.telemetryFlushInterval = TelemetryFlushIntervalSeconds
                        * (0.8f + (float)TelemetryRandom.NextDouble() * 0.4f);
                }
            }

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

            if (Telemetry != null)
            {
                telemetryFlushTimer += Time.deltaTime;
                telemetryRetryDelay = Mathf.Max(0f, telemetryRetryDelay - Time.deltaTime);
                if (telemetryFlushTimer >= telemetryFlushInterval && telemetryRetryDelay <= 0f)
                {
                    telemetryFlushTimer = 0f;
                    _ = FlushTelemetryAsync();
                }
            }
        }

        public void Dispose()
        {
            SendAllBatchedFlags();
            _ = FlushTelemetryAndDestroyAsync();
        }

        private async Task FlushTelemetryAndDestroyAsync()
        {
            await FlushTelemetryAsync(true);
            await FlushTelemetryAsync(true);
            if (gameObject != null)
            {
                Object.Destroy(gameObject);
            }
        }




        public async Task ResolveFlagsAsync(List<string> flagKeys, Dictionary<string, object> evaluationContext, Action<ResolveFlagsResponse, string> callback)
        {
            string url = ConfidenceEndpointUrls.Build(baseUrl, ConfidenceEndpointUrls.ResolveFlagsPath);

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
                    version = SdkVersion
                }
            };

            string jsonBody = JsonConvert.SerializeObject(requestBody);
            var stopwatch = Stopwatch.StartNew();
            RequestStatus resolveStatus = RequestStatus.Success;
            bool requestCompleted = false;

            try
            {
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

                    requestCompleted = true;
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        ResolveFlagsResponse responseData;
                        try
                        {
                            string jsonResponse = request.downloadHandler.text;

                            // Parse the JSON response
                            responseData = JsonConvert.DeserializeObject<ResolveFlagsResponse>(jsonResponse);
                        }
                        catch (Exception ex)
                        {
                            resolveStatus = RequestStatus.Error;
                            callback?.Invoke(null, $"Failed to parse response: {ex.Message}");
                            return;
                        }

                        callback?.Invoke(responseData, null);
                    }
                    else
                    {
                        resolveStatus = RequestStatus.Error;
                        string errorMsg = $"Network request failed: {request.error}";
                        callback?.Invoke(null, errorMsg);
                    }
                }
            }
            finally
            {
                stopwatch.Stop();
                if (!requestCompleted)
                {
                    resolveStatus = RequestStatus.Error;
                }

                // Track resolve latency (best-effort)
                try
                {
                    Telemetry?.TrackResolveLatency((ulong)stopwatch.ElapsedMilliseconds, resolveStatus);
                    FlushTelemetryIfNeeded();
                }
                catch (Exception ex)
                {
                    Debug.Log($"Telemetry latency tracking error (best-effort): {ex.Message}");
                }
            }
        }

        internal void FlushTelemetryIfNeeded()
        {
            if (Telemetry?.IsFull == true && telemetryRetryDelay <= 0f)
            {
                _ = FlushTelemetryAsync();
            }
        }

        private Task FlushTelemetryAsync(bool ignoreRetryDelay = false)
        {
            if (Telemetry == null || (!ignoreRetryDelay && telemetryRetryDelay > 0f))
            {
                return Task.CompletedTask;
            }

            if (telemetryFlushInProgress)
            {
                return telemetryFlushTask;
            }

            telemetryFlushInProgress = true;
            telemetryFlushTask = UploadTelemetryAsync();
            return telemetryFlushTask;
        }

        private async Task UploadTelemetryAsync()
        {
            TelemetrySnapshot snapshot = null;
            try
            {
                snapshot = Telemetry.TakeSnapshot();
                if (snapshot == null)
                {
                    return;
                }

                string url = ConfidenceEndpointUrls.Build(baseUrl, ConfidenceEndpointUrls.TelemetryPath);
                var requestBody = new Dictionary<string, object>
                {
                    { "clientSecret", clientSecret },
                    { "monitoring", snapshot.ToMonitoringPayload() },
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

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        throw new InvalidOperationException(request.error);
                    }
                }

                consecutiveTelemetryFailures = 0;
                telemetryRetryDelay = 0f;
            }
            catch (Exception ex)
            {
                if (snapshot != null)
                {
                    Telemetry.Restore(snapshot);
                }

                consecutiveTelemetryFailures++;
                telemetryRetryDelay = Mathf.Min(
                    TelemetryFlushIntervalSeconds * Mathf.Pow(2, consecutiveTelemetryFailures - 1),
                    MaxTelemetryRetryDelaySeconds);
                Debug.Log($"Telemetry upload error (best-effort): {ex.Message}");
            }
            finally
            {
                telemetryFlushInProgress = false;
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                _ = FlushTelemetryAsync(true);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                _ = FlushTelemetryAsync(true);
            }
        }

        private void OnApplicationQuit()
        {
            _ = FlushTelemetryAsync(true);
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

                string url = ConfidenceEndpointUrls.Build(baseUrl, ConfidenceEndpointUrls.ApplyFlagsPath);

                var requestBody = new ApplyFlagsRequest
                {
                    flags = flagsToSend,
                    sendTime = DateTime.UtcNow,
                    clientSecret = this.clientSecret,
                    resolveToken = resolveToken,
                    sdk = new SdkInfo
                    {
                        id = sdkId,
                        version = SdkVersion
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


        [Serializable, Preserve]
        private class ApplyFlagsRequest
        {
            public List<AppliedFlag> flags;
            [JsonConverter(typeof(CustomDateTimeConverter))]
            public DateTime sendTime;
            public string clientSecret;
            public string resolveToken;
            public SdkInfo sdk;
        }

        [Serializable, Preserve]
        private class AppliedFlag
        {
            public string flag;
            [JsonConverter(typeof(CustomDateTimeConverter))]
            public DateTime applyTime;
        }

        [Serializable, Preserve]
        private class SdkInfo
        {
            public string id;
            public string version;
        }

        [Serializable, Preserve]
        private class ResolveFlagsRequest
        {
            public List<string> flags;
            public Dictionary<string, object> evaluationContext;
            public string clientSecret;
            public bool apply;
            public SdkInfo sdk;
        }

        [Serializable, Preserve]
        public class ResolveFlagsResponse
        {
            public List<ResolvedFlag> resolvedFlags;
            public string resolveToken;
        }

        [Serializable, Preserve]
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
