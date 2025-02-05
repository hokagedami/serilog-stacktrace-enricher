using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

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
            var stackTrace = new StackTrace(true);
            var frames = stackTrace.GetFrames();
            
            if (frames == null || frames.Length == 0)
                return;

            var relevantFrame = FindRelevantFrame(frames);
            if (relevantFrame == null)
                return;

            AddCallStackProperties(logEvent, propertyFactory, relevantFrame);
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
            return null;

        // Return the first non-skipped frame, optionally offset by configuration
        var targetIndex = Math.Min(_configuration.FrameOffset, relevantFrames.Length - 1);
        return relevantFrames[targetIndex];
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
}