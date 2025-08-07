using System.ComponentModel.DataAnnotations;
using Spotify.Confidence.Sdk.Options;
using Xunit;

namespace Spotify.Confidence.Sdk.Tests;

public class ConfidenceOptionsTests
{
    [Fact]
    public void Validate_WithValidOptions_DoesNotThrow()
    {
        // Arrange
        var options = new ConfidenceOptions
        {

            ClientSecret = "test-client-secret"
        };

        // Act & Assert
        options.Validate();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithInvalidClientSecret_ThrowsValidationException(string clientSecret)
    {
        // Arrange
        var options = new ConfidenceOptions
        {

            ClientSecret = clientSecret
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => options.Validate());
        Assert.Equal("ClientSecret is required", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithInvalidResolveUrl_ThrowsValidationException(string resolveUrl)
    {
        // Arrange
        var options = new ConfidenceOptions
        {

            ClientSecret = "test-client-secret",
            ResolveUrl = resolveUrl,
            EventUrl = "https://api.test.com"
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => options.Validate());
        Assert.Equal("ResolveUrl is required", exception.Message);
    }

    [Fact]
    public void Validate_WithNullResolveUrl_ThrowsValidationException()
    {
        // Arrange
        var options = new ConfidenceOptions
        {

            ClientSecret = "test-client-secret",
            ResolveUrl = null!,
            EventUrl = "https://api.test.com"
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => options.Validate());
        Assert.Equal("ResolveUrl is required", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidTimeoutSeconds_ThrowsValidationException(int timeoutSeconds)
    {
        // Arrange
        var options = new ConfidenceOptions
        {

            ClientSecret = "test-client-secret",
            TimeoutSeconds = timeoutSeconds
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => options.Validate());
        Assert.Equal("TimeoutSeconds must be greater than 0", exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    public void Validate_WithInvalidMaxRetries_ThrowsValidationException(int maxRetries)
    {
        // Arrange
        var options = new ConfidenceOptions
        {

            ClientSecret = "test-client-secret",
            MaxRetries = maxRetries
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => options.Validate());
        Assert.Equal("MaxRetries must be greater than or equal to 0", exception.Message);
    }

    [Fact]
    public void Validate_WithNullClientSecret_ThrowsValidationException()
    {
        // Arrange
        var options = new ConfidenceOptions
        {

            ClientSecret = null!
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => options.Validate());
        Assert.Equal("ClientSecret is required", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithInvalidEventUrl_ThrowsValidationException(string eventUrl)
    {
        // Arrange
        var options = new ConfidenceOptions
        {

            ClientSecret = "test-client-secret",
            ResolveUrl = "https://api.test.com",
            EventUrl = eventUrl
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => options.Validate());
        Assert.Equal("EventUrl is required", exception.Message);
    }

    [Fact]
    public void Validate_WithNullEventUrl_ThrowsValidationException()
    {
        // Arrange
        var options = new ConfidenceOptions
        {

            ClientSecret = "test-client-secret",
            ResolveUrl = "https://api.test.com",
            EventUrl = null!
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => options.Validate());
        Assert.Equal("EventUrl is required", exception.Message);
    }
}
