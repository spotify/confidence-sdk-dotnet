using Confidence.Flags.Admin.V1;
using Confidence.Flags.Resolver.V1;
using Confidence.Iam.Types.V1;
using Grpc.Core;
using RustGuest;
using Spotify.Confidence.OpenFeature.Local.Models;

namespace Spotify.Confidence.OpenFeature.Local.Services;

public interface IResolveLogger
{
    void Log(LogResolveRequest logResolveRequest);
}

public class ResolveLoggerService(string clientId, string clientSecret, CallInvoker callInvoker) : IResolveLogger
{
    private readonly string _clientId = clientId;
    private readonly string _clientSecret = clientSecret;
    private readonly FlagAdminService.FlagAdminServiceClient _grpcClient = new FlagAdminService.FlagAdminServiceClient(callInvoker);

    public void Log(LogResolveRequest logResolveRequest)
    {
        var EvaluationContext = logResolveRequest.EvaluationContext;
        var Sdk = SdkId.DotnetConfidence;
        // var accountClient = new AccountClient("accountName", new Client("clientId", "clientSecret"), new ClientCredential("clientId", "clientSecret"));

        // append to in memory list
        Console.WriteLine($"Resolve: {logResolveRequest.ResolveId}");
    }
}