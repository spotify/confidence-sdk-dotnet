using System.Text.Json.Serialization;

namespace Spotify.Confidence.Sdk.Models;

/// <summary>
/// Represents a response from the Confidence API.
/// </summary>
/// <typeparam name="T">The type of the response data.</typeparam>
internal class ApiResponse<T>
{
    /// <summary>
    /// Gets or sets the response data.
    /// </summary>
    [JsonPropertyName("data")]
    public T Data { get; set; } = default!;

    /// <summary>
    /// Gets or sets any error information.
    /// </summary>
    [JsonPropertyName("error")]
    public ApiError? Error { get; set; }
}

/// <summary>
/// Represents an error from the Confidence API.
/// </summary>
internal class ApiError
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
