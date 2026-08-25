using System;

namespace UnityOpenFeature.Providers
{
    internal static class ConfidenceEndpointUrls
    {
        public const string DefaultBaseUrl = "https://resolver.confidence.dev";
        public const string ResolveFlagsPath = "v1/flags:resolve";
        public const string ApplyFlagsPath = "v1/flags:apply";

        public static string NormalizeBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("Base URL is required", nameof(baseUrl));
            }

            return baseUrl.TrimEnd('/');
        }

        public static string Build(string baseUrl, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path is required", nameof(path));
            }

            return $"{NormalizeBaseUrl(baseUrl)}/{path.TrimStart('/')}";
        }
    }
}
