#nullable enable

using System.Collections.Generic;
using UnityOpenFeature.Core;

namespace UnityOpenFeature.Providers
{
    /// <summary>
    /// Builds the payload published for a tracking event.
    ///
    /// This lives apart from <c>ConfidenceProvider</c> and takes plain primitives
    /// rather than an <c>EvaluationContext</c> so it carries no UnityEngine
    /// dependency and can be compiled — and therefore tested — outside Unity.
    /// <c>ConfidenceProvider.Track</c> calls this, so the tests exercise the
    /// mapping that actually ships instead of a copy of it.
    /// </summary>
    internal static class TrackingPayloadBuilder
    {
        /// <summary>
        /// The payload key holding evaluation context. An attribute of the same
        /// name in the event details is dropped rather than allowed to overwrite
        /// the context the provider derives.
        /// </summary>
        internal const string ContextKey = "context";

        /// <summary>The payload key holding the event's predefined numeric value.</summary>
        internal const string ValueKey = "value";

        /// <summary>The payload key holding the targeting key inside the context.</summary>
        internal const string TargetingKeyKey = "targeting_key";

        /// <summary>
        /// Maps event details and evaluation context onto the published payload.
        /// </summary>
        /// <param name="details">Event details, or null.</param>
        /// <param name="targetingKey">Targeting key from the evaluation context, or null.</param>
        /// <param name="attributes">Evaluation context attributes, or null.</param>
        internal static Dictionary<string, object> Build(
            TrackingEventDetails? details,
            string? targetingKey,
            IEnumerable<KeyValuePair<string, string>>? attributes)
        {
            var payload = new Dictionary<string, object>();

            if (details != null)
            {
                foreach (var kvp in details.AsDictionary())
                {
                    if (kvp.Key != ContextKey)
                    {
                        payload[kvp.Key] = kvp.Value;
                    }
                }

                if (details.Value != null)
                {
                    payload[ValueKey] = details.Value.Value;
                }
            }

            var contextDict = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(targetingKey))
            {
                contextDict[TargetingKeyKey] = targetingKey!;
            }

            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    contextDict[attr.Key] = attr.Value;
                }
            }

            if (contextDict.Count > 0)
            {
                payload[ContextKey] = contextDict;
            }

            return payload;
        }
    }
}
