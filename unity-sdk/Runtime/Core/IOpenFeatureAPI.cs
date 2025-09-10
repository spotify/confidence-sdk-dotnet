using System;
using UnityOpenFeature.Client;

namespace UnityOpenFeature.Core
{
    public interface IOpenFeatureAPI
    {
        IFeatureProvider Provider { get; }
        EvaluationContext EvaluationContext { get; }
        void SetProvider(IFeatureProvider provider, Action<bool, string> callback = null);
        void SetEvaluationContext(EvaluationContext context, Action<bool, string> callback = null);
        IFeatureClient GetClient(string domain = "");
        event Action<IFeatureProvider> ProviderChanged;
    }
}

