using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Serilog.Enrichers.CallStack;

/// <summary>
/// Thread-safe cache for reflection results to improve performance.
/// </summary>
internal static class MethodInfoCache
{
    private static readonly ConcurrentDictionary<MethodBase, CachedMethodInfo> _cache = new();
    private static readonly ConcurrentDictionary<Type, string> _typeNameCache = new();

    /// <summary>
    /// Gets cached method information, computing it if not already cached.
    /// </summary>
    /// <param name="method">The method to get information for.</param>
    /// <returns>Cached method information.</returns>
    public static CachedMethodInfo GetMethodInfo(MethodBase method)
    {
        if (method == null)
            return CachedMethodInfo.Empty;

        return _cache.GetOrAdd(method, ComputeMethodInfo);
    }

    /// <summary>
    /// Gets cached type name, computing it if not already cached.
    /// </summary>
    /// <param name="type">The type to get the name for.</param>
    /// <returns>Cached type name.</returns>
    public static string GetTypeName(Type type)
    {
        if (type == null)
            return string.Empty;

        return _typeNameCache.GetOrAdd(type, ComputeTypeName);
    }

    /// <summary>
    /// Clears the method info cache. Useful for testing or memory management.
    /// </summary>
    public static void ClearCache()
    {
        _cache.Clear();
        _typeNameCache.Clear();
    }

    /// <summary>
    /// Gets the current cache statistics.
    /// </summary>
    /// <returns>Cache statistics.</returns>
    public static CacheStatistics GetCacheStatistics()
    {
        return new CacheStatistics
        {
            MethodCacheSize = _cache.Count,
            TypeNameCacheSize = _typeNameCache.Count
        };
    }

    private static CachedMethodInfo ComputeMethodInfo(MethodBase method)
    {
        try
        {
            var declaringType = method.DeclaringType;
            var typeName = declaringType != null ? GetTypeName(declaringType) : string.Empty;
            var methodName = method.Name;
            var fullName = !string.IsNullOrEmpty(typeName) ? $"{typeName}.{methodName}" : methodName;

            return new CachedMethodInfo
            {
                MethodName = methodName,
                TypeName = typeName,
                FullName = fullName,
                IsValid = true
            };
        }
        catch
        {
            // Return empty info if reflection fails
            return CachedMethodInfo.Empty;
        }
    }

    private static string ComputeTypeName(Type type)
    {
        try
        {
            return type.FullName ?? type.Name;
        }
        catch
        {
            return type.Name;
        }
    }
}

/// <summary>
/// Cached method information to avoid repeated reflection calls.
/// </summary>
internal readonly struct CachedMethodInfo
{
    public static readonly CachedMethodInfo Empty = new()
    {
        MethodName = string.Empty,
        TypeName = string.Empty,
        FullName = string.Empty,
        IsValid = false
    };

    public string MethodName { get; init; }
    public string TypeName { get; init; }
    public string FullName { get; init; }
    public bool IsValid { get; init; }
}

/// <summary>
/// Cache statistics for monitoring performance.
/// </summary>
public readonly struct CacheStatistics
{
    public int MethodCacheSize { get; init; }
    public int TypeNameCacheSize { get; init; }
}