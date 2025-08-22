using System.Reflection;
using Confidence.Flags.Resolver.V1;
using Confidence.Wasm;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spotify.Confidence.OpenFeature.Local.Logging;
using Wasmtime;

namespace Spotify.Confidence.OpenFeature.Local;

/// <summary>
/// Wraps a WebAssembly module to provide flag resolution functionality.
/// </summary>
public class WasmResolver : IDisposable
{
    private readonly ILogger<WasmResolver> _logger;
    private readonly Engine? _engine;
    private readonly Wasmtime.Module? _module;
    private readonly Store? _store;
    private readonly Instance? _instance;
    private readonly Function? _resolveFunction;
    private readonly Memory? _memory;
    private readonly Function? _allocFunction;
    private readonly Function? _deallocFunction;
    private readonly Function? _setResolverStateFunction;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WasmResolver"/> class from an embedded resource.
    /// </summary>
    /// <param name="resourceName">Name of the embedded resource containing the WASM file.</param>
    /// <param name="assembly">Assembly containing the embedded resource. If null, uses the calling assembly.</param>
    /// <param name="logger">Optional logger instance.</param>
    public WasmResolver(string resourceName, Assembly? assembly = null, ILogger<WasmResolver>? logger = null)
    {
        _logger = logger ?? NullLogger<WasmResolver>.Instance;
        assembly ??= Assembly.GetExecutingAssembly();

        try
        {
            var wasmBytes = LoadEmbeddedResource(resourceName, assembly);
            _engine = new Engine();
            _module = Wasmtime.Module.FromBytes(_engine, resourceName, wasmBytes);
            _store = new Store(_engine);
            
            try
            {               
                var imports = new object[]
                {
                    // Import wasm_msg_current_thread_id - (result i32) - NO parameters  
                    Function.FromCallback(_store, () => 
                    {
                        var threadId = Environment.CurrentManagedThreadId;
                        WasmResolverLogger.WasmImportCalledWithThreadId(_logger, "wasm_msg_current_thread_id", threadId);
                        return threadId;
                    }),
                    /*
                    // Import log_resolve - (param i32) (result i32)
                    Function.FromCallback(_store, (int ptr) => 
                    {
                        WasmResolverLogger.WasmImportCalled(_logger, "log_resolve", ptr);
                        return 0; // Return success
                    }),
                    
                    // Import log_assign - (param i32) (result i32)
                    Function.FromCallback(_store, (int ptr) => 
                    {
                        WasmResolverLogger.WasmImportCalled(_logger, "log_assign", ptr);
                        return 0;
                    }),
                    */

                    // Import wasm_msg_host_current_time - (param i32) (result proto Timestamp)
                    Function.FromCallback(_store, (int ptr) => 
                    {
                        Timestamp timestamp = new Timestamp();
                        timestamp.Seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        WasmResolverLogger.WasmImportCalledWithTimestamp(_logger, "wasm_msg_host_current_time", ptr, timestamp.Seconds);
                        return TransferRequest(CreateWasmRequest(timestamp.ToByteArray()));
                    })
                };
                
                _instance = new Instance(_store, _module, imports);
                WasmResolverLogger.ModuleInstantiated(_logger, imports.Length);
            }
            catch (Exception ex)
            {
                WasmResolverLogger.ImportsNeeded(_logger, ex.Message);
            }
            if (_instance == null)
            {
                throw new InvalidOperationException("Failed to instantiate WASM module, _instance is null");
            }

            try
            {
                _resolveFunction = _instance.GetFunction("wasm_msg_guest_resolve");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get wasm_msg_guest_resolve function, _resolveFunction is null", ex);
            }
            
            try
            {
                _allocFunction = _instance.GetFunction("wasm_msg_alloc");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get wasm_msg_alloc function, _allocFunction is null", ex);
            }
            
            try
            {
                _deallocFunction = _instance.GetFunction("wasm_msg_free");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get wasm_msg_free function, _deallocFunction is null", ex);
            }
            
            try
            {
                _setResolverStateFunction = _instance.GetFunction("wasm_msg_guest_set_resolver_state");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get wasm_msg_guest_set_resolver_state function, _setResolverStateFunction is null", ex);
            }
            
            try
            {
                _memory = _instance.GetMemory("memory");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get memory, _memory is null", ex);
            }

            WasmResolverLogger.WasmResolverInitialized(_logger, $"embedded resource {resourceName}");
        }
        catch (Exception ex)
        {
            WasmResolverLogger.WasmResolverInitializationFailed(_logger, $"embedded resource {resourceName}", ex);
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// Loads a WASM file from an embedded resource.
    /// </summary>
    /// <param name="resourceName">Name of the embedded resource.</param>
    /// <param name="assembly">Assembly containing the resource.</param>
    /// <returns>Byte array containing the WASM file contents.</returns>
    private static byte[] LoadEmbeddedResource(string resourceName, Assembly assembly)
    {
        // Try to find the resource with the exact name first
        var fullResourceName = resourceName;
        using var stream = assembly.GetManifestResourceStream(fullResourceName);
        
        if (stream == null)
        {
            // If not found, try with the assembly name prefix
            var assemblyName = assembly.GetName().Name;
            fullResourceName = $"{assemblyName}.{resourceName}";
            using var prefixedStream = assembly.GetManifestResourceStream(fullResourceName);
            return ReadStreamToBytes(prefixedStream ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found."));
        }
        
        return ReadStreamToBytes(stream);
    }

    /// <summary>
    /// Reads a stream to a byte array.
    /// </summary>
    /// <param name="stream">Stream to read.</param>
    /// <returns>Byte array containing the stream contents.</returns>
    private static byte[] ReadStreamToBytes(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Wraps data in a confidence.wasm.Request message and serializes it to bytes.
    /// </summary>
    /// <param name="data">The data to wrap in the request.</param>
    /// <returns>Serialized request bytes.</returns>
    private static byte[] CreateWasmRequest(byte[] data)
    { 
        return new Request
        {
            Data = ByteString.CopyFrom(data)
        }.ToByteArray();
    }

    /// <summary>
    /// Transfers a request to WASM memory and returns the pointer, similar to Java transferRequest method.
    /// </summary>
    /// <param name="data">The data to transfer to WASM memory.</param>
    /// <returns>Pointer to the request data in WASM memory.</returns>
    private int TransferRequest(byte[] data)
    {
        var requestPtr = AllocateMemory(data.Length);
        WriteToMemory(requestPtr, data);
        return requestPtr;
    }

    /// <summary>
    /// Sets the resolver state in the WASM module.
    /// </summary>
    /// <param name="stateBytes">The serialized ResolverState protobuf bytes to set in the resolver</param>
    /// <returns>True if the state was set successfully, false otherwise</returns>
    public bool SetResolverState(byte[] stateBytes)
    {
        ObjectDisposedException.ThrowIf(_disposed, new ObjectDisposedException(nameof(WasmResolver)));
        ArgumentNullException.ThrowIfNull(stateBytes);

        try
        {
            var requestBytes = CreateWasmRequest(stateBytes);
            var requestPtr = TransferRequest(requestBytes);

            var result = _setResolverStateFunction!.Invoke(requestPtr);
                   
            DeallocateMemory(requestPtr);
            
            var success = result != null;
            
            if (success)
            {
                WasmResolverLogger.SetResolverStateSuccess(_logger, stateBytes.Length);
                return true;
            }
            else
            {
                WasmResolverLogger.SetResolverStateFailed(_logger, "WASM function returned null", null);
                return false;
            }
        }
        catch (Exception ex)
        {
            WasmResolverLogger.SetResolverStateFailed(_logger, "Error setting resolver state in WASM module", ex);
            return false;
        }
    }

    /// <summary>
    /// Resolves a feature flag using the WASM module.
    /// </summary>
    /// <param name="request">The resolve request containing flag key, context, and credentials.</param>
    /// <returns>The resolution response.</returns>
    public ResolveFlagsResponse Resolve(ResolveFlagsRequest request)
    {
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, new ObjectDisposedException(nameof(WasmResolver)));
            ArgumentNullException.ThrowIfNull(_resolveFunction);
            ArgumentNullException.ThrowIfNull(_allocFunction);
            ArgumentNullException.ThrowIfNull(_store);

            var requestBytes = CreateWasmRequest(request.ToByteArray());            
            var requestPtr = TransferRequest(requestBytes);
            var resultPtr = (int)_resolveFunction.Invoke(requestPtr)!;
            
            DeallocateMemory(requestPtr);
            
            var response = ConsumeResponse(resultPtr, bytes => ResolveFlagsResponse.Parser.ParseFrom(bytes));
            return response ?? new ResolveFlagsResponse();
        }
        catch (Exception ex)
        {
            WasmResolverLogger.FlagResolutionError(_logger, request.Flags[0], ex);
            return new ResolveFlagsResponse();
        }
    }

    private int AllocateMemory(int size)
    {
        if (_allocFunction == null)
        {
            throw new InvalidOperationException("Alloc function not found in WASM module");
        }

        var result = _allocFunction.Invoke(size);
        return (int)result!;
    }

    private void DeallocateMemory(int ptr)
    {
        if (_deallocFunction == null)
        {
            return; // Silently fail if dealloc function is not available
        }

        _deallocFunction.Invoke(ptr);
    }

    private void WriteToMemory(int ptr, byte[] data)
    {
        if (_memory == null)
        {
            throw new InvalidOperationException("Memory not found in WASM module");
        }

        var span = _memory.GetSpan<byte>(0);
        data.CopyTo(span.Slice(ptr));
    }

    /// <summary>
    /// Reads a byte array from WASM memory at the specified address.
    /// Based on the Java implementation's consume() method.
    /// </summary>
    /// <param name="addr">Memory address returned by WASM function</param>
    /// <returns>Byte array read from WASM memory</returns>
    private byte[] ConsumeBytes(int addr)
    {
        ArgumentNullException.ThrowIfNull(_memory);

        var span = _memory.GetSpan<byte>(0);

        if (addr < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(addr), "Address is too small to read length prefix");
        }

        int len = BitConverter.ToInt32(span.Slice(addr - 4, 4));
        int dataLen = len - 4;

        if (addr + dataLen > span.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(addr), "Data length exceeds memory bounds");
        }

        var data = span.Slice(addr, dataLen).ToArray();

        DeallocateMemory(addr);

        return data;
    }

    /// <summary>
    /// Processes a WASM response using the same pattern as the Java implementation.
    /// </summary>
    /// <param name="addr">Memory address returned by WASM function.</param>
    /// <param name="parser">Function to parse the response data bytes into the desired type.</param>
    /// <typeparam name="T">The type to parse the response into.</typeparam>
    /// <returns>Parsed response object.</returns>
    private T ConsumeResponse<T>(int addr, Func<byte[], T> parser)
    {
        try
        {
            var response = Response.Parser.ParseFrom(ConsumeBytes(addr));
            
            if (response.HasError)
            {
                throw new InvalidOperationException($"WASM module returned error: {response.Error}");
            }
            else if (response.HasData)
            {
                return parser(response.Data.ToByteArray());
            }
            else
            {
                throw new InvalidOperationException("WASM response contains neither data nor error");
            }
        }
        catch (InvalidProtocolBufferException ex)
        {
            throw new InvalidOperationException($"Failed to parse WASM response as protobuf: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Disposes the WASM resolver and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of dispose pattern.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
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
                _store?.Dispose();
                _module?.Dispose();
                _engine?.Dispose();
            }
            catch (Exception ex)
            {
                WasmResolverLogger.DisposalError(_logger, ex);
            }
        }

        _disposed = true;
    }
}
