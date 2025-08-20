using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Serilog.Enrichers.CallStack;

/// <summary>
/// Lazy evaluation wrapper for call stack information to defer expensive operations.
/// </summary>
internal sealed class LazyCallStackInfo
{
    private readonly Lazy<StackFrame[]> _frames;
    private readonly Lazy<string> _callStackString;
    private readonly CallStackEnricherConfiguration _configuration;

    public LazyCallStackInfo(CallStackEnricherConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        // Lazy initialization of stack frames - only computed when first accessed
        _frames = new Lazy<StackFrame[]>(CaptureStackFrames, LazyThreadSafetyMode.ExecutionAndPublication);
        
        // Lazy initialization of call stack string - only built when serialized
        _callStackString = new Lazy<string>(BuildCallStackString, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Gets the stack frames, computing them only when first accessed.
    /// </summary>
    public StackFrame[] Frames => _frames.Value;

    /// <summary>
    /// Gets the call stack string, building it only when first accessed.
    /// </summary>
    public string CallStackString => _callStackString.Value;

    /// <summary>
    /// Checks if the call stack has been evaluated yet.
    /// </summary>
    public bool IsFramesCaptured => _frames.IsValueCreated;

    /// <summary>
    /// Checks if the call stack string has been built yet.
    /// </summary>
    public bool IsCallStackStringBuilt => _callStackString.IsValueCreated;

    /// <summary>
    /// Gets the most relevant frame without building the full call stack string.
    /// </summary>
    /// <returns>The most relevant stack frame or null if none found.</returns>
    public StackFrame? GetRelevantFrame()
    {
        var frames = Frames;
        if (frames == null || frames.Length == 0)
            return null;

        return FindRelevantFrame(frames);
    }

    private StackFrame[] CaptureStackFrames()
    {
        try
        {
#if NET6_0_OR_GREATER
            // Use enhanced stack trace capabilities available in .NET 6+
            var stackTrace = new StackTrace(true);
            return stackTrace.GetFrames() ?? Array.Empty<StackFrame>();
#else
            // Standard stack trace for older frameworks
            var stackTrace = new StackTrace(true);
            return stackTrace.GetFrames() ?? new StackFrame[0];
#endif
        }
        catch
        {
#if NET6_0_OR_GREATER
            return Array.Empty<StackFrame>();
#else
            return new StackFrame[0];
#endif
        }
    }

    private string BuildCallStackString()
    {
        var frames = Frames;
        if (frames == null || frames.Length == 0)
            return string.Empty;

        return CallStackBuilder.BuildCallStackString(frames, _configuration);
    }

    private StackFrame? FindRelevantFrame(StackFrame[] frames)
    {
        return CallStackBuilder.FindRelevantFrame(frames, _configuration);
    }
}

/// <summary>
/// Helper class containing shared call stack building logic.
/// </summary>
internal static class CallStackBuilder
{
    /// <summary>
    /// Builds a call stack string from frames using the specified configuration.
    /// </summary>
    /// <param name="frames">The stack frames to process.</param>
    /// <param name="configuration">The enricher configuration.</param>
    /// <returns>A formatted call stack string.</returns>
    public static string BuildCallStackString(StackFrame[] frames, CallStackEnricherConfiguration configuration)
    {
        var relevantFrames = frames
            .Where(frame => !ShouldSkipFrame(frame, configuration))
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
        var startIndex = Math.Min(configuration.FrameOffset, relevantFrames.Length - 1);
#if NET8_0_OR_GREATER
        // Use Span<T> for better performance in .NET 8+
        var frameSpan = relevantFrames.AsSpan(startIndex);
        relevantFrames = frameSpan.ToArray();
#else
        relevantFrames = relevantFrames.Skip(startIndex).ToArray();
#endif

        // Limit the number of frames
        if (configuration.MaxFrames > 0 && relevantFrames.Length > configuration.MaxFrames)
        {
            relevantFrames = relevantFrames.Take(configuration.MaxFrames).ToArray();
        }

        // Use StringBuilder pool for efficient string building
        return StringBuilderPool.GetStringAndReturn(sb =>
        {
            var isFirst = true;
            foreach (var frame in relevantFrames)
            {
                var frameString = FormatStackFrame(frame, configuration);
                if (!string.IsNullOrEmpty(frameString))
                {
                    if (!isFirst)
                    {
                        sb.Append(" --> ");
                    }
                    sb.Append(frameString);
                    isFirst = false;
                }
            }
        });
    }

    /// <summary>
    /// Finds the most relevant frame for logging purposes.
    /// </summary>
    /// <param name="frames">The frames to search.</param>
    /// <param name="configuration">The enricher configuration.</param>
    /// <returns>The most relevant frame or null if none found.</returns>
    public static StackFrame? FindRelevantFrame(StackFrame[] frames, CallStackEnricherConfiguration configuration)
    {
        var relevantFrames = frames
            .Where(frame => !ShouldSkipFrame(frame, configuration))
            .ToArray();

        if (relevantFrames.Length == 0)
        {
            // Fallback strategy if all frames were filtered
            var fallbackFrames = frames
                .Where(frame => !ShouldSkipEnricherFrame(frame))
                .ToArray();
            
            if (fallbackFrames.Length > 0)
            {
                var fallbackIndex = Math.Min(configuration.FrameOffset, fallbackFrames.Length - 1);
                return fallbackFrames[fallbackIndex];
            }
            
            return null;
        }

        var targetIndex = Math.Min(configuration.FrameOffset, relevantFrames.Length - 1);
        return relevantFrames[targetIndex];
    }

    private static string FormatStackFrame(StackFrame frame, CallStackEnricherConfiguration configuration)
    {
        var method = frame.GetMethod();
        if (method == null)
            return string.Empty;

        var cachedInfo = MethodInfoCache.GetMethodInfo(method);
        if (!cachedInfo.IsValid)
            return string.Empty;

        return StringBuilderPool.GetStringAndReturn(sb =>
        {
            var hasContent = false;

            if (configuration.IncludeTypeName)
            {
                var typeName = configuration.UseFullTypeName 
                    ? cachedInfo.TypeName
                    : GetShortTypeName(cachedInfo.TypeName);
                
                var methodName = GetMethodName(method, configuration);
                sb.Append(typeName).Append('.').Append(methodName);
                hasContent = true;
            }
            else if (configuration.IncludeMethodName)
            {
                var methodName = GetMethodName(method, configuration);
                sb.Append(methodName);
                hasContent = true;
            }

            if (configuration.IncludeLineNumber && hasContent)
            {
                var lineNumber = frame.GetFileLineNumber();
                if (lineNumber > 0)
                {
                    sb.Append(':').Append(lineNumber);
                }
            }
        });
    }

    private static string GetShortTypeName(string fullTypeName)
    {
        if (string.IsNullOrEmpty(fullTypeName))
            return fullTypeName;

        var lastDotIndex = fullTypeName.LastIndexOf('.');
        return lastDotIndex >= 0 ? fullTypeName.Substring(lastDotIndex + 1) : fullTypeName;
    }

    private static string GetMethodName(System.Reflection.MethodBase method, CallStackEnricherConfiguration configuration)
    {
        if (!configuration.IncludeParameters)
            return method.Name;

        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return $"{method.Name}()";

        return StringBuilderPool.GetStringAndReturn(sb =>
        {
            sb.Append(method.Name).Append('(');
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(parameters[i].ParameterType.Name);
            }
            sb.Append(')');
        });
    }

    private static bool ShouldSkipFrame(StackFrame frame, CallStackEnricherConfiguration configuration)
    {
        var method = frame.GetMethod();
        if (method == null)
            return true;

        var declaringType = method.DeclaringType;
        if (declaringType == null)
            return true;

        var typeName = declaringType.FullName ?? declaringType.Name;

        if (typeName.StartsWith("Serilog.", StringComparison.Ordinal))
            return true;

        foreach (var ns in configuration.NamespacesToSkip)
        {
            if (typeName.StartsWith(ns, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool ShouldSkipEnricherFrame(StackFrame frame)
    {
        var method = frame.GetMethod();
        if (method == null)
            return true;

        var declaringType = method.DeclaringType;
        if (declaringType == null)
            return true;

        var typeName = declaringType.FullName ?? declaringType.Name;

        return typeName.StartsWith("Serilog.", StringComparison.Ordinal);
    }
}