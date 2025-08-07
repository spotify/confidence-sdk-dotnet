namespace Spotify.Confidence.Sdk.Exceptions;

/// <summary>
/// Represents errors that occur during Confidence SDK operations.
/// </summary>
public class ConfidenceException : Exception
{
    /// <summary>
    /// Gets the error code from the API, if available.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfidenceException"/> class.
    /// </summary>
    public ConfidenceException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfidenceException"/> class.
    /// Creates a new instance of <see cref="ConfidenceException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConfidenceException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfidenceException"/> class.
    /// Creates a new instance of <see cref="ConfidenceException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The error code from the API.</param>
    public ConfidenceException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfidenceException"/> class.
    /// Creates a new instance of <see cref="ConfidenceException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public ConfidenceException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfidenceException"/> class.
    /// Creates a new instance of <see cref="ConfidenceException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The error code from the API.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public ConfidenceException(string message, string errorCode, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
