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
using Wasmtime;
using Confidence.Wasm;
using Confidence.Flags.Resolver.V1;

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
                // Don't initialize with null pointer - wait for proper state to be set later
                WasmResolverLogger.ResolverInitialized(_logger, "Skipped initialization - will set proper state later");
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
    /// Wraps data in a confidence.wasm.Request message and serializes it to bytes.
    /// </summary>
    /// <param name="data">The data to wrap in the request.</param>
    /// <returns>Serialized request bytes.</returns>
    private static byte[] CreateWasmRequest(byte[] data)
    {
        var request = new Request
        {
            Data = Google.Protobuf.ByteString.CopyFrom(data)
        };
        using var stream = new MemoryStream();
        using var output = new Google.Protobuf.CodedOutputStream(stream);
        request.WriteTo(output);
        output.Flush();
        return stream.ToArray();
    }

    /// <summary>
    /// Prints user-friendly details about the resolve request.
    /// </summary>
    /// <param name="request">The request to print.</param>
    private static void PrintRequestDetails(ResolveFlagsRequest request)
    {
        Console.WriteLine($"[WasmResolver] 🔍 Resolve Request Details:");
        Console.WriteLine($"  📝 Flags: [{string.Join(", ", request.Flags.Select(f => $"'{f}'"))}]");
        Console.WriteLine($"  🔑 Client Secret: '{(string.IsNullOrEmpty(request.ClientSecret) ? "<empty>" : new string('*', Math.Min(8, request.ClientSecret.Length)))}'");
        
        if (request.EvaluationContext.Fields.Count > 0)
        {
            Console.WriteLine($"  🎯 Context ({request.EvaluationContext.Fields.Count} fields):");
            foreach (var kvp in request.EvaluationContext.Fields.Take(5)) // Show first 5 context fields
            {
                Console.WriteLine($"    • {kvp.Key}: {kvp.Value}");
            }
            if (request.EvaluationContext.Fields.Count > 5)
            {
                Console.WriteLine($"    ... and {request.EvaluationContext.Fields.Count - 5} more fields");
            }
        }
        else
        {
            Console.WriteLine($"  🎯 Context: <empty>");
        }
    }

    /// <summary>
    /// Prints user-friendly details about the response from WASM.
    /// </summary>
    /// <param name="addr">Memory address of the response.</param>
    /// <param name="responseBytes">The response bytes.</param>
    private static void PrintResponseDetails(int addr, byte[] responseBytes)
    {
        Console.WriteLine($"[WasmResolver] 📨 WASM Response Details:");
        Console.WriteLine($"  📍 Memory Address: {addr}");
        Console.WriteLine($"  📏 Response Size: {responseBytes.Length} bytes");
        
        // Show first 32 bytes in hex
        var preview = responseBytes.Take(32).ToArray();
        Console.WriteLine($"  🔢 First 32 bytes (hex): {string.Join(" ", preview.Select(b => $"0x{b:X2}"))}");
        
        // Try to interpret as UTF-8 string
        try
        {
            var readableString = Encoding.UTF8.GetString(preview)
                .Replace('\0', '�')
                .Replace('\r', '\\')
                .Replace('\n', '\\');
            
            if (!string.IsNullOrWhiteSpace(readableString))
            {
                Console.WriteLine($"  📝 As UTF-8 text: \"{readableString}\"");
                
                // Check if it looks like an error message
                if (readableString.ToLower().Contains("error") || 
                    readableString.ToLower().Contains("not found") || 
                    readableString.ToLower().Contains("invalid") ||
                    readableString.ToLower().Contains("could not"))
                {
                    Console.WriteLine($"  ⚠️  Looks like an error message!");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Could not interpret as UTF-8: {ex.Message}");
        }
        
        // Try to show full readable string if response is small enough
        if (responseBytes.Length <= 512)
        {
            try
            {
                var fullString = Encoding.UTF8.GetString(responseBytes)
                    .Replace('\0', '�')
                    .Replace('\r', '\\')
                    .Replace('\n', '\\');
                
                if (!string.IsNullOrWhiteSpace(fullString) && fullString != Encoding.UTF8.GetString(preview).Replace('\0', '�').Replace('\r', '\\').Replace('\n', '\\'))
                {
                    Console.WriteLine($"  📄 Full response text: \"{fullString}\"");
                }
            }
            catch
            {
                // Ignore if full string conversion fails
            }
        }
    }

    /// <summary>
    /// Prints user-friendly details about the final resolve response.
    /// </summary>
    /// <param name="response">The resolve response to print.</param>
    /// <param name="flagName">The name of the flag that was resolved.</param>
    private static void PrintResolveResponse(ResolveFlagsResponse? response, string flagName)
    {
        Console.WriteLine($"[WasmResolver] 🎯 Final Resolution Result for '{flagName}':");
        
        if (response == null)
        {
            Console.WriteLine($"  ❌ Response: <null>");
            return;
        }
        
        Console.WriteLine($"  ✅ Success: {response.ResolvedFlags.Count > 0}");
        
        if (response.ResolvedFlags.Count > 0)
        {
            Console.WriteLine($"  📄 Value: {response.ResolvedFlags[0].Value}");
            Console.WriteLine($"  🏷️  Variant: {response.ResolvedFlags[0].Variant}");
            Console.WriteLine($"  📝 Reason: {response.ResolvedFlags[0].Reason}");
        }
        else
        {
            Console.WriteLine($"  ❌ Response: {response.ToString()}");
        }
        
        // Show value type if available
        if (response.ResolvedFlags.Count > 0 && response.ResolvedFlags[0].Value != null)
        {
            var valueType = response.ResolvedFlags[0].Value.GetType().Name;
            Console.WriteLine($"  🔢 Value Type: {valueType}");
        }
    }

    /// <summary>
    /// Converts a ResolveRequest to ResolveFlagsRequest protobuf bytes.
    /// </summary>
    /// <param name="request">The request to convert.</param>
    /// <returns>Serialized ResolveFlagsRequest bytes.</returns>
    private static byte[] ConvertToResolveFlagsRequestBytes(ResolveFlagsRequest request)
    {
        // For now, create a minimal protobuf structure manually
        // TODO: Replace with generated protobuf classes once available
        using var stream = new MemoryStream();
        using var output = new Google.Protobuf.CodedOutputStream(stream);
        
        // Field 1: repeated string flags
        if (request.Flags.Count > 0)
        {
            output.WriteTag(1, Google.Protobuf.WireFormat.WireType.LengthDelimited);
            output.WriteString(request.Flags[0]);
        }
        
        // Field 2: google.protobuf.Struct evaluation_context  
        if (request.EvaluationContext.Fields.Count > 0)
        {
            var contextStruct = new Google.Protobuf.WellKnownTypes.Struct();
            foreach (var kvp in request.EvaluationContext.Fields)
            {
                contextStruct.Fields[kvp.Key] = Google.Protobuf.WellKnownTypes.Value.ForString(kvp.Value?.ToString() ?? string.Empty);
            }
            output.WriteTag(2, Google.Protobuf.WireFormat.WireType.LengthDelimited);
            output.WriteMessage(contextStruct);
        }
        
        // Field 3: string client_secret
        if (!string.IsNullOrEmpty(request.ClientSecret))
        {
            output.WriteTag(3, Google.Protobuf.WireFormat.WireType.LengthDelimited);
            output.WriteString(request.ClientSecret);
        }
        
        // Field 4: bool apply (required, default to true)
        output.WriteTag(4, Google.Protobuf.WireFormat.WireType.Varint);
        output.WriteBool(true);
        
        output.Flush();
        return stream.ToArray();
    }

    /// <summary>
    /// Converts ResolveFlagsResponse protobuf bytes to ResolveResponse.
    /// </summary>
    /// <param name="responseBytes">The protobuf response bytes.</param>
    /// <returns>Converted ResolveResponse object.</returns>
    private static ResolveFlagsResponse ConvertFromResolveFlagsResponseBytes(byte[] responseBytes)
    {
        try
        {
            Console.WriteLine($"[WasmResolver] Response bytes as UTF-8 string: {System.Text.Encoding.UTF8.GetString(responseBytes)}");
            
            ResolveFlagsResponse response = ResolveFlagsResponse.Parser.ParseFrom(responseBytes);
            
            Console.WriteLine($"[WasmResolver] Parsed {responseBytes.Length} bytes of ResolveFlagsResponse");
            
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WasmResolver] Failed to parse ResolveFlagsResponse: {ex.Message}");
            
            return new ResolveFlagsResponse();
        }
    }

    /// <summary>
    /// Transfers a request to WASM memory and returns the pointer, similar to Java transferRequest method.
    /// </summary>
    /// <param name="data">The data to transfer to WASM memory.</param>
    /// <returns>Pointer to the request data in WASM memory.</returns>
    private int TransferRequest(byte[] data)
    {
        var requestBytes = CreateWasmRequest(data);
        var requestPtr = AllocateMemory(requestBytes.Length);
        WriteToMemory(requestPtr, requestBytes);
        return requestPtr;
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
            // Transfer the state bytes to WASM memory
            var requestPtr = TransferRequest(stateBytes);

            Console.WriteLine($"[WasmResolver] Transferred {stateBytes.Length} bytes to WASM memory at pointer {requestPtr}");

            // Find the set resolver state function
            var setStateFunction = _instance.GetFunction("wasm_msg_guest_set_resolver_state");
            
            Console.WriteLine($"[WasmResolver] Calling WASM function with pointer={requestPtr}");
            
            // Call the function with just the pointer (WASM function signature requires 1 parameter)
            var result = setStateFunction.Invoke(requestPtr);
            
            Console.WriteLine($"[WasmResolver] WASM function returned: {result}");
            
            // Deallocate the input memory
            DeallocateMemory(requestPtr);
            
            // For now, let's just check if we got a non-null result (we'll add protobuf parsing later)
            var success = result != null;
            
            if (success)
            {
                Console.WriteLine($"Successfully called WASM set_resolver_state function with {stateBytes.Length} bytes");
                _logger.LogInformation("Successfully set resolver state ({Length} bytes)", stateBytes.Length);
                
                // TODO: Add protobuf response parsing once basic call works
                // For now, assume success if we got here without exception
                return true;
            }
            else
            {
                Console.WriteLine("WASM function returned null");
                _logger.LogWarning("WASM function returned null");
                return false;
            }
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
    public async Task<ResolveFlagsResponse> ResolveAsync(ResolveFlagsRequest request, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WasmResolver));
        }

        if (_resolveFunction == null || _allocFunction == null || _store == null)
        {
            return new ResolveFlagsResponse();
        }

        try
        {
            // Print user-friendly request details
            PrintRequestDetails(request);
            
            // Convert to protobuf ResolveFlagsRequest format (following Java implementation)
            var requestBytes = ConvertToResolveFlagsRequestBytes(request);
            
            // Technical details about the protobuf bytes
            Console.WriteLine($"[WasmResolver] 📦 Protobuf Details: {requestBytes.Length} bytes");
            Console.WriteLine($"  🔢 First 32 bytes: {string.Join(" ", requestBytes.Take(32).Select(b => $"0x{b:X2}"))}");
            
            // Transfer request to WASM memory (following Java transferRequest pattern)
            var requestPtr = TransferRequest(requestBytes);
            
            Console.WriteLine($"[WasmResolver] ✅ Transferred to WASM memory at pointer {requestPtr}");

            // Call the resolve function (following Java pattern: wasmMsgGuestResolve.apply(reqPtr)[0])
            var resultPtr = (int)_resolveFunction.Invoke(requestPtr)!;
            
            Console.WriteLine($"[WasmResolver] 🎯 WASM function returned pointer: {resultPtr}");
            
            // Deallocate request memory
            DeallocateMemory(requestPtr);
            
            // Consume response (following Java pattern: consumeResponse(respPtr, ResolveFlagsResponse::parseFrom))
            var responseBytes = ConsumeResponse(resultPtr, bytes => bytes);
            
            // Print user-friendly response details
            PrintResponseDetails(resultPtr, responseBytes);
            
            var response = ConvertFromResolveFlagsResponseBytes(responseBytes);
            
            Console.WriteLine($"[WasmResolver] ✅ Successfully processed WASM response");
            
            // Print user-friendly final response details
            var finalResponse = response ?? new ResolveFlagsResponse();
            
            PrintResolveResponse(finalResponse, request.Flags[0]);
            
            return finalResponse;
        }
        catch (Exception ex)
        {
            WasmResolverLogger.FlagResolutionError(_logger, request.Flags[0], ex);
            var errorResponse = new ResolveFlagsResponse();
            
            // Print user-friendly error response details
            PrintResolveResponse(errorResponse, request.Flags[0]);
            
            return errorResponse;
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
    /// Reads a byte array from WASM memory at the specified address.
    /// Based on the Java implementation's consume() method.
    /// </summary>
    /// <param name="addr">Memory address returned by WASM function</param>
    /// <returns>Byte array read from WASM memory</returns>
    private byte[] ConsumeBytes(int addr)
    {
        if (_memory == null)
        {
            throw new InvalidOperationException("Memory not found in WASM module");
        }

        var span = _memory.GetSpan<byte>(0);
        
        // Low-level memory examination for debugging
        var first1024Bytes = span.Slice(addr, Math.Min(1024, span.Length - addr)).ToArray();
        Console.WriteLine($"[WasmResolver] 🔍 Raw memory examination at address {addr}");
        Console.WriteLine($"  📊 Raw bytes (first 64): {string.Join(" ", first1024Bytes.Take(64).Select(b => $"0x{b:X2}"))}...");
        
        // Try to interpret as UTF-8 string for debugging
        try
        {
            var readableString = Encoding.UTF8.GetString(first1024Bytes).Replace('\0', '�').Replace('\r', '\\').Replace('\n', '\\');
            var preview = readableString.Substring(0, Math.Min(100, readableString.Length));
            Console.WriteLine($"  📝 Raw UTF-8 preview: \"{preview}...\"");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ UTF-8 conversion failed: {ex.Message}");
        }
        
        // The WASM module returns a pointer to a length-prefixed byte array
        // First 4 bytes contain the length (little-endian)
        var lengthBytes = span.Slice(addr, 4);
        var length = BitConverter.ToInt32(lengthBytes);
        
        Console.WriteLine($"[WasmResolver] Length prefix reads as: {length} (0x{length:X8})");
        
        // Sanity check the length - if it's too large, something is wrong
        if (length < 0 || length > 1024 * 1024) // Max 1MB
        {
            Console.WriteLine($"[WasmResolver] ERROR: Invalid length {length}, falling back to old string reading method");
            // Fall back to the old null-terminated string reading
            return Encoding.UTF8.GetBytes(ReadStringFromMemory(addr));
        }
        
        Console.WriteLine($"[WasmResolver] Reading {length} bytes from WASM memory at address {addr}");
        
        // Read the actual data starting after the length prefix
        var dataBytes = span.Slice(addr + 4, length).ToArray();
        
        return dataBytes;
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
            var responseBytes = ConsumeBytes(addr);
            var response = Response.Parser.ParseFrom(responseBytes);
            
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
        catch (Google.Protobuf.InvalidProtocolBufferException ex)
        {
            throw new InvalidOperationException($"Failed to parse WASM response as protobuf: {ex.Message}", ex);
        }
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
