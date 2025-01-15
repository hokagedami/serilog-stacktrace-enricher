using Serilog.Core;
using Serilog.Events;

namespace Serilog.Enrichers.CallStack.Tests;

/// <summary>
/// Test helper class for creating log event properties.
/// </summary>
public class PropertyFactory : ILogEventPropertyFactory
{
    /// <summary>
    /// Creates a log event property with the specified name and value.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <param name="destructureObjects">Whether to destructure the value.</param>
    /// <returns>A new log event property.</returns>
    public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
    {
        var scalarValue = new ScalarValue(value);
        return new LogEventProperty(name, scalarValue);
    }
}