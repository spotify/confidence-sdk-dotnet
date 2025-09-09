using System;
using UnityOpenFeature.Client;

namespace UnityOpenFeature.Core
{
    public interface IOpenFeatureAPI
    {
        IFeatureProvider Provider { get; }
        EvaluationContext EvaluationContext { get; }
        void SetProvider(IFeatureProvider provider);
        void SetEvaluationContext(EvaluationContext context);
        IFeatureClient GetClient(string domain = "");
        event Action<IFeatureProvider> ProviderChanged;
    }
}

