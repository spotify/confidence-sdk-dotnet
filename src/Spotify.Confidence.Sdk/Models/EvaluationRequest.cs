using System.Text.Json.Serialization;

namespace Spotify.Confidence.Sdk.Models;

/// <summary>
/// Represents a request to evaluate a flag.
/// </summary>
internal class EvaluationRequest
{
    /// <summary>
    /// Gets or sets the client secret for authentication.
    /// </summary>
    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether to apply the flag evaluation.
    /// </summary>
    [JsonPropertyName("apply")]
    public bool Apply { get; set; } = true;

    /// <summary>
    /// Gets or sets the evaluation context.
    /// </summary>
    [JsonPropertyName("evaluation_context")]
    public IDictionary<string, object> EvaluationContext { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the flags to evaluate.
    /// </summary>
    [JsonPropertyName("flags")]
    public string[] Flags { get; set; }

    /// <summary>
    /// Gets or sets the SDK information.
    /// </summary>
    [JsonPropertyName("sdk")]
    public SdkInfo Sdk { get; set; } = new SdkInfo();

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationRequest"/> class.
    /// Creates a new instance of <see cref="EvaluationRequest"/>.
    /// </summary>
    /// <param name="flagKey">The flag key to evaluate.</param>
    /// <param name="context">The context to use for evaluation.</param>
    /// <param name="clientSecret">The client secret for authentication.</param>
    public EvaluationRequest(string flagKey, ConfidenceContext? context, string clientSecret)
    {
        ClientSecret = clientSecret;
        Flags = new[] { ConfidenceClient.GetFullFlagKey(flagKey) };
        if (context != null)
        {
            EvaluationContext = new Dictionary<string, object>();
            foreach (var attr in context.Attributes)
            {
                EvaluationContext[attr.Key] = attr.Value;
            }
        }
    }
}

/// <summary>
/// Represents SDK information in the request.
/// </summary>
internal class SdkInfo
{
    /// <summary>
    /// Gets or sets the SDK ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "SDK_ID_DOTNET_CONFIDENCE";

    /// <summary>
    /// Gets or sets the SDK version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = ConfidenceClient.SDK_VERSION;
}
