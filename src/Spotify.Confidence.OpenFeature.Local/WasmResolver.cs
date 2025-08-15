using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spotify.Confidence.OpenFeature.Local.Logging;
using Spotify.Confidence.OpenFeature.Local.Models;
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
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WasmResolver"/> class from a file path.
    /// </summary>
    /// <param name="wasmFilePath">Path to the WASM file to load.</param>
    /// <param name="logger">Optional logger instance.</param>
    public WasmResolver(string wasmFilePath, ILogger<WasmResolver>? logger = null)
    {
        _logger = logger ?? NullLogger<WasmResolver>.Instance;

        try
        {
            // Initialize Wasmtime engine
            _engine = new Engine();
            
            // Load and compile the WASM module
            var wasmBytes = File.ReadAllBytes(wasmFilePath);
            _module = Wasmtime.Module.FromBytes(_engine, wasmFilePath, wasmBytes);
            
            // Create a store and try instantiation with imports
            _store = new Store(_engine);
            
            try
            {
                _instance = new Instance(_store, _module, Array.Empty<object>());
            }
            catch (Exception ex)
            {
                WasmResolverLogger.ImportsNeeded(_logger, ex.Message);
                
                // Create imports - trying wasm_msg_current_thread_id first
                var imports = new object[]
                {
                    // Import 1: wasm_msg_current_thread_id - (result i32) - NO parameters  
                    Function.FromCallback(_store, () => 
                    {
                        var threadId = Environment.CurrentManagedThreadId;
                        WasmResolverLogger.WasmImportCalledWithThreadId(_logger, "wasm_msg_current_thread_id", threadId);
                        return threadId;
                    }),
                    
                    // Import 2: log_resolve - (param i32) (result i32)
                    Function.FromCallback(_store, (int ptr) => 
                    {
                        WasmResolverLogger.WasmImportCalled(_logger, "log_resolve", ptr);
                        return 0; // Return success
                    }),
                    
                    // Import 3: log_assign - (param i32) (result i32)
                    Function.FromCallback(_store, (int ptr) => 
                    {
                        WasmResolverLogger.WasmImportCalled(_logger, "log_assign", ptr);
                        return 0;
                    }),
                    
                    // Import 4: wasm_msg_host_current_time - (param i32) (result i32)
                    Function.FromCallback(_store, (int ptr) => 
                    {
                        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        WasmResolverLogger.WasmImportCalledWithTimestamp(_logger, "wasm_msg_host_current_time", ptr, timestamp);
                        return (int)timestamp; // Return current time as i32
                    })
                };
                
                _instance = new Instance(_store, _module, imports);
                WasmResolverLogger.ModuleInstantiated(_logger, imports.Length);
            }

            // Get exported functions and log what's available
            try
            {
                _resolveFunction = _instance.GetFunction("wasm_msg_guest_resolve");
                WasmResolverLogger.ExportFound(_logger, "wasm_msg_guest_resolve");
            }
            catch (Exception ex)
            {
                WasmResolverLogger.ExportNotFound(_logger, "wasm_msg_guest_resolve", ex.Message, ex);
            }
            
            try
            {
                _allocFunction = _instance.GetFunction("wasm_msg_alloc");
                WasmResolverLogger.ExportFound(_logger, "wasm_msg_alloc");
            }
            catch (Exception ex)
            {
                WasmResolverLogger.ExportNotFound(_logger, "wasm_msg_alloc", ex.Message, ex);
            }
            
            try
            {
                _deallocFunction = _instance.GetFunction("wasm_msg_free");
                WasmResolverLogger.ExportFound(_logger, "wasm_msg_free");
            }
            catch (Exception ex)
            {
                WasmResolverLogger.ExportNotFound(_logger, "wasm_msg_free", ex.Message, ex);
            }
            
            try
            {
                var setResolverStateFunction = _instance.GetFunction("wasm_msg_guest_set_resolver_state");
                WasmResolverLogger.ExportFound(_logger, "wasm_msg_guest_set_resolver_state");
                
                // Initialize the resolver with basic state
                var initResult = setResolverStateFunction.Invoke(0); // Pass 0 as basic initialization
                WasmResolverLogger.ResolverInitialized(_logger, initResult);
            }
            catch (Exception ex)
            {
                WasmResolverLogger.SetResolverStateFailed(_logger, ex.Message, ex);
            }
            
            try
            {
                _memory = _instance.GetMemory("memory");
                WasmResolverLogger.ExportFound(_logger, "memory");
            }
            catch (Exception ex)
            {
                WasmResolverLogger.ExportNotFound(_logger, "memory", ex.Message, ex);
            }

            WasmResolverLogger.WasmResolverInitialized(_logger, wasmFilePath);
        }
        catch (Exception ex)
        {
            WasmResolverLogger.WasmResolverInitializationFailed(_logger, wasmFilePath, ex);
            Dispose();
            throw;
        }
    }

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
            // Initialize Wasmtime engine
            _engine = new Engine();
            
            // Load and compile the WASM module from embedded resource
            var wasmBytes = LoadEmbeddedResource(resourceName, assembly);
            _module = Wasmtime.Module.FromBytes(_engine, resourceName, wasmBytes);
            
            // Create a store and try instantiation with imports
            _store = new Store(_engine);
            
            try
            {
                _instance = new Instance(_store, _module, Array.Empty<object>());
            }
            catch (Exception ex)
            {
                WasmResolverLogger.ImportsNeeded(_logger, ex.Message);
                
                // Create imports - trying wasm_msg_current_thread_id first
                var imports = new object[]
                {
                    // Import 1: wasm_msg_current_thread_id - (result i32) - NO parameters  
                    Function.FromCallback(_store, () => 
                    {
                        var threadId = Environment.CurrentManagedThreadId;
                        WasmResolverLogger.WasmImportCalledWithThreadId(_logger, "wasm_msg_current_thread_id", threadId);
                        return threadId;
                    }),
                    
                    // Import 2: log_resolve - (param i32) (result i32)
                    Function.FromCallback(_store, (int ptr) => 
                    {
                        WasmResolverLogger.WasmImportCalled(_logger, "log_resolve", ptr);
                        return 0; // Return success
                    }),
                    
                    // Import 3: log_assign - (param i32) (result i32)
                    Function.FromCallback(_store, (int ptr) => 
                    {
                        WasmResolverLogger.WasmImportCalled(_logger, "log_assign", ptr);
                        return 0;
                    }),
                    
                    // Import 4: wasm_msg_host_current_time - (param i32) (result i32)
                    Function.FromCallback(_store, (int ptr) => 
                    {
                        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        WasmResolverLogger.WasmImportCalledWithTimestamp(_logger, "wasm_msg_host_current_time", ptr, timestamp);
                        return (int)timestamp; // Return current time as i32
                    })
                };
                
                _instance = new Instance(_store, _module, imports);
                WasmResolverLogger.ModuleInstantiated(_logger, imports.Length);
            }

            // Get exported functions and log what's available
            try
            {
                _resolveFunction = _instance.GetFunction("wasm_msg_guest_resolve");
                WasmResolverLogger.ExportFound(_logger, "wasm_msg_guest_resolve");
            }
            catch (Exception ex)
            {
                WasmResolverLogger.ExportNotFound(_logger, "wasm_msg_guest_resolve", ex.Message, ex);
            }
            
            try
            {
                _allocFunction = _instance.GetFunction("wasm_msg_alloc");
                WasmResolverLogger.ExportFound(_logger, "wasm_msg_alloc");
            }
            catch (Exception ex)
            {
                WasmResolverLogger.ExportNotFound(_logger, "wasm_msg_alloc", ex.Message, ex);
            }
            
            try
            {
                _deallocFunction = _instance.GetFunction("wasm_msg_free");
                WasmResolverLogger.ExportFound(_logger, "wasm_msg_free");
            }
            catch (Exception ex)
            {
                WasmResolverLogger.ExportNotFound(_logger, "wasm_msg_free", ex.Message, ex);
            }
            
            try
            {
                var setResolverStateFunction = _instance.GetFunction("wasm_msg_guest_set_resolver_state");
                WasmResolverLogger.ExportFound(_logger, "wasm_msg_guest_set_resolver_state");
                
                // Initialize the resolver with basic state
                var initResult = setResolverStateFunction.Invoke(0); // Pass 0 as basic initialization
                WasmResolverLogger.ResolverInitialized(_logger, initResult);
            }
            catch (Exception ex)
            {
                WasmResolverLogger.SetResolverStateFailed(_logger, ex.Message, ex);
            }
            
            try
            {
                _memory = _instance.GetMemory("memory");
                WasmResolverLogger.ExportFound(_logger, "memory");
            }
            catch (Exception ex)
            {
                WasmResolverLogger.ExportNotFound(_logger, "memory", ex.Message, ex);
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
            
            if (prefixedStream == null)
            {
                // List available resources for debugging
                var availableResources = assembly.GetManifestResourceNames();
                var resourceList = string.Join(", ", availableResources);
                throw new FileNotFoundException(
                    $"Embedded resource '{resourceName}' not found. Available resources: {resourceList}");
            }
            
            return ReadStreamToBytes(prefixedStream);
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
    /// Sets the resolver state in the WASM module.
    /// </summary>
    /// <param name="stateBytes">The serialized ResolverState protobuf bytes to set in the resolver</param>
    /// <returns>True if the state was set successfully, false otherwise</returns>
    public bool SetResolverState(byte[] stateBytes)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WasmResolver));
        }

        if (stateBytes == null || stateBytes.Length == 0)
        {
            _logger.LogWarning("Cannot set empty or null resolver state");
            return false;
        }

        if (_instance == null)
        {
            _logger.LogWarning("Cannot set resolver state: WASM instance not initialized");
            return false;
        }

        try
        {
            // Allocate memory in WASM for the state bytes
            var statePtr = AllocateMemory(stateBytes.Length);
            WriteToMemory(statePtr, stateBytes);

            // Find the set resolver state function
            var setStateFunction = _instance.GetFunction("wasm_msg_guest_set_resolver_state");
            
            // Call the function with the state pointer
            var result = setStateFunction.Invoke(statePtr);
            
            // Deallocate the memory
            DeallocateMemory(statePtr);
            
            var success = result != null && Convert.ToInt32(result) == 0; // Assuming 0 means success
            
            if (success)
            {
                _logger.LogInformation("Successfully set resolver state ({Length} bytes)", stateBytes.Length);
            }
            else
            {
                _logger.LogWarning("Failed to set resolver state, WASM function returned: {Result}", result);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting resolver state in WASM module");
            return false;
        }
    }

    /// <summary>
    /// Resolves a feature flag using the WASM module.
    /// </summary>
    /// <param name="request">The resolve request containing flag key, context, and credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolution response.</returns>
    public async Task<ResolveResponse> ResolveAsync(ResolveRequest request, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WasmResolver));
        }

        if (_resolveFunction == null || _allocFunction == null || _store == null)
        {
            return new ResolveResponse
            {
                Success = false,
                Error = "WASM resolver functions not initialized",
                Reason = "ERROR"
            };
        }

        try
        {
            // Serialize request to JSON
            var requestJson = JsonSerializer.Serialize(request);
            var requestBytes = Encoding.UTF8.GetBytes(requestJson);

            // Allocate memory in WASM for the request
            var requestPtr = AllocateMemory(requestBytes.Length);
            WriteToMemory(requestPtr, requestBytes);

            // Call the resolve function (WASM function expects only the pointer argument)
            var resultPtr = _resolveFunction.Invoke(requestPtr);
            
            // Read the result
            var resultJson = ReadStringFromMemory((int)resultPtr!);
            
            // Deallocate request memory
            DeallocateMemory(requestPtr);
            
            // Parse and return the response
            var response = JsonSerializer.Deserialize<ResolveResponse>(resultJson);
            return response ?? new ResolveResponse 
            { 
                Success = false, 
                Error = "Failed to deserialize WASM response",
                Reason = "ERROR"
            };
        }
        catch (Exception ex)
        {
            WasmResolverLogger.FlagResolutionError(_logger, request.Flag, ex);
            return new ResolveResponse
            {
                Success = false,
                Error = ex.Message,
                Reason = "ERROR"
            };
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

    private string ReadStringFromMemory(int ptr)
    {
        if (_memory == null)
        {
            throw new InvalidOperationException("Memory not found in WASM module");
        }

        var span = _memory.GetSpan<byte>(0);
        
        // Find the null terminator
        var length = 0;
        while (ptr + length < span.Length && span[ptr + length] != 0)
        {
            length++;
        }

        // Read the string
        var bytes = span.Slice(ptr, length).ToArray();
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Disposes the WASM resolver and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

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

        _disposed = true;
    }
}
