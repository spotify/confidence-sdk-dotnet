using RustGuest;

namespace Spotify.Confidence.OpenFeature.Local.Services;

public interface IResolveLogger
{
    void Log(LogResolveRequest logResolveRequest);
}

public class ResolveLoggerService(string clientId, string clientSecret) : IResolveLogger
{
    private readonly string _clientId = clientId;
    private readonly string _clientSecret = clientSecret;

    public void Log(LogResolveRequest logResolveRequest)
    {
        Console.WriteLine($"Resolve: {logResolveRequest}");
    }
}