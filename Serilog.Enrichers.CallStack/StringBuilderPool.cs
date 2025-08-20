using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace Serilog.Enrichers.CallStack;

/// <summary>
/// Object pool for StringBuilder instances to reduce allocations.
/// </summary>
internal static class StringBuilderPool
{
    private static readonly ConcurrentQueue<StringBuilder> _pool = new();
    private static volatile int _poolSize = 0;
    private const int MaxPoolSize = 32;
    private const int DefaultCapacity = 256;
    private const int MaxCapacity = 4096;

    /// <summary>
    /// Gets a StringBuilder from the pool or creates a new one.
    /// </summary>
    /// <returns>A StringBuilder instance ready for use.</returns>
    public static StringBuilder Get()
    {
        if (_pool.TryDequeue(out var stringBuilder))
        {
            Interlocked.Decrement(ref _poolSize);
            stringBuilder.Clear();
            return stringBuilder;
        }

        return new StringBuilder(DefaultCapacity);
    }

    /// <summary>
    /// Returns a StringBuilder to the pool for reuse.
    /// </summary>
    /// <param name="stringBuilder">The StringBuilder to return to the pool.</param>
    public static void Return(StringBuilder stringBuilder)
    {
        if (stringBuilder == null)
            return;

        // Don't pool extremely large builders to avoid memory bloat
        if (stringBuilder.Capacity > MaxCapacity)
            return;

        // Don't exceed maximum pool size
        if (_poolSize >= MaxPoolSize)
            return;

        stringBuilder.Clear();
        _pool.Enqueue(stringBuilder);
        Interlocked.Increment(ref _poolSize);
    }

    /// <summary>
    /// Gets a StringBuilder, executes an action with it, and returns it to the pool.
    /// </summary>
    /// <param name="action">Action to execute with the StringBuilder.</param>
    /// <returns>The resulting string.</returns>
    public static string GetStringAndReturn(Action<StringBuilder> action)
    {
        var sb = Get();
        try
        {
            action(sb);
            return sb.ToString();
        }
        finally
        {
            Return(sb);
        }
    }

    /// <summary>
    /// Gets current pool statistics.
    /// </summary>
    /// <returns>Pool statistics.</returns>
    public static PoolStatistics GetStatistics()
    {
        return new PoolStatistics
        {
            PoolSize = _poolSize,
            MaxPoolSize = MaxPoolSize
        };
    }
}

/// <summary>
/// Statistics about the StringBuilder pool.
/// </summary>
public readonly struct PoolStatistics
{
    public int PoolSize { get; init; }
    public int MaxPoolSize { get; init; }
}