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

        try
        {
            var authChannel = CreateChannel();
            var resolverChannel = CreateChannel();
            var authClient = new AuthService.AuthServiceClient(authChannel);
            _tokenHolder = new TokenHolder(_clientId, _clientSecret, authClient);
            var jwtInterceptor = new JwtAuthClientInterceptor(_tokenHolder);
            var callInvoker = resolverChannel.Intercept(jwtInterceptor);
            _grpcClient = new ResolverStateService.ResolverStateServiceClient(callInvoker);
            _grpcChannel = resolverChannel;
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

            var request = new ResolverStateRequest();
            using var streamingCall = _grpcClient.FullResolverState(request, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: cancellationToken);
            
            ResolverState? resolverState = null;
            var messageCount = 0;
            while (await streamingCall.ResponseStream.MoveNext(cancellationToken))
            {
                messageCount++;
                resolverState = streamingCall.ResponseStream.Current;
                break;
            }

            if (resolverState == null)
            {
                ConfidenceStateServiceLogger.NoResolverStateReceived(_logger, null);
                return null;
            }
            
            var stateBytes = resolverState.ToByteArray();

            ConfidenceStateServiceLogger.SuccessfullyFetchedResolverStateViaGrpc(_logger, null);
            return stateBytes;
        }
        catch (Grpc.Core.RpcException ex)
        {
            ConfidenceStateServiceLogger.GrpcErrorOccurred(_logger, ex.StatusCode, ex);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            ConfidenceStateServiceLogger.RequestCanceled(_logger, ex);
            return null;
        }
        catch (Exception ex)
        {
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