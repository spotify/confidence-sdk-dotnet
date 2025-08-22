namespace Spotify.Confidence.OpenFeature.Local.Services;

using RustGuest;

public interface IAssignmentLogger
{
    void Log(LogAssignRequest logAssignRequest);
}

public class AssignmentLoggerService(string clientId, string clientSecret) : IAssignmentLogger
{
    private readonly string _clientId = clientId;
    private readonly string _clientSecret = clientSecret;

    public void Log(LogAssignRequest logAssignRequest)
    {
        Console.WriteLine($"Assignment: {logAssignRequest}");
    }
}
