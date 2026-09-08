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
        private const string SdkVersion = "0.4.0"; // x-release-please-version
        public const string DefaultBaseUrl = ConfidenceEndpointUrls.DefaultBaseUrl;
        private string baseUrl = DefaultBaseUrl;
        private string clientSecret;

        // Simple dictionary to store flag applies: flagKey -> AppliedFlag
        private Dictionary<string, AppliedFlag> appliedFlags = new Dictionary<string, AppliedFlag>();

        // Lock for thread-safe access to appliedFlags dictionary
        private readonly object appliedFlagsLock = new object();

        // Event buffer for tracking events
        private List<EventData> eventBuffer = new List<EventData>();
        private readonly object eventBufferLock = new object();
        private const int MAX_EVENT_BUFFER_SIZE = 100;

        // Kick a flush once the buffer is this full, so it normally drains
        // before reaching capacity rather than having to drop anything.
        private const int EVENT_BUFFER_FLUSH_THRESHOLD = (MAX_EVENT_BUFFER_SIZE * 4) / 5;

        // Events discarded because the buffer was full. Exposed via
        // DroppedEventCount so the loss is observable instead of log-only.
        private int droppedEventCount;

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
        private readonly SemaphoreSlim telemetryFlushSemaphore = new SemaphoreSlim(1, 1);
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

        private async Task FlushTelemetryAsync(bool force = false)
        {
            if (Telemetry == null)
            {
                return;
            }

            bool acquired;
            if (force)
            {
                await telemetryFlushSemaphore.WaitAsync();
                acquired = true;
            }
            else
            {
                acquired = await telemetryFlushSemaphore.WaitAsync(0);
            }

            if (!acquired)
            {
                return;
            }

            try
            {
                if (!force && telemetryRetryDelay > 0f)
                {
                    return;
                }

                await UploadTelemetryAsync();
            }
            finally
            {
                telemetryFlushSemaphore.Release();
            }
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
        }

        // Applied flags accumulate in appliedFlags and are only sent by
        // Checkpoint(), which otherwise runs on a CHECKPOINT_INTERVAL timer in
        // Update(). Backgrounding or quitting therefore lost up to an interval's
        // worth of applies, so these checkpoints flush them too. Checkpoint()
        // also flushes buffered events, so it replaces the FlushEventsAsync call
        // rather than being added alongside it.
        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                Checkpoint();
                _ = FlushTelemetryAsync(true);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Checkpoint();
                _ = FlushTelemetryAsync(true);
            }
        }

        private void OnApplicationQuit()
        {
            Checkpoint();
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
            // Flush buffered events alongside flag applies
            _ = FlushEventsAsync();

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
        public void TrackEvent(string eventDefinition, Dictionary<string, object> payload)
        {
            try
            {
                var eventData = new EventData
                {
                    eventDefinition = eventDefinition,
                    eventTime = DateTime.UtcNow,
                    payload = payload ?? new Dictionary<string, object>()
                };

                bool dropped;
                bool shouldFlush;
                int droppedTotal;

                lock (eventBufferLock)
                {
                    dropped = eventBuffer.Count >= MAX_EVENT_BUFFER_SIZE;
                    if (dropped)
                    {
                        // Drop the incoming event rather than shifting the list to
                        // evict the oldest: RemoveAt(0) is O(n) under this lock on
                        // the caller's thread, and the buffered events have already
                        // waited, so they are the ones closest to being delivered.
                        droppedEventCount++;
                    }
                    else
                    {
                        eventBuffer.Add(eventData);
                    }

                    droppedTotal = droppedEventCount;
                    shouldFlush = eventBuffer.Count >= EVENT_BUFFER_FLUSH_THRESHOLD;
                }

                if (dropped)
                {
                    // Rate limited: a full buffer means every subsequent call drops,
                    // and one warning per event would bury the rest of the log.
                    if (droppedTotal == 1 || droppedTotal % MAX_EVENT_BUFFER_SIZE == 0)
                    {
                        Debug.LogWarning(
                            $"Event buffer full ({MAX_EVENT_BUFFER_SIZE}); dropped {droppedTotal} event(s) so far. " +
                            "See ConfidenceApiClient.DroppedEventCount.");
                    }
                }

                if (shouldFlush)
                {
                    _ = FlushEventsAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"TrackEvent error (best-effort): {ex.Message}");
            }
        }

        /// <summary>
        /// Number of tracking events discarded because the buffer was full.
        /// Non-zero means events were lost and never published.
        /// </summary>
        public int DroppedEventCount
        {
            get
            {
                lock (eventBufferLock)
                {
                    return droppedEventCount;
                }
            }
        }

        private async Task FlushEventsAsync()
        {
            List<EventData> eventsToSend;

            lock (eventBufferLock)
            {
                if (eventBuffer.Count == 0)
                {
                    return;
                }

                eventsToSend = new List<EventData>(eventBuffer);
                eventBuffer.Clear();
            }

            try
            {
                string url = ConfidenceEndpointUrls.Build(
                    ConfidenceEndpointUrls.EventsBaseUrl,
                    ConfidenceEndpointUrls.PublishEventsPath);

                var requestBody = new PublishEventsRequest
                {
                    clientSecret = this.clientSecret,
                    sendTime = DateTime.UtcNow,
                    sdk = new SdkInfo
                    {
                        id = sdkId,
                        version = SdkVersion
                    },
                    events = eventsToSend
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
                        Debug.Log($"Event publish failed (best-effort): {request.error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Event publish error (best-effort): {ex.Message}");
            }
        }

        private void SendAllBatchedFlags()
        {
            Checkpoint();
            _ = FlushEventsAsync();
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

        [Serializable, Preserve]
        internal class EventData
        {
            [JsonProperty("event_definition")]
            public string eventDefinition;
            [JsonProperty("event_time")]
            [JsonConverter(typeof(CustomDateTimeConverter))]
            public DateTime eventTime;
            public Dictionary<string, object> payload;
        }

        [Serializable, Preserve]
        private class PublishEventsRequest
        {
            [JsonProperty("client_secret")]
            public string clientSecret;
            [JsonProperty("send_time")]
            [JsonConverter(typeof(CustomDateTimeConverter))]
            public DateTime sendTime;
            public SdkInfo sdk;
            public List<EventData> events;
        }
    }
}
