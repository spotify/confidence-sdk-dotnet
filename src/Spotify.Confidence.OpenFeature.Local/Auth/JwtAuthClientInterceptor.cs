using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Spotify.Confidence.OpenFeature.Local.Auth;

/// <summary>
/// gRPC client interceptor that adds JWT authentication to requests.
/// </summary>
public class JwtAuthClientInterceptor : Interceptor
{
    private readonly TokenHolder _tokenHolder;
    private readonly ILogger<JwtAuthClientInterceptor> _logger;

    private static readonly Action<ILogger, Exception> LogAuthHeaderError =
        LoggerMessage.Define(LogLevel.Error, new EventId(2001, "AuthHeaderError"), "Error adding JWT authentication header.");

    public JwtAuthClientInterceptor(TokenHolder tokenHolder, ILogger<JwtAuthClientInterceptor>? logger = null)
    {
        _tokenHolder = tokenHolder ?? throw new ArgumentNullException(nameof(tokenHolder));
        _logger = logger ?? NullLogger<JwtAuthClientInterceptor>.Instance;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var authContext = AddAuthenticationHeaders(context);
        return continuation(request, authContext);
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var authContext = AddAuthenticationHeaders(context);
        return continuation(request, authContext);
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var authContext = AddAuthenticationHeaders(context);
        return continuation(authContext);
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var authContext = AddAuthenticationHeaders(context);
        return continuation(authContext);
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var authContext = AddAuthenticationHeaders(context);
        return continuation(request, authContext);
    }

    private ClientInterceptorContext<TRequest, TResponse> AddAuthenticationHeaders<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        try
        {
            var token = _tokenHolder.GetToken();
            
            var headers = new Metadata();
            if (context.Options.Headers != null)
            {
                foreach (var header in context.Options.Headers)
                {
                    headers.Add(header);
                }
            }
            
            headers.Add("Authorization", $"Bearer {token.AccessToken}");
            
            var newOptions = context.Options.WithHeaders(headers);
            
            return new ClientInterceptorContext<TRequest, TResponse>(
                context.Method,
                context.Host,
                newOptions);
        }
        catch (Exception ex)
        {
            LogAuthHeaderError(_logger, ex);
            throw new InvalidOperationException("Error adding JWT authentication header", ex);
        }
    }
}
