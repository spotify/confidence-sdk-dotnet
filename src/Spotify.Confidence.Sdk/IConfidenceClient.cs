using Spotify.Confidence.Sdk.Models;

namespace Spotify.Confidence.Sdk;

/// <summary>
/// Interface for the Confidence client.
/// </summary>
public interface IConfidenceClient
{
    /// <summary>
    /// Evaluates a boolean flag.
    /// </summary>
    /// <param name="flagKey">The key of the flag to evaluate.</param>
    /// <param name="defaultValue">The default value to return if the flag cannot be resolved.</param>
    /// <param name="context">The context to use for evaluation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The evaluation result.</returns>
    Task<EvaluationResult<bool>> EvaluateBooleanFlagAsync(
        string flagKey,
        bool defaultValue,
        ConfidenceContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a string flag.
    /// </summary>
    /// <param name="flagKey">The key of the flag to evaluate.</param>
    /// <param name="defaultValue">The default value to return if the flag cannot be resolved.</param>
    /// <param name="context">The context to use for evaluation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The evaluation result.</returns>
    Task<EvaluationResult<string>> EvaluateStringFlagAsync(
        string flagKey,
        string defaultValue,
        ConfidenceContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a numeric flag.
    /// </summary>
    /// <param name="flagKey">The key of the flag to evaluate.</param>
    /// <param name="context">The context to use for evaluation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The evaluation result.</returns>
    Task<EvaluationResult<double>> EvaluateNumericFlagAsync(
        string flagKey,
        double defaultValue,
        ConfidenceContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a JSON flag.
    /// </summary>
    /// <param name="flagKey">The key of the flag to evaluate.</param>
    /// <param name="defaultValue">The default value to return if the flag cannot be resolved.</param>
    /// <param name="context">The context to use for evaluation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The evaluation result.</returns>
    Task<EvaluationResult<object>> EvaluateJsonFlagAsync(
        string flagKey,
        object defaultValue,
        ConfidenceContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks an event with the specified name and data.
    /// </summary>
    /// <param name="eventName">The name of the event to track.</param>
    /// <param name="data">Additional data to include with the event.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the event has been tracked.</returns>
    Task TrackAsync(
        string eventName,
        IDictionary<string, object>? data = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a flag with the specified key and default value.
    /// </summary>
    /// <typeparam name="T">The type of the flag value.</typeparam>
    /// <param name="flagKey">The key of the flag to resolve.</param>
    /// <param name="defaultValue">The default value to return if the flag cannot be resolved.</param>
    /// <param name="context">The context to use for evaluation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The evaluation result.</returns>
    Task<EvaluationResult<T>> ResolveFlagAsync<T>(
        string flagKey,
        T defaultValue,
        ConfidenceContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the boolean value of a flag for the specified key and context.
    /// </summary>
    /// <param name="flagKey">The key of the flag to evaluate. Can use dot notation for nested properties (e.g., "flag-name.property-name").</param>
    /// <param name="defaultValue">The default value to return if the flag cannot be resolved.</param>
    /// <param name="context">The context to use for evaluation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The flag value or the default value if resolution fails.</returns>
    Task<bool> GetBoolValueAsync(
        string flagKey,
        bool defaultValue,
        ConfidenceContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the string value of a flag for the specified key and context.
    /// </summary>
    /// <param name="flagKey">The key of the flag to evaluate. Can use dot notation for nested properties (e.g., "flag-name.property-name").</param>
    /// <param name="defaultValue">The default value to return if the flag cannot be resolved.</param>
    /// <param name="context">The context to use for evaluation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The flag value or the default value if resolution fails.</returns>
    Task<string> GetStringValueAsync(
        string flagKey,
        string defaultValue,
        ConfidenceContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the numeric value of a flag for the specified key and context.
    /// </summary>
    /// <param name="flagKey">The key of the flag to evaluate. Can use dot notation for nested properties (e.g., "flag-name.property-name").</param>
    /// <param name="defaultValue">The default value to return if the flag cannot be resolved.</param>
    /// <param name="context">The context to use for evaluation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The flag value or the default value if resolution fails.</returns>
    Task<double> GetNumericValueAsync(
        string flagKey,
        double defaultValue,
        ConfidenceContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the integer value of a flag for the specified key and context.
    /// </summary>
    /// <param name="flagKey">The key of the flag to evaluate. Can use dot notation for nested properties (e.g., "flag-name.property-name").</param>
    /// <param name="defaultValue">The default value to return if the flag cannot be resolved.</param>
    /// <param name="context">The context to use for evaluation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The flag value or the default value if resolution fails.</returns>
    Task<int> GetIntValueAsync(
        string flagKey,
        int defaultValue,
        ConfidenceContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the JSON/object value of a flag for the specified key and context.
    /// This method is useful for complex flag structures like dictionaries, arrays, or custom objects.
    /// </summary>
    /// <param name="flagKey">The key of the flag to evaluate.</param>
    /// <param name="defaultValue">The default value to return if the flag cannot be resolved.</param>
    /// <param name="context">The context to use for evaluation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The flag value or the default value if resolution fails.</returns>
    Task<object> GetJsonValueAsync(
        string flagKey,
        object defaultValue,
        ConfidenceContext? context = null,
        CancellationToken cancellationToken = default);
}
