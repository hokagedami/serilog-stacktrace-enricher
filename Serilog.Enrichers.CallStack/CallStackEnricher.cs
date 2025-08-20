using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace Serilog.Enrichers.CallStack;

/// <summary>
/// Enriches log events with call stack information including method names, file names, and line numbers.
/// </summary>
public class CallStackEnricher : ILogEventEnricher
{
    private readonly CallStackEnricherConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallStackEnricher"/> class with default configuration.
    /// </summary>
    public CallStackEnricher() : this(new CallStackEnricherConfiguration())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CallStackEnricher"/> class with the specified configuration.
    /// </summary>
    /// <param name="configuration">The configuration for the enricher.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    public CallStackEnricher(CallStackEnricherConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Enriches the log event with call stack information.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">Factory for creating log event properties.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        try
        {
#if NET6_0_OR_GREATER
            // Use enhanced stack trace capabilities available in .NET 6+
            var stackTrace = new StackTrace(true);
            var frames = stackTrace.GetFrames();
#else
            // Standard stack trace for older frameworks
            var stackTrace = new StackTrace(true);
            var frames = stackTrace.GetFrames();
#endif
            
            // Performance optimization: early exit if no frames
            if (frames?.Length == 0)
                return;
            
            if (frames == null || frames.Length == 0)
                return;

            if (_configuration.UseExceptionLikeFormat)
            {
                var callStackString = BuildCallStackString(frames);
                if (!string.IsNullOrEmpty(callStackString))
                {
                    var property = propertyFactory.CreateProperty(_configuration.CallStackPropertyName, callStackString);
                    logEvent.AddPropertyIfAbsent(property);
                }
            }
            else
            {
                var relevantFrame = FindRelevantFrame(frames);
                if (relevantFrame == null)
                    return;

                AddCallStackProperties(logEvent, propertyFactory, relevantFrame);
            }
        }
        catch (Exception ex) when (_configuration.SuppressExceptions)
        {
            // Silently ignore exceptions when configured to do so
            _configuration.OnException?.Invoke(ex);
            if (_configuration.OnException != null)
            {
                try
                {
                    _configuration.OnException(ex);
                }
                catch
                {
                    // Ignore exceptions in exception handler
                }
            }
        }
    }

    /// <summary>
    /// Finds the most relevant stack frame for logging purposes.
    /// </summary>
    /// <param name="frames">The stack frames to search.</param>
    /// <returns>The most relevant stack frame, or null if none found.</returns>
    private StackFrame? FindRelevantFrame(StackFrame[] frames)
    {
        // Skip frames until we get past Serilog infrastructure
        var relevantFrames = frames
            .Where(frame => !ShouldSkipFrame(frame))
            .ToArray();

        if (relevantFrames.Length == 0)
        {
            // If all frames were skipped, fall back to the first available frame
            // (excluding the enricher itself)
            var fallbackFrames = frames
                .Where(frame => !ShouldSkipEnricherFrame(frame))
                .ToArray();
            
            if (fallbackFrames.Length > 0)
            {
                var fallbackIndex = Math.Min(_configuration.FrameOffset, fallbackFrames.Length - 1);
                return fallbackFrames[fallbackIndex];
            }
            
            return null;
        }

        // Return the first non-skipped frame, optionally offset by configuration
        var targetIndex = Math.Min(_configuration.FrameOffset, relevantFrames.Length - 1);
        return relevantFrames[targetIndex];
    }

    /// <summary>
    /// Determines whether a stack frame should be skipped for enricher-only filtering.
    /// </summary>
    /// <param name="frame">The stack frame to evaluate.</param>
    /// <returns>True if the frame should be skipped, false otherwise.</returns>
    private bool ShouldSkipEnricherFrame(StackFrame frame)
    {
        var method = frame.GetMethod();
        if (method == null)
            return true;

        var declaringType = method.DeclaringType;
        if (declaringType == null)
            return true;

        var typeName = declaringType.FullName ?? declaringType.Name;

        // Only skip Serilog infrastructure and this enricher
        if (typeName.StartsWith("Serilog.", StringComparison.Ordinal))
            return true;

        return false;
    }

    /// <summary>
    /// Determines whether a stack frame should be skipped.
    /// </summary>
    /// <param name="frame">The stack frame to evaluate.</param>
    /// <returns>True if the frame should be skipped, false otherwise.</returns>
    private bool ShouldSkipFrame(StackFrame frame)
    {
        var method = frame.GetMethod();
        if (method == null)
            return true;

        var declaringType = method.DeclaringType;
        if (declaringType == null)
            return true;

        var typeName = declaringType.FullName ?? declaringType.Name;

        // Skip Serilog infrastructure
        if (typeName.StartsWith("Serilog.", StringComparison.Ordinal))
            return true;

        // Skip this enricher
        if (typeName.StartsWith("Serilog.Enrichers.CallStack.", StringComparison.Ordinal))
            return true;

        // Skip reflection and runtime infrastructure
        if (typeName.StartsWith("System.Reflection.", StringComparison.Ordinal) ||
            typeName.StartsWith("System.Runtime", StringComparison.Ordinal) ||
            typeName.StartsWith("System.Threading.", StringComparison.Ordinal) ||
            typeName.Equals("System.RuntimeMethodHandle", StringComparison.Ordinal) ||
            typeName.Equals("System.RuntimeType", StringComparison.Ordinal) ||
            typeName.Contains("Task`1") ||
            typeName.Contains("TaskScheduler"))
            return true;

        // Skip xUnit framework
        if (typeName.StartsWith("Xunit.", StringComparison.Ordinal) ||
            typeName.StartsWith("Microsoft.Extensions.DependencyInjection.", StringComparison.Ordinal))
            return true;

        // Skip common method names that are framework-related
        var methodName = method.Name;
        if (methodName == "InvokeMethod" || 
            methodName == "InvokeWithNoArgs" || 
            methodName == "Invoke" ||
            methodName == "InternalInvoke" ||
            methodName == "InnerInvoke" ||
            methodName == "RunInternal" ||
            methodName == "Start" ||
            methodName.StartsWith("MoveNext"))
            return true;

        // Skip user-defined namespaces
        foreach (var skipNamespace in _configuration.SkipNamespaces)
        {
            if (typeName.StartsWith(skipNamespace, StringComparison.Ordinal))
                return true;
        }

        // Skip user-defined types
        foreach (var skipType in _configuration.SkipTypes)
        {
            if (string.Equals(typeName, skipType, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Adds call stack properties to the log event.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">Factory for creating log event properties.</param>
    /// <param name="frame">The stack frame to extract information from.</param>
    private void AddCallStackProperties(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, StackFrame frame)
    {
        var method = frame.GetMethod();
        if (method == null)
            return;

        var properties = new Dictionary<string, object?>();

        // Add method information
        if (_configuration.IncludeMethodName)
        {
            var methodName = GetMethodName(method);
            properties[_configuration.MethodNamePropertyName] = methodName;
        }

        // Add type information
        if (_configuration.IncludeTypeName && method.DeclaringType != null)
        {
            var typeName = _configuration.UseFullTypeName 
                ? method.DeclaringType.FullName ?? method.DeclaringType.Name
                : method.DeclaringType.Name;
            properties[_configuration.TypeNamePropertyName] = typeName;
        }

        // Add file information
        var fileName = frame.GetFileName();
        if (_configuration.IncludeFileName && !string.IsNullOrEmpty(fileName))
        {
            var fileNameToUse = _configuration.UseFullFileName 
                ? fileName 
                : Path.GetFileName(fileName);
            properties[_configuration.FileNamePropertyName] = fileNameToUse;
        }

        // Add line number
        if (_configuration.IncludeLineNumber)
        {
            var lineNumber = frame.GetFileLineNumber();
            if (lineNumber > 0)
            {
                properties[_configuration.LineNumberPropertyName] = lineNumber;
            }
        }

        // Add column number
        if (_configuration.IncludeColumnNumber)
        {
            var columnNumber = frame.GetFileColumnNumber();
            if (columnNumber > 0)
            {
                properties[_configuration.ColumnNumberPropertyName] = columnNumber;
            }
        }

        // Add assembly information
        if (_configuration.IncludeAssemblyName && method.DeclaringType?.Assembly != null)
        {
            var assemblyName = method.DeclaringType.Assembly.GetName().Name;
            if (!string.IsNullOrEmpty(assemblyName))
            {
                properties[_configuration.AssemblyNamePropertyName] = assemblyName;
            }
        }

        // Create and add properties to the log event
        foreach (var kvp in properties)
        {
            if (kvp.Value != null)
            {
                var property = propertyFactory.CreateProperty(kvp.Key, kvp.Value);
                logEvent.AddPropertyIfAbsent(property);
            }
        }
    }

    /// <summary>
    /// Gets a formatted method name including parameters if configured.
    /// </summary>
    /// <param name="method">The method to get the name for.</param>
    /// <returns>The formatted method name.</returns>
    private string GetMethodName(MethodBase method)
    {
        if (!_configuration.IncludeMethodParameters)
            return method.Name;

        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return $"{method.Name}()";

        var parameterNames = parameters.Select(p => 
            _configuration.UseFullParameterTypes 
                ? $"{p.ParameterType.FullName ?? p.ParameterType.Name} {p.Name}"
                : $"{p.ParameterType.Name} {p.Name}");

        return $"{method.Name}({string.Join(", ", parameterNames)})";
    }

    /// <summary>
    /// Builds an exception-like call stack string from stack frames.
    /// </summary>
    /// <param name="frames">The stack frames to process.</param>
    /// <returns>A formatted call stack string.</returns>
    private string BuildCallStackString(StackFrame[] frames)
    {
        var relevantFrames = frames
            .Where(frame => !ShouldSkipFrame(frame))
            .ToArray();

        if (relevantFrames.Length == 0)
        {
            // Fallback to non-skipped frames if all were filtered
            relevantFrames = frames
                .Where(frame => !ShouldSkipEnricherFrame(frame))
                .ToArray();
        }

        if (relevantFrames.Length == 0)
            return string.Empty;

        // Apply frame offset
        var startIndex = Math.Min(_configuration.FrameOffset, relevantFrames.Length - 1);
#if NET8_0_OR_GREATER
        // Use Span<T> for better performance in .NET 8+
        var frameSpan = relevantFrames.AsSpan(startIndex);
        relevantFrames = frameSpan.ToArray();
#else
        relevantFrames = relevantFrames.Skip(startIndex).ToArray();
#endif

        // Limit the number of frames
        if (_configuration.MaxFrames > 0 && relevantFrames.Length > _configuration.MaxFrames)
        {
            relevantFrames = relevantFrames.Take(_configuration.MaxFrames).ToArray();
        }

        var callStackParts = new List<string>();
        
        foreach (var frame in relevantFrames)
        {
            var frameString = FormatStackFrame(frame);
            if (!string.IsNullOrEmpty(frameString))
            {
                callStackParts.Add(frameString);
            }
        }

#if NET6_0_OR_GREATER
        // Use optimized string.Join for .NET 6+ with better memory efficiency
        return callStackParts.Count > 0 ? string.Join(" --> ", callStackParts) : string.Empty;
#else
        // Standard string joining for older frameworks
        return callStackParts.Count > 0 ? string.Join(" --> ", callStackParts) : string.Empty;
#endif
    }

    /// <summary>
    /// Formats a single stack frame into a string representation.
    /// </summary>
    /// <param name="frame">The stack frame to format.</param>
    /// <returns>A formatted string representing the stack frame.</returns>
    private string FormatStackFrame(StackFrame frame)
    {
        var method = frame.GetMethod();
        if (method == null)
            return string.Empty;

        // Use cached method information for better performance
        var cachedInfo = MethodInfoCache.GetMethodInfo(method);
        if (!cachedInfo.IsValid)
            return string.Empty;

        var parts = new List<string>();

        // Add type and method name using cached values
        if (_configuration.IncludeTypeName)
        {
            var typeName = _configuration.UseFullTypeName 
                ? cachedInfo.TypeName
                : GetShortTypeName(cachedInfo.TypeName);
            
            var methodName = GetMethodName(method);
            parts.Add($"{typeName}.{methodName}");
        }
        else if (_configuration.IncludeMethodName)
        {
            var methodName = GetMethodName(method);
            parts.Add(methodName);
        }

        // Add line number if available and configured
        if (_configuration.IncludeLineNumber)
        {
            var lineNumber = frame.GetFileLineNumber();
            if (lineNumber > 0)
            {
                var lastPart = parts.LastOrDefault();
                if (!string.IsNullOrEmpty(lastPart))
                {
                    parts[parts.Count - 1] = $"{lastPart}:{lineNumber}";
                }
            }
        }

        return parts.Count > 0 ? string.Join(" ", parts) : string.Empty;
    }

    /// <summary>
    /// Extracts the short type name from a full type name.
    /// </summary>
    /// <param name="fullTypeName">The full type name.</param>
    /// <returns>The short type name without namespace.</returns>
    private static string GetShortTypeName(string fullTypeName)
    {
        if (string.IsNullOrEmpty(fullTypeName))
            return fullTypeName;

        var lastDotIndex = fullTypeName.LastIndexOf('.');
        return lastDotIndex >= 0 ? fullTypeName.Substring(lastDotIndex + 1) : fullTypeName;
    }
}