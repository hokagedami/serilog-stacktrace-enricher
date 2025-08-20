using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Serilog.Enrichers.CallStack;

/// <summary>
/// Provides async-aware call stack detection and analysis.
/// </summary>
internal static class AsyncCallStackDetector
{
    /// <summary>
    /// Determines if the current call stack contains async methods.
    /// </summary>
    /// <param name="frames">The stack frames to analyze.</param>
    /// <returns>True if async methods are detected in the call stack.</returns>
    public static bool ContainsAsyncMethods(StackFrame[] frames)
    {
        if (frames == null || frames.Length == 0)
            return false;

        return frames.Any(frame => IsAsyncMethod(frame));
    }

    /// <summary>
    /// Filters out async state machine frames to get cleaner call stacks.
    /// </summary>
    /// <param name="frames">The stack frames to filter.</param>
    /// <returns>Filtered frames with async noise removed.</returns>
    public static StackFrame[] FilterAsyncNoise(StackFrame[] frames)
    {
        if (frames == null || frames.Length == 0)
            return frames;

        return frames.Where(frame => !IsAsyncStateMachineFrame(frame)).ToArray();
    }

    /// <summary>
    /// Gets async-aware method information that handles state machine generated code.
    /// </summary>
    /// <param name="frame">The stack frame to analyze.</param>
    /// <returns>Async-aware method information.</returns>
    public static AsyncMethodInfo GetAsyncMethodInfo(StackFrame frame)
    {
        var method = frame.GetMethod();
        if (method == null)
            return AsyncMethodInfo.Empty;

        var declaringType = method.DeclaringType;
        if (declaringType == null)
            return AsyncMethodInfo.Empty;

        // Check if this is an async state machine
        if (IsAsyncStateMachine(declaringType))
        {
            return ExtractOriginalAsyncMethod(declaringType, method);
        }

        // Check if the method itself is async
        var isAsync = IsAsyncMethod(frame);
        var returnsTask = method.ReturnType == typeof(Task) || 
                         method.ReturnType == typeof(ValueTask) ||
                         (method.ReturnType.IsGenericType && 
                          (method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>) ||
                           method.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>)));

        return new AsyncMethodInfo
        {
            OriginalMethodName = method.Name,
            DeclaringTypeName = declaringType.FullName ?? declaringType.Name,
            IsAsync = isAsync || returnsTask,
            IsStateMachineMethod = false,
            HasAsyncStateMachineAttribute = HasAsyncStateMachineAttribute(method)
        };
    }

    /// <summary>
    /// Creates an async-friendly call stack that shows original method names instead of state machine internals.
    /// </summary>
    /// <param name="frames">The stack frames to process.</param>
    /// <param name="configuration">The enricher configuration.</param>
    /// <returns>An async-friendly formatted call stack string.</returns>
    public static string CreateAsyncFriendlyCallStack(StackFrame[] frames, CallStackEnricherConfiguration configuration)
    {
        if (frames == null || frames.Length == 0)
            return string.Empty;

        var asyncAwareFrames = frames
            .Select(frame => new AsyncAwareFrame(frame, GetAsyncMethodInfo(frame)))
            .Where(af => !af.AsyncInfo.IsStateMachineMethod || af.AsyncInfo.IsAsync)
            .ToArray();

        return StringBuilderPool.GetStringAndReturn(sb =>
        {
            var isFirst = true;
            foreach (var asyncFrame in asyncAwareFrames)
            {
                var frameString = FormatAsyncAwareFrame(asyncFrame, configuration);
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

    private static bool IsAsyncMethod(StackFrame frame)
    {
        var method = frame.GetMethod();
        if (method == null)
            return false;

        // Check for async state machine attribute
        if (HasAsyncStateMachineAttribute(method))
            return true;

        // Check for Task/ValueTask return types
        var returnType = method.ReturnType;
        if (returnType == typeof(Task) || returnType == typeof(ValueTask))
            return true;

        if (returnType.IsGenericType)
        {
            var genericDef = returnType.GetGenericTypeDefinition();
            if (genericDef == typeof(Task<>) || genericDef == typeof(ValueTask<>))
                return true;
        }

        return false;
    }

    private static bool IsAsyncStateMachineFrame(StackFrame frame)
    {
        var method = frame.GetMethod();
        if (method == null)
            return false;

        var declaringType = method.DeclaringType;
        if (declaringType == null)
            return false;

        return IsAsyncStateMachine(declaringType);
    }

    private static bool IsAsyncStateMachine(Type type)
    {
        // Check if type implements IAsyncStateMachine
        return typeof(IAsyncStateMachine).IsAssignableFrom(type) ||
               type.GetInterfaces().Any(i => i == typeof(IAsyncStateMachine));
    }

    private static bool HasAsyncStateMachineAttribute(System.Reflection.MethodBase method)
    {
        return method.GetCustomAttributes(typeof(AsyncStateMachineAttribute), false).Length > 0;
    }

    private static AsyncMethodInfo ExtractOriginalAsyncMethod(Type stateMachineType, System.Reflection.MethodBase stateMachineMethod)
    {
        // Try to extract the original method name from the state machine type name
        var typeName = stateMachineType.Name;
        
        // State machine types usually follow pattern: <MethodName>d__1
        var methodNameMatch = System.Text.RegularExpressions.Regex.Match(typeName, @"<(.+)>d__\d+");
        var originalMethodName = methodNameMatch.Success ? methodNameMatch.Groups[1].Value : stateMachineMethod.Name;

        // Get the declaring type of the state machine (which should be nested in the original type)
        var originalType = stateMachineType.DeclaringType ?? stateMachineType;

        return new AsyncMethodInfo
        {
            OriginalMethodName = originalMethodName,
            DeclaringTypeName = originalType.FullName ?? originalType.Name,
            IsAsync = true,
            IsStateMachineMethod = true,
            HasAsyncStateMachineAttribute = true
        };
    }

    private static string FormatAsyncAwareFrame(AsyncAwareFrame asyncFrame, CallStackEnricherConfiguration configuration)
    {
        return StringBuilderPool.GetStringAndReturn(sb =>
        {
            if (configuration.IncludeTypeName)
            {
                var typeName = configuration.UseFullTypeName 
                    ? asyncFrame.AsyncInfo.DeclaringTypeName
                    : GetShortTypeName(asyncFrame.AsyncInfo.DeclaringTypeName);
                
                sb.Append(typeName).Append('.');
            }

            sb.Append(asyncFrame.AsyncInfo.OriginalMethodName);
            
            if (asyncFrame.AsyncInfo.IsAsync)
            {
                sb.Append("(async)");
            }

            if (configuration.IncludeLineNumber)
            {
                var lineNumber = asyncFrame.Frame.GetFileLineNumber();
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
}

/// <summary>
/// Information about async methods extracted from stack frames.
/// </summary>
internal readonly struct AsyncMethodInfo
{
    public static readonly AsyncMethodInfo Empty = new()
    {
        OriginalMethodName = string.Empty,
        DeclaringTypeName = string.Empty,
        IsAsync = false,
        IsStateMachineMethod = false,
        HasAsyncStateMachineAttribute = false
    };

    public string OriginalMethodName { get; init; }
    public string DeclaringTypeName { get; init; }
    public bool IsAsync { get; init; }
    public bool IsStateMachineMethod { get; init; }
    public bool HasAsyncStateMachineAttribute { get; init; }
}

/// <summary>
/// Combines a stack frame with its async method information.
/// </summary>
internal readonly struct AsyncAwareFrame
{
    public StackFrame Frame { get; }
    public AsyncMethodInfo AsyncInfo { get; }

    public AsyncAwareFrame(StackFrame frame, AsyncMethodInfo asyncInfo)
    {
        Frame = frame;
        AsyncInfo = asyncInfo;
    }
}