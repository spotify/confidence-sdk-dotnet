using System;
using System.Collections.Generic;
using UnityEngine;
using UnityOpenFeature.Client;

namespace UnityOpenFeature.Core
{
    public sealed class OpenFeatureAPI : IOpenFeatureAPI
    {
        private static readonly Lazy<OpenFeatureAPI> instance = new(() => new OpenFeatureAPI());
        public static OpenFeatureAPI Instance => instance.Value;

        private IFeatureProvider provider;
        private EvaluationContext evaluationContext = new();
        private readonly Dictionary<string, IFeatureClient> clients = new();

        public IFeatureProvider Provider => provider;
        public EvaluationContext EvaluationContext => evaluationContext;

        public event Action<IFeatureProvider> ProviderChanged;

        private OpenFeatureAPI() { }

        public void SetProvider(IFeatureProvider newProvider)
        {
            if (newProvider == null) { Debug.LogError("Provider cannot be null"); return; }
            try
            {
                provider?.Shutdown();
                provider = newProvider;
                provider.Initialize(evaluationContext);
                Debug.Log($"Provider set to: {provider.Name}");
                ProviderChanged?.Invoke(provider);
            }
            catch (Exception ex) { Debug.LogError($"Failed to set provider: {ex.Message}"); }
        }

        public void SetEvaluationContext(EvaluationContext context)
        {
            evaluationContext = context ?? new EvaluationContext();
            provider?.Initialize(evaluationContext);
        }

        public IFeatureClient GetClient(string domain = "")
        {
            if (!clients.TryGetValue(domain, out var client))
            {
                client = new FeatureClient(this, domain);
                clients[domain] = client;
            }
            return client;
        }
    }
}

