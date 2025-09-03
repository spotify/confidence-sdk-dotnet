namespace Spotify.Confidence.OpenFeature.Local.Services;

using System.Collections.Concurrent;
using global::Confidence.Flags.Admin.V1;
using global::Confidence.Flags.Resolver.V1.Events;
using Grpc.Core;
using RustGuest;
using Spotify.Confidence.OpenFeature.Local.Models;
using Spotify.Confidence.OpenFeature.Local.Utils;

public interface IAssignmentLogger : IDisposable
{
    void Log(LogAssignRequest logAssignRequest);
}

public class AssignmentLoggerService : IAssignmentLogger
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly FlagAdminService.FlagAdminServiceClient _grpcClient;
    private readonly ConcurrentQueue<FlagAssigned> _assignmentQueue;
    private readonly Timer _timer;
    private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(5, 5);
    private readonly object _queueLock = new object();
    private volatile bool _disposed;

    public AssignmentLoggerService(string clientId, string clientSecret, CallInvoker callInvoker, TimeSpan? timerInterval = null)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _grpcClient = new FlagAdminService.FlagAdminServiceClient(callInvoker);
        _assignmentQueue = new ConcurrentQueue<FlagAssigned>();
        
        // Default to 10 seconds if not specified
        var interval = timerInterval ?? TimeSpan.FromSeconds(10);
        _timer = new Timer(TimerCallback, null, interval, interval);
    }

    public void Log(LogAssignRequest logAssignRequest)
    {
        if (_disposed)
        {
            return;
        }
        /*
        Console.WriteLine($"Before try");
        try {
            Console.WriteLine($"Logging assignment: {logAssignRequest.ResolveId}, {logAssignRequest.Client.Account.Name}, {logAssignRequest.Client.ClientName}, {logAssignRequest.Client.ClientCredentialName}");
            var accountClient = new AccountClient(
            logAssignRequest.Client.Account.Name,
            new global::Confidence.Iam.Types.V1.Client { Name = logAssignRequest.Client.ClientName },
            new global::Confidence.Iam.Types.V1.ClientCredential { Name = logAssignRequest.Client.ClientCredentialName });
            Console.WriteLine($"Created account client: {logAssignRequest.Client.Account.Name}");
            var flagAssigned = FlagLogger.CreateFlagAssigned(logAssignRequest.ResolveId, logAssignRequest.AssignedFlags, accountClient);
            Console.WriteLine($"Created flag assigned: {flagAssigned.ResolveId}");
            _assignmentQueue.Enqueue(flagAssigned);
            Console.WriteLine($"Enqueued assignment: {logAssignRequest.ResolveId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error logging assignment: {ex.Message}");
        }
        */
        Console.WriteLine("End of logAssignment");
    }

    private void TimerCallback(object? state)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            CheckPoint();
        }
        catch (Exception ex)
        {
            // Log error but don't let timer crash
            Console.WriteLine($"Error in AssignmentLoggerService timer: {ex.Message}");
        }
    }

    private void CheckPoint()
    {
        if (_disposed)
        {
            return;
        }

        Console.WriteLine($"Checking point");

        // Extract batches from queue in single-threaded manner
        var batches = new List<List<FlagAssigned>>();
        lock (_queueLock)
        {
            while (!_assignmentQueue.IsEmpty)
            {
                var batch = new List<FlagAssigned>();
                
                // Create batch with at most 100 items
                while (batch.Count < 100 && _assignmentQueue.TryDequeue(out var assignment))
                {
                    batch.Add(assignment);
                }
                
                if (batch.Count > 0)
                {
                    batches.Add(batch);
                }
            }
        }

        if (batches.Count == 0)
        {
            return;
        }

        // Send batches concurrently (up to 5 at a time)
        foreach (var batch in batches)
        {
            _ = Task.Run(() => SendBatch(batch)); // Fire and forget
        }
    }

    private void SendBatch(List<FlagAssigned> assignments)
    {
        _sendSemaphore.Wait();
        try
        {
            try
            {
                // Create and send the request with this batch
                var request = new WriteFlagAssignedRequest();
                request.FlagAssigned.AddRange(assignments);
                Console.WriteLine($"Sending batch of {assignments.Count} assignment(s)");
                
                // Send the request
                _grpcClient.WriteFlagAssigned(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending batch: {ex.Message}");
                
                // Re-enqueue failed assignments for retry
                foreach (var assignment in assignments)
                {
                    _assignmentQueue.Enqueue(assignment);
                }
            }
        }
        finally
        {
            _sendSemaphore.Release();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Stop the timer
            _timer?.Dispose();
            
            // Send any remaining assignments
            try
            {
                CheckPoint();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during final send in Dispose: {ex.Message}");
            }
            
            // Dispose the semaphore
            _sendSemaphore?.Dispose();
        }

        _disposed = true;
    }
}
