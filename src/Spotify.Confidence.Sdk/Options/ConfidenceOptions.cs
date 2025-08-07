using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Spotify.Confidence.Sdk.Options;

/// <summary>
/// Configuration options for the Confidence client.
/// </summary>
public class ConfidenceOptions
{
    private Region _region = Region.Global;

    /// <summary>
    /// Gets or sets the client secret used for authentication.
    /// </summary>
    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the region for the Confidence API.
    /// Defaults to Global region.
    /// </summary>
    public Region Region
    {
        get => _region;
        set
        {
            _region = value;
            ResolveUrl = RegionUrlHelper.GetResolverUrl(value);
            EventUrl = RegionUrlHelper.GetEventTrackingUrl(value);
        }
    }

    /// <summary>
    /// Gets or sets the base URL for the Confidence API.
    /// This is automatically set based on the Region property.
    /// </summary>
    public string ResolveUrl { get; set; }

    /// <summary>
    /// Gets or sets the base URL for event tracking.
    /// This is automatically set based on the Region property.
    /// </summary>
    public string EventUrl { get; set; }

    /// <summary>
    /// Gets or sets the timeout for HTTP requests in seconds.
    /// Defaults to 10 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum number of retries for failed requests.
    /// Defaults to 3 retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the minimum log level for the Confidence SDK.
    /// Defaults to Information level.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfidenceOptions"/> class.
    /// Creates a new instance of <see cref="ConfidenceOptions"/>.
    /// </summary>
    public ConfidenceOptions()
    {
        // Initialize URLs with the default Global region
        ResolveUrl = RegionUrlHelper.GetResolverUrl(_region);
        EventUrl = RegionUrlHelper.GetEventTrackingUrl(_region);
    }

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <exception cref="ValidationException">Thrown when required fields are missing or invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            throw new ValidationException("ClientSecret is required");
        }

        if (string.IsNullOrWhiteSpace(ResolveUrl))
        {
            throw new ValidationException("ResolveUrl is required");
        }

        if (string.IsNullOrWhiteSpace(EventUrl))
        {
            throw new ValidationException("EventUrl is required");
        }

        if (TimeoutSeconds <= 0)
        {
            throw new ValidationException("TimeoutSeconds must be greater than 0");
        }

        if (MaxRetries < 0)
        {
            throw new ValidationException("MaxRetries must be greater than or equal to 0");
        }
    }
}
