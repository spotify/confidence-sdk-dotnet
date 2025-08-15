using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confidence.Iam.V1;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Spotify.Confidence.OpenFeature.Local.Auth;

/// <summary>
/// Manages JWT tokens for Confidence API authentication.
/// Handles token acquisition, caching, and automatic renewal.
/// </summary>
public class TokenHolder : IDisposable
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly AuthService.AuthServiceClient _authClient;
    private readonly ILogger<TokenHolder> _logger;
    private readonly object _lockObject = new();
    
    private Token? _currentToken;
    private bool _disposed;

    public TokenHolder(string clientId, string clientSecret, AuthService.AuthServiceClient authClient, ILogger<TokenHolder>? logger = null)
    {
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
        _authClient = authClient ?? throw new ArgumentNullException(nameof(authClient));
        _logger = logger ?? NullLogger<TokenHolder>.Instance;
    }

    /// <summary>
    /// Gets a valid JWT token, acquiring a new one if necessary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A valid JWT token</returns>
    public async Task<Token> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lockObject)
        {
            // Check if we have a valid token that's not expired
            if (_currentToken != null && !_currentToken.IsExpired)
            {
                return _currentToken;
            }
        }

        // Need to acquire a new token
        return await AcquireNewTokenAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a valid JWT token synchronously, acquiring a new one if necessary.
    /// </summary>
    /// <returns>A valid JWT token</returns>
    public Token GetToken()
    {
        return GetTokenAsync().GetAwaiter().GetResult();
    }

    private async Task<Token> AcquireNewTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Acquiring new JWT token for client ID: {ClientId}", _clientId);
            Console.WriteLine($"[TokenHolder] Acquiring new JWT token for client ID: {_clientId}");

            var request = new RequestAccessTokenRequest
            {
                GrantType = "client_credentials",
                ClientId = _clientId,
                ClientSecret = _clientSecret
            };

            Console.WriteLine($"[TokenHolder] Making gRPC call to RequestAccessToken...");
            var response = await _authClient.RequestAccessTokenAsync(request, deadline: DateTime.UtcNow.AddSeconds(10), cancellationToken: cancellationToken);
            Console.WriteLine($"[TokenHolder] Received token response, decoding JWT...");
            
            // Decode the JWT to extract claims like the Java implementation
            var jwtHandler = new JwtSecurityTokenHandler();
            var decodedJwt = jwtHandler.ReadJwtToken(response.AccessToken_);
            Console.WriteLine($"[TokenHolder] JWT decoded successfully, expires at: {decodedJwt.ValidTo}");
            
            // Extract account name from claims (similar to ACCOUNT_NAME_CLAIM in Java)
            var accountNameClaim = decodedJwt.Claims.FirstOrDefault(c => c.Type == "account_name" || c.Type == "sub");
            var accountName = accountNameClaim?.Value ?? "unknown";
            Console.WriteLine($"[TokenHolder] Extracted account name: {accountName}");
            
            // Use the actual JWT expiration time
            var expiration = decodedJwt.ValidTo;
            
            var token = new Token(response.AccessToken_, accountName, expiration);
            
            lock (_lockObject)
            {
                _currentToken = token;
            }

            _logger.LogDebug("Successfully acquired JWT token for account '{AccountName}' (expires at {ExpirationTime})", 
                accountName, expiration);
            Console.WriteLine($"[TokenHolder] Token cached successfully");
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire JWT token for client ID: {ClientId}", _clientId);
            Console.WriteLine($"[TokenHolder] ERROR acquiring token: {ex.Message}");
            Console.WriteLine($"[TokenHolder] Exception type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[TokenHolder] Inner exception: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    /// <summary>
    /// Represents a JWT token with expiration tracking and account information.
    /// </summary>
    public class Token
    {
        private readonly DateTime _expiryTime;

        public Token(string accessToken, string accountName, DateTime expiration)
        {
            AccessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
            AccountName = accountName ?? throw new ArgumentNullException(nameof(accountName));
            
            // Use 1-hour safety margin like the Java implementation
            _expiryTime = expiration.AddHours(-1);
        }

        /// <summary>
        /// The JWT access token string.
        /// </summary>
        public string AccessToken { get; }

        /// <summary>
        /// The account name extracted from the JWT token.
        /// </summary>
        public string AccountName { get; }

        /// <summary>
        /// Whether the token is expired (or will expire within 1 hour).
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= _expiryTime;

        /// <summary>
        /// Time remaining until the token expires.
        /// </summary>
        public TimeSpan TimeUntilExpiry => _expiryTime - DateTime.UtcNow;

        /// <summary>
        /// The actual expiration time from the JWT (with 1-hour safety margin applied).
        /// </summary>
        public DateTime ExpiryTime => _expiryTime;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Clear the current token
            lock (_lockObject)
            {
                _currentToken = null;
            }
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
