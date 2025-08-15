using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confidence.Flags.Admin.V1;
using Confidence.Iam.V1;
using Grpc.Net.Client;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spotify.Confidence.OpenFeature.Local.Logging;
using Spotify.Confidence.OpenFeature.Local.Auth;
using Google.Protobuf;

namespace Spotify.Confidence.OpenFeature.Local.Services;

/// <summary>
/// Service responsible for fetching configuration state from the Confidence backend via gRPC.
/// </summary>
public class ConfidenceStateService : IDisposable
{
    private readonly GrpcChannel _grpcChannel;
    private readonly ResolverStateService.ResolverStateServiceClient _grpcClient;
    private readonly TokenHolder _tokenHolder;
    private readonly ILogger<ConfidenceStateService> _logger;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private bool _disposed;



    public ConfidenceStateService(string clientId, string clientSecret, ILogger<ConfidenceStateService>? logger = null)
    {
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
        _logger = logger ?? NullLogger<ConfidenceStateService>.Instance;

        _logger.LogInformation("Initializing ConfidenceStateService for client ID: {ClientId}", _clientId);

        try
        {
                    // Step 1: Create separate channels for auth and resolver services
        _logger.LogDebug("Step 1: Creating gRPC channels for auth and resolver services");
        var authChannel = CreateChannel();
        var resolverChannel = CreateChannel();
        _logger.LogDebug("Successfully created gRPC channels");

        // Step 2: Create auth service client using the auth channel
        _logger.LogDebug("Step 2: Creating AuthService client");
        var authClient = new AuthService.AuthServiceClient(authChannel);
        _logger.LogDebug("Successfully created AuthService client");

            // Step 3: Create token holder for JWT management
            _logger.LogDebug("Step 3: Creating TokenHolder for JWT management");
            _tokenHolder = new TokenHolder(_clientId, _clientSecret, authClient);
            _logger.LogDebug("Successfully created TokenHolder");

            // Step 4: Create JWT interceptor and authenticated client for resolver service
            _logger.LogDebug("Step 4: Creating JWT interceptor and authenticated call invoker for resolver service");
            var jwtInterceptor = new JwtAuthClientInterceptor(_tokenHolder);
            var callInvoker = resolverChannel.Intercept(jwtInterceptor);
            _logger.LogDebug("Successfully created authenticated call invoker");

            // Step 5: Create resolver state service client using authenticated call invoker
            _logger.LogDebug("Step 5: Creating ResolverStateService client");
            _grpcClient = new ResolverStateService.ResolverStateServiceClient(callInvoker);
            _logger.LogDebug("Successfully created ResolverStateService client");
            
            // Keep reference to resolver channel for disposal (auth channel will be disposed with TokenHolder)
            _grpcChannel = resolverChannel;
            
            _logger.LogInformation("ConfidenceStateService initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ConfidenceStateService");
            throw;
        }
    }

    /// <summary>
    /// Creates a gRPC channel for the AuthService.
    /// </summary>
    private static GrpcChannel CreateChannel()
    {
        var useGrpcPlaintext = Environment.GetEnvironmentVariable("CONFIDENCE_GRPC_PLAINTEXT")
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

        var address = "https://edge-grpc.spotify.com:443";

        Console.WriteLine($"[ConfidenceStateService] Creating auth gRPC channel to: {address} (plaintext: {useGrpcPlaintext})");

        return GrpcChannel.ForAddress(address);
    }

    /// <summary>
    /// Fetches the resolver state from the Confidence backend using gRPC.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Byte array containing the serialized ResolverState protobuf, or null if failed.</returns>
    public async Task<byte[]?> FetchResolverStateAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            ConfidenceStateServiceLogger.FetchingResolverStateViaGrpc(_logger, null);
            Console.WriteLine($"[ConfidenceStateService] Starting resolver state fetch process");

            // Create the gRPC request for full resolver state
            Console.WriteLine($"[ConfidenceStateService] Creating ResolverStateRequest");
            var request = new ResolverStateRequest();
            Console.WriteLine($"[ConfidenceStateService] Created ResolverStateRequest successfully");

            // Make the gRPC streaming call to get the full resolver state
            Console.WriteLine($"[ConfidenceStateService] Making gRPC streaming call to FullResolverState endpoint");
            using var streamingCall = _grpcClient.FullResolverState(request, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: cancellationToken);
            Console.WriteLine($"[ConfidenceStateService] Created streaming call successfully, waiting for response...");
            
            // Read the streaming response - should contain a single ResolverState message
            Console.WriteLine($"[ConfidenceStateService] Reading streaming response from FullResolverState");
            ResolverState? resolverState = null;
            var messageCount = 0;
            while (await streamingCall.ResponseStream.MoveNext(cancellationToken))
            {
                messageCount++;
                Console.WriteLine($"[ConfidenceStateService] Received ResolverState message #{messageCount} from stream");
                resolverState = streamingCall.ResponseStream.Current;
                Console.WriteLine($"[ConfidenceStateService] Message contains {resolverState?.Flags.Count ?? 0} flags");
                break; // Take the first (and should be only) message
            }

            if (resolverState == null)
            {
                Console.WriteLine($"[ConfidenceStateService] ERROR: No ResolverState received from streaming gRPC call after {messageCount} attempts");
                ConfidenceStateServiceLogger.NoResolverStateReceived(_logger, null);
                return null;
            }
            
            Console.WriteLine($"[ConfidenceStateService] Successfully received ResolverState with {resolverState.Flags.Count} flags, converting to byte array");
            
            // Convert the ResolverState to bytes
            var stateBytes = resolverState.ToByteArray();
            Console.WriteLine($"[ConfidenceStateService] Successfully converted ResolverState to {stateBytes.Length} bytes");
            
            // Log some details about the state
            if (resolverState.Flags.Count > 0)
            {
                Console.WriteLine($"[ConfidenceStateService] State contains flags: {string.Join(", ", resolverState.Flags.Take(3).Select(f => f.Name))}");
                if (resolverState.Flags.Count > 3)
                {
                    Console.WriteLine($"[ConfidenceStateService] ... and {resolverState.Flags.Count - 3} more flags");
                }
            }
            
            ConfidenceStateServiceLogger.SuccessfullyFetchedResolverStateViaGrpc(_logger, null);
            return stateBytes;
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError("gRPC error occurred: Status={StatusCode}, Detail='{Detail}', Message='{Message}'", 
                ex.StatusCode, ex.Status.Detail, ex.Message);
            ConfidenceStateServiceLogger.GrpcErrorOccurred(_logger, ex.StatusCode, ex);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogInformation("Request was cancelled while fetching resolver state");
            ConfidenceStateServiceLogger.RequestCanceled(_logger, ex);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching resolver state: {ErrorType}", ex.GetType().Name);
            ConfidenceStateServiceLogger.UnexpectedErrorOccurred(_logger, ex);
            return null;
        }
    }

    /// <summary>
    /// Validates that the fetched state is valid protobuf bytes.
    /// </summary>
    /// <param name="stateBytes">The state bytes to validate</param>
    /// <returns>True if the state is valid, false otherwise</returns>
    public bool ValidateState(byte[]? stateBytes)
    {
        if (stateBytes == null || stateBytes.Length == 0)
        {
            return false;
        }

        try
        {
            // Try to parse the bytes as a ResolverState protobuf
            ResolverState.Parser.ParseFrom(stateBytes);
            
            ConfidenceStateServiceLogger.ResolverStateValidationPassed(_logger, null);
            return true;
        }
        catch (InvalidProtocolBufferException ex)
        {
            ConfidenceStateServiceLogger.ResolverStateContainsInvalidProtobuf(_logger, ex);
            return false;
        }
    }



    /// <summary>
    /// Converts a Protobuf Struct to a .NET object for JSON serialization.
    /// </summary>
    private static object? ConvertProtobufStructToObject(Google.Protobuf.WellKnownTypes.Struct? pbStruct)
    {
        if (pbStruct == null)
            return null;

        var result = new Dictionary<string, object?>();
        foreach (var field in pbStruct.Fields)
        {
            result[field.Key] = ConvertProtobufValueToObject(field.Value);
        }
        return result;
    }

    /// <summary>
    /// Converts a Protobuf Value to a .NET object.
    /// </summary>
    private static object? ConvertProtobufValueToObject(Google.Protobuf.WellKnownTypes.Value? pbValue)
    {
        if (pbValue == null)
            return null;

        return pbValue.KindCase switch
        {
            Google.Protobuf.WellKnownTypes.Value.KindOneofCase.NullValue => null,
            Google.Protobuf.WellKnownTypes.Value.KindOneofCase.BoolValue => pbValue.BoolValue,
            Google.Protobuf.WellKnownTypes.Value.KindOneofCase.NumberValue => pbValue.NumberValue,
            Google.Protobuf.WellKnownTypes.Value.KindOneofCase.StringValue => pbValue.StringValue,
            Google.Protobuf.WellKnownTypes.Value.KindOneofCase.StructValue => ConvertProtobufStructToObject(pbValue.StructValue),
            Google.Protobuf.WellKnownTypes.Value.KindOneofCase.ListValue => pbValue.ListValue.Values.Select(ConvertProtobufValueToObject).ToArray(),
            _ => null
        };
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            try
            {
                _tokenHolder?.Dispose();
                _grpcChannel?.Dispose();
            }
            catch (Exception ex)
            {
                ConfidenceStateServiceLogger.ErrorDisposingGrpcChannel(_logger, ex);
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