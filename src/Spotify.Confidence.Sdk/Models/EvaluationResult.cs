using System.Text.Json.Serialization;

namespace Spotify.Confidence.Sdk.Models;

/// <summary>
/// Represents the response from a flag evaluation request.
/// </summary>
internal class ResolveResponse
{
    /// <summary>
    /// Gets or sets the resolved flags.
    /// </summary>
    [JsonPropertyName("resolvedFlags")]
    public ResolvedFlag[] ResolvedFlags { get; set; } = Array.Empty<ResolvedFlag>();

    /// <summary>
    /// Gets or sets the resolve token.
    /// </summary>
    [JsonPropertyName("resolveToken")]
    public string ResolveToken { get; set; } = string.Empty;
}

/// <summary>
/// Represents the result of a flag evaluation.
/// </summary>
/// <typeparam name="T">The type of the flag value.</typeparam>
public class EvaluationResult<T>
{
    /// <summary>
    /// Gets or sets the value of the flag.
    /// </summary>
    [JsonPropertyName("value")]
    public T Value { get; set; } = default!;

    /// <summary>
    /// Gets or sets the reason for the evaluation result.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the variant that was selected.
    /// </summary>
    [JsonPropertyName("variant")]
    public string? Variant { get; set; }

    /// <summary>
    /// Gets a value indicating whether the evaluation was successful.
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess { get; internal set; } = true;

    /// <summary>
    /// Gets the error message if the evaluation failed.
    /// </summary>
    [JsonIgnore]
    public string? ErrorMessage { get; internal set; }

    /// <summary>
    /// Gets the exception that caused the evaluation to fail, if any.
    /// </summary>
    [JsonIgnore]
    public Exception? Exception { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationResult{T}"/> class.
    /// Creates a new instance of <see cref="EvaluationResult{T}"/>.
    /// </summary>
    public EvaluationResult()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationResult{T}"/> class.
    /// Creates a new instance of <see cref="EvaluationResult{T}"/> with the specified value.
    /// </summary>
    /// <param name="value">The value of the flag.</param>
    public EvaluationResult(T value)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationResult{T}"/> class.
    /// Creates a new instance of <see cref="EvaluationResult{T}"/> with the specified value, reason, and variant.
    /// </summary>
    /// <param name="value">The value of the flag.</param>
    /// <param name="reason">The reason for the evaluation result.</param>
    /// <param name="variant">The variant that was selected.</param>
    public EvaluationResult(T value, string? reason, string? variant)
    {
        Value = value;
        Reason = reason;
        Variant = variant;
    }
}

/// <summary>
/// Helper class for creating evaluation results.
/// </summary>
public static class EvaluationResult
{
    /// <summary>
    /// Creates a successful evaluation result.
    /// </summary>
    /// <typeparam name="T">The type of the flag value.</typeparam>
    /// <param name="value">The flag value.</param>
    /// <param name="reason">The evaluation reason.</param>
    /// <param name="variant">The variant that was selected.</param>
    /// <returns>A successful evaluation result.</returns>
    public static EvaluationResult<T> Success<T>(T value, string? reason = null, string? variant = null)
    {
        return new EvaluationResult<T>(value, reason, variant)
        {
            IsSuccess = true
        };
    }

    /// <summary>
    /// Creates a failed evaluation result with the specified default value.
    /// </summary>
    /// <typeparam name="T">The type of the flag value.</typeparam>
    /// <param name="defaultValue">The default value to return when evaluation fails.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>A failed evaluation result.</returns>
    public static EvaluationResult<T> Failure<T>(T defaultValue, string errorMessage, Exception? exception = null)
    {
        return new EvaluationResult<T>(defaultValue)
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            Reason = "ERROR"
        };
    }
}
