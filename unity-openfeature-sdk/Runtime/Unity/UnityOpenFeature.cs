using UnityEngine;
using UnityOpenFeature.Core;
using UnityOpenFeature.Providers;

namespace UnityOpenFeature.Unity
{
    public class UnityOpenFeature : MonoBehaviour
    {
        [Header("OpenFeature Configuration")]
        [SerializeField] private string playerTargetingKey = "unity-player";
        [SerializeField] private InMemoryProvider inMemoryProvider;

        public static UnityOpenFeature Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeOpenFeature();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeOpenFeature()
        {
            var context = new EvaluationContext(playerTargetingKey)
                .SetAttribute("platform", Application.platform.ToString())
                .SetAttribute("version", Application.version)
                .SetAttribute("unity_version", Application.unityVersion);

            OpenFeatureAPI.Instance.SetEvaluationContext(context);

            if (inMemoryProvider != null)
            {
                OpenFeatureAPI.Instance.SetProvider(inMemoryProvider);
            }
            else
            {
                var defaultProvider = new InMemoryProvider();
                defaultProvider.SetFlag("enable_advanced_physics", true);
                defaultProvider.SetFlag("enable_new_ui", false);
                defaultProvider.SetFlag("score_multiplier", 1.0f);
                defaultProvider.SetFlag("max_enemies", 10);
                defaultProvider.SetFlag("tutorial_mode", true);
                OpenFeatureAPI.Instance.SetProvider(defaultProvider);
            }

            Debug.Log("UnityOpenFeature initialized");
        }

        public bool GetBooleanFlag(string flagKey, bool defaultValue = false)
        {
            return OpenFeatureAPI.Instance.GetClient().GetBooleanValue(flagKey, defaultValue);
        }

        public string GetStringFlag(string flagKey, string defaultValue = "")
        {
            return OpenFeatureAPI.Instance.GetClient().GetStringValue(flagKey, defaultValue);
        }

        public int GetIntegerFlag(string flagKey, int defaultValue = 0)
        {
            return OpenFeatureAPI.Instance.GetClient().GetIntegerValue(flagKey, defaultValue);
        }

        public float GetFloatFlag(string flagKey, float defaultValue = 0f)
        {
            return OpenFeatureAPI.Instance.GetClient().GetFloatValue(flagKey, defaultValue);
        }

        public T GetObjectFlag<T>(string flagKey, T defaultValue = default)
        {
            return OpenFeatureAPI.Instance.GetClient().GetObjectValue(flagKey, defaultValue);
        }

        public void SetFlag(string key, object value)
        {
            if (inMemoryProvider != null)
            {
                inMemoryProvider.SetFlag(key, value);
                Debug.Log($"Flag '{key}' set to '{value}'");
            }
        }
    }
}

